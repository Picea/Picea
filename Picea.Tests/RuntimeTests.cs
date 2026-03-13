// =============================================================================
// Shared Runtime Tests
// =============================================================================
// Proves the AutomatonRuntime correctly implements the monadic left fold
// with Observer and Interpreter extension points, using the Thermostat domain.
// =============================================================================

namespace Picea.Tests;

public class RuntimeTests
{
    [Fact]
    public async Task Dispatch_UpdatesState()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOff());

        Assert.Equal(18m, runtime.State.CurrentTemp);
        Assert.False(runtime.State.Heating);
    }

    [Fact]
    public async Task Observer_ReceivesCorrectArguments()
    {
        var observed = new List<(ThermostatState State, ThermostatEvent Event, ThermostatEffect Effect)>();

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.Capture(observed), ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(25m));

        Assert.Single(observed);
        Assert.Equal(25m, observed[0].State.CurrentTemp);
        Assert.IsType<ThermostatEvent.TemperatureRecorded>(observed[0].Event);
        Assert.IsType<ThermostatEffect.None>(observed[0].Effect);
    }

    [Fact]
    public async Task Interpreter_FeedbackEventsAreDispatched()
    {
        var feedbackCount = 0;

        // Interpreter: on ActivateHeater effect, simulate a sensor reading (once)
        Interpreter<ThermostatEffect, ThermostatEvent> interpreter = effect =>
        {
            if (effect is ThermostatEffect.ActivateHeater && feedbackCount == 0)
            {
                feedbackCount++;
                return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                    Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.TemperatureRecorded(19m)]));
            }

            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, interpreter);

        // HeaterTurnedOn -> Heating=true, ActivateHeater effect -> interpreter returns TemperatureRecorded(19)
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());

        // State: CurrentTemp=19, Heating=true (HeaterTurnedOn + feedback TemperatureRecorded)
        Assert.Equal(19m, runtime.State.CurrentTemp);
        Assert.True(runtime.State.Heating);
    }

    [Fact]
    public async Task ObserverComposition_Then_BothObserversAreCalled()
    {
        var firstCalls = 0;
        var secondCalls = 0;

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> first = (_, _, _) =>
        {
            firstCalls++;
            return PipelineResult.Ok;
        };

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> second = (_, _, _) =>
        {
            secondCalls++;
            return PipelineResult.Ok;
        };

        var combined = first.Then(second);

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), combined, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());

        Assert.Equal(2, firstCalls);
        Assert.Equal(2, secondCalls);
    }

    [Fact]
    public void Reset_ReplacesStateWithoutTransition()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        runtime.Reset(new ThermostatState(25m, 30m, true, true));

        Assert.Equal(25m, runtime.State.CurrentTemp);
        Assert.Equal(30m, runtime.State.TargetTemp);
        Assert.True(runtime.State.Heating);
        Assert.Empty(runtime.Events);
    }

    [Fact]
    public async Task Events_RecordedIncludingFeedback()
    {
        var feedbackCount = 0;

        Interpreter<ThermostatEffect, ThermostatEvent> interpreter = effect =>
        {
            if (effect is ThermostatEffect.ActivateHeater && feedbackCount == 0)
            {
                feedbackCount++;
                return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                    Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.TemperatureRecorded(19m)]));
            }

            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, interpreter);

        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());

        // Events: HeaterTurnedOn, TemperatureRecorded(19) (feedback)
        Assert.Equal(2, runtime.Events.Count);
        Assert.IsType<ThermostatEvent.HeaterTurnedOn>(runtime.Events[0]);
        Assert.IsType<ThermostatEvent.TemperatureRecorded>(runtime.Events[1]);
    }

    [Fact]
    public async Task Start_CreatesRuntimeAndInterpretsInitEffect()
    {
        var runtime = await AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        // Thermostat.Initialize() produces (CurrentTemp=20, TargetTemp=22, Heating=false, Active=true), None
        Assert.Equal(20m, runtime.State.CurrentTemp);
        Assert.Equal(22m, runtime.State.TargetTemp);
        Assert.False(runtime.State.Heating);
        Assert.True(runtime.State.Active);
        Assert.Empty(runtime.Events);
    }

    // =========================================================================
    // Thread Safety
    // =========================================================================

    [Fact]
    public async Task ConcurrentDispatches_AreSerializedAndProduceCorrectFinalState()
    {
        // Arrange: 100 concurrent temperature readings
        const int concurrency = 100;
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        // Act: fire all dispatches concurrently
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(15m)).AsTask())
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert: every event was applied — no lost updates
        Assert.Equal(15m, runtime.State.CurrentTemp);
        Assert.Equal(concurrency, runtime.Events.Count);
    }

    [Fact]
    public async Task ConcurrentMixedDispatches_ProduceCorrectFinalState()
    {
        // Arrange: 50 HeaterTurnedOn + 30 HeaterTurnedOff = 80 events total
        const int onCount = 50;
        const int offCount = 30;

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        // Act: interleave on/off events concurrently
        var onTasks = Enumerable.Range(0, onCount)
            .Select(_ => runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn()).AsTask());
        var offTasks = Enumerable.Range(0, offCount)
            .Select(_ => runtime.Dispatch(new ThermostatEvent.HeaterTurnedOff()).AsTask());

        await Task.WhenAll(onTasks.Concat(offTasks));

        // Assert: all events were serialized — no lost updates
        Assert.Equal(onCount + offCount, runtime.Events.Count);
    }

    // =========================================================================
    // Cancellation
    // =========================================================================

    [Fact]
    public async Task Dispatch_ThrowsWhenCancelled()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(25m), cts.Token).AsTask());

        // State should be unchanged
        Assert.Equal(20m, runtime.State.CurrentTemp);
    }

    [Fact]
    public async Task InterpretEffect_ThrowsWhenCancelled()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runtime.InterpretEffect(new ThermostatEffect.None(), cts.Token).AsTask());
    }

    [Fact]
    public async Task Start_ThrowsWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
                .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp, cancellationToken: cts.Token).AsTask());
    }

    [Fact]
    public async Task CancellationDuringFeedbackLoop_StopsProcessing()
    {
        using var cts = new CancellationTokenSource();
        var interpreterCalls = 0;

        // Interpreter: SendNotification -> [AlertRaised] -> SendNotification -> ... infinite loop.
        // Cancel after the 2nd interpreter call.
        Interpreter<ThermostatEffect, ThermostatEvent> cancellingInterpreter = effect =>
        {
            interpreterCalls++;
            if (interpreterCalls >= 2)
            {
                cts.Cancel();
            }
            if (effect is ThermostatEffect.SendNotification)
            {
                return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                    Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.AlertRaised("loop")]));
            }
            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, cancellingInterpreter);

        // AlertRaised -> SendNotification effect -> interpreter returns [AlertRaised] -> loop
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => runtime.Dispatch(new ThermostatEvent.AlertRaised("test"), cts.Token).AsTask());

        // The loop was stopped before depth 64
        Assert.True(interpreterCalls >= 2);
        Assert.True(interpreterCalls < AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>.MaxFeedbackDepth);
    }

    // =========================================================================
    // Feedback Depth Guard
    // =========================================================================

    [Fact]
    public async Task FeedbackLoop_ThrowsAtMaxDepth()
    {
        // Interpreter: SendNotification -> [AlertRaised] -> SendNotification -> ... infinite loop
        // AlertRaised produces SendNotification effect, creating a cycle
        Interpreter<ThermostatEffect, ThermostatEvent> runawayInterpreter = effect =>
        {
            if (effect is ThermostatEffect.SendNotification)
            {
                return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                    Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.AlertRaised("loop")]));
            }
            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, runawayInterpreter);

        // AlertRaised -> SendNotification -> AlertRaised -> ... -> depth exceeded
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => runtime.Dispatch(new ThermostatEvent.AlertRaised("test")).AsTask());

        Assert.Contains("maximum depth", ex.Message);
        Assert.Contains(AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
            .MaxFeedbackDepth.ToString(), ex.Message);
    }

    [Fact]
    public void MaxFeedbackDepth_Is64()
    {
        Assert.Equal(64, AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>.MaxFeedbackDepth);
    }

    // =========================================================================
    // Null Safety
    // =========================================================================

    [Fact]
    public void Constructor_ThrowsOnNullObserver()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
                new ThermostatState(20m, 22m, false, true), null!, ThermostatInterpreters.NoOp));
    }

    [Fact]
    public void Constructor_ThrowsOnNullInterpreter()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
                new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, null!));
    }

    // =========================================================================
    // Unserialized (threadSafe=false)
    // =========================================================================

    [Fact]
    public async Task Dispatch_Unserialized_UpdatesState()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: false);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));

        Assert.Equal(18m, runtime.State.CurrentTemp);
    }

    [Fact]
    public async Task Start_Unserialized_CreatesRuntime()
    {
        var runtime = await AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp, threadSafe: false);

        Assert.Equal(20m, runtime.State.CurrentTemp);
        Assert.True(runtime.State.Active);
    }

    [Fact]
    public async Task InterpretEffect_Unserialized_Works()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: false);

        await runtime.InterpretEffect(new ThermostatEffect.None());

        Assert.Equal(20m, runtime.State.CurrentTemp); // state unchanged
    }

    // =========================================================================
    // Event Tracking Disabled (trackEvents=false)
    // =========================================================================

    [Fact]
    public async Task Dispatch_TrackingDisabled_DoesNotRecordEvents()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            trackEvents: false);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());

        Assert.Equal(18m, runtime.State.CurrentTemp);
        Assert.True(runtime.State.Heating);
        Assert.Empty(runtime.Events); // no events recorded
    }

    [Fact]
    public async Task Dispatch_TrackingEnabled_RecordsEvents()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            trackEvents: true);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));

        Assert.Single(runtime.Events);
    }

    // =========================================================================
    // Lean Mode (threadSafe=false, trackEvents=false)
    // =========================================================================

    [Fact]
    public async Task LeanMode_DispatchesCorrectly()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: false, trackEvents: false);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());
        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(23m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOff());

        Assert.Equal(23m, runtime.State.CurrentTemp);
        Assert.False(runtime.State.Heating);
        Assert.Empty(runtime.Events);
    }

    [Fact]
    public async Task LeanMode_FeedbackLoopWorks()
    {
        var feedbackCount = 0;

        Interpreter<ThermostatEffect, ThermostatEvent> interpreter = effect =>
        {
            if (effect is ThermostatEffect.ActivateHeater && feedbackCount == 0)
            {
                feedbackCount++;
                return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                    Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.TemperatureRecorded(19m)]));
            }
            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, interpreter,
            threadSafe: false, trackEvents: false);

        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());

        Assert.Equal(19m, runtime.State.CurrentTemp);
        Assert.True(runtime.State.Heating);
        Assert.Empty(runtime.Events);
    }

    [Fact]
    public async Task LeanMode_Start_Works()
    {
        var runtime = await AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
                threadSafe: false, trackEvents: false);

        Assert.Equal(20m, runtime.State.CurrentTemp);
        Assert.Empty(runtime.Events);
    }

    // =========================================================================
    // Thread-Safe Reset
    // =========================================================================

    [Fact]
    public void Reset_ThreadSafe_AcquiresGate()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: true);

        runtime.Reset(new ThermostatState(25m, 30m, true, true));

        Assert.Equal(25m, runtime.State.CurrentTemp);
        Assert.Equal(30m, runtime.State.TargetTemp);
    }

    [Fact]
    public void Reset_Unserialized_Works()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: false);

        runtime.Reset(new ThermostatState(25m, 30m, true, true));

        Assert.Equal(25m, runtime.State.CurrentTemp);
    }

    [Fact]
    public async Task Reset_ThreadSafe_WaitsForInFlightDispatch()
    {
        // Verify Reset doesn't corrupt state when a dispatch is in-flight.
        // We use a slow observer to hold the gate, then verify Reset waits.
        var dispatchStarted = new TaskCompletionSource();
        var allowDispatchToFinish = new TaskCompletionSource();

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> slowObserver =
            async (_, _, _) =>
            {
                dispatchStarted.SetResult();
                await allowDispatchToFinish.Task;
                return Result<Unit, PipelineError>.Ok(Unit.Value);
            };

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), slowObserver, ThermostatInterpreters.NoOp,
            threadSafe: true);

        // Start a dispatch that will hold the gate via the slow observer
        var dispatchTask = runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m)).AsTask();
        await dispatchStarted.Task;

        // Reset should block because the gate is held — run it on a background thread
        var resetCompleted = false;
        var resetTask = Task.Run(() =>
        {
            runtime.Reset(new ThermostatState(99m, 99m, false, true));
            resetCompleted = true;
        });

        // Give Reset a moment — it should NOT complete yet
        await Task.Delay(50);
        Assert.False(resetCompleted, "Reset should be blocked while dispatch holds the gate");

        // Release the dispatch
        allowDispatchToFinish.SetResult();
        await dispatchTask;
        await resetTask;

        Assert.True(resetCompleted);
        Assert.Equal(99m, runtime.State.CurrentTemp);
    }

    // =========================================================================
    // IDisposable
    // =========================================================================

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        runtime.Dispose();
        runtime.Dispose(); // Should not throw
    }

    [Fact]
    public async Task Dispose_AfterUse_DoesNotThrow()
    {
        using var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        Assert.Equal(18m, runtime.State.CurrentTemp);
    }

    // =========================================================================
    // Observer Combinators — Where
    // =========================================================================

    [Fact]
    public async Task ObserverWhere_PredicateTrue_InvokesObserver()
    {
        var called = false;

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> inner = (_, _, _) =>
        {
            called = true;
            return PipelineResult.Ok;
        };

        var filtered = inner.Where((_, evt, _) => evt is ThermostatEvent.TemperatureRecorded);

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await filtered(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.True(called);
        Assert.True(result.IsOk);
    }

    [Fact]
    public async Task ObserverWhere_PredicateFalse_SkipsObserver()
    {
        var called = false;

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> inner = (_, _, _) =>
        {
            called = true;
            return PipelineResult.Ok;
        };

        var filtered = inner.Where((_, evt, _) => evt is ThermostatEvent.HeaterTurnedOn);

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await filtered(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.False(called);
        Assert.True(result.IsOk);
    }

    // =========================================================================
    // Observer Combinators — Select
    // =========================================================================

    [Fact]
    public async Task ObserverSelect_ProjectsArguments()
    {
        ThermostatState? receivedState = null;

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> inner = (s, _, _) =>
        {
            receivedState = s;
            return PipelineResult.Ok;
        };

        // Select projects from (string, int, bool) → (ThermostatState, ThermostatEvent, ThermostatEffect)
        var projected = inner.Select<ThermostatState, ThermostatEvent, ThermostatEffect, string, int, bool>(
            (_, _, _) => (
                new ThermostatState(99m, 99m, true, true),
                new ThermostatEvent.HeaterTurnedOn(),
                new ThermostatEffect.None()));

        var result = await projected("hello", 42, true);

        Assert.NotNull(receivedState);
        Assert.Equal(99m, receivedState.CurrentTemp);
        Assert.True(result.IsOk);
    }

    // =========================================================================
    // Observer Combinators — Catch
    // =========================================================================

    [Fact]
    public async Task ObserverCatch_OnSuccess_PassesThrough()
    {
        var catchCalled = false;

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> inner = (_, _, _) =>
            PipelineResult.Ok;

        var caught = inner.Catch(_ =>
        {
            catchCalled = true;
            return Result<Unit, PipelineError>.Ok(Unit.Value);
        });

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await caught(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.False(catchCalled);
        Assert.True(result.IsOk);
    }

    [Fact]
    public async Task ObserverCatch_OnError_InvokesHandler()
    {
        var pipelineError = new PipelineError("test error", "TestSource");

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> inner = (_, _, _) =>
            new ValueTask<Result<Unit, PipelineError>>(
                Result<Unit, PipelineError>.Err(pipelineError));

        PipelineError? capturedError = null;
        var caught = inner.Catch(err =>
        {
            capturedError = err;
            return Result<Unit, PipelineError>.Ok(Unit.Value); // recover
        });

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await caught(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.NotNull(capturedError);
        Assert.Equal("test error", capturedError.Value.Message);
        Assert.True(result.IsOk);
    }

    [Fact]
    public async Task ObserverCatch_HandlerCanReError()
    {
        var originalError = new PipelineError("original");
        var replacementError = new PipelineError("replaced", "Recovery");

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> inner = (_, _, _) =>
            new ValueTask<Result<Unit, PipelineError>>(
                Result<Unit, PipelineError>.Err(originalError));

        var caught = inner.Catch(_ =>
            Result<Unit, PipelineError>.Err(replacementError));

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await caught(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.True(result.IsErr);
        Assert.Equal("replaced", result.Error.Message);
    }

    // =========================================================================
    // Observer Combinators — Combine
    // =========================================================================

    [Fact]
    public async Task ObserverCombine_BothSucceed_ReturnsOk()
    {
        var firstCalled = false;
        var secondCalled = false;

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> first = (_, _, _) =>
        {
            firstCalled = true;
            return PipelineResult.Ok;
        };

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> second = (_, _, _) =>
        {
            secondCalled = true;
            return PipelineResult.Ok;
        };

        var combined = first.Combine(second);

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await combined(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.True(firstCalled);
        Assert.True(secondCalled);
        Assert.True(result.IsOk);
    }

    [Fact]
    public async Task ObserverCombine_FirstFails_SecondStillRuns()
    {
        var secondCalled = false;
        var error = new PipelineError("first failed");

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> first = (_, _, _) =>
            new ValueTask<Result<Unit, PipelineError>>(
                Result<Unit, PipelineError>.Err(error));

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> second = (_, _, _) =>
        {
            secondCalled = true;
            return PipelineResult.Ok;
        };

        var combined = first.Combine(second);

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await combined(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.True(secondCalled, "second observer should still run when first fails");
        Assert.True(result.IsErr);
        Assert.Equal("first failed", result.Error.Message);
    }

    [Fact]
    public async Task ObserverCombine_SecondFails_ReturnsSecondError()
    {
        var error = new PipelineError("second failed");

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> first = (_, _, _) =>
            PipelineResult.Ok;

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> second = (_, _, _) =>
            new ValueTask<Result<Unit, PipelineError>>(
                Result<Unit, PipelineError>.Err(error));

        var combined = first.Combine(second);

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await combined(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.True(result.IsErr);
        Assert.Equal("second failed", result.Error.Message);
    }

    [Fact]
    public async Task ObserverCombine_BothFail_ReturnsFirstError()
    {
        var error1 = new PipelineError("first");
        var error2 = new PipelineError("second");

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> first = (_, _, _) =>
            new ValueTask<Result<Unit, PipelineError>>(
                Result<Unit, PipelineError>.Err(error1));

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> second = (_, _, _) =>
            new ValueTask<Result<Unit, PipelineError>>(
                Result<Unit, PipelineError>.Err(error2));

        var combined = first.Combine(second);

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await combined(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.True(result.IsErr);
        Assert.Equal("first", result.Error.Message);
    }

    // =========================================================================
    // Observer Combinators — Then (error short-circuit)
    // =========================================================================

    [Fact]
    public async Task ObserverThen_FirstFails_SecondNotCalled()
    {
        var secondCalled = false;
        var error = new PipelineError("first failed");

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> first = (_, _, _) =>
            new ValueTask<Result<Unit, PipelineError>>(
                Result<Unit, PipelineError>.Err(error));

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> second = (_, _, _) =>
        {
            secondCalled = true;
            return PipelineResult.Ok;
        };

        var chained = first.Then(second);

        var state = new ThermostatState(20m, 22m, false, true);
        var result = await chained(state, new ThermostatEvent.TemperatureRecorded(25m), new ThermostatEffect.None());

        Assert.False(secondCalled, "second observer should not run when first fails (Then short-circuits)");
        Assert.True(result.IsErr);
        Assert.Equal("first failed", result.Error.Message);
    }

    // =========================================================================
    // Interpreter Combinators — Then
    // =========================================================================

    [Fact]
    public async Task InterpreterThen_ConcatenatesEvents()
    {
        Interpreter<ThermostatEffect, ThermostatEvent> first = _ =>
            new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.TemperatureRecorded(18m)]));

        Interpreter<ThermostatEffect, ThermostatEvent> second = _ =>
            new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.HeaterTurnedOn()]));

        var chained = first.Then(second);

        var result = await chained(new ThermostatEffect.None());

        Assert.True(result.IsOk);
        Assert.Equal(2, result.Value.Length);
        Assert.IsType<ThermostatEvent.TemperatureRecorded>(result.Value[0]);
        Assert.IsType<ThermostatEvent.HeaterTurnedOn>(result.Value[1]);
    }

    [Fact]
    public async Task InterpreterThen_FirstFails_ShortCircuits()
    {
        var secondCalled = false;
        var error = new PipelineError("interpreter failed");

        Interpreter<ThermostatEffect, ThermostatEvent> first = _ =>
            new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Err(error));

        Interpreter<ThermostatEffect, ThermostatEvent> second = _ =>
        {
            secondCalled = true;
            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        var chained = first.Then(second);

        var result = await chained(new ThermostatEffect.None());

        Assert.False(secondCalled);
        Assert.True(result.IsErr);
        Assert.Equal("interpreter failed", result.Error.Message);
    }

    [Fact]
    public async Task InterpreterThen_BothEmpty_ReturnsEmptyArray()
    {
        Interpreter<ThermostatEffect, ThermostatEvent> first = _ =>
            new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));

        Interpreter<ThermostatEffect, ThermostatEvent> second = _ =>
            new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));

        var chained = first.Then(second);

        var result = await chained(new ThermostatEffect.None());

        Assert.True(result.IsOk);
        Assert.Empty(result.Value);
    }

    // =========================================================================
    // Interpreter Combinators — Where
    // =========================================================================

    [Fact]
    public async Task InterpreterWhere_PredicateTrue_InvokesInterpreter()
    {
        var called = false;

        Interpreter<ThermostatEffect, ThermostatEvent> inner = _ =>
        {
            called = true;
            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.HeaterTurnedOn()]));
        };

        var filtered = inner.Where(effect => effect is ThermostatEffect.ActivateHeater);

        var result = await filtered(new ThermostatEffect.ActivateHeater());

        Assert.True(called);
        Assert.True(result.IsOk);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task InterpreterWhere_PredicateFalse_ReturnsEmptyEvents()
    {
        var called = false;

        Interpreter<ThermostatEffect, ThermostatEvent> inner = _ =>
        {
            called = true;
            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.HeaterTurnedOn()]));
        };

        var filtered = inner.Where(effect => effect is ThermostatEffect.ActivateHeater);

        var result = await filtered(new ThermostatEffect.None());

        Assert.False(called);
        Assert.True(result.IsOk);
        Assert.Empty(result.Value);
    }

    // =========================================================================
    // Interpreter Combinators — Select
    // =========================================================================

    [Fact]
    public async Task InterpreterSelect_ProjectsEffect()
    {
        ThermostatEffect? receivedEffect = null;

        Interpreter<ThermostatEffect, ThermostatEvent> inner = effect =>
        {
            receivedEffect = effect;
            return new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        // Select projects from string → ThermostatEffect
        var projected = inner.Select<ThermostatEffect, ThermostatEvent, string>(
            _ => new ThermostatEffect.ActivateHeater());

        var result = await projected("any-string");

        Assert.NotNull(receivedEffect);
        Assert.IsType<ThermostatEffect.ActivateHeater>(receivedEffect);
        Assert.True(result.IsOk);
    }

    // =========================================================================
    // Interpreter Combinators — Catch
    // =========================================================================

    [Fact]
    public async Task InterpreterCatch_OnSuccess_PassesThrough()
    {
        var catchCalled = false;

        Interpreter<ThermostatEffect, ThermostatEvent> inner = _ =>
            new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok(
                    [new ThermostatEvent.HeaterTurnedOn()]));

        var caught = inner.Catch(_ =>
        {
            catchCalled = true;
            return Result<ThermostatEvent[], PipelineError>.Ok([]);
        });

        var result = await caught(new ThermostatEffect.None());

        Assert.False(catchCalled);
        Assert.True(result.IsOk);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task InterpreterCatch_OnError_InvokesHandler()
    {
        var pipelineError = new PipelineError("interpreter error", "TestSource");

        Interpreter<ThermostatEffect, ThermostatEvent> inner = _ =>
            new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Err(pipelineError));

        PipelineError? capturedError = null;
        var caught = inner.Catch(err =>
        {
            capturedError = err;
            return Result<ThermostatEvent[], PipelineError>.Ok(
                [new ThermostatEvent.HeaterTurnedOff()]); // recover with fallback
        });

        var result = await caught(new ThermostatEffect.None());

        Assert.NotNull(capturedError);
        Assert.Equal("interpreter error", capturedError.Value.Message);
        Assert.True(result.IsOk);
        Assert.Single(result.Value);
        Assert.IsType<ThermostatEvent.HeaterTurnedOff>(result.Value[0]);
    }

    [Fact]
    public async Task InterpreterCatch_HandlerCanReError()
    {
        var originalError = new PipelineError("original");
        var replacementError = new PipelineError("replaced", "Recovery");

        Interpreter<ThermostatEffect, ThermostatEvent> inner = _ =>
            new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Err(originalError));

        var caught = inner.Catch(_ =>
            Result<ThermostatEvent[], PipelineError>.Err(replacementError));

        var result = await caught(new ThermostatEffect.None());

        Assert.True(result.IsErr);
        Assert.Equal("replaced", result.Error.Message);
    }
}
