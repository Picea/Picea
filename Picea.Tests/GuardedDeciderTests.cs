using Picea.Commanding;

namespace Picea.Tests;

public class GuardedDeciderTests
{
    [Test]
    public async Task Handle_AuthorizationDenial_DoesNotMutateState()
    {
        var denials = new List<DenialKind>();
        var runtime = await CreateRuntime((kind, _, _, _, _) =>
        {
            denials.Add(kind);
            return ValueTask.CompletedTask;
        });

        var stateBefore = runtime.State;
        var eventCountBefore = runtime.Events.Count;

        var result = await runtime.Handle(
            new ThermostatPrincipal(ThermostatRole.Guest),
            new ThermostatCommand.SetTarget(25m));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ThermostatError.Unauthorized>();
        await Assert.That(runtime.State).IsEqualTo(stateBefore);
        await Assert.That(runtime.Events.Count).IsEqualTo(eventCountBefore);
        await Assert.That(denials.Count).IsEqualTo(1);
        await Assert.That(denials[0]).IsEqualTo(DenialKind.Authorization);
    }

    [Test]
    public async Task Handle_ValidationDenial_DoesNotMutateState()
    {
        var denials = new List<DenialKind>();
        var runtime = await CreateRuntime((kind, _, _, _, _) =>
        {
            denials.Add(kind);
            return ValueTask.CompletedTask;
        });

        var stateBefore = runtime.State;
        var eventCountBefore = runtime.Events.Count;

        var result = await runtime.Handle(
            new ThermostatPrincipal(ThermostatRole.Operator),
            new ThermostatCommand.SetTarget(50m));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ThermostatError.InvalidTarget>();
        await Assert.That(runtime.State).IsEqualTo(stateBefore);
        await Assert.That(runtime.Events.Count).IsEqualTo(eventCountBefore);
        await Assert.That(denials.Count).IsEqualTo(1);
        await Assert.That(denials[0]).IsEqualTo(DenialKind.Validation);
    }

    [Test]
    public async Task Handle_Success_AppliesEvents()
    {
        var runtime = await CreateRuntime();

        var result = await runtime.Handle(
            new ThermostatPrincipal(ThermostatRole.Operator),
            new ThermostatCommand.SetTarget(25m));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(runtime.Events.Count).IsEqualTo(2);
        await Assert.That(runtime.Events[0]).IsTypeOf<ThermostatEvent.TargetSet>();
        await Assert.That(runtime.Events[1]).IsTypeOf<ThermostatEvent.HeaterTurnedOn>();
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(25m);
        await Assert.That(runtime.State.Heating).IsTrue();
    }

    [Test]
    public async Task Handle_ConcurrentMixed_DenialsAndSuccess_CompleteWithoutGateStall()
    {
        var denials = new List<DenialKind>();
        var runtime = await CreateRuntime((kind, _, _, _, _) =>
        {
            denials.Add(kind);
            return ValueTask.CompletedTask;
        });

        var tasks = new[]
        {
            runtime.Handle(
                new ThermostatPrincipal(ThermostatRole.Guest),
                new ThermostatCommand.SetTarget(25m)).AsTask(),
            runtime.Handle(
                new ThermostatPrincipal(ThermostatRole.Operator),
                new ThermostatCommand.SetTarget(50m)).AsTask(),
            runtime.Handle(
                new ThermostatPrincipal(ThermostatRole.Operator),
                new ThermostatCommand.RecordReading(18m)).AsTask()
        };

        var results = await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(5));

        await Assert.That(results.Count(r => r.IsOk)).IsEqualTo(1);
        await Assert.That(results.Count(r => r.IsErr)).IsEqualTo(2);
        await Assert.That(denials.Count).IsEqualTo(2);
        await Assert.That(denials.Contains(DenialKind.Authorization)).IsTrue();
        await Assert.That(denials.Contains(DenialKind.Validation)).IsTrue();

        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(18m);
        await Assert.That(runtime.State.TargetTemp).IsEqualTo(22m);
        await Assert.That(runtime.State.Heating).IsTrue();
        await Assert.That(runtime.Events.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Handle_AuthorizationDenial_ReleasesGate_ForFollowingCommand()
    {
        var runtime = await CreateRuntime();
        var initialState = runtime.State;

        var denied = await runtime.Handle(
            new ThermostatPrincipal(ThermostatRole.Guest),
            new ThermostatCommand.SetTarget(25m));

        await Assert.That(denied.IsErr).IsTrue();
        await Assert.That(runtime.State).IsEqualTo(initialState);
        await Assert.That(runtime.Events).IsEmpty();

        var allowed = await runtime.Handle(
            new ThermostatPrincipal(ThermostatRole.Operator),
            new ThermostatCommand.RecordReading(18m));

        await Assert.That(allowed.IsOk).IsTrue();
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(18m);
        await Assert.That(runtime.State.Heating).IsTrue();
    }

    [Test]
    public async Task Handle_WhenDenialObserverThrows_GateIsReleasedForNextCommand()
    {
        var runtime = await CreateRuntime((_, _, _, _, _) =>
            ValueTask.FromException(new InvalidOperationException("denial observer failure")));

        await Assert.That(() => runtime.Handle(
                new ThermostatPrincipal(ThermostatRole.Guest),
                new ThermostatCommand.SetTarget(25m)).AsTask())
            .ThrowsExactly<InvalidOperationException>();

        var allowed = await runtime.Handle(
            new ThermostatPrincipal(ThermostatRole.Operator),
            new ThermostatCommand.RecordReading(18m));

        await Assert.That(allowed.IsOk).IsTrue();
        await Assert.That(runtime.State.CurrentTemp).IsEqualTo(18m);
    }

    private static async Task<GuardedDecidingRuntime<Thermostat, ThermostatGuardPolicy, ThermostatPrincipal, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>> CreateRuntime(
        DenialObserver<ThermostatPrincipal, ThermostatState, ThermostatCommand, ThermostatError>? denialObserver = null)
    {
        return await GuardedDecidingRuntime<Thermostat, ThermostatGuardPolicy, ThermostatPrincipal, ThermostatState, ThermostatCommand,
            ThermostatEvent, ThermostatEffect, ThermostatError, Unit>.Start(
                default,
                ThermostatObservers.NoOp,
                ThermostatInterpreters.NoOp,
                denialObserver);
    }
}
