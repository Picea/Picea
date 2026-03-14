// =============================================================================
// Tracing Tests
// =============================================================================
// Verifies that the Automaton runtime emits OpenTelemetry-compatible
// Activity spans via System.Diagnostics.ActivitySource.
// Uses the Thermostat domain throughout.
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Picea.Tests;

public class TracingTests
{
    /// <summary>
    /// Collects activities emitted by the Automaton ActivitySource during a test.
    /// Uses <see cref="ConcurrentBag{T}"/> because <see cref="ActivityListener.ActivityStopped"/>
    /// may fire from any thread when TUnit runs tests in parallel.
    /// </summary>
    private sealed class ActivityCollector : IDisposable
    {
        private readonly ActivityListener _listener;
        public ConcurrentBag<Activity> Activities { get; } = [];

        public ActivityCollector()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == AutomatonDiagnostics.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => Activities.Add(activity)
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose() => _listener.Dispose();
    }

    // =========================================================================
    // Runtime Tracing
    // =========================================================================

    [Test]
    public async Task Dispatch_EmitsTracingSpan()
    {
        using var collector = new ActivityCollector();

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));

        var dispatch = collector.Activities.FirstOrDefault(a => a.DisplayName == "Automaton.Dispatch");
        await Assert.That(dispatch).IsNotNull();
        await Assert.That(dispatch.GetTagItem("automaton.type")).IsEqualTo("Thermostat");
        await Assert.That(dispatch.GetTagItem("automaton.event.type")).IsEqualTo("TemperatureRecorded");
        await Assert.That(dispatch.Status).IsEqualTo(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task Start_EmitsTracingSpan()
    {
        using var collector = new ActivityCollector();

        _ = await AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>
            .Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        var start = collector.Activities.FirstOrDefault(a => a.DisplayName == "Automaton.Start");
        await Assert.That(start).IsNotNull();
        await Assert.That(start.GetTagItem("automaton.type")).IsEqualTo("Thermostat");
        await Assert.That(start.GetTagItem("automaton.state.type")).IsEqualTo("ThermostatState");
        await Assert.That(start.Status).IsEqualTo(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task InterpretEffect_EmitsTracingSpan()
    {
        using var collector = new ActivityCollector();

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.InterpretEffect(new ThermostatEffect.None());

        var interpret = collector.Activities.FirstOrDefault(a => a.DisplayName == "Automaton.InterpretEffect");
        await Assert.That(interpret).IsNotNull();
        await Assert.That(interpret.GetTagItem("automaton.type")).IsEqualTo("Thermostat");
        await Assert.That(interpret.GetTagItem("automaton.effect.type")).IsEqualTo("None");
        await Assert.That(interpret.Status).IsEqualTo(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task Dispatch_SetsErrorStatusOnFailure()
    {
        using var collector = new ActivityCollector();

        Interpreter<ThermostatEffect, ThermostatEvent> throwingInterpreter =
            _ => throw new InvalidOperationException("test fault");

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, throwingInterpreter);

        // HeaterTurnedOn produces ActivateHeater effect -> interpreter throws
        await Assert.That(() => runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn()).AsTask()).ThrowsExactly<InvalidOperationException>();

        var dispatch = collector.Activities.FirstOrDefault(a =>
            a.DisplayName == "Automaton.Dispatch"
            && a.Status == ActivityStatusCode.Error);
        await Assert.That(dispatch).IsNotNull();
        await Assert.That(dispatch.StatusDescription).Contains("test fault");
    }

    [Test]
    public async Task MultipleDispatches_EmitMultipleSpans()
    {
        using var collector = new ActivityCollector();

        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(18m));
        await runtime.Dispatch(new ThermostatEvent.HeaterTurnedOn());
        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(23m));

        // Filter by specific event types to avoid cross-test interference
        // (ActivityListener is process-global; parallel tests may add spans).
        var dispatches = collector.Activities
            .Where(a => a.DisplayName == "Automaton.Dispatch")
            .ToList();
        await Assert.That(dispatches.Count >= 3).IsTrue().Because($"Expected at least 3 Dispatch spans, got {dispatches.Count}");

        // Verify we got the specific event types we dispatched
        var eventTypes = dispatches.Select(a => a.GetTagItem("automaton.event.type")).ToList();
        await Assert.That(eventTypes).Contains("TemperatureRecorded");
        await Assert.That(eventTypes).Contains("HeaterTurnedOn");
    }

    // =========================================================================
    // Decider Tracing
    // =========================================================================

    [Test]
    public async Task DeciderHandle_EmitsTracingSpan_OnSuccess()
    {
        using var collector = new ActivityCollector();

        var runtime = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>.Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Handle(new ThermostatCommand.RecordReading(18m));

        var handle = collector.Activities.FirstOrDefault(a =>
            a.DisplayName == "Automaton.Decider.Handle"
            && Equals(a.GetTagItem("automaton.command.type"), "RecordReading"));
        await Assert.That(handle).IsNotNull();
        await Assert.That(handle.GetTagItem("automaton.type")).IsEqualTo("Thermostat");
        await Assert.That(handle.GetTagItem("automaton.command.type")).IsEqualTo("RecordReading");
        await Assert.That(handle.GetTagItem("automaton.result")).IsEqualTo("ok");
        await Assert.That(handle.Status).IsEqualTo(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task DeciderHandle_EmitsTracingSpan_OnRejection()
    {
        using var collector = new ActivityCollector();

        var runtime = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>.Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Handle(new ThermostatCommand.SetTarget(50m)); // exceeds MaxTarget

        var handle = collector.Activities.FirstOrDefault(a =>
            a.DisplayName == "Automaton.Decider.Handle"
            && Equals(a.GetTagItem("automaton.command.type"), "SetTarget"));
        await Assert.That(handle).IsNotNull();
        await Assert.That(handle.GetTagItem("automaton.result")).IsEqualTo("error");
        await Assert.That(handle.GetTagItem("automaton.error.type")).IsEqualTo("InvalidTarget");
        // Command rejection is NOT a fault — status should be Ok
        await Assert.That(handle.Status).IsEqualTo(ActivityStatusCode.Ok);
    }

    [Test]
    public async Task DeciderStart_EmitsTracingSpan()
    {
        using var collector = new ActivityCollector();

        _ = await DecidingRuntime<Thermostat, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>.Start(default, ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        var start = collector.Activities.FirstOrDefault(a => a.DisplayName == "Automaton.Decider.Start");
        await Assert.That(start).IsNotNull();
        await Assert.That(start.GetTagItem("automaton.type")).IsEqualTo("Thermostat");
        await Assert.That(start.Status).IsEqualTo(ActivityStatusCode.Ok);
    }

    // =========================================================================
    // No-listener fast path
    // =========================================================================

    [Test]
    public async Task Dispatch_WorksWithNoListener()
    {
        // No ActivityCollector — no listener registered.
        // Verify that the runtime still works correctly (StartActivity returns null).
        var runtime = new AutomatonRuntime<Thermostat, ThermostatState, ThermostatEvent, ThermostatEffect, Unit>(
            new ThermostatState(20m, 22m, false, true), ThermostatObservers.NoOp, ThermostatInterpreters.NoOp);

        await runtime.Dispatch(new ThermostatEvent.TemperatureRecorded(25m));

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(25m);
    }
}
