using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Picea.Commanding;

/// <summary>
/// A command that has passed one or more validation/authorization stages.
/// </summary>
/// <typeparam name="TCommand">The wrapped command type.</typeparam>
public readonly record struct ValidCommand<TCommand>(TCommand Command);

/// <summary>
/// Validates command shape/business constraints before decision.
/// </summary>
/// <typeparam name="TState">The decider state.</typeparam>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TError">The domain error type.</typeparam>
/// <param name="state">The current state snapshot.</param>
/// <param name="command">The incoming command.</param>
/// <returns>An accepted command wrapper or a validation error.</returns>
public delegate Result<ValidCommand<TCommand>, TError> Validator<in TState, TCommand, TError>(
    TState state,
    TCommand command);

/// <summary>
/// Authorizes whether a principal may issue a command in the current state.
/// </summary>
/// <typeparam name="TPrincipal">The principal/actor type.</typeparam>
/// <typeparam name="TState">The decider state.</typeparam>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TError">The domain error type.</typeparam>
/// <param name="principal">The principal attempting to execute the command.</param>
/// <param name="state">The current state snapshot.</param>
/// <param name="command">The incoming command.</param>
/// <returns>An accepted command wrapper or an authorization error.</returns>
public delegate Result<ValidCommand<TCommand>, TError> Policy<in TPrincipal, in TState, TCommand, TError>(
    TPrincipal principal,
    TState state,
    TCommand command);

/// <summary>
/// Identifies the stage that denied a command.
/// </summary>
public enum DenialKind
{
    None = 0,
    Authorization = 1,
    Validation = 2
}

/// <summary>
/// Observes denied commands for auditing/telemetry.
/// </summary>
/// <typeparam name="TPrincipal">The principal/actor type.</typeparam>
/// <typeparam name="TState">The decider state.</typeparam>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TError">The domain error type.</typeparam>
/// <param name="kind">The stage that denied the command.</param>
/// <param name="principal">The principal attempting to execute the command.</param>
/// <param name="state">The state at decision time.</param>
/// <param name="command">The denied command.</param>
/// <param name="error">The denial reason.</param>
public delegate ValueTask DenialObserver<in TPrincipal, in TState, in TCommand, in TError>(
    DenialKind kind,
    TPrincipal principal,
    TState state,
    TCommand command,
    TError error);

public interface GuardedAuthorization<TPrincipal, TState, TCommand, TError>
{
    /// <summary>
    /// Principal-based authorization stage.
    /// </summary>
    static abstract Policy<TPrincipal, TState, TCommand, TError> Authorize { get; }
}

public interface GuardedValidation<TState, TCommand, TError>
{

    /// <summary>
    /// Command validation stage.
    /// </summary>
    static abstract Validator<TState, TCommand, TError> Validate { get; }
}

/// <summary>
/// A Decider with explicit authorization and validation stages.
/// </summary>
/// <typeparam name="TState">The aggregate state.</typeparam>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <typeparam name="TEffect">The effect type.</typeparam>
/// <typeparam name="TParameters">Initialization parameters.</typeparam>
public interface GuardedDecider<TState, TCommand, TEvent, TEffect, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    /// <summary>
    /// Produces events for an already authorized and validated command.
    /// </summary>
    static abstract TEvent[] Decide(TState state, ValidCommand<TCommand> command);

    /// <summary>
    /// Whether the automaton has reached a terminal state.
    /// </summary>
    static virtual bool IsTerminal(TState state) => false;
}

