// =============================================================================
// Picea Benchmarks — Hot path performance measurements
// =============================================================================
// Measures the core runtime overhead:
//   • Dispatch (single, batch, with observer, with interpreter feedback)
//   • DecidingRuntime.Handle (accept / reject)
//   • Observer composition via Then combinator
//
// Uses a deliberately trivial domain (BenchDomain) to isolate framework cost.
// =============================================================================

using BenchmarkDotNet.Attributes;

namespace Picea.Benchmarks;

[MemoryDiagnoser]
[JsonExporterAttribute.FullCompressed]
public class PiceaBenchmarks
{
    // ── Runtimes rebuilt per iteration to prevent list growth bias ────

    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _runtimeNoOp = null!;
    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _runtimeObserver = null!;
    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _runtimeFeedback = null!;
    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _runtimeComposed = null!;
    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _runtimeEventLog = null!;
    private DecidingRuntime<BenchDecider, BenchState, BenchCommand, BenchEvent, BenchEffect, BenchError, Unit> _decider = null!;

    // ── Safe-no-track runtimes (threadSafe=true, trackEvents=false) ─

    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _safeNoTrackNoOp = null!;
    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _safeNoTrackFeedback = null!;
    private DecidingRuntime<BenchDecider, BenchState, BenchCommand, BenchEvent, BenchEffect, BenchError, Unit> _safeNoTrackDecider = null!;

    // ── Lean runtimes (threadSafe=false, trackEvents=false) ──────────

    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _leanNoOp = null!;
    private AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit> _leanFeedback = null!;
    private DecidingRuntime<BenchDecider, BenchState, BenchCommand, BenchEvent, BenchEffect, BenchError, Unit> _leanDecider = null!;

    // ── Record-based lean runtimes (abstract record DU, no boxing) ──

    private AutomatonRuntime<RecBenchAutomaton, RecBenchState, RecBenchEvent, RecBenchEffect, Unit> _recLeanNoOp = null!;
    private AutomatonRuntime<RecBenchAutomaton, RecBenchState, RecBenchEvent, RecBenchEffect, Unit> _recLeanFeedback = null!;
    private DecidingRuntime<RecBenchDecider, RecBenchState, RecBenchCommand, RecBenchEvent, RecBenchEffect, RecBenchError, Unit> _recLeanDecider = null!;

    // ── Pre-allocated events / commands ──────────────────────────────

    private static readonly BenchEvent.Increment _singleEvent = new(1);
    private static readonly BenchEvent.WithEffect _effectEvent = new(1);
    private static readonly BenchCommand.Add _acceptCommand = new(1);
    private static readonly BenchCommand.Reject _rejectCommand = new();

    // ── Pre-allocated record-based events / commands ─────────────────

    private static readonly RecBenchEvent.Increment _recSingleEvent = new(1);
    private static readonly RecBenchEvent.WithEffect _recEffectEvent = new(1);
    private static readonly RecBenchCommand.Add _recAcceptCommand = new(1);
    private static readonly RecBenchCommand.Reject _recRejectCommand = new();

    [IterationSetup]
    public void Setup()
    {
        var (initState, _) = BenchAutomaton.Initialize(default);

        _runtimeNoOp = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, BenchObservers.NoOp, BenchInterpreters.NoOp);

