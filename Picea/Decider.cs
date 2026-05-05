// =============================================================================
// Decider — Command Validation for Automatons
// =============================================================================
// The Decider pattern (Jérémie Chassaing, 2021) adds a command validation layer
// to the Automaton kernel. It separates:
//
//     intent  (Command)  →  decision  (Decide)  →  fact  (Event)  →  evolution  (Transition)
//
// Mathematically, Decide is a Kleisli arrow:
//
//     decide : Command → Reader<State, Result<Events, Error>>
// =============================================================================

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Picea;

/// <summary>
/// A Decider is an Automaton that validates commands before transitioning.
/// </summary>
/// <typeparam name="TState">The state type managed by the decider.</typeparam>
/// <typeparam name="TCommand">The command type representing intent.</typeparam>
/// <typeparam name="TEvent">The event type emitted when commands are accepted.</typeparam>
/// <typeparam name="TEffect">The effect type produced by state transitions.</typeparam>
/// <typeparam name="TError">The domain error type returned when command validation fails.</typeparam>
/// <typeparam name="TParameters">The initialization parameter type for the underlying automaton.</typeparam>
public interface Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    /// <summary>
    /// Validates a command against the current state, producing events or an error.
    /// </summary>
    /// <param name="state">The current state used for command validation.</param>
    /// <param name="command">The command to validate and decide.</param>
    /// <returns>An <c>Ok</c> containing events when accepted, or an <c>Err</c> containing a domain error.</returns>
    static abstract Result<TEvent[], TError> Decide(TState state, TCommand command);

    /// <summary>
    /// Whether the automaton has reached a terminal state.
    /// </summary>
    /// <param name="state">The state to inspect.</param>
    /// <returns><c>true</c> when no further command handling should occur; otherwise <c>false</c>.</returns>
    static virtual bool IsTerminal(TState state) => false;
}