/// <summary>
/// Runtime for a secure staged decider: authorize, validate, decide, then dispatch.
/// </summary>
public sealed class GuardedDecidingRuntime<TGuardedDecider, TGuardedPolicy, TValidation, TPrincipal, TState, TCommand, TEvent, TEffect, TError, TParameters> : IDisposable
    where TGuardedDecider : GuardedDecider<TState, TCommand, TEvent, TEffect, TParameters>
    where TGuardedPolicy : GuardedAuthorization<TPrincipal, TState, TCommand, TError>
    where TValidation : GuardedValidation<TState, TCommand, TError>
{
    private static readonly string _deciderTypeName = typeof(TGuardedDecider).Name;
    private static readonly Policy<TPrincipal, TState, TCommand, TError> _authorize = TGuardedPolicy.Authorize;
    private static readonly Validator<TState, TCommand, TError> _validate = TValidation.Validate;
    private static readonly DenialObserver<TPrincipal, TState, TCommand, TError> _noOpDenialObserver =
        static (_, _, _, _, _) => ValueTask.CompletedTask;

    private readonly AutomatonRuntime<TGuardedDecider, TState, TEvent, TEffect, TParameters> _core;
    private readonly DenialObserver<TPrincipal, TState, TCommand, TError> _denialObserver;

    /// <summary>
    /// Gets the current guarded decider state snapshot.
    /// </summary>
    public TState State => _core.State;

    /// <summary>
    /// Gets a snapshot of events dispatched through guarded command handling.
    /// </summary>
    public IReadOnlyList<TEvent> Events => _core.Events;

    /// <summary>
    /// Gets whether the guarded decider is in a terminal state.
    /// </summary>
    public bool IsTerminal => TGuardedDecider.IsTerminal(_core.State);

    private GuardedDecidingRuntime(
        AutomatonRuntime<TGuardedDecider, TState, TEvent, TEffect, TParameters> core,
        DenialObserver<TPrincipal, TState, TCommand, TError> denialObserver)
    {
        _core = core;
        _denialObserver = denialObserver;
    }

    /// <summary>
    /// Starts a guarded deciding runtime.
    /// </summary>
    /// <param name="parameters">The guarded decider initialization parameters.</param>
    /// <param name="observer">Observer pipeline capability for transition side effects.</param>
    /// <param name="interpreter">Interpreter pipeline capability for feedback events.</param>
    /// <param name="denialObserver">Optional observer for authorization/validation denials.</param>
    /// <param name="threadSafe">Whether command handling is serialized through a gate.</param>
    /// <param name="trackEvents">Whether dispatched events are stored in runtime history.</param>
    /// <param name="cancellationToken">Token used to cancel runtime startup.</param>
    /// <returns>A started guarded deciding runtime.</returns>
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public static async ValueTask<GuardedDecidingRuntime<TGuardedDecider, TGuardedPolicy, TValidation, TPrincipal, TState, TCommand, TEvent, TEffect, TError, TParameters>> Start(
        TParameters parameters,
        Observer<TState, TEvent, TEffect> observer,
        Interpreter<TEffect, TEvent> interpreter,
        DenialObserver<TPrincipal, TState, TCommand, TError>? denialObserver = null,
        bool threadSafe = true,
        bool trackEvents = true,
        CancellationToken cancellationToken = default)
    {
        using var activity = AutomatonDiagnostics.StartActivity("Automaton.GuardedDecider.Start");
        activity?.SetTag("automaton.type", _deciderTypeName);

        var core = await AutomatonRuntime<TGuardedDecider, TState, TEvent, TEffect, TParameters>
            .Start(parameters, observer, interpreter, threadSafe, trackEvents, cancellationToken).ConfigureAwait(false);

        activity?.SetStatus(ActivityStatusCode.Ok);
        return new GuardedDecidingRuntime<TGuardedDecider, TGuardedPolicy, TValidation, TPrincipal, TState, TCommand, TEvent, TEffect, TError, TParameters>(
            core,
            denialObserver ?? _noOpDenialObserver);
    }

    /// <summary>
    /// Handles a command for a principal using a staged guarded pipeline.
    /// </summary>
    /// <param name="principal">The principal issuing the command.</param>
    /// <param name="command">The command to evaluate against current state.</param>
    /// <param name="cancellationToken">Token used to cancel command handling.</param>
    /// <returns>
    /// Updated state on success, or a domain error when authorization, validation, or decision fails.
    /// </returns>
    public ValueTask<Result<TState, TError>> Handle(
        TPrincipal principal,
        TCommand command,
        CancellationToken cancellationToken = default)
    {
        var activity = AutomatonDiagnostics.StartActivity("Automaton.GuardedDecider.Handle");
        activity?.SetTag("automaton.type", _deciderTypeName);

        if (_core.IsThreadSafe)
        {
            var waitTask = _core.Gate.WaitAsync(cancellationToken);
            if (waitTask.IsCompletedSuccessfully)
                return HandleAfterGate(principal, command, activity, cancellationToken);

            return AwaitGateThenHandle(waitTask, principal, command, activity, cancellationToken);
        }

        return HandleUnserialized(principal, command, activity, cancellationToken);
    }

    private ValueTask<Result<TState, TError>> HandleUnserialized(
        TPrincipal principal,
        TCommand command,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        try
        {
            var task = HandleCore(principal, command, activity, cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                activity?.Dispose();
                return new ValueTask<Result<TState, TError>>(task.Result);
            }

            return AwaitHandleCoreUnserialized(task, activity);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            throw;
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private static async ValueTask<Result<TState, TError>> AwaitHandleCoreUnserialized(
        ValueTask<Result<TState, TError>> task,
        Activity? activity)
    {
        using var _ = activity;
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private ValueTask<Result<TState, TError>> HandleAfterGate(
        TPrincipal principal,
        TCommand command,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        try
        {
            var task = HandleCore(principal, command, activity, cancellationToken);
            if (task.IsCompletedSuccessfully)
            {
                activity?.Dispose();
                _core.Gate.Release();
                return new ValueTask<Result<TState, TError>>(task.Result);
            }

            return AwaitHandleCoreThenRelease(task, activity);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            _core.Gate.Release();
            throw;
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<TState, TError>> AwaitHandleCoreThenRelease(
        ValueTask<Result<TState, TError>> task,
        Activity? activity)
    {
        using var _ = activity;
        try
        {
            return await task.ConfigureAwait(false);
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
    private async ValueTask<Result<TState, TError>> AwaitGateThenHandle(
        Task waitTask,
        TPrincipal principal,
        TCommand command,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        using var _ = activity;
        await waitTask.ConfigureAwait(false);
        try
        {
            return await HandleCore(principal, command, activity, cancellationToken).ConfigureAwait(false);
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

    private async ValueTask<Result<TState, TError>> HandleCore(
        TPrincipal principal,
        TCommand command,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        var state = _core.State;

        var authorized = _authorize(principal, state, command);
        if (authorized.IsErr)
        {
            authorized.TryGetError(out var authorizationError);
            await ObserveDenial(DenialKind.Authorization, principal, state, command, authorizationError).ConfigureAwait(false);
            activity?.SetTag("automaton.result", "denied");
            activity?.SetTag("automaton.denial.kind", DenialKind.Authorization.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Result<TState, TError>.Err(authorizationError);
        }

        authorized.TryGetValue(out var authorizedCommand);
        var validated = _validate(state, authorizedCommand.Command);
        if (validated.IsErr)
        {
            validated.TryGetError(out var validationError);
            await ObserveDenial(DenialKind.Validation, principal, state, command, validationError).ConfigureAwait(false);
            activity?.SetTag("automaton.result", "denied");
            activity?.SetTag("automaton.denial.kind", DenialKind.Validation.ToString());
            activity?.SetStatus(ActivityStatusCode.Ok);
            return Result<TState, TError>.Err(validationError);
        }

        validated.TryGetValue(out var validatedCommand);
        var events = ContractGuards.RequireNonNullArray(TGuardedDecider.Decide(state, validatedCommand));
        for (var i = 0; i < events.Length; i++)
        {
            var dispatchResult = await _core.DispatchUnlocked(events[i], 0, cancellationToken).ConfigureAwait(false);
            if (dispatchResult.IsErr)
            {
                dispatchResult.TryGetError(out var dispatchError);
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {dispatchError}",
                    dispatchError.Exception);
            }
        }

        activity?.SetTag("automaton.result", "ok");
        activity?.SetStatus(ActivityStatusCode.Ok);
        return Result<TState, TError>.Ok(_core.State);
    }

    private ValueTask ObserveDenial(
        DenialKind kind,
        TPrincipal principal,
        TState state,
        TCommand command,
        TError error) =>
        _denialObserver(kind, principal, state, command, error);

    public void Reset(TState state) => _core.Reset(state);
    public void Dispose() => _core.Dispose();
}
