// =============================================================================
// Decider — Staged Command Pipeline for Automatons
// =============================================================================
// The Decider pattern (Jérémie Chassaing, 2021) adds a three-stage command
// pipeline to the Automaton kernel:
//
//     intent (Command) → validate → authorize → decide → fact (Event) → evolve (Transition)
//
// Each stage is a Kleisli arrow, composed under the Result monad:
//
//     validate  : Command            → Reader<State, Result<Validated<Command>, Error>>
//     authorize : (Validated<Command>, AuthContext) → Reader<State, Result<Unit, Error>>
//     decide    : Validated<Command> → Reader<State, Result<Events, Error>>
//
//     δ = decide ∘ authorize ∘ validate   (short-circuits on first rejection)
//
// Hoare-style contracts per stage:
//     { StateInvariant(s) }                        validate(s, c)   { Validated(c') ∨ Error }
//     { Validated(c') }                            authorize(s, c') { Permitted ∨ Forbidden }
//     { Validated(c') ∧ Permitted ∧ Invariant(s) } decide(s, c')   { Events ∧ Invariant(s') }
// =============================================================================

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Picea;

/// <summary>
/// The result of the <see cref="Decider{TState,TCommand,TEvent,TEffect,TError,TParameters}.Validate"/> stage.
/// A sum type with two cases: <see cref="Valid"/> (command passed validation) and
/// <see cref="Invalid"/> (command was rejected by domain invariants).
/// Encodes both success and failure in the type itself.
/// </summary>
/// <typeparam name="TCommand">The command type being validated.</typeparam>
/// <typeparam name="TError">The error type for validation failures.</typeparam>
public abstract record Validated<TCommand, TError>
{
    private Validated() { }

    /// <summary>
    /// The command passed validation. Acts as a type-level proof that the command is
    /// feasible given the domain state at validation time, and may proceed through
    /// <see cref="Decider{TState,TCommand,TEvent,TEffect,TError,TParameters}.Authorize"/>
    /// and <see cref="Decider{TState,TCommand,TEvent,TEffect,TError,TParameters}.Decide"/>.
    /// </summary>
    public sealed record Valid(TCommand Value) : Validated<TCommand, TError>;

    /// <summary>
    /// The command failed validation — it violates a domain invariant in the current state.
    /// The domain error is carried in the InvalidError field.
    /// </summary>
    public sealed record Invalid(TError InvalidError) : Validated<TCommand, TError>;
}

/// <summary>
/// Formal composition helpers for the staged Decider pipeline.
/// These helpers make the short-circuiting composition explicit and testable.
/// </summary>
internal static class DeciderComposition
{
    /// <summary>
    /// Lifts a <see cref="Validated{TCommand,TError}"/> value into <see cref="Result{TSuccess,TError}"/>,
    /// preserving successful commands and propagating validation errors.
    /// </summary>
    public static Result<Validated<TCommand, TError>, TError> ValidateToResult<TCommand, TError>(
        Validated<TCommand, TError> validated) =>
        validated switch
        {
            Validated<TCommand, TError>.Valid valid =>
                Result<Validated<TCommand, TError>, TError>.Ok(valid),

            Validated<TCommand, TError>.Invalid(var error) =>
                Result<Validated<TCommand, TError>, TError>.Err(error),

            _ => throw new UnreachableException()
        };

    /// <summary>
    /// Lifts the authorization stage into the Result channel while preserving the
    /// validated command when authorization succeeds.
    /// </summary>
    public static Result<Validated<TCommand, TError>, TError> AuthorizeToResult<TCommand, TError>(
        Validated<TCommand, TError> validated,
        Result<Unit, TError> authorization) =>
        authorization.Match(
            ok: _ => Result<Validated<TCommand, TError>, TError>.Ok(validated),
            err: error => Result<Validated<TCommand, TError>, TError>.Err(error));

    /// <summary>
    /// Composes validate -> authorize(authContext) -> decide as an explicit monadic pipeline.
    /// The composition short-circuits on the first rejection.
    /// </summary>
    public static Result<TEvent[], TError> Compose<TState, TCommand, TEvent, TError, TAuthorizationContext>(
        TState state,
        TCommand command,
        TAuthorizationContext authorizationContext,
        Func<TState, TCommand, Validated<TCommand, TError>> validate,
        Func<TState, Validated<TCommand, TError>, TAuthorizationContext, Result<Unit, TError>> authorize,
        Func<TState, Validated<TCommand, TError>, Result<TEvent[], TError>> decide) =>
        ValidateToResult(validate(state, command))
            .Bind(validated => AuthorizeToResult(validated, authorize(state, validated, authorizationContext)))
            .Bind(validated => decide(state, validated));
}

