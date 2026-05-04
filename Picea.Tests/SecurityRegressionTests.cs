using Picea.Commanding;

namespace Picea.Tests;

/// <summary>
/// Security regression coverage mapped to squad security requirements:
/// - Authorization/validation denials must not mutate runtime state or dispatch side effects.
/// - Hash-chain verification must detect tampering and anchor mismatches.
/// - Hash-chain configuration must reject invalid hashing capabilities.
/// </summary>
public sealed class SecurityRegressionTests
{
    [Test]
    public async Task GuardedRuntime_AuthorizationDenial_DoesNotDispatchObserverOrInterpreter()
    {
        var observerCalls = 0;
        var interpreterCalls = 0;
        var denialKinds = new List<DenialKind>();

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> observer = (_, _, _) =>
        {
            observerCalls++;
            return PipelineResult.Ok;
        };

        Interpreter<ThermostatEffect, ThermostatEvent> interpreter = _ =>
        {
            interpreterCalls++;
            return ValueTask.FromResult(Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        DenialObserver<ThermostatPrincipal, ThermostatState, ThermostatCommand, ThermostatError> denialObserver =
            (kind, _, _, _, _) =>
            {
                denialKinds.Add(kind);
                return ValueTask.CompletedTask;
            };

        var runtime = await GuardedDecidingRuntime<GuardedThermostat, ThermostatAuthorizationPolicy, ThermostatValidationPolicy, ThermostatPrincipal, ThermostatState,
            ThermostatCommand, ThermostatEvent, ThermostatEffect, ThermostatError, Unit>
            .Start(default, observer, interpreter, denialObserver);
        var observerCallsBefore = observerCalls;
        var interpreterCallsBefore = interpreterCalls;

        var stateBefore = runtime.State;
        var result = await runtime.Handle(
            new ThermostatPrincipal(ThermostatRole.Guest),
            new ThermostatCommand.SetTarget(25m));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ThermostatError.Unauthorized>();
        await Assert.That(runtime.State).IsEqualTo(stateBefore);
        await Assert.That(runtime.Events).IsEmpty();
        await Assert.That(observerCalls).IsEqualTo(observerCallsBefore);
        await Assert.That(interpreterCalls).IsEqualTo(interpreterCallsBefore);
        await Assert.That(denialKinds).HasSingleItem();
        await Assert.That(denialKinds[0]).IsEqualTo(DenialKind.Authorization);
    }

    [Test]
    public async Task GuardedRuntime_ValidationDenial_DoesNotDispatchObserverOrInterpreter()
    {
        var observerCalls = 0;
        var interpreterCalls = 0;
        var denialKinds = new List<DenialKind>();

        Observer<ThermostatState, ThermostatEvent, ThermostatEffect> observer = (_, _, _) =>
        {
            observerCalls++;
            return PipelineResult.Ok;
        };

        Interpreter<ThermostatEffect, ThermostatEvent> interpreter = _ =>
        {
            interpreterCalls++;
            return ValueTask.FromResult(Result<ThermostatEvent[], PipelineError>.Ok([]));
        };

        DenialObserver<ThermostatPrincipal, ThermostatState, ThermostatCommand, ThermostatError> denialObserver =
            (kind, _, _, _, _) =>
            {
                denialKinds.Add(kind);
                return ValueTask.CompletedTask;
            };

        var runtime = await GuardedDecidingRuntime<GuardedThermostat, ThermostatAuthorizationPolicy, ThermostatValidationPolicy, ThermostatPrincipal, ThermostatState,
            ThermostatCommand, ThermostatEvent, ThermostatEffect, ThermostatError, Unit>
            .Start(default, observer, interpreter, denialObserver);
        var observerCallsBefore = observerCalls;
        var interpreterCallsBefore = interpreterCalls;

        var stateBefore = runtime.State;
        var result = await runtime.Handle(
            new ThermostatPrincipal(ThermostatRole.Operator),
            new ThermostatCommand.SetTarget(Thermostat.MaxTarget + 1m));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsTypeOf<ThermostatError.InvalidTarget>();
        await Assert.That(runtime.State).IsEqualTo(stateBefore);
        await Assert.That(runtime.Events).IsEmpty();
        await Assert.That(observerCalls).IsEqualTo(observerCallsBefore);
        await Assert.That(interpreterCalls).IsEqualTo(interpreterCallsBefore);
        await Assert.That(denialKinds).HasSingleItem();
        await Assert.That(denialKinds[0]).IsEqualTo(DenialKind.Validation);
    }

    [Test]
    public async Task HashChain_TamperedPayload_FailsFullChainAndAnchorVerification()
    {
        const string anchor = "trusted-anchor-v1";
        var serializer = new JsonEventSerializer();
        var source = CreateAuditLog(anchor, serializer);
        var tamperedEntries = source.Entries.ToArray();
        tamperedEntries[1] = tamperedEntries[1] with { Event = new AuditEvent(999) };

        var loaded = await HashChainEventLog<AuditEvent>.LoadAsync(
            Storage(tamperedEntries),
            serializer,
            HashChainOptions.Sha256(anchor));

        await Assert.That(loaded.VerifyChain()).IsFalse();
        await Assert.That(loaded.VerifyAnchor(anchor)).IsFalse();
    }

    [Test]
    public async Task HashChain_MismatchedTrustedAnchor_IsRejectedEvenWhenInternalChainIsConsistent()
    {
        const string trustedAnchor = "anchor-A";
        const string wrongAnchor = "anchor-B";
        var serializer = new JsonEventSerializer();
        var log = CreateAuditLog(trustedAnchor, serializer);

        await Assert.That(log.VerifyChain()).IsTrue();
        await Assert.That(log.VerifyAnchor(trustedAnchor)).IsTrue();
        await Assert.That(log.VerifyAnchor(wrongAnchor)).IsFalse();
    }

    [Test]
    public async Task HashChain_InvalidHashingCapabilities_AreRejectedAtConstruction()
    {
        var nullHasher = await Assert.That(() => new HashChainEventLog<AuditEvent>(
                hashing: new HashChainOptions(
                    null!,
                    static bytes => Convert.ToHexString(bytes),
                    "anchor")))
            .ThrowsExactly<ArgumentException>();

        await Assert.That(nullHasher.ParamName!).IsEqualTo("hashing");

        var nullEncoder = await Assert.That(() => new HashChainEventLog<AuditEvent>(
                hashing: new HashChainOptions(
                    static bytes => bytes,
                    null!,
                    "anchor")))
            .ThrowsExactly<ArgumentException>();

        await Assert.That(nullEncoder.ParamName!).IsEqualTo("hashing");
    }

    private static HashChainEventLog<AuditEvent> CreateAuditLog(string anchorHash, EventSerializer serializer)
    {
        var (_, log) = HashChainEventLog<AuditEvent>.Create<Unit, Unit>(
            serializer,
            HashChainOptions.Sha256(anchorHash),
            timestampFactory: static () => new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));

        log.Append(new AuditEvent(1), new DateTimeOffset(2026, 4, 20, 12, 0, 0, TimeSpan.Zero));
        log.Append(new AuditEvent(2), new DateTimeOffset(2026, 4, 20, 12, 1, 0, TimeSpan.Zero));
        log.Append(new AuditEvent(3), new DateTimeOffset(2026, 4, 20, 12, 2, 0, TimeSpan.Zero));

        return log;
    }

    private static HashChainLogStorage<AuditEvent> Storage(IReadOnlyList<HashChainLogEntry<AuditEvent>> entries) =>
        new(
            SaveEntries: static (_, _) => ValueTask.CompletedTask,
            LoadEntries: _ => ToAsync(entries));

    private static async IAsyncEnumerable<HashChainLogEntry<AuditEvent>> ToAsync(IReadOnlyList<HashChainLogEntry<AuditEvent>> entries)
    {
        await ValueTask.CompletedTask;

        for (var i = 0; i < entries.Count; i++)
            yield return entries[i];
    }

    private readonly record struct AuditEvent(int Value);

}