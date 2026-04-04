// =============================================================================
// Record-Based Benchmark Domain — Abstract record hierarchies (no boxing)
// =============================================================================
// Mirror of BenchDomain using abstract record DUs instead of
// interface + record struct.  This eliminates boxing at every generic
// boundary crossing, proving the framework itself is zero-alloc when
// the domain types are reference types with cached singletons.
//
// Allocation profile (lean dispatch, no-op observer/interpreter):
//   interface-based domain : 72 B  (3 boxing operations)
//   record-based domain   :  0 B  (no boxing, cached singletons)
// =============================================================================

using System.Diagnostics;

namespace Picea.Benchmarks;

// ── State (value type — stays on the stack) ───────────────────

public record struct RecBenchState(int Value);

// ── Events (reference types — no boxing through generic TEvent) ─

public abstract record RecBenchEvent
{
    public sealed record Increment(int Amount) : RecBenchEvent;
    public sealed record WithEffect(int Amount) : RecBenchEvent;
}

// ── Effects (reference types + cached singleton for None) ─────

public abstract record RecBenchEffect
{
    /// <summary>Cached singleton — returns the same instance every time, 0 B per use.</summary>
    public static readonly RecBenchEffect NoneInstance = new None();

    public sealed record None : RecBenchEffect;
    public sealed record Trigger(int FeedbackAmount) : RecBenchEffect;
}

// ── Commands (reference types — pre-allocated in benchmarks) ──

public abstract record RecBenchCommand
{
    public sealed record Add(int Amount) : RecBenchCommand;
    public sealed record Reject : RecBenchCommand;
}

// ── Errors (reference types + cached singleton for Rejected) ──

public abstract record RecBenchError
{
    /// <summary>Cached singleton — avoids allocation on the reject hot path.</summary>
    public static readonly RecBenchError RejectedInstance = new Rejected();

    public sealed record Rejected : RecBenchError;
}

// ── Automaton (pure transitions) ──────────────────────────────

public class RecBenchAutomaton
    : Automaton<RecBenchState, RecBenchEvent, RecBenchEffect, Unit>
{
    public static (RecBenchState State, RecBenchEffect Effect) Initialize(Unit _) =>
        (new RecBenchState(0), RecBenchEffect.NoneInstance);

    public static (RecBenchState State, RecBenchEffect Effect) Transition(
        RecBenchState state, RecBenchEvent @event) =>
        @event switch
        {
            RecBenchEvent.Increment(var n) =>
                (new RecBenchState(state.Value + n), RecBenchEffect.NoneInstance),

            RecBenchEvent.WithEffect(var n) =>
                (new RecBenchState(state.Value + n), new RecBenchEffect.Trigger(1)),

            _ => throw new UnreachableException()
        };
}

// ── Decider (adds command validation) ─────────────────────────

public class RecBenchDecider
    : Decider<RecBenchState, RecBenchCommand, RecBenchEvent, RecBenchEffect, RecBenchError, Unit>
{
    public static (RecBenchState State, RecBenchEffect Effect) Initialize(Unit _) =>
        RecBenchAutomaton.Initialize(default);

    public static (RecBenchState State, RecBenchEffect Effect) Transition(
        RecBenchState state, RecBenchEvent @event) =>
        RecBenchAutomaton.Transition(state, @event);

    public static Validated<RecBenchCommand, RecBenchError> Validate(
        RecBenchState state, RecBenchCommand command) =>
        command switch
        {
            RecBenchCommand.Reject =>
                new Validated<RecBenchCommand, RecBenchError>.Invalid(RecBenchError.RejectedInstance),

            _ => new Validated<RecBenchCommand, RecBenchError>.Valid(command)
        };

    public static Result<RecBenchEvent[], RecBenchError> Decide(
        RecBenchState state, Validated<RecBenchCommand, RecBenchError> validated) =>
        validated is not Validated<RecBenchCommand, RecBenchError>.Valid(var command)
            ? throw new UnreachableException()
            : command switch
            {
                RecBenchCommand.Add(var n) =>
                    Result<RecBenchEvent[], RecBenchError>
                        .Ok([new RecBenchEvent.Increment(n)]),

                _ => throw new UnreachableException()
            };
}

// ── Observers & Interpreters ──────────────────────────────────

public static class RecBenchObservers
{
    public static readonly Observer<RecBenchState, RecBenchEvent, RecBenchEffect> NoOp =
        (_, _, _) => PipelineResult.Ok;
}

public static class RecBenchInterpreters
{
    public static readonly Interpreter<RecBenchEffect, RecBenchEvent> NoOp =
        _ => InterpreterResult<RecBenchEvent>.Empty;

    /// <summary>
    /// Interpreter that produces one feedback event per <see cref="RecBenchEffect.Trigger"/>.
    /// Used to benchmark the interpreter feedback loop (1 level deep).
    /// </summary>
    public static readonly Interpreter<RecBenchEffect, RecBenchEvent> SingleFeedback =
        effect => effect switch
        {
            RecBenchEffect.Trigger(var n) =>
                new ValueTask<Result<RecBenchEvent[], PipelineError>>(
                    Result<RecBenchEvent[], PipelineError>.Ok([new RecBenchEvent.Increment(n)])),
            _ => InterpreterResult<RecBenchEvent>.Empty
        };
}
