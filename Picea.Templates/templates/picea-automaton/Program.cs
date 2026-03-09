using System.Diagnostics;
using Picea;

// ── Domain: Pure Automaton ──────────────────────────────────────────────

/// <summary>The state of the counter.</summary>
public record CounterState(int Count);

/// <summary>Events that drive state transitions.</summary>
public interface CounterEvent
{
    record struct Increment : CounterEvent;
    record struct Decrement : CounterEvent;
}

/// <summary>Effects produced by transitions (none in this example).</summary>
public interface CounterEffect
{
    record struct None : CounterEffect;
}

/// <summary>
/// A simple counter automaton — a Mealy machine with no side effects.
/// </summary>
public class Counter : Automaton<CounterState, CounterEvent, CounterEffect, Unit>
{
    public static (CounterState, CounterEffect) Initialize(Unit _) =>
        (new CounterState(0), new CounterEffect.None());

    public static (CounterState, CounterEffect) Transition(CounterState state, CounterEvent @event) =>
        @event switch
        {
            CounterEvent.Increment => (state with { Count = state.Count + 1 }, new CounterEffect.None()),
            CounterEvent.Decrement => (state with { Count = state.Count - 1 }, new CounterEffect.None()),
            _ => throw new UnreachableException()
        };
}

// ── Runtime: Wire the automaton to the console ──────────────────────────

var runtime = await AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>
    .Start(
        default,
        observer: (state, @event, effect) =>
        {
            Console.WriteLine($"{@event} → {state}");
            return PipelineResult.Ok;
        },
        interpreter: _ => new ValueTask<Result<CounterEvent[], PipelineError>>(
            Result<CounterEvent[], PipelineError>.Ok([])));

await runtime.Dispatch(new CounterEvent.Increment());
await runtime.Dispatch(new CounterEvent.Increment());
await runtime.Dispatch(new CounterEvent.Decrement());

Console.WriteLine($"\nFinal state: {runtime.State}");
