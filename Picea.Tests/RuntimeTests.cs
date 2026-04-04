// =============================================================================
// Shared Runtime Tests
// =============================================================================
// Proves the AutomatonRuntime correctly implements the monadic left fold
// with Observer and Interpreter extension points, using the Thermostat domain.
// =============================================================================

namespace Picea.Tests;

public class RuntimeTests
{
    [Test]
    public async Task Dispatch_UpdatesState()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOff());

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(18m);
        await Assert.That(runtime.State.Heating).IsFalse();
    }

    [Test]
    public async Task Observer_ReceivesCorrectArguments()
    {
        var observed = new List<(ThermostatState State, ThermostatEvent Event, ThermostatEffect Effect)>();

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.Capture(observed), ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(25m));

        await Assert.That(observed).HasSingleItem();
        await Assert.That(observed[0].State.CurrentTemp).IsEqualTo(25m);
        await Assert.That(observed[0].Event).IsTypeOf<ThermostatEvent.TemperatureRecorded>();
        await Assert.That(observed[0].Effect).IsTypeOf<ThermostatEffect.None>();
    }

    [Test]
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
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(19m);
        await Assert.That(runtime.State.Heating).IsTrue();
    }

    [Test]
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

        await Assert.That(firstCalls).IsEqualTo(2);
        await Assert.That(secondCalls).IsEqualTo(2);
    }

    [Test]
    public async Task Reset_ReplacesStateWithoutTransition()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        runtime.Reset(new ThermostatState(25m, 30m, true, true));

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(25m);
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(30m);
        await Assert.That(runtime.State.Heating).IsTrue();
        await Assert.That(runtime.Events).IsEmpty();
    }

    [Test]
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
        await Assert.That(runtime.Events.Count).IsEqualTo(2);
        await Assert.That(runtime.Events[0]).IsTypeOf<ThermostatEvent.HeaterTurnedOn>();
        await Assert.That(runtime.Events[1]).IsTypeOf<ThermostatEvent.TemperatureRecorded>();
    }

    [Test]
    public async Task Events_ReturnSnapshot_NotLiveBackingCollection()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));

        var snapshot = runtime.Events;

        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(snapshot[0]).IsTypeOf<ThermostatEvent.TemperatureRecorded>();
        await Assert.That(runtime.Events.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Start_CreatesRuntimeAndInterpretsInitEffect()
    {
        var runtime = await AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        // Thermostat.Initialize() produces (CurrentTemp=20, TargetTemp=22, Heating=false, Active=true), None
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(20m);
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(22m);
        await Assert.That(runtime.State.Heating).IsFalse();
        await Assert.That(runtime.State.Active).IsTrue();
        await Assert.That(runtime.Events).IsEmpty();
    }

    // =========================================================================
    // Thread Safety
    // =========================================================================

    [Test]
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
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(15m);
        await Assert.That(runtime.Events.Count).IsEqualTo(concurrency);
    }

    [Test]
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
        await Assert.That(runtime.Events.Count).IsEqualTo(onCount + offCount);
    }

    // =========================================================================
    // Cancellation
    // =========================================================================

    [Test]
    public async Task Dispatch_ThrowsWhenCancelled()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(25m), cts.Token).AsTask())
            .Throws<OperationCanceledException>();

        // State should be unchanged
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(20m);
    }

    [Test]
    public async Task InterpretEffect_ThrowsWhenCancelled()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => runtime.InterpretEffect(new ThermostatEffect.None(), cts.Token).AsTask())
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task Start_ThrowsWhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.That(() => AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
                .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp, cancellationToken: cts.Token).AsTask())
            .Throws<OperationCanceledException>();
    }

    [Test]
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
        await Assert.That(() => runtime.Dispatch(new ThermostatEvent.AlertRaised("test"), cts.Token).AsTask())
            .Throws<OperationCanceledException>();

        // The loop was stopped before depth 64
        await Assert.That(interpreterCalls >= 2).IsTrue();
        await Assert.That(interpreterCalls < AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>.MaxFeedbackDepth)
            .IsTrue();
    }

    // =========================================================================
    // Feedback Depth Guard
    // =========================================================================

    [Test]
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
        var ex = await Assert.That(() => runtime.Dispatch(new ThermostatEvent.AlertRaised("test")).AsTask())
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(ex.Message).Contains("maximum depth");
        await Assert.That(ex.Message).Contains(
            AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
                .MaxFeedbackDepth.ToString());
    }

    [Test]
    public async Task MaxFeedbackDepth_Is64()
    {
        await Assert.That(AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>.MaxFeedbackDepth)
            .IsEqualTo(64);
    }

    // =========================================================================
    // Null Safety
    // =========================================================================

    [Test]
    public async Task Constructor_ThrowsOnNullObserver()
    {
        await Assert.That(() =>
            new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
                new ThermostatState(20m, 22m, false, true), null!, ThermostatInterpreters.NoOp))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Constructor_ThrowsOnNullInterpreter()
    {
        await Assert.That(() =>
            new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
                new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Dispatch_ThrowsWhenInterpreterReturnsNullEventArray()
    {
        Interpreter<ThermostatEffect, ThermostatEvent> invalidInterpreter =
            _ => new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok((ThermostatEvent[])null!));

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, invalidInterpreter);

        var ex = await Assert.That(() => runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn()).AsTask())
            .ThrowsExactly<ArgumentNullException>();

        await Assert.That(ex.ParamName).Contains("value");
    }

    // =========================================================================
    // Unserialized (threadSafe=false)
    // =========================================================================

    [Test]
    public async Task Dispatch_Unserialized_UpdatesState()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: false);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(18m);
    }

    [Test]
    public async Task Start_Unserialized_CreatesRuntime()
    {
        var runtime = await AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp, threadSafe: false);

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(20m);
        await Assert.That(runtime.State.Active).IsTrue();
    }

    [Test]
    public async Task InterpretEffect_Unserialized_Works()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: false);

        await runtime.InterpretEffect(new ThermostatEffect.None());

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(20m); // state unchanged
    }

    // =========================================================================
    // Event Tracking Disabled (trackEvents=false)
    // =========================================================================

    [Test]
    public async Task Dispatch_TrackingDisabled_DoesNotRecordEvents()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            trackEvents: false);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(18m);
        await Assert.That(runtime.State.Heating).IsTrue();
        await Assert.That(runtime.Events).IsEmpty(); // no events recorded
    }

    [Test]
    public async Task Dispatch_TrackingEnabled_RecordsEvents()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            trackEvents: true);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));

        await Assert.That(runtime.Events).HasSingleItem();
    }

    // =========================================================================
    // Lean Mode (threadSafe=false, trackEvents=false)
    // =========================================================================

    [Test]
    public async Task LeanMode_DispatchesCorrectly()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: false, trackEvents: false);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());
        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(23m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOff());

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(23m);
        await Assert.That(runtime.State.Heating).IsFalse();
        await Assert.That(runtime.Events).IsEmpty();
    }

    [Test]
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

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(19m);
        await Assert.That(runtime.State.Heating).IsTrue();
        await Assert.That(runtime.Events).IsEmpty();
    }

    [Test]
    public async Task LeanMode_Start_Works()
    {
        var runtime = await AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
                threadSafe: false, trackEvents: false);

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(20m);
        await Assert.That(runtime.Events).IsEmpty();
    }

    // =========================================================================
    // Thread-Safe Reset
    // =========================================================================

    [Test]
    public async Task Reset_ThreadSafe_AcquiresGate()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: true);

        runtime.Reset(new ThermostatState(25m, 30m, true, true));

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(25m);
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(30m);
    }

    [Test]
    public async Task Reset_Unserialized_Works()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
            threadSafe: false);

        runtime.Reset(new ThermostatState(25m, 30m, true, true));

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(25m);
    }

    [Test]
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
        await Assert.That(resetCompleted).IsFalse().Because("Reset should be blocked while dispatch holds the gate");

        // Release the dispatch
        allowDispatchToFinish.SetResult();
        await dispatchTask;
        await resetTask;

        await Assert.That(resetCompleted).IsTrue();
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(99m);
    }

    [Test]
    public async Task State_ReadsDoNotExposeHalfMutatedReferenceState()
    {
        var runtime = new AutomatonRuntime<MutableStateAutomaton, MutableBoxState, MutableBoxEvent, MutableBoxEffect, Unit>(
            new MutableBoxState(0, 0), MutableStateObservers.NoOp, MutableStateInterpreters.NoOp,
            threadSafe: true);

        var dispatchTask = Task.Run(async () =>
        {
            for (var i = 0; i < 2_000; i++)
                await runtime.Dispatch(new MutableBoxEvent.SetPair(i));
        });

        var readTask = Task.Run(async () =>
        {
            for (var i = 0; i < 50_000; i++)
            {
                var snapshot = runtime.State;
                if (snapshot.Left != snapshot.Right)
                    return false;

                await Task.Yield();
            }

            return true;
        });

        await Task.WhenAll(dispatchTask, readTask).WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(readTask.Result).IsTrue();
    }

    // =========================================================================
    // IDisposable
    // =========================================================================

    [Test]
    public Task Dispose_CanBeCalledMultipleTimes()
    {
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        runtime.Dispose();
        runtime.Dispose(); // Should not throw

        return Task.CompletedTask;
    }

    [Test]
    public async Task Dispose_AfterUse_DoesNotThrow()
    {
        using var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(18m);
    }

    private sealed class MutableBoxState
    {
        public int Left;
        public int Right;

        public MutableBoxState(int left, int right)
        {
            Left = left;
            Right = right;
        }
    }

    private abstract record MutableBoxEvent
    {
        public sealed record SetPair(int Value) : MutableBoxEvent;
    }

    private readonly record struct MutableBoxEffect;

    private sealed class MutableStateAutomaton : Automaton<MutableBoxState, MutableBoxEvent, MutableBoxEffect, Unit>
    {
        public static (MutableBoxState State, MutableBoxEffect Effect) Initialize(Unit _) =>
            (new MutableBoxState(0, 0), default);

        public static (MutableBoxState State, MutableBoxEffect Effect) Transition(MutableBoxState state, MutableBoxEvent @event)
        {
            var next = new MutableBoxState(state.Left, state.Right);
            if (@event is MutableBoxEvent.SetPair setPair)
            {
                next.Left = setPair.Value;
                Thread.SpinWait(10_000);
                next.Right = setPair.Value;
            }

            return (next, default);
        }
    }

    private static class MutableStateObservers
    {
        public static readonly Observer<MutableBoxState, MutableBoxEvent, MutableBoxEffect> NoOp =
            (_, _, _) => PipelineResult.Ok;
    }

    private static class MutableStateInterpreters
    {
        public static readonly Interpreter<MutableBoxEffect, MutableBoxEvent> NoOp =
            _ => InterpreterResult<MutableBoxEvent>.Empty;
    }

    // =========================================================================
    // Observer Combinators — Where
    // =========================================================================

    [Test]
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

        await Assert.That(called).IsTrue();
        await Assert.That(result.IsOk).IsTrue();
    }

    [Test]
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

        await Assert.That(called).IsFalse();
        await Assert.That(result.IsOk).IsTrue();
    }

    // =========================================================================
    // Observer Combinators — Select
    // =========================================================================

    [Test]
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

        await Assert.That(receivedState).IsNotNull();
        await Assert.That(receivedState!.CurrentTemp).IsEqualTo(99m);
        await Assert.That(result.IsOk).IsTrue();
    }

    // =========================================================================
    // Observer Combinators — Catch
    // =========================================================================

    [Test]
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

        await Assert.That(catchCalled).IsFalse();
        await Assert.That(result.IsOk).IsTrue();
    }

    [Test]
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

        await Assert.That(capturedError).IsNotNull();
        await Assert.That(capturedError.Value.Message).IsEqualTo("test error");
        await Assert.That(result.IsOk).IsTrue();
    }

    [Test]
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

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error.Message).IsEqualTo("replaced");
    }

    // =========================================================================
    // Observer Combinators — Combine
    // =========================================================================

    [Test]
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

        await Assert.That(firstCalled).IsTrue();
        await Assert.That(secondCalled).IsTrue();
        await Assert.That(result.IsOk).IsTrue();
    }

    [Test]
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

        await Assert.That(secondCalled).IsTrue().Because("second observer should still run when first fails");
        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error.Message).IsEqualTo("first failed");
    }

    [Test]
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

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error.Message).IsEqualTo("second failed");
    }

    [Test]
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

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error.Message).IsEqualTo("first");
    }

    // =========================================================================
    // Observer Combinators — Then (error short-circuit)
    // =========================================================================

    [Test]
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

        await Assert.That(secondCalled).IsFalse().Because("second observer should not run when first fails (Then short-circuits)");
        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error.Message).IsEqualTo("first failed");
    }

    // =========================================================================
    // Interpreter Combinators — Then
    // =========================================================================

    [Test]
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

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Length).IsEqualTo(2);
        await Assert.That(result.Value[0]).IsTypeOf<ThermostatEvent.TemperatureRecorded>();
        await Assert.That(result.Value[1]).IsTypeOf<ThermostatEvent.HeaterTurnedOn>();
    }

    [Test]
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

        await Assert.That(secondCalled).IsFalse();
        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error.Message).IsEqualTo("interpreter failed");
    }

    [Test]
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

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }

    // =========================================================================
    // Interpreter Combinators — Where
    // =========================================================================

    [Test]
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

        await Assert.That(called).IsTrue();
        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).HasSingleItem();
    }

    [Test]
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

        await Assert.That(called).IsFalse();
        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEmpty();
    }

    // =========================================================================
    // Interpreter Combinators — Select
    // =========================================================================

    [Test]
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

        await Assert.That(receivedEffect).IsNotNull();
        await Assert.That(receivedEffect).IsTypeOf<ThermostatEffect.ActivateHeater>();
        await Assert.That(result.IsOk).IsTrue();
    }

    // =========================================================================
    // Interpreter Combinators — Catch
    // =========================================================================

    [Test]
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

        await Assert.That(catchCalled).IsFalse();
        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).HasSingleItem();
    }

    [Test]
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

        await Assert.That(capturedError).IsNotNull();
        await Assert.That(capturedError.Value.Message).IsEqualTo("interpreter error");
        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).HasSingleItem();
        await Assert.That(result.Value[0]).IsTypeOf<ThermostatEvent.HeaterTurnedOff>();
    }

    [Test]
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

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error.Message).IsEqualTo("replaced");
    }
}
