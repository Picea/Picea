# ADR 002: Shared Runtime as Monadic Left Fold

## Status

Accepted

## Context

The Automaton kernel (ADR 001) defines a pure transition function. We need a runtime that:
- Executes the transition function in a loop
- Handles side effects (observation and interpretation)
- Supports feedback loops (effects → events → more transitions)
- Is shared across all patterns (MVU, Event Sourcing, Actors)

## Decision

We implement the runtime as a **monadic left fold** — a sequential reduction over a stream of events, where each step may produce effects that feed back into the stream.

The runtime loop:

```text
Dispatch(event):
  1. (newState, effect) = Transition(state, event)
  2. state = newState
  3. observerResult = Observer(state, event, effect)     ← side effect
  4. feedbackEvents = Interpreter(effect)                ← side effect
  5. for each feedbackEvent: Dispatch(feedbackEvent)     ← recursion
```

The "monadic" aspect: each step produces a `Result<T, PipelineError>`. Errors short-circuit the pipeline via monadic bind (`>>=`).

Key design choices:

1. **Observer and Interpreter as delegates** — Not interfaces. This allows inline lambdas, closures, and composition via extension methods (`Then`, `Where`, `Catch`, `Combine`).

2. **Observer sees (state, event, effect)** — The full transition triple. This is more information than most observers need, but it enables MVU (which needs all three) without separate observer types per pattern.

3. **Interpreter returns `TEvent[]`** — Feedback events are dispatched back into the loop. Return `[]` for fire-and-forget effects.

4. **Feedback depth limit** — `MaxFeedbackDepth = 64` prevents infinite loops from misconfigured interpreters.

5. **Thread safety via SemaphoreSlim** — All public entry points are serialized. This is configurable (`threadSafe` parameter) for single-threaded environments like WASM.

## Consequences

### Positive
- **Single runtime for all patterns** — MVU, ES, and Actors all use `AutomatonRuntime`
- **Composable pipelines** — Observer and Interpreter compose via `Then`, `Where`, `Catch`
- **Error propagation as values** — No exceptions in the happy path; errors flow through `Result`
- **Zero-alloc happy path** — `PipelineResult.Ok` is pre-allocated; `ValueTask` avoids heap allocation for synchronous observers

### Negative
- **Observer sees too much** — Simple observers (e.g., logging) receive the full triple even when they only need the event
- **Feedback loops can be surprising** — A single `Dispatch` can trigger many transitions. The depth limit mitigates but doesn't eliminate confusion
- **SemaphoreSlim overhead** — Thread safety adds ~1µs per dispatch even in single-threaded environments (mitigated by `threadSafe: false`)

## Alternatives Considered

### Interface-Based Observer/Interpreter
Use `IObserver<T>` or custom interfaces instead of delegates. Rejected because delegates compose more naturally (lambdas, closures) and don't require class boilerplate.

### Separate Runtimes Per Pattern
Build `MvuRuntime`, `EventSourcingRuntime`, `ActorRuntime` independently. Rejected because they share 90% of the logic (the fold loop). The shared runtime avoids duplication and ensures consistent behavior.

### Rx-Based Observable Stream
Model the event stream as an `IObservable<T>`. Rejected because it introduces a large dependency (System.Reactive), adds complexity for simple use cases, and makes the feedback loop harder to reason about.

## References

- [Left fold (Wikipedia)](https://en.wikipedia.org/wiki/Fold_(higher-order_function))
- [Kleisli composition](https://en.wikipedia.org/wiki/Kleisli_category)
- [Railway-oriented programming](https://fsharpforfunandprofit.com/rop/)