/// <summary>
/// A Decider is an Automaton with a three-stage command pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Commands pass through three ordered stages before any state transition occurs:
/// <list type="number">
///   <item><description><see cref="Validate"/> — feasibility: is the command consistent with domain invariants?</description></item>
///   <item><description><see cref="Authorize"/> — permission: is the caller permitted to issue this command? (default: always permitted)</description></item>
///   <item><description><see cref="Decide"/> — decision: given a validated and authorized command, which events result?</description></item>
/// </list>
/// </para>
/// <para>
/// The pipeline short-circuits on the first rejection. All three stages execute atomically inside a
/// single gate in <see cref="DecidingRuntime{TDecider,TState,TCommand,TEvent,TEffect,TError,TParameters}"/>.
/// </para>
/// </remarks>
public interface Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    /// <summary>
    /// Stage 1 — Validates a command for feasibility against the current domain state.
    /// Returns <see cref="Validated{TCommand,TError}.Valid"/> on success,
    /// or <see cref="Validated{TCommand,TError}.Invalid"/> when the command violates a domain invariant.
    /// </summary>
    static abstract Validated<TCommand, TError> Validate(TState state, TCommand command);

    /// <summary>
    /// Stage 2 — Authorizes a validated command against the current state.
    /// Returns <see cref="Result{TSuccess,TError}.Ok(TSuccess)"/> when permitted, or
    /// <see cref="Result{TSuccess,TError}.Err(TError)"/> with the denial reason when the command is forbidden.
    /// </summary>
    /// <remarks>Default implementation: always permits. Override to enforce access control policies.</remarks>
    static virtual Result<Unit, TError> Authorize<TAuthorizationContext>(
        TState state,
        Validated<TCommand, TError> command,
        TAuthorizationContext authorizationContext) =>
        Result<Unit, TError>.Ok(Unit.Value);

    /// <summary>
    /// Stage 3 — Produces domain events from a <see cref="Validated{TCommand,TError}.Valid"/> command.
    /// </summary>
    static abstract Result<TEvent[], TError> Decide(TState state, Validated<TCommand, TError> command);

    /// <summary>
    /// Whether the automaton has reached a terminal state.
    /// </summary>
    static virtual bool IsTerminal(TState state) => false;
}