        _runtimeObserver = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, BenchObservers.Touch, BenchInterpreters.NoOp);

        _runtimeFeedback = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, BenchObservers.NoOp, BenchInterpreters.SingleFeedback);

        var composed = BenchObservers.NoOp.Then(BenchObservers.Touch);
        _runtimeComposed = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, composed, BenchInterpreters.NoOp);

        var (eventLogObserver, _) = EventLog.Create<BenchState, BenchEvent, BenchEffect>();
        _runtimeEventLog = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, eventLogObserver, BenchInterpreters.NoOp);

        _decider = DecidingRuntime<BenchDecider, BenchState, BenchCommand, BenchEvent, BenchEffect, BenchError, Unit>
            .Start(default, BenchObservers.NoOp, BenchInterpreters.NoOp)
            .GetAwaiter().GetResult();

        // Lean runtimes — no semaphore, no event tracking
        _leanNoOp = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, BenchObservers.NoOp, BenchInterpreters.NoOp,
            threadSafe: false, trackEvents: false);

        _leanFeedback = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, BenchObservers.NoOp, BenchInterpreters.SingleFeedback,
            threadSafe: false, trackEvents: false);

        _leanDecider = DecidingRuntime<BenchDecider, BenchState, BenchCommand, BenchEvent, BenchEffect, BenchError, Unit>
            .Start(default, BenchObservers.NoOp, BenchInterpreters.NoOp,
                threadSafe: false, trackEvents: false)
            .GetAwaiter().GetResult();

        // Safe-no-track runtimes — thread-safe, but no event tracking
        _safeNoTrackNoOp = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, BenchObservers.NoOp, BenchInterpreters.NoOp,
            threadSafe: true, trackEvents: false);

        _safeNoTrackFeedback = new AutomatonRuntime<BenchAutomaton, BenchState, BenchEvent, BenchEffect, Unit>(
            initState, BenchObservers.NoOp, BenchInterpreters.SingleFeedback,
            threadSafe: true, trackEvents: false);

        _safeNoTrackDecider = DecidingRuntime<BenchDecider, BenchState, BenchCommand, BenchEvent, BenchEffect, BenchError, Unit>
            .Start(default, BenchObservers.NoOp, BenchInterpreters.NoOp,
                threadSafe: true, trackEvents: false)
            .GetAwaiter().GetResult();

        // Record-based lean runtimes — abstract record DU, no boxing
        var (recInitState, _) = RecBenchAutomaton.Initialize(default);

        _recLeanNoOp = new AutomatonRuntime<RecBenchAutomaton, RecBenchState, RecBenchEvent, RecBenchEffect, Unit>(
            recInitState, RecBenchObservers.NoOp, RecBenchInterpreters.NoOp,
            threadSafe: false, trackEvents: false);

        _recLeanFeedback = new AutomatonRuntime<RecBenchAutomaton, RecBenchState, RecBenchEvent, RecBenchEffect, Unit>(
            recInitState, RecBenchObservers.NoOp, RecBenchInterpreters.SingleFeedback,
            threadSafe: false, trackEvents: false);

        _recLeanDecider = DecidingRuntime<RecBenchDecider, RecBenchState, RecBenchCommand, RecBenchEvent, RecBenchEffect, RecBenchError, Unit>
            .Start(default, RecBenchObservers.NoOp, RecBenchInterpreters.NoOp,
                threadSafe: false, trackEvents: false)
            .GetAwaiter().GetResult();
    }

    // ── Dispatch benchmarks ──────────────────────────────────────────

    [Benchmark(Description = "Dispatch (no-op observer, no-op interpreter)")]
    public ValueTask<Result<Unit, PipelineError>> Dispatch_Single()
        => _runtimeNoOp.Dispatch(_singleEvent);

    [Benchmark(Description = "Dispatch (observer touches state/event/effect)")]
    public ValueTask<Result<Unit, PipelineError>> Dispatch_WithObserver()
        => _runtimeObserver.Dispatch(_singleEvent);

    [Benchmark(Description = "Dispatch × 100 (batch, no-op)")]
    public async Task Dispatch_Batch_100()
    {
        for (var i = 0; i < 100; i++)
            _ = await _runtimeNoOp.Dispatch(_singleEvent);
    }

    [Benchmark(Description = "Dispatch with interpreter feedback (1 level)")]
    public ValueTask<Result<Unit, PipelineError>> Dispatch_WithFeedback()
        => _runtimeFeedback.Dispatch(_effectEvent);

    // [OperationsPerInvoke] is required here: this benchmark is ~200 ns per call, which is far
    // below BDN's MinIterationTime. With [IterationSetup] forcing invocationCount=1, a single
    // iteration is so short that timer granularity (~100 ns on Linux CI) accounts for >50% noise,
    // making the 5% regression threshold meaningless. Looping 10 000 times per invocation raises
    // each iteration to ~2 ms so timer noise drops to <0.01 %. BDN divides the measured time
    // by the OperationsPerInvoke count, keeping the reported number as "cost per dispatch".
    [Benchmark(Description = "Dispatch with composed observer (Then)", OperationsPerInvoke = 10_000)]
    public async ValueTask<Result<Unit, PipelineError>> Dispatch_ComposedObserver()
    {
        Result<Unit, PipelineError> result = default;
        for (var i = 0; i < 10_000; i++)
            result = await _runtimeComposed.Dispatch(_singleEvent);
        return result;
    }

    [Benchmark(Description = "Dispatch with EventLog observer append")]
    public ValueTask<Result<Unit, PipelineError>> Dispatch_WithEventLogObserver()
        => _runtimeEventLog.Dispatch(_singleEvent);

    // ── Decider benchmarks ───────────────────────────────────────────

    [Benchmark(Description = "Handle — accept (1 event dispatched)")]
    public ValueTask<Result<BenchState, BenchError>> Handle_Accept()
        => _decider.Handle(_acceptCommand);

    [Benchmark(Description = "Handle — reject (0 events, error returned)")]
    public ValueTask<Result<BenchState, BenchError>> Handle_Reject()
        => _decider.Handle(_rejectCommand);

    // ── Safe-no-track benchmarks (threadSafe=true, trackEvents=false) ─

    [Benchmark(Description = "Safe Dispatch (no tracking)")]
    public ValueTask<Result<Unit, PipelineError>> Safe_NoTrack_Dispatch_Single()
        => _safeNoTrackNoOp.Dispatch(_singleEvent);

    [Benchmark(Description = "Safe Dispatch with feedback (no tracking)")]
    public ValueTask<Result<Unit, PipelineError>> Safe_NoTrack_Dispatch_WithFeedback()
        => _safeNoTrackFeedback.Dispatch(_effectEvent);

    [Benchmark(Description = "Safe Handle — accept (no tracking)")]
    public ValueTask<Result<BenchState, BenchError>> Safe_NoTrack_Handle_Accept()
        => _safeNoTrackDecider.Handle(_acceptCommand);

    [Benchmark(Description = "Safe Handle — reject (no tracking)")]
    public ValueTask<Result<BenchState, BenchError>> Safe_NoTrack_Handle_Reject()
        => _safeNoTrackDecider.Handle(_rejectCommand);

    // ── Lean benchmarks (threadSafe=false, trackEvents=false) ────────

    [Benchmark(Description = "Lean Dispatch (no-op, unserialized, no tracking)")]
    public ValueTask<Result<Unit, PipelineError>> Lean_Dispatch_Single()
        => _leanNoOp.Dispatch(_singleEvent);

    [Benchmark(Description = "Lean Dispatch with feedback (unserialized, no tracking)")]
    public ValueTask<Result<Unit, PipelineError>> Lean_Dispatch_WithFeedback()
        => _leanFeedback.Dispatch(_effectEvent);

    [Benchmark(Description = "Lean Handle — accept (unserialized, no tracking)")]
    public ValueTask<Result<BenchState, BenchError>> Lean_Handle_Accept()
        => _leanDecider.Handle(_acceptCommand);

    [Benchmark(Description = "Lean Handle — reject (unserialized, no tracking)")]
    public ValueTask<Result<BenchState, BenchError>> Lean_Handle_Reject()
        => _leanDecider.Handle(_rejectCommand);

    // ── Record-based lean benchmarks (abstract record DU, no boxing) ─

    [Benchmark(Description = "Lean Dispatch (record-based, zero-alloc)")]
    public ValueTask<Result<Unit, PipelineError>> Rec_Lean_Dispatch_Single()
        => _recLeanNoOp.Dispatch(_recSingleEvent);

    [Benchmark(Description = "Lean Dispatch with feedback (record-based)")]
    public ValueTask<Result<Unit, PipelineError>> Rec_Lean_Dispatch_WithFeedback()
        => _recLeanFeedback.Dispatch(_recEffectEvent);

    [Benchmark(Description = "Lean Handle — accept (record-based)")]
    public ValueTask<Result<RecBenchState, RecBenchError>> Rec_Lean_Handle_Accept()
        => _recLeanDecider.Handle(_recAcceptCommand);

    // [OperationsPerInvoke] is required here: reject is effectively zero-cost (no events emitted,
    // no state mutation) so each call is ~100 ns. The same timer-noise problem as
    // Dispatch_ComposedObserver applies — see comment above for the full rationale.
    [Benchmark(Description = "Lean Handle — reject (record-based, zero-alloc)", OperationsPerInvoke = 10_000)]
    public async ValueTask<Result<RecBenchState, RecBenchError>> Rec_Lean_Handle_Reject()
    {
        Result<RecBenchState, RecBenchError> result = default;
        for (var i = 0; i < 10_000; i++)
            result = await _recLeanDecider.Handle(_recRejectCommand);
        return result;
    }
}
