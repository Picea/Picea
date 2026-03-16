// =============================================================================
// Automaton Runtime
// =============================================================================
// The shared runtime abstraction underlying MVU, Event Sourcing, and Actors.
//
// Mathematically, the runtime is a monadic left fold over an event stream:
//
//     foldM : (State -> Event -> M (State, Effect)) -> State -> [Event] -> M State
//
// It is parameterized by two extension points:
//
// 1. Observer  — sees each (State, Event, Effect) triple after transition.
//                Returns Result<Unit, PipelineError> to propagate errors as values.
//                Used for rendering (MVU), persisting (ES), or logging.
//
// 2. Interpreter — converts effects into feedback events.
//                   Returns Result<Events, PipelineError> to propagate errors as values.
//                   Used for effect handling / command execution.
//
// Both Observer and Interpreter form monadic pipelines: they compose via
// standard FP combinators (Then, Where, Select, Catch, Combine) using
// C#/.NET naming conventions (LINQ-style Where/Select).
//
// Every specialized runtime (MVU, ES, Actor) is an instance of this
// structure with specific Observer and Interpreter implementations.
//
// Thread safety:
//     All public entry points (Dispatch, InterpretEffect, Start) are serialized
//     via a SemaphoreSlim. Concurrent callers are queued, never interleaved.
//     Reading State or Events while a Dispatch is in-flight is synchronized
//     with transitions and returns a consistent snapshot.
//
// Feedback depth:
//     Interpreter feedback loops (effect → events → effect → …) are bounded
//     by MaxFeedbackDepth (default 64). Exceeding this throws
//     InvalidOperationException to prevent stack overflows from runaway loops.
// =============================================================================

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Picea;

/// <summary>
/// A structured error from an Observer or Interpreter pipeline stage.
/// </summary>
public readonly record struct PipelineError(
    string Message,
    string? Source = null,
    Exception? Exception = null)
{
    /// <inheritdoc/>
    public override string ToString() =>
        Source is not null ? $"[{Source}] {Message}" : Message;
}

/// <summary>
/// Observes each transition triple (state, event, effect) after the automaton steps.
/// </summary>
public delegate ValueTask<Result<Unit, PipelineError>> Observer<in TState, in TEvent, in TEffect>(
    TState state,
    TEvent @event,
    TEffect effect);

/// <summary>
/// Interprets an effect by converting it into zero or more feedback events.
/// </summary>
public delegate ValueTask<Result<TEvent[], PipelineError>> Interpreter<in TEffect, TEvent>(TEffect effect);

/// <summary>
/// Pre-allocated Result values for common pipeline outcomes.
/// </summary>
public static class PipelineResult
{
    /// <summary>
    /// A completed ValueTask containing Ok(Unit) — the happy path for observers.
    /// </summary>
    public static readonly ValueTask<Result<Unit, PipelineError>> Ok =
        new(Result<Unit, PipelineError>.Ok(Unit.Value));
}

/// <summary>
/// Pre-allocated Result values for common interpreter pipeline outcomes.
/// </summary>
/// <remarks>
/// <para>
/// Provides a cached empty result for interpreters that produce no feedback events,
/// analogous to <see cref="PipelineResult.Ok"/> for observers.
/// </para>
/// <example>
/// <code>
/// // Instead of constructing a new ValueTask + Result + empty array each time:
/// Interpreter&lt;MyEffect, MyEvent&gt; noOp = _ =>
///     new ValueTask&lt;Result&lt;MyEvent[], PipelineError&gt;&gt;(
///         Result&lt;MyEvent[], PipelineError&gt;.Ok([]));
///
/// // Use the pre-allocated empty result:
/// Interpreter&lt;MyEffect, MyEvent&gt; noOp = _ =>
///     InterpreterResult&lt;MyEvent&gt;.Empty;
/// </code>
/// </example>
/// </remarks>
/// <typeparam name="TEvent">The event type produced by the interpreter.</typeparam>
public static class InterpreterResult<TEvent>
{
    /// <summary>
    /// A completed ValueTask containing Ok(Array.Empty) — the happy path for interpreters
    /// that produce no feedback events.
    /// </summary>
    public static readonly ValueTask<Result<TEvent[], PipelineError>> Empty =
        new(Result<TEvent[], PipelineError>.Ok(Array.Empty<TEvent>()));
}