/// <summary>
/// Runtime that executes the three-stage command pipeline (validate → authorize → decide)
/// before dispatching events into the automaton.
/// </summary>
public sealed class DecidingRuntime<TDecider, TState, TCommand, TEvent, TEffect, TError, TParameters> : IDisposable
    where TDecider : Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
{
    private static readonly string _deciderTypeName = typeof(TDecider).Name;
    private static readonly string _stateTypeName = typeof(TState).Name;

    private readonly AutomatonRuntime<TDecider, TState, TEvent, TEffect, TParameters> _core;

    public TState State => _core.State;
    public IReadOnlyList<TEvent> Events => _core.Events;
    public bool IsTerminal => TDecider.IsTerminal(_core.State);

    private DecidingRuntime(AutomatonRuntime<TDecider, TState, TEvent, TEffect, TParameters> core)
    {
        _core = core;
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public static async ValueTask<DecidingRuntime<TDecider, TState, TCommand, TEvent, TEffect, TError, TParameters>> Start(
        TParameters parameters,
        Observer<TState, TEvent, TEffect> observer,
        Interpreter<TEffect, TEvent> interpreter,
        bool threadSafe = true,
        bool trackEvents = true,
        CancellationToken cancellationToken = default)
    {
        using var activity = AutomatonDiagnostics.Source.StartActivity("Automaton.Decider.Start");
        activity?.SetTag("automaton.type", _deciderTypeName);
        activity?.SetTag("automaton.state.type", _stateTypeName);

        var core = await AutomatonRuntime<TDecider, TState, TEvent, TEffect, TParameters>
            .Start(parameters, observer, interpreter, threadSafe, trackEvents, cancellationToken).ConfigureAwait(false);

        activity?.SetStatus(ActivityStatusCode.Ok);
        return new DecidingRuntime<TDecider, TState, TCommand, TEvent, TEffect, TError, TParameters>(core);
    }

    public ValueTask<Result<TState, TError>> Handle(TCommand command, CancellationToken cancellationToken = default) =>
        Handle(command, Unit.Value, cancellationToken);

    public ValueTask<Result<TState, TError>> Handle<TAuthorizationContext>(
        TCommand command,
        TAuthorizationContext authorizationContext,
        CancellationToken cancellationToken = default)
    {
        var activity = AutomatonDiagnostics.Source.StartActivity("Automaton.Decider.Handle");
        activity?.SetTag("automaton.type", _deciderTypeName);
        activity?.SetTag("automaton.command.type", command?.GetType().Name);

        if (_core.IsThreadSafe)
        {
            var waitTask = _core.Gate.WaitAsync(cancellationToken);
            if (waitTask.IsCompletedSuccessfully)
                return HandleAfterGate(command, authorizationContext, activity, cancellationToken);

            return AwaitGateThenHandle(waitTask, command, authorizationContext, activity, cancellationToken);
        }

        return HandleUnserialized(command, authorizationContext, activity, cancellationToken);
    }

    private ValueTask<Result<TState, TError>> HandleUnserialized<TAuthorizationContext>(
        TCommand command,
        TAuthorizationContext authorizationContext,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        try
        {
            var validated = TDecider.Validate(_core.State, command);
            if (validated is Validated<TCommand, TError>.Invalid(var validationError))
            {
                activity?.SetTag("automaton.pipeline.stage", "validate");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", validationError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                return new ValueTask<Result<TState, TError>>(
                    Result<TState, TError>.Err(validationError));
            }

            var auth = TDecider.Authorize(_core.State, validated, authorizationContext);
            if (auth.IsErr)
            {
                var authError = auth.Error;
                activity?.SetTag("automaton.pipeline.stage", "authorize");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", authError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                return new ValueTask<Result<TState, TError>>(
                    Result<TState, TError>.Err(authError));
            }

            var decided = TDecider.Decide(_core.State, validated);
            if (decided.IsOk)
            {
                return DispatchEventsAndReturnOkUnserialized(
                    ContractGuards.RequireNonNullArray(decided.Value),
                    activity,
                    cancellationToken);
            }
            else
            {
                var decisionError = decided.Error;
                activity?.SetTag("automaton.pipeline.stage", "decide");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", decisionError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                return new ValueTask<Result<TState, TError>>(
                    Result<TState, TError>.Err(decisionError));
            }
        }
        catch (OperationCanceledException)
        {
            activity?.Dispose();
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            throw;
        }
    }

    private ValueTask<Result<TState, TError>> DispatchEventsAndReturnOkUnserialized(
        TEvent[] events, Activity? activity, CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 0; i < events.Length; i++)
            {
                var dispatchTask = _core.DispatchUnlocked(events[i], cancellationToken);
                if (!dispatchTask.IsCompletedSuccessfully)
                    return AwaitRemainingEventsAndReturnOkUnserialized(dispatchTask, events, i + 1, activity, cancellationToken);

                var dispatchResult = dispatchTask.Result;
                if (dispatchResult.IsErr)
                    throw new InvalidOperationException(
                        $"Pipeline error during dispatch: {dispatchResult.Error}",
                        dispatchResult.Error.Exception);
            }

            activity?.SetTag("automaton.result", "ok");
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.Dispose();
            return new ValueTask<Result<TState, TError>>(
                Result<TState, TError>.Ok(_core.State));
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException)
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            throw;
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<TState, TError>> AwaitRemainingEventsAndReturnOkUnserialized(
        ValueTask<Result<Unit, PipelineError>> pendingTask, TEvent[] events, int startIndex,
        Activity? activity, CancellationToken cancellationToken)
    {
        using var _ = activity;
        try
        {
            var pendingResult = await pendingTask.ConfigureAwait(false);
            if (pendingResult.IsErr)
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {pendingResult.Error}",
                    pendingResult.Error.Exception);

            for (var i = startIndex; i < events.Length; i++)
            {
                var result = await _core.DispatchUnlocked(events[i], cancellationToken).ConfigureAwait(false);
                if (result.IsErr)
                    throw new InvalidOperationException(
                        $"Pipeline error during dispatch: {result.Error}",
                        result.Error.Exception);
            }

            activity?.SetTag("automaton.result", "ok");
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Result<TState, TError>.Ok(_core.State);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private ValueTask<Result<TState, TError>> HandleAfterGate<TAuthorizationContext>(
        TCommand command,
        TAuthorizationContext authorizationContext,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        try
        {
            var validated = TDecider.Validate(_core.State, command);
            if (validated is Validated<TCommand, TError>.Invalid(var validationError))
            {
                activity?.SetTag("automaton.pipeline.stage", "validate");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", validationError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                _core.Gate.Release();
                return new ValueTask<Result<TState, TError>>(
                    Result<TState, TError>.Err(validationError));
            }

            var auth = TDecider.Authorize(_core.State, validated, authorizationContext);
            if (auth.IsErr)
            {
                var authError = auth.Error;
                activity?.SetTag("automaton.pipeline.stage", "authorize");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", authError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                _core.Gate.Release();
                return new ValueTask<Result<TState, TError>>(
                    Result<TState, TError>.Err(authError));
            }

            var decided = TDecider.Decide(_core.State, validated);
            if (decided.IsOk)
            {
                return DispatchEventsAndReturnOk(
                    ContractGuards.RequireNonNullArray(decided.Value),
                    activity,
                    cancellationToken);
            }
            else
            {
                var decisionError = decided.Error;
                activity?.SetTag("automaton.pipeline.stage", "decide");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", decisionError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                _core.Gate.Release();
                return new ValueTask<Result<TState, TError>>(
                    Result<TState, TError>.Err(decisionError));
            }
        }
        catch (OperationCanceledException)
        {
            activity?.Dispose();
            _core.Gate.Release();
            throw;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            _core.Gate.Release();
            throw;
        }
    }

    private ValueTask<Result<TState, TError>> DispatchEventsAndReturnOk(
        TEvent[] events, Activity? activity, CancellationToken cancellationToken)
    {
        for (var i = 0; i < events.Length; i++)
        {
            var dispatchTask = _core.DispatchUnlocked(events[i], cancellationToken);
            if (!dispatchTask.IsCompletedSuccessfully)
                return AwaitRemainingEventsAndReturnOk(dispatchTask, events, i + 1, activity, cancellationToken);

            var dispatchResult = dispatchTask.Result;
            if (dispatchResult.IsErr)
            {
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {dispatchResult.Error}",
                    dispatchResult.Error.Exception);
            }
        }

        activity?.SetTag("automaton.result", "ok");
        activity?.SetStatus(ActivityStatusCode.Ok);
        activity?.Dispose();
        _core.Gate.Release();
        return new ValueTask<Result<TState, TError>>(
            Result<TState, TError>.Ok(_core.State));
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<TState, TError>> AwaitRemainingEventsAndReturnOk(
        ValueTask<Result<Unit, PipelineError>> pendingTask, TEvent[] events, int startIndex,
        Activity? activity, CancellationToken cancellationToken)
    {
        using var _ = activity;
        try
        {
            var pendingResult = await pendingTask.ConfigureAwait(false);
            if (pendingResult.IsErr)
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {pendingResult.Error}",
                    pendingResult.Error.Exception);

            for (var i = startIndex; i < events.Length; i++)
            {
                var result = await _core.DispatchUnlocked(events[i], cancellationToken).ConfigureAwait(false);
                if (result.IsErr)
                    throw new InvalidOperationException(
                        $"Pipeline error during dispatch: {result.Error}",
                        result.Error.Exception);
            }

            activity?.SetTag("automaton.result", "ok");
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Result<TState, TError>.Ok(_core.State);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _core.Gate.Release();
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<TState, TError>> AwaitGateThenHandle<TAuthorizationContext>(
        Task waitTask,
        TCommand command,
        TAuthorizationContext authorizationContext,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        using var _ = activity;
        await waitTask.ConfigureAwait(false);
        try
        {
            var validated = TDecider.Validate(_core.State, command);
            if (validated is Validated<TCommand, TError>.Invalid(var validationError))
            {
                activity?.SetTag("automaton.pipeline.stage", "validate");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", validationError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result<TState, TError>.Err(validationError);
            }

            var auth = TDecider.Authorize(_core.State, validated, authorizationContext);
            if (auth.IsErr)
            {
                var authError = auth.Error;
                activity?.SetTag("automaton.pipeline.stage", "authorize");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", authError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result<TState, TError>.Err(authError);
            }

            var decided = TDecider.Decide(_core.State, validated);
            if (decided.IsOk)
            {
                var events = ContractGuards.RequireNonNullArray(decided.Value);
                for (var i = 0; i < events.Length; i++)
                {
                    var result = await _core.DispatchUnlocked(events[i], cancellationToken).ConfigureAwait(false);
                    if (result.IsErr)
                        throw new InvalidOperationException(
                            $"Pipeline error during dispatch: {result.Error}",
                            result.Error.Exception);
                }

                activity?.SetTag("automaton.result", "ok");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result<TState, TError>.Ok(_core.State);
            }
            else
            {
                var decisionError = decided.Error;
                activity?.SetTag("automaton.pipeline.stage", "decide");
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", decisionError?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result<TState, TError>.Err(decisionError);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _core.Gate.Release();
        }
    }

    public void Reset(TState state) => _core.Reset(state);
    public void Dispose() => _core.Dispose();
}