/// <summary>
/// Runtime that validates commands via Decide before dispatching events.
/// </summary>
/// <typeparam name="TDecider">The decider implementation that owns validation and transition logic.</typeparam>
/// <typeparam name="TState">The state type managed by the runtime.</typeparam>
/// <typeparam name="TCommand">The command type handled by the runtime.</typeparam>
/// <typeparam name="TEvent">The event type emitted after successful decisions.</typeparam>
/// <typeparam name="TEffect">The effect type produced by transitions.</typeparam>
/// <typeparam name="TError">The domain error type returned for rejected commands.</typeparam>
/// <typeparam name="TParameters">The initialization parameter type for startup.</typeparam>
public sealed class DecidingRuntime<TDecider, TState, TCommand, TEvent, TEffect, TError, TParameters> : IDisposable
    where TDecider : Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
{
    private static readonly string _deciderTypeName = typeof(TDecider).Name;
    private static readonly string _stateTypeName = typeof(TState).Name;

    private readonly AutomatonRuntime<TDecider, TState, TEvent, TEffect, TParameters> _core;

    /// <summary>
    /// Gets the current decider state snapshot.
    /// </summary>
    public TState State => _core.State;

    /// <summary>
    /// Gets a snapshot of events dispatched through command handling.
    /// </summary>
    public IReadOnlyList<TEvent> Events => _core.Events;

    /// <summary>
    /// Gets whether the decider is in a terminal state.
    /// </summary>
    public bool IsTerminal => TDecider.IsTerminal(_core.State);

    private DecidingRuntime(AutomatonRuntime<TDecider, TState, TEvent, TEffect, TParameters> core)
    {
        _core = core;
    }

    /// <summary>
    /// Starts a command-handling runtime for the decider.
    /// </summary>
    /// <param name="parameters">The decider initialization parameters.</param>
    /// <param name="observer">Observer pipeline capability for transition side effects.</param>
    /// <param name="interpreter">Interpreter pipeline capability for feedback events.</param>
    /// <param name="threadSafe">Whether command handling is serialized through a gate.</param>
    /// <param name="trackEvents">Whether dispatched events are stored in runtime history.</param>
    /// <param name="cancellationToken">Token used to cancel runtime startup.</param>
    /// <returns>A started deciding runtime.</returns>
    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public static async ValueTask<DecidingRuntime<TDecider, TState, TCommand, TEvent, TEffect, TError, TParameters>> Start(
        TParameters parameters,
        Observer<TState, TEvent, TEffect> observer,
        Interpreter<TEffect, TEvent> interpreter,
        bool threadSafe = true,
        bool trackEvents = true,
        CancellationToken cancellationToken = default)
    {
        using var activity = AutomatonDiagnostics.StartActivity("Automaton.Decider.Start");
        activity?.SetTag("automaton.type", _deciderTypeName);
        activity?.SetTag("automaton.state.type", _stateTypeName);

        var core = await AutomatonRuntime<TDecider, TState, TEvent, TEffect, TParameters>
            .Start(parameters, observer, interpreter, threadSafe, trackEvents, cancellationToken).ConfigureAwait(false);

        activity?.SetStatus(ActivityStatusCode.Ok);
        return new DecidingRuntime<TDecider, TState, TCommand, TEvent, TEffect, TError, TParameters>(core);
    }

    /// <summary>
    /// Handles a command by deciding events and dispatching them atomically.
    /// </summary>
    /// <param name="command">The command to evaluate against current state.</param>
    /// <param name="cancellationToken">Token used to cancel command handling.</param>
    /// <returns>
    /// Updated state on success, or a domain error when decision fails.
    /// </returns>
    public ValueTask<Result<TState, TError>> Handle(TCommand command, CancellationToken cancellationToken = default)
    {
        if (!AutomatonDiagnostics.IsEnabled)
            return HandleWithoutTracing(command, cancellationToken);

        var activity = AutomatonDiagnostics.StartActivity("Automaton.Decider.Handle");
        activity?.SetTag("automaton.type", _deciderTypeName);
        activity?.SetTag("automaton.command.type", command?.GetType().Name);

        if (_core.IsThreadSafe)
        {
            var waitTask = _core.Gate.WaitAsync(cancellationToken);
            if (waitTask.IsCompletedSuccessfully)
                return HandleAfterGate(command, activity, cancellationToken);

            return AwaitGateThenHandle(waitTask, command, activity, cancellationToken);
        }

        return HandleUnserialized(command, activity, cancellationToken);
    }

    private ValueTask<Result<TState, TError>> HandleWithoutTracing(TCommand command, CancellationToken cancellationToken)
    {
        if (_core.IsThreadSafe)
        {
            var waitTask = _core.Gate.WaitAsync(cancellationToken);
            if (waitTask.IsCompletedSuccessfully)
                return HandleAfterGateWithoutTracing(command, cancellationToken);

            return AwaitGateThenHandleWithoutTracing(waitTask, command, cancellationToken);
        }

        return HandleUnserializedWithoutTracing(command, cancellationToken);
    }

    private ValueTask<Result<TState, TError>> HandleUnserializedWithoutTracing(
        TCommand command, CancellationToken cancellationToken)
    {
        var decided = TDecider.Decide(_core.State, command);
        if (decided.IsOk)
        {
            decided.TryGetValue(out var decidedEvents);
            return DispatchEventsAndReturnOkUnserializedWithoutTracing(
                ContractGuards.RequireNonNullArray(decidedEvents),
                cancellationToken);
        }

        decided.TryGetError(out var error);
        return new ValueTask<Result<TState, TError>>(
            Result<TState, TError>.Err(error));
    }

    private ValueTask<Result<TState, TError>> DispatchEventsAndReturnOkUnserializedWithoutTracing(
        TEvent[] events,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < events.Length; i++)
        {
            var dispatchTask = _core.DispatchUnlocked(events[i], 0, cancellationToken);
            if (!dispatchTask.IsCompletedSuccessfully)
                return AwaitRemainingEventsAndReturnOkUnserializedWithoutTracing(dispatchTask, events, i + 1, cancellationToken);

            var dispatchResult = dispatchTask.Result;
            if (dispatchResult.IsErr)
            {
                dispatchResult.TryGetError(out var dispatchError);
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {dispatchError}",
                    dispatchError.Exception);
            }
        }

        return new ValueTask<Result<TState, TError>>(
            Result<TState, TError>.Ok(_core.State));
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<TState, TError>> AwaitRemainingEventsAndReturnOkUnserializedWithoutTracing(
        ValueTask<Result<Unit, PipelineError>> pendingTask, TEvent[] events, int startIndex,
        CancellationToken cancellationToken)
    {
        var pendingResult = await pendingTask.ConfigureAwait(false);
        if (pendingResult.IsErr)
        {
            pendingResult.TryGetError(out var pendingError);
            throw new InvalidOperationException(
                $"Pipeline error during dispatch: {pendingError}",
                pendingError.Exception);
        }

        for (var i = startIndex; i < events.Length; i++)
        {
            var result = await _core.DispatchUnlocked(events[i], 0, cancellationToken).ConfigureAwait(false);
            if (result.IsErr)
            {
                result.TryGetError(out var dispatchError);
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {dispatchError}",
                    dispatchError.Exception);
            }
        }

        return Result<TState, TError>.Ok(_core.State);
    }

    private ValueTask<Result<TState, TError>> HandleAfterGateWithoutTracing(TCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var decided = TDecider.Decide(_core.State, command);
            if (decided.IsOk)
            {
                decided.TryGetValue(out var decidedEvents);
                return DispatchEventsAndReturnOkWithoutTracing(
                    ContractGuards.RequireNonNullArray(decidedEvents),
                    cancellationToken);
            }

            decided.TryGetError(out var error);
            _core.Gate.Release();
            return new ValueTask<Result<TState, TError>>(
                Result<TState, TError>.Err(error));
        }
        catch
        {
            _core.Gate.Release();
            throw;
        }
    }

    private ValueTask<Result<TState, TError>> DispatchEventsAndReturnOkWithoutTracing(TEvent[] events,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < events.Length; i++)
        {
            var dispatchTask = _core.DispatchUnlocked(events[i], 0, cancellationToken);
            if (!dispatchTask.IsCompletedSuccessfully)
                return AwaitRemainingEventsAndReturnOkWithoutTracing(dispatchTask, events, i + 1, cancellationToken);

            var dispatchResult = dispatchTask.Result;
            if (dispatchResult.IsErr)
            {
                dispatchResult.TryGetError(out var dispatchError);
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {dispatchError}",
                    dispatchError.Exception);
            }
        }

        _core.Gate.Release();
        return new ValueTask<Result<TState, TError>>(
            Result<TState, TError>.Ok(_core.State));
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<TState, TError>> AwaitRemainingEventsAndReturnOkWithoutTracing(
        ValueTask<Result<Unit, PipelineError>> pendingTask, TEvent[] events, int startIndex,
        CancellationToken cancellationToken)
    {
        try
        {
            var pendingResult = await pendingTask.ConfigureAwait(false);
            if (pendingResult.IsErr)
            {
                pendingResult.TryGetError(out var pendingError);
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {pendingError}",
                    pendingError.Exception);
            }

            for (var i = startIndex; i < events.Length; i++)
            {
                var result = await _core.DispatchUnlocked(events[i], 0, cancellationToken).ConfigureAwait(false);
                if (result.IsErr)
                {
                    result.TryGetError(out var dispatchError);
                    throw new InvalidOperationException(
                        $"Pipeline error during dispatch: {dispatchError}",
                        dispatchError.Exception);
                }
            }

            return Result<TState, TError>.Ok(_core.State);
        }
        finally
        {
            _core.Gate.Release();
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<TState, TError>> AwaitGateThenHandleWithoutTracing(Task waitTask,
        TCommand command, CancellationToken cancellationToken)
    {
        await waitTask.ConfigureAwait(false);
        try
        {
            var decided = TDecider.Decide(_core.State, command);
            if (decided.IsOk)
            {
                decided.TryGetValue(out var decidedEvents);
                var events = ContractGuards.RequireNonNullArray(decidedEvents);
                foreach (var @event in events)
                {
                    var result = await _core.DispatchUnlocked(@event, 0, cancellationToken).ConfigureAwait(false);
                    if (result.IsErr)
                    {
                        result.TryGetError(out var dispatchError);
                        throw new InvalidOperationException(
                            $"Pipeline error during dispatch: {dispatchError}",
                            dispatchError.Exception);
                    }
                }

                return Result<TState, TError>.Ok(_core.State);
            }

            decided.TryGetError(out var error);
            return Result<TState, TError>.Err(error);
        }
        finally
        {
            _core.Gate.Release();
        }
    }

    private ValueTask<Result<TState, TError>> HandleUnserialized(
        TCommand command, Activity? activity, CancellationToken cancellationToken)
    {
        try
        {
            var decided = TDecider.Decide(_core.State, command);
            if (decided.IsOk)
            {
                decided.TryGetValue(out var decidedEvents);
                return DispatchEventsAndReturnOkUnserialized(
                    ContractGuards.RequireNonNullArray(decidedEvents),
                    activity,
                    cancellationToken);
            }
            else
            {
                decided.TryGetError(out var error);
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", error?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                return new ValueTask<Result<TState, TError>>(
                    Result<TState, TError>.Err(error));
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
        TEvent[] events,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 0; i < events.Length; i++)
            {
                var dispatchTask = _core.DispatchUnlocked(events[i], 0, cancellationToken);
                if (!dispatchTask.IsCompletedSuccessfully)
                    return AwaitRemainingEventsAndReturnOkUnserialized(dispatchTask, events, i + 1, activity, cancellationToken);

                var dispatchResult = dispatchTask.Result;
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
        Activity? activity,
        CancellationToken cancellationToken)
    {
        using var _ = activity;
        try
        {
            var pendingResult = await pendingTask.ConfigureAwait(false);
            if (pendingResult.IsErr)
            {
                pendingResult.TryGetError(out var pendingError);
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {pendingError}",
                    pendingError.Exception);
            }

            for (var i = startIndex; i < events.Length; i++)
            {
                var result = await _core.DispatchUnlocked(events[i], 0, cancellationToken).ConfigureAwait(false);
                if (result.IsErr)
                {
                    result.TryGetError(out var dispatchError);
                    throw new InvalidOperationException(
                        $"Pipeline error during dispatch: {dispatchError}",
                        dispatchError.Exception);
                }
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

    private ValueTask<Result<TState, TError>> HandleAfterGate(TCommand command, Activity? activity,
        CancellationToken cancellationToken)
    {
        try
        {
            var decided = TDecider.Decide(_core.State, command);
            if (decided.IsOk)
            {
                decided.TryGetValue(out var decidedEvents);
                return DispatchEventsAndReturnOk(
                    ContractGuards.RequireNonNullArray(decidedEvents),
                    activity, cancellationToken);
            }
            else
            {
                decided.TryGetError(out var error);
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", error?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                _core.Gate.Release();
                return new ValueTask<Result<TState, TError>>(
                    Result<TState, TError>.Err(error));
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

    private ValueTask<Result<TState, TError>> DispatchEventsAndReturnOk(TEvent[] events, Activity? activity,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < events.Length; i++)
        {
            var dispatchTask = _core.DispatchUnlocked(events[i], 0, cancellationToken);
            if (!dispatchTask.IsCompletedSuccessfully)
                return AwaitRemainingEventsAndReturnOk(dispatchTask, events, i + 1, activity, cancellationToken);

            var dispatchResult = dispatchTask.Result;
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
        activity?.Dispose();
        _core.Gate.Release();
        return new ValueTask<Result<TState, TError>>(
            Result<TState, TError>.Ok(_core.State));
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<TState, TError>> AwaitRemainingEventsAndReturnOk(
        ValueTask<Result<Unit, PipelineError>> pendingTask, TEvent[] events, int startIndex,
        Activity? activity,
        CancellationToken cancellationToken)
    {
        using var _ = activity;
        try
        {
            var pendingResult = await pendingTask.ConfigureAwait(false);
            if (pendingResult.IsErr)
            {
                pendingResult.TryGetError(out var pendingError);
                throw new InvalidOperationException(
                    $"Pipeline error during dispatch: {pendingError}",
                    pendingError.Exception);
            }

            for (var i = startIndex; i < events.Length; i++)
            {
                var result = await _core.DispatchUnlocked(events[i], 0, cancellationToken).ConfigureAwait(false);
                if (result.IsErr)
                {
                    result.TryGetError(out var dispatchError);
                    throw new InvalidOperationException(
                        $"Pipeline error during dispatch: {dispatchError}",
                        dispatchError.Exception);
                }
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
    private async ValueTask<Result<TState, TError>> AwaitGateThenHandle(Task waitTask, TCommand command,
        Activity? activity, CancellationToken cancellationToken)
    {
        using var _ = activity;
        await waitTask.ConfigureAwait(false);
        try
        {
            var decided = TDecider.Decide(_core.State, command);
            if (decided.IsOk)
            {
                decided.TryGetValue(out var decidedEvents);
                var events = ContractGuards.RequireNonNullArray(decidedEvents);
                foreach (var t in events)
                {
                    var result = await _core.DispatchUnlocked(t, 0, cancellationToken).ConfigureAwait(false);
                    if (result.IsErr)
                    {
                        result.TryGetError(out var dispatchError);
                        throw new InvalidOperationException(
                            $"Pipeline error during dispatch: {dispatchError}",
                            dispatchError.Exception);
                    }
                }

                activity?.SetTag("automaton.result", "ok");
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result<TState, TError>.Ok(_core.State);
            }
            else
            {
                decided.TryGetError(out var error);
                activity?.SetTag("automaton.result", "error");
                activity?.SetTag("automaton.error.type", error?.GetType().Name);
                activity?.SetStatus(ActivityStatusCode.Ok);
                return Result<TState, TError>.Err(error);
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

    /// <summary>
    /// Resets the runtime state to the provided value.
    /// </summary>
    /// <param name="state">The state value to set as the new current state.</param>
    public void Reset(TState state) => _core.Reset(state);

    /// <summary>
    /// Releases resources owned by the runtime.
    /// </summary>
    public void Dispose() => _core.Dispose();
}
