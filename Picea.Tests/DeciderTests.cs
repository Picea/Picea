// =============================================================================
// Decider Tests
// =============================================================================
// Tests the Decider pattern: command validation, error rejection, state
// invariants, and Result<TSuccess, TError> algebraic operations.
// Uses the Thermostat domain for all command/error scenarios.
// =============================================================================

namespace Picea.Tests;

public class DeciderTests
{

    // =========================================================================
    // DecidingRuntime — Command Handling
    // =========================================================================

    [Test]
    public async Task Handle_RecordReading_TransitionsState()
    {
        var runtime = await CreateRuntime();

        var result = await runtime.Handle(new ThermostatCommand.RecordReading(18m));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.CurrentTemp).IsEqualTo(18m);
        // RecordReading(18) when target=22, not heating → [TemperatureRecorded(18), HeaterTurnedOn]
        await Assert.That(runtime.State.Heating).IsTrue();
    }

    [Test]
    public async Task Handle_SetTarget_UpdatesTargetTemperature()
    {
        var runtime = await CreateRuntime();

        var result = await runtime.Handle(new ThermostatCommand.SetTarget(25m));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.TargetTemp).IsEqualTo(25m);
    }

    [Test]
    public async Task Handle_RecordReading_SameAsTarget_NoHeaterChange()
    {
        var runtime = await CreateRuntime();

        // Initialize: CurrentTemp=20, TargetTemp=22, Heating=false
        // RecordReading(22) → temp >= target, not heating → just TemperatureRecorded
        var eventsBefore = runtime.Events.Count;

        var result = await runtime.Handle(new ThermostatCommand.RecordReading(22m));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.CurrentTemp).IsEqualTo(22m);
        await Assert.That(result.Value.Heating).IsFalse();
        // Only 1 event: TemperatureRecorded(22)
        await Assert.That(runtime.Events.Count).IsEqualTo(eventsBefore + 1);
    }

    [Test]
    public async Task Handle_SetTarget_AboveMax_ReturnsInvalidTargetError()
    {
        var runtime = await CreateRuntime();

        var result = await runtime.Handle(new ThermostatCommand.SetTarget(Thermostat.MaxTarget + 1));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ThermostatError.InvalidTarget>();
        var invalidTarget = (ThermostatError.InvalidTarget)result.Error;
        await Assert.That(invalidTarget.Target).IsEqualTo(Thermostat.MaxTarget + 1);
        await Assert.That(invalidTarget.Min).IsEqualTo(Thermostat.MinTarget);
        await Assert.That(invalidTarget.Max).IsEqualTo(Thermostat.MaxTarget);
        // State unchanged
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(22m);
    }

    [Test]
    public async Task Handle_SetTarget_BelowMin_ReturnsInvalidTargetError()
    {
        var runtime = await CreateRuntime();

        var result = await runtime.Handle(new ThermostatCommand.SetTarget(Thermostat.MinTarget - 1));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ThermostatError.InvalidTarget>();
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(22m);
    }

    [Test]
    public async Task Handle_BoundaryValues_AcceptsExactMax()
    {
        var runtime = await CreateRuntime();

        var result = await runtime.Handle(new ThermostatCommand.SetTarget(Thermostat.MaxTarget));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.TargetTemp).IsEqualTo(Thermostat.MaxTarget);
    }

    [Test]
    public async Task Handle_BoundaryValues_AcceptsExactMin()
    {
        var runtime = await CreateRuntime();

        var result = await runtime.Handle(new ThermostatCommand.SetTarget(Thermostat.MinTarget));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.TargetTemp).IsEqualTo(Thermostat.MinTarget);
    }

    [Test]
    public async Task Handle_BoundaryValues_RejectsAboveMax()
    {
        var runtime = await CreateRuntime();

        await runtime.Handle(new ThermostatCommand.SetTarget(Thermostat.MaxTarget));
        var result = await runtime.Handle(new ThermostatCommand.SetTarget(Thermostat.MaxTarget + 0.1m));

        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    public async Task Handle_Shutdown_SetsTerminalState()
    {
        var runtime = await CreateRuntime();

        var result = await runtime.Handle(new ThermostatCommand.Shutdown());

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Active).IsFalse();
        await Assert.That(runtime.IsTerminal).IsTrue();
    }

    [Test]
    public async Task Handle_ShutdownWhenAlreadyShutDown_ReturnsAlreadyShutdownError()
    {
        var runtime = await CreateRuntime();

        await runtime.Handle(new ThermostatCommand.Shutdown());
        var result = await runtime.Handle(new ThermostatCommand.Shutdown());

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ThermostatError.AlreadyShutdown>();
    }

    [Test]
    public async Task Handle_CommandAfterShutdown_ReturnsSystemInactiveError()
    {
        var runtime = await CreateRuntime();

        await runtime.Handle(new ThermostatCommand.Shutdown());
        var result = await runtime.Handle(new ThermostatCommand.RecordReading(25m));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ThermostatError.SystemInactive>();
    }

    [Test]
    public async Task Handle_ErrorDoesNotMutateState()
    {
        var runtime = await CreateRuntime();

        await runtime.Handle(new ThermostatCommand.RecordReading(18m));
        var stateBeforeError = runtime.State;
        var eventCountBeforeError = runtime.Events.Count;

        // Invalid command — target out of range
        await runtime.Handle(new ThermostatCommand.SetTarget(50m));

        await Assert.That(runtime.State).IsEqualTo(stateBeforeError);
        await Assert.That(runtime.Events.Count).IsEqualTo(eventCountBeforeError);
    }

    [Test]
    public async Task Handle_ObserverSeesAllTransitions()
    {
        var observed = new List<(ThermostatState State, ThermostatEvent Event, ThermostatEffect Effect)>();

        var runtime = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>.Start(default, ThermostatObservers.Capture(observed), ThermostatInterpreters.NoOp);

        // RecordReading(18) when target=22, not heating → [TemperatureRecorded(18), HeaterTurnedOn]
        await runtime.Handle(new ThermostatCommand.RecordReading(18m));

        await Assert.That(observed.Count).IsEqualTo(2);
        await Assert.That(observed[0].State.CurrentTemp).IsEqualTo(18m);
        await Assert.That(observed[0].State.Heating).IsFalse(); // After TemperatureRecorded, before HeaterTurnedOn
        await Assert.That(observed[1].State.Heating).IsTrue();  // After HeaterTurnedOn
    }

    [Test]
    public async Task Handle_ObserverNotCalledOnError()
    {
        var observerCallCount = 0;
        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> observer = (_, _, _) =>
        {
            observerCallCount++;
            return PipelineResult.Ok;
        };

        var runtime = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>.Start(default, observer, ThermostatInterpreters.NoOp);

        // Invalid target — should not trigger observer
        await runtime.Handle(new ThermostatCommand.SetTarget(50m));

        await Assert.That(observerCallCount).IsEqualTo(0);
    }

    [Test]
    public async Task Handle_PipelineFailure_ReleasesGateExactlyOnce()
    {
        var blockObserver = false;
        var blockStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowBlockToFinish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var blockSignalSent = false;

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> observer = (_, _, _) =>
        {
            if (blockObserver)
            {
                if (!blockSignalSent)
                {
                    blockSignalSent = true;
                    blockStarted.SetResult();
                }
                return WaitForRelease();
            }

            return new ValueTask<Result<Unit, PipelineError>>(
                Result<Unit, PipelineError>.Err(new PipelineError("boom", "test")));
        };

        async ValueTask<Result<Unit, PipelineError>> WaitForRelease()
        {
            await allowBlockToFinish.Task;
            return Result<Unit, PipelineError>.Ok(Unit.Value);
        }

        var runtime = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>.Start(default, observer, ThermostatInterpreters.NoOp);

        var failure = await Assert.That(() => runtime.Handle(new ThermostatCommand.RecordReading(18m)).AsTask())
            .ThrowsExactly<InvalidOperationException>();

        await Assert.That(failure.Message).Contains("Pipeline error during dispatch");

        blockObserver = true;
        var blockedHandle = runtime.Handle(new ThermostatCommand.RecordReading(18m)).AsTask();
        await blockStarted.Task;

        var thirdHandleCompleted = false;
        var thirdHandle = Task.Run(async () =>
        {
            await runtime.Handle(new ThermostatCommand.RecordReading(18m));
            thirdHandleCompleted = true;
        });

        await Task.Delay(50);
        await Assert.That(thirdHandleCompleted).IsFalse().Because("the gate should still allow only one in-flight handle after a failure");

        allowBlockToFinish.SetResult();
        await blockedHandle;
        await thirdHandle;

        await Assert.That(thirdHandleCompleted).IsTrue();
    }

    [Test]
    public async Task IsTerminal_InitiallyFalse_TrueAfterShutdown()
    {
        var runtime = await CreateRuntime();

        await Assert.That(runtime.IsTerminal).IsFalse();

        await runtime.Handle(new ThermostatCommand.RecordReading(18m));
        await Assert.That(runtime.IsTerminal).IsFalse();

        await runtime.Handle(new ThermostatCommand.Shutdown());
        await Assert.That(runtime.IsTerminal).IsTrue();
    }

    // =========================================================================
    // Decide — Pure Function Tests (no runtime needed)
    // =========================================================================

    [Test]
    public async Task Decide_IsPure_SameInputProducesSameOutput()
    {
        var state = new ThermostatState(20m, 22m, false, true);
        var command = (ThermostatCommand)new ThermostatCommand.RecordReading(18m);

        var result1 = Thermostat.Decide(state, command);
        var result2 = Thermostat.Decide(state, command);

        await Assert.That(result1.IsOk).IsTrue();
        await Assert.That(result2.IsOk).IsTrue();
        await Assert.That(result2.Value.SequenceEqual(result1.Value)).IsTrue();
    }

    [Test]
    public async Task Decide_RecordReading_Cold_ReturnsTemperatureRecordedAndHeaterTurnedOn()
    {
        var state = new ThermostatState(20m, 22m, false, true);

        var result = Thermostat.Decide(state, new ThermostatCommand.RecordReading(18m));

        await Assert.That(result.IsOk).IsTrue();
        var events = result.Value.ToList();
        await Assert.That(events.Count).IsEqualTo(2);
        await Assert.That(events[0]).IsTypeOf<ThermostatEvent.TemperatureRecorded>();
        await Assert.That(events[1]).IsTypeOf<ThermostatEvent.HeaterTurnedOn>();
    }

    [Test]
    public async Task Decide_RecordReading_Hot_ReturnsAlertRaised()
    {
        var state = new ThermostatState(20m, 22m, false, true);

        var result = Thermostat.Decide(state, new ThermostatCommand.RecordReading(36m));

        await Assert.That(result.IsOk).IsTrue();
        var events = result.Value.ToList();
        await Assert.That(events.Count).IsEqualTo(2);
        await Assert.That(events[0]).IsTypeOf<ThermostatEvent.TemperatureRecorded>();
        await Assert.That(events[1]).IsTypeOf<ThermostatEvent.AlertRaised>();
    }

    // =========================================================================
    // Result<TSuccess, TError> — Algebraic Operations
    // =========================================================================

    [Test]
    public async Task Result_Ok_IsOk()
    {
        var result = Result<int, string>.Ok(42);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.IsErr).IsFalse();
    }

    [Test]
    public async Task Result_Err_IsErr()
    {
        var result = Result<int, string>.Err("oops");

        await Assert.That(result.IsOk).IsFalse();
        await Assert.That(result.IsErr).IsTrue();
    }

    [Test]
    public async Task Result_PatternMatch_DispatchesCorrectly()
    {
        var ok = Result<int, string>.Ok(42);
        var err = Result<int, string>.Err("fail");

        var okMessage = ok.IsOk ? ok.Value.ToString() : ok.Error;
        var errMessage = err.IsOk ? err.Value.ToString() : err.Error;

        await Assert.That(okMessage).IsEqualTo("42");
        await Assert.That(errMessage).IsEqualTo("fail");
    }

    [Test]
    public async Task Result_Map_TransformsSuccess()
    {
        var ok = Result<int, string>.Ok(21);

        var mapped = ok.Map(v => v * 2);

        await Assert.That(mapped.IsOk).IsTrue();
        await Assert.That(mapped.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Result_Map_PreservesError()
    {
        var err = Result<int, string>.Err("fail");

        var mapped = err.Map(v => v * 2);

        await Assert.That(mapped.IsErr).IsTrue();
        await Assert.That(mapped.Error).IsEqualTo("fail");
    }

    [Test]
    public async Task Result_Bind_ChainsSuccess()
    {
        var ok = Result<int, string>.Ok(21);

        var bound = ok.Bind(v => Result<string, string>.Ok($"value: {v * 2}"));

        await Assert.That(bound.IsOk).IsTrue();
        await Assert.That(bound.Value).IsEqualTo("value: 42");
    }

    [Test]
    public async Task Result_Bind_ShortCircuitsOnError()
    {
        var err = Result<int, string>.Err("fail");

        var bound = err.Bind(v => Result<string, string>.Ok($"value: {v}"));

        await Assert.That(bound.IsErr).IsTrue();
        await Assert.That(bound.Error).IsEqualTo("fail");
    }

    [Test]
    public async Task Result_MapError_TransformsError()
    {
        var err = Result<int, string>.Err("fail");

        var mapped = err.MapError(e => e.Length);

        await Assert.That(mapped.IsErr).IsTrue();
        await Assert.That(mapped.Error).IsEqualTo(4);
    }

    [Test]
    public async Task Result_MapError_PreservesSuccess()
    {
        var ok = Result<int, string>.Ok(42);

        var mapped = ok.MapError(e => e.Length);

        await Assert.That(mapped.IsOk).IsTrue();
        await Assert.That(mapped.Value).IsEqualTo(42);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private static async Task<DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>> CreateRuntime()
    {
        return await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>.Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);
    }

    // =========================================================================
    // Concurrent Handle
    // =========================================================================

    [Test]
    public async Task ConcurrentHandles_AreSerializedAndProduceCorrectFinalState()
    {
        var runtime = await CreateRuntime();

        // Fire 50 concurrent RecordReading commands
        var tasks = Enumerable.Range(0, 50)
            .Select(i => runtime.Handle(new ThermostatCommand.RecordReading(15m)).AsTask())
            .ToArray();

        await Task.WhenAll(tasks);

        // All should succeed (no state corruption)
        foreach (var t in tasks)
            await Assert.That(t.Result.IsOk).IsTrue();
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(15m);
    }

    [Test]
    public async Task ConcurrentHandles_MixedValidAndInvalid_StateConsistent()
    {
        var runtime = await CreateRuntime();

        // Mix valid and invalid commands concurrently
        var tasks = new List<Task<Result<ThermostatState, ThermostatError>>>();
        for (var i = 0; i < 20; i++)
        {
            // Even: valid command, Odd: invalid command
            tasks.Add(i % 2 == 0
                ? runtime.Handle(new ThermostatCommand.RecordReading(18m)).AsTask()
                : runtime.Handle(new ThermostatCommand.SetTarget(50m)).AsTask());
        }

        var results = await Task.WhenAll(tasks);

        // All valid commands should succeed, all invalid should fail
        for (var i = 0; i < 20; i++)
        {
            if (i % 2 == 0)
                await Assert.That(results[i].IsOk).IsTrue();
            else
                await Assert.That(results[i].IsErr).IsTrue();
        }

        // State should reflect the valid commands
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(18m);
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(22m); // Unchanged — invalid commands rejected
    }

    // =========================================================================
    // Unserialized Handle (threadSafe=false)
    // =========================================================================

    [Test]
    public async Task Handle_Unserialized_AcceptsValidCommand()
    {
        var runtime = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp, threadSafe: false);

        var result = await runtime.Handle(new ThermostatCommand.RecordReading(18m));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.CurrentTemp).IsEqualTo(18m);
    }

    [Test]
    public async Task Handle_Unserialized_RejectsInvalidCommand()
    {
        var runtime = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp, threadSafe: false);

        var result = await runtime.Handle(new ThermostatCommand.SetTarget(50m));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(22m);
    }

    // =========================================================================
    // Lean Mode (threadSafe=false, trackEvents=false)
    // =========================================================================

    [Test]
    public async Task Handle_LeanMode_WorksCorrectly()
    {
        var runtime = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp,
                threadSafe: false, trackEvents: false);

        var result = await runtime.Handle(new ThermostatCommand.RecordReading(18m));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.CurrentTemp).IsEqualTo(18m);
        await Assert.That(runtime.State.Heating).IsTrue();
        await Assert.That(runtime.Events).IsEmpty(); // No events tracked
    }

    // =========================================================================
    // IDisposable
    // =========================================================================

    [Test]
    public async Task DecidingRuntime_Dispose_Works()
    {
        var runtime = await CreateRuntime();

        await runtime.Handle(new ThermostatCommand.RecordReading(18m));

        runtime.Dispose();
        runtime.Dispose(); // Should not throw on double dispose
    }

    // =========================================================================
    // Result Edge Cases
    // =========================================================================

    [Test]
    public async Task Result_Value_ThrowsOnErr()
    {
        var result = Result<int, string>.Err("fail");

        var ex = await Assert.That(() => result.Value).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex.Message).Contains("Err");
    }

    [Test]
    public async Task Result_Error_ThrowsOnOk()
    {
        var result = Result<int, string>.Ok(42);

        var ex = await Assert.That(() => result.Error).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex.Message).Contains("Ok");
    }

    [Test]
    public async Task Result_ToString_Ok()
    {
        var result = Result<int, string>.Ok(42);

        await Assert.That(result.ToString()).IsEqualTo("Ok(42)");
    }

    [Test]
    public async Task Result_ToString_Err()
    {
        var result = Result<int, string>.Err("fail");

        await Assert.That(result.ToString()).IsEqualTo("Err(fail)");
    }

    // =========================================================================
    // LINQ Query Syntax (Monad Comprehension)
    // =========================================================================

    [Test]
    public async Task Result_Select_TransformsSuccess()
    {
        var ok = Result<int, string>.Ok(21);

        var result = from v in ok select v * 2;

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Result_Select_PreservesError()
    {
        var err = Result<int, string>.Err("fail");

        var result = from v in err select v * 2;

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("fail");
    }

    [Test]
    public async Task Result_SelectMany_ChainsSuccess()
    {
        var a = Result<int, string>.Ok(20);

        var result =
            from x in a
            from y in Result<int, string>.Ok(x + 1)
            select x + y;

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(41);
    }

    [Test]
    public async Task Result_SelectMany_ShortCircuitsOnFirstError()
    {
        var err = Result<int, string>.Err("first failed");

        var secondCalled = false;
        var result =
            from x in err
            from y in Invoke(() => { secondCalled = true; return Result<int, string>.Ok(99); })
            select x + y;

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("first failed");
        await Assert.That(secondCalled).IsFalse();
    }

    [Test]
    public async Task Result_SelectMany_ShortCircuitsOnSecondError()
    {
        var ok = Result<int, string>.Ok(42);

        var result =
            from x in ok
            from y in Result<int, string>.Err("second failed")
            select x + y;

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("second failed");
    }

    [Test]
    public async Task Result_SelectMany_ThreeFromClauses()
    {
        var result =
            from a in Result<int, string>.Ok(1)
            from b in Result<int, string>.Ok(2)
            from c in Result<int, string>.Ok(3)
            select a + b + c;

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(6);
    }

    /// <summary>
    /// Helper to track whether a function is called during LINQ short-circuiting.
    /// </summary>
    private static T Invoke<T>(Func<T> f) => f();
}