internal static class ContractGuards
{
    public static T[] RequireNonNullArray<T>(T[]? values, [CallerArgumentExpression(nameof(values))] string? paramName = null) =>
        values ?? throw new InvalidOperationException($"{paramName ?? "Array"} must not be null.");
}

/// <summary>
/// The shared automaton runtime: a monadic left fold with Observer and Interpreter.
/// </summary>
public sealed class AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters> : IDisposable
    where TAutomaton : Automaton<TState, TEvent, TEffect, TParameters>
{
    public const int MaxFeedbackDepth = 64;

    private static readonly string _automatonTypeName = typeof(TAutomaton).Name;
    private static readonly string _stateTypeName = typeof(TState).Name;

    private TState _state;
    private readonly Observer<TState, TEvent, TEffect> _observer;
    private readonly Interpreter<TEffect, TEvent> _interpreter;
    private readonly List<TEvent>? _events;
    private TEvent[] _eventsSnapshot = Array.Empty<TEvent>();
    private bool _eventsSnapshotDirty;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _snapshotLock = new();
    private readonly bool _threadSafe;

    public TState State
    {
        get
        {
            lock (_snapshotLock)
                return _state;
        }
    }

    public IReadOnlyList<TEvent> Events
    {
        get
        {
            lock (_snapshotLock)
            {
                if (_events is null)
                    return Array.Empty<TEvent>();

                if (_eventsSnapshotDirty)
                {
                    _eventsSnapshot = _events.Count == 0 ? Array.Empty<TEvent>() : _events.ToArray();
                    _eventsSnapshotDirty = false;
                }

                return _eventsSnapshot;
            }
        }
    }

    internal SemaphoreSlim Gate => _gate;
    internal bool IsThreadSafe => _threadSafe;

    public AutomatonRuntime(
        TState initialState,
        Observer<TState, TEvent, TEffect> observer,
        Interpreter<TEffect, TEvent> interpreter,
        bool threadSafe = true,
        bool trackEvents = true)
    {
        _state = initialState;
        _observer = observer ?? throw new ArgumentNullException(nameof(observer));
        _interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
        _threadSafe = threadSafe;
        _events = trackEvents ? [] : null;
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    public static async ValueTask<AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>> Start(
        TParameters parameters,
        Observer<TState, TEvent, TEffect> observer,
        Interpreter<TEffect, TEvent> interpreter,
        bool threadSafe = true,
        bool trackEvents = true,
        CancellationToken cancellationToken = default)
    {
        using var activity = AutomatonDiagnostics.Source.StartActivity("Automaton.Start");
        activity?.SetTag("automaton.type", _automatonTypeName);
        activity?.SetTag("automaton.state.type", _stateTypeName);

        var (state, effect) = TAutomaton.Initialize(parameters);
        var runtime = new AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>(state, observer, interpreter, threadSafe, trackEvents);
        await runtime.InterpretEffect(effect, cancellationToken).ConfigureAwait(false);

        activity?.SetStatus(ActivityStatusCode.Ok);
        return runtime;
    }

    public ValueTask<Result<Unit, PipelineError>> Dispatch(TEvent @event, CancellationToken cancellationToken = default)
    {
        var activity = AutomatonDiagnostics.Source.StartActivity("Automaton.Dispatch");
        activity?.SetTag("automaton.type", _automatonTypeName);
        activity?.SetTag("automaton.event.type", @event?.GetType().Name);

        if (_threadSafe)
        {
            var waitTask = _gate.WaitAsync(cancellationToken);
            if (waitTask.IsCompletedSuccessfully)
                return DispatchAfterGate(@event, cancellationToken, activity);

            return AwaitGateThenDispatch(waitTask, @event, cancellationToken, activity);
        }

        return DispatchUnserialized(@event, cancellationToken, activity);
    }

    private ValueTask<Result<Unit, PipelineError>> DispatchUnserialized(
        TEvent @event, CancellationToken cancellationToken, Activity? activity)
    {
        try
        {
            var coreTask = DispatchCore(@event, cancellationToken);
            if (coreTask.IsCompletedSuccessfully)
            {
                var result = coreTask.Result;
                if (result.IsOk)
                    activity?.SetStatus(ActivityStatusCode.Ok);
                else
                    activity?.SetStatus(ActivityStatusCode.Error, result.Error.Message);
                activity?.Dispose();
                return new ValueTask<Result<Unit, PipelineError>>(result);
            }

            return AwaitCoreUnserialized(coreTask, activity);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            throw;
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<Unit, PipelineError>> AwaitCoreUnserialized(
        ValueTask<Result<Unit, PipelineError>> coreTask, Activity? activity)
    {
        using var _ = activity;
        try
        {
            var result = await coreTask.ConfigureAwait(false);
            if (result.IsOk)
                activity?.SetStatus(ActivityStatusCode.Ok);
            else
                activity?.SetStatus(ActivityStatusCode.Error, result.Error.Message);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private ValueTask<Result<Unit, PipelineError>> DispatchAfterGate(
        TEvent @event, CancellationToken cancellationToken, Activity? activity)
    {
        try
        {
            var coreTask = DispatchCore(@event, cancellationToken);
            if (coreTask.IsCompletedSuccessfully)
            {
                var result = coreTask.Result;
                if (result.IsOk)
                    activity?.SetStatus(ActivityStatusCode.Ok);
                else
                    activity?.SetStatus(ActivityStatusCode.Error, result.Error.Message);
                activity?.Dispose();
                _gate.Release();
                return new ValueTask<Result<Unit, PipelineError>>(result);
            }

            return AwaitCoreThenRelease(coreTask, activity);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            _gate.Release();
            throw;
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<Unit, PipelineError>> AwaitCoreThenRelease(
        ValueTask<Result<Unit, PipelineError>> coreTask, Activity? activity)
    {
        using var _ = activity;
        try
        {
            var result = await coreTask.ConfigureAwait(false);
            if (result.IsOk)
                activity?.SetStatus(ActivityStatusCode.Ok);
            else
                activity?.SetStatus(ActivityStatusCode.Error, result.Error.Message);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<Unit, PipelineError>> AwaitGateThenDispatch(
        Task waitTask, TEvent @event, CancellationToken cancellationToken, Activity? activity)
    {
        using var _ = activity;
        await waitTask.ConfigureAwait(false);
        try
        {
            var result = await DispatchCore(@event, cancellationToken).ConfigureAwait(false);
            if (result.IsOk)
                activity?.SetStatus(ActivityStatusCode.Ok);
            else
                activity?.SetStatus(ActivityStatusCode.Error, result.Error.Message);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public ValueTask InterpretEffect(TEffect effect, CancellationToken cancellationToken = default)
    {
        var activity = AutomatonDiagnostics.Source.StartActivity("Automaton.InterpretEffect");
        activity?.SetTag("automaton.type", _automatonTypeName);
        activity?.SetTag("automaton.effect.type", effect?.GetType().Name);

        if (_threadSafe)
        {
            var waitTask = _gate.WaitAsync(cancellationToken);
            if (waitTask.IsCompletedSuccessfully)
                return InterpretEffectAfterGate(effect, cancellationToken, activity);

            return AwaitGateThenInterpret(waitTask, effect, cancellationToken, activity);
        }

        return InterpretEffectUnserialized(effect, cancellationToken, activity);
    }

    private ValueTask InterpretEffectUnserialized(TEffect effect, CancellationToken cancellationToken, Activity? activity)
    {
        try
        {
            var coreTask = InterpretEffectCore(effect, cancellationToken);
            if (coreTask.IsCompletedSuccessfully)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                return ValueTask.CompletedTask;
            }

            return AwaitInterpretCoreUnserialized(coreTask, activity);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            throw;
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    private async ValueTask AwaitInterpretCoreUnserialized(ValueTask coreTask, Activity? activity)
    {
        using var _ = activity;
        try
        {
            await coreTask.ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private ValueTask InterpretEffectAfterGate(TEffect effect, CancellationToken cancellationToken, Activity? activity)
    {
        try
        {
            var coreTask = InterpretEffectCore(effect, cancellationToken);
            if (coreTask.IsCompletedSuccessfully)
            {
                activity?.SetStatus(ActivityStatusCode.Ok);
                activity?.Dispose();
                _gate.Release();
                return ValueTask.CompletedTask;
            }

            return AwaitInterpretCoreThenRelease(coreTask, activity);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.Dispose();
            _gate.Release();
            throw;
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    private async ValueTask AwaitInterpretCoreThenRelease(ValueTask coreTask, Activity? activity)
    {
        using var _ = activity;
        try
        {
            await coreTask.ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    private async ValueTask AwaitGateThenInterpret(
        Task waitTask, TEffect effect, CancellationToken cancellationToken, Activity? activity)
    {
        using var _ = activity;
        await waitTask.ConfigureAwait(false);
        try
        {
            await InterpretEffectCore(effect, cancellationToken).ConfigureAwait(false);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Reset(TState state)
    {
        if (_threadSafe)
        {
            _gate.Wait();
            try
            {
                lock (_snapshotLock)
                    _state = state;
            }
            finally { _gate.Release(); }
        }
        else
        {
            lock (_snapshotLock)
                _state = state;
        }
    }

    public void Dispose() => _gate.Dispose();

    internal ValueTask<Result<Unit, PipelineError>> DispatchUnlocked(
        TEvent @event, CancellationToken cancellationToken, int depth = 0)
        => DispatchCore(@event, cancellationToken, depth);

    private ValueTask<Result<Unit, PipelineError>> DispatchCore(
        TEvent @event, CancellationToken cancellationToken, int depth = 0)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TState newState;
        TEffect effect;

        lock (_snapshotLock)
        {
            _events?.Add(@event);
            if (_events is not null)
                _eventsSnapshotDirty = true;
            (newState, effect) = TAutomaton.Transition(_state, @event);
            _state = newState;
        }

        var observerTask = _observer(newState, @event, effect);
        if (observerTask.IsCompletedSuccessfully)
        {
            var observerResult = observerTask.Result;
            if (observerResult.IsErr)
                return new ValueTask<Result<Unit, PipelineError>>(observerResult);

            return InterpretEffectCoreWithResult(effect, cancellationToken, depth);
        }

        return AwaitObserverThenInterpret(observerTask, effect, cancellationToken, depth);
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<Unit, PipelineError>> AwaitObserverThenInterpret(
        ValueTask<Result<Unit, PipelineError>> observerTask, TEffect effect,
        CancellationToken cancellationToken, int depth)
    {
        var observerResult = await observerTask.ConfigureAwait(false);
        if (observerResult.IsErr)
            return observerResult;

        return await InterpretEffectCoreWithResult(effect, cancellationToken, depth).ConfigureAwait(false);
    }

    private ValueTask<Result<Unit, PipelineError>> InterpretEffectCoreWithResult(
        TEffect effect, CancellationToken cancellationToken, int depth = 0)
    {
        if (depth > MaxFeedbackDepth)
            throw new InvalidOperationException(
                $"Interpreter feedback loop exceeded maximum depth of {MaxFeedbackDepth}. " +
                "This usually indicates an infinite feedback cycle where an effect always " +
                "produces events whose transitions produce the same effect.");

        cancellationToken.ThrowIfCancellationRequested();

        var interpreterTask = _interpreter(effect);
        if (interpreterTask.IsCompletedSuccessfully)
        {
            var interpreterResult = interpreterTask.Result;
            if (interpreterResult.IsErr)
                return new ValueTask<Result<Unit, PipelineError>>(
                    Result<Unit, PipelineError>.Err(interpreterResult.Error));

            return DispatchFeedbackEventsWithResult(
                ContractGuards.RequireNonNullArray(interpreterResult.Value),
                cancellationToken,
                depth);
        }

        return AwaitInterpreterThenDispatchWithResult(interpreterTask, cancellationToken, depth);
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<Unit, PipelineError>> AwaitInterpreterThenDispatchWithResult(
        ValueTask<Result<TEvent[], PipelineError>> interpreterTask,
        CancellationToken cancellationToken, int depth)
    {
        var interpreterResult = await interpreterTask.ConfigureAwait(false);
        if (interpreterResult.IsErr)
            return Result<Unit, PipelineError>.Err(interpreterResult.Error);

        return await DispatchFeedbackEventsWithResult(
                ContractGuards.RequireNonNullArray(interpreterResult.Value),
                cancellationToken,
                depth)
            .ConfigureAwait(false);
    }

    private ValueTask<Result<Unit, PipelineError>> DispatchFeedbackEventsWithResult(
        TEvent[] feedbackEvents,
        CancellationToken cancellationToken, int depth)
    {
        feedbackEvents = ContractGuards.RequireNonNullArray(feedbackEvents);

        if (feedbackEvents.Length == 0)
            return PipelineResult.Ok;

        return DispatchFeedbackEventsWithResultAsync(feedbackEvents, cancellationToken, depth);
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private async ValueTask<Result<Unit, PipelineError>> DispatchFeedbackEventsWithResultAsync(
        TEvent[] feedbackEvents,
        CancellationToken cancellationToken, int depth)
    {
        for (var i = 0; i < feedbackEvents.Length; i++)
        {
            var result = await DispatchCore(feedbackEvents[i], cancellationToken, depth + 1)
                .ConfigureAwait(false);
            if (result.IsErr)
                return result;
        }

        return Result<Unit, PipelineError>.Ok(Unit.Value);
    }

    private ValueTask InterpretEffectCore(TEffect effect, CancellationToken cancellationToken, int depth = 0)
    {
        var resultTask = InterpretEffectCoreWithResult(effect, cancellationToken, depth);
        if (resultTask.IsCompletedSuccessfully)
        {
            var result = resultTask.Result;
            if (result.IsErr)
                ThrowInterpreterPipelineError(result.Error);
            return ValueTask.CompletedTask;
        }

        return AwaitInterpretEffectCoreUnwrap(resultTask);
    }

    private static void ThrowInterpreterPipelineError(PipelineError error) =>
        throw new InvalidOperationException($"Interpreter pipeline failed: {error}", error.Exception);

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    private static async ValueTask AwaitInterpretEffectCoreUnwrap(
        ValueTask<Result<Unit, PipelineError>> resultTask)
    {
        var result = await resultTask.ConfigureAwait(false);
        if (result.IsErr)
            ThrowInterpreterPipelineError(result.Error);
    }
}

/// <summary>
/// Combinators for composing observers into monadic pipelines.
/// </summary>
public static class ObserverExtensions
{
    public static Observer<TState, TEvent, TEffect> Then<TState, TEvent, TEffect>(
        this Observer<TState, TEvent, TEffect> first,
        Observer<TState, TEvent, TEffect> second) =>
        (state, @event, effect) =>
        {
            var t1 = first(state, @event, effect);
            if (t1.IsCompletedSuccessfully)
            {
                var r1 = t1.Result;
                if (r1.IsErr)
                    return new ValueTask<Result<Unit, PipelineError>>(r1);
                return second(state, @event, effect);
            }

            return AwaitFirstThenSecond(t1, second, state, @event, effect);
        };

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private static async ValueTask<Result<Unit, PipelineError>> AwaitFirstThenSecond<TState, TEvent, TEffect>(
        ValueTask<Result<Unit, PipelineError>> first,
        Observer<TState, TEvent, TEffect> second,
        TState state, TEvent @event, TEffect effect)
    {
        var r1 = await first.ConfigureAwait(false);
        if (r1.IsErr)
            return r1;
        return await second(state, @event, effect).ConfigureAwait(false);
    }

    public static Observer<TState, TEvent, TEffect> Where<TState, TEvent, TEffect>(
        this Observer<TState, TEvent, TEffect> observer,
        Func<TState, TEvent, TEffect, bool> predicate) =>
        (state, @event, effect) =>
            predicate(state, @event, effect)
                ? observer(state, @event, effect)
                : PipelineResult.Ok;

    public static Observer<TState1, TEvent1, TEffect1>
        Select<TState2, TEvent2, TEffect2, TState1, TEvent1, TEffect1>(
            this Observer<TState2, TEvent2, TEffect2> observer,
            Func<TState1, TEvent1, TEffect1, (TState2 State, TEvent2 Event, TEffect2 Effect)> project) =>
        (state, @event, effect) =>
        {
            var (s2, e2, eff2) = project(state, @event, effect);
            return observer(s2, e2, eff2);
        };

    public static Observer<TState, TEvent, TEffect> Catch<TState, TEvent, TEffect>(
        this Observer<TState, TEvent, TEffect> observer,
        Func<PipelineError, Result<Unit, PipelineError>> handler) =>
        (state, @event, effect) =>
        {
            var task = observer(state, @event, effect);
            if (task.IsCompletedSuccessfully)
            {
                var result = task.Result;
                return result.IsErr
                    ? new ValueTask<Result<Unit, PipelineError>>(handler(result.Error))
                    : task;
            }

            return AwaitThenCatch(task, handler);
        };

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private static async ValueTask<Result<Unit, PipelineError>> AwaitThenCatch(
        ValueTask<Result<Unit, PipelineError>> task,
        Func<PipelineError, Result<Unit, PipelineError>> handler)
    {
        var result = await task.ConfigureAwait(false);
        return result.IsErr ? handler(result.Error) : result;
    }

    public static Observer<TState, TEvent, TEffect> Combine<TState, TEvent, TEffect>(
        this Observer<TState, TEvent, TEffect> first,
        Observer<TState, TEvent, TEffect> second) =>
        (state, @event, effect) =>
        {
            var t1 = first(state, @event, effect);
            if (t1.IsCompletedSuccessfully)
            {
                var r1 = t1.Result;
                var t2 = second(state, @event, effect);
                if (t2.IsCompletedSuccessfully)
                {
                    var r2 = t2.Result;
                    return r1.IsErr
                        ? new ValueTask<Result<Unit, PipelineError>>(r1)
                        : new ValueTask<Result<Unit, PipelineError>>(r2);
                }

                return AwaitSecondThenCombine(r1, t2);
            }

            return AwaitFirstThenRunSecondThenCombine(t1, second, state, @event, effect);
        };

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private static async ValueTask<Result<Unit, PipelineError>> AwaitSecondThenCombine(
        Result<Unit, PipelineError> r1,
        ValueTask<Result<Unit, PipelineError>> t2)
    {
        var r2 = await t2.ConfigureAwait(false);
        return r1.IsErr ? r1 : r2;
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private static async ValueTask<Result<Unit, PipelineError>> AwaitFirstThenRunSecondThenCombine<TState, TEvent, TEffect>(
        ValueTask<Result<Unit, PipelineError>> t1,
        Observer<TState, TEvent, TEffect> second,
        TState state,
        TEvent @event,
        TEffect effect)
    {
        var r1 = await t1.ConfigureAwait(false);
        var r2 = await second(state, @event, effect).ConfigureAwait(false);
        return r1.IsErr ? r1 : r2;
    }
}

/// <summary>
/// Combinators for composing interpreters into monadic pipelines.
/// </summary>
public static class InterpreterExtensions
{
    public static Interpreter<TEffect, TEvent> Then<TEffect, TEvent>(
        this Interpreter<TEffect, TEvent> first,
        Interpreter<TEffect, TEvent> second) =>
        effect =>
        {
            var t1 = first(effect);
            if (t1.IsCompletedSuccessfully)
            {
                var r1 = t1.Result;
                if (r1.IsErr)
                    return new ValueTask<Result<TEvent[], PipelineError>>(r1);

                var firstEvents = r1.Value;
                var t2 = second(effect);
                if (t2.IsCompletedSuccessfully)
                {
                    var r2 = t2.Result;
                    if (r2.IsErr)
                        return new ValueTask<Result<TEvent[], PipelineError>>(r2);

                    var secondEvents = r2.Value;
                    var combined = ConcatEvents(firstEvents, secondEvents);
                    return new ValueTask<Result<TEvent[], PipelineError>>(
                        Result<TEvent[], PipelineError>.Ok(combined));
                }

                return AwaitSecondInterpreter(firstEvents, t2);
            }

            return AwaitFirstThenSecondInterpreter(t1, second, effect);
        };

    private static TEvent[] ConcatEvents<TEvent>(TEvent[] first, TEvent[] second) =>
        ContractGuards.RequireNonNullArray(first) is var safeFirst
        && ContractGuards.RequireNonNullArray(second) is var safeSecond
            ? safeFirst.Length == 0 ? safeSecond
            : safeSecond.Length == 0 ? safeFirst
            : [.. safeFirst, .. safeSecond]
            : throw new UnreachableException();

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private static async ValueTask<Result<TEvent[], PipelineError>> AwaitSecondInterpreter<TEvent>(
        TEvent[] firstEvents,
        ValueTask<Result<TEvent[], PipelineError>> secondTask)
    {
        var r2 = await secondTask.ConfigureAwait(false);
        return r2.IsErr
            ? r2
            : Result<TEvent[], PipelineError>.Ok(ConcatEvents(firstEvents, r2.Value));
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private static async ValueTask<Result<TEvent[], PipelineError>> AwaitFirstThenSecondInterpreter<TEffect, TEvent>(
        ValueTask<Result<TEvent[], PipelineError>> firstTask,
        Interpreter<TEffect, TEvent> second,
        TEffect effect)
    {
        var r1 = await firstTask.ConfigureAwait(false);
        if (r1.IsErr)
            return r1;

        var r2 = await second(effect).ConfigureAwait(false);
        return r2.IsErr
            ? r2
            : Result<TEvent[], PipelineError>.Ok(ConcatEvents(r1.Value, r2.Value));
    }

    public static Interpreter<TEffect, TEvent> Where<TEffect, TEvent>(
        this Interpreter<TEffect, TEvent> interpreter,
        Func<TEffect, bool> predicate) =>
        effect =>
            predicate(effect)
                ? interpreter(effect)
                : new ValueTask<Result<TEvent[], PipelineError>>(
                    Result<TEvent[], PipelineError>.Ok([]));

    public static Interpreter<TEffect1, TEvent> Select<TEffect2, TEvent, TEffect1>(
        this Interpreter<TEffect2, TEvent> interpreter,
        Func<TEffect1, TEffect2> project) =>
        effect => interpreter(project(effect));

    public static Interpreter<TEffect, TEvent> Catch<TEffect, TEvent>(
        this Interpreter<TEffect, TEvent> interpreter,
        Func<PipelineError, Result<TEvent[], PipelineError>> handler) =>
        effect =>
        {
            var task = interpreter(effect);
            if (task.IsCompletedSuccessfully)
            {
                var result = task.Result;
                return result.IsErr
                    ? new ValueTask<Result<TEvent[], PipelineError>>(handler(result.Error))
                    : task;
            }

            return AwaitInterpreterThenCatch(task, handler);
        };

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
    private static async ValueTask<Result<TEvent[], PipelineError>> AwaitInterpreterThenCatch<TEvent>(
        ValueTask<Result<TEvent[], PipelineError>> task,
        Func<PipelineError, Result<TEvent[], PipelineError>> handler)
    {
        var result = await task.ConfigureAwait(false);
        return result.IsErr ? handler(result.Error) : result;
    }
}
