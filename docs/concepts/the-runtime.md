# The Runtime

The `AutomatonRuntime` executes the kernel's transition function in a loop: dispatch → transition → observe → interpret → feedback.

## The Loop

```text
Dispatch(event):
  1. (newState, effect) = Transition(state, event)     ← pure
  2. state = newState
  3. observerResult = Observer(state, event, effect)     ← side effect
  4. feedbackEvents = Interpreter(effect)                ← side effect
  5. for each feedbackEvent: Dispatch(feedbackEvent)     ← recursion
```

Step 1 is pure. Steps 3–5 are where side effects live.

## The Observer

```csharp
public delegate ValueTask<Result<Unit, PipelineError>> Observer<in TState, in TEvent, in TEffect>(
    TState state, TEvent @event, TEffect effect);
```

The observer sees every transition triple `(state, event, effect)` after the automaton steps. It's the extension point for:

| Pattern | Observer Does |
| ------- | ------------- |
| **MVU** | Renders the view (HTML, DOM diff) |
| **Event Sourcing** | Persists the event to the store |
| **Actor** | Sends messages to other actors |
| **Logging** | Records the transition for debugging |
| **Metrics** | Tracks transition counts, latencies |

### Composition

Observers compose via extension methods:

```csharp
var pipeline = logger.Then(metrics).Then(persister);
```

| Combinator | Behavior |
| ---------- | -------- |
| `Then` | Sequential, short-circuits on error |
| `Where` | Guards with a predicate |
| `Select` | Transforms inputs (contramap) |
| `Catch` | Handles errors |
| `Combine` | Sequential, does NOT short-circuit |

See [Observer Composition](../guides/observer-composition.md) for recipes.

## The Interpreter

```csharp
public delegate ValueTask<Result<TEvent[], PipelineError>> Interpreter<in TEffect, TEvent>(TEffect effect);
```

The interpreter converts effects into feedback events. Feedback events are dispatched back into the automaton, creating a closed loop:

```text
TemperatureReading(18°C)
  → Transition → (state, TurnOnHeater)
    → Interpreter(TurnOnHeater) → [HeaterStarted]
      → Dispatch(HeaterStarted)
        → Transition → (state{Heating=true}, None)
          → Interpreter(None) → []  ← loop ends
```

Return `[]` (empty array) for fire-and-forget effects.

## Error Handling

Both observer and interpreter return `Result<T, PipelineError>`. Errors propagate as values through the pipeline:

```csharp
public readonly record struct PipelineError(
    string Message,
    string? Source = null,
    Exception? Exception = null);
```

The happy path uses pre-allocated `PipelineResult.Ok` for zero allocation.

## Thread Safety

All public entry points are serialized via `SemaphoreSlim(1, 1)` by default. Concurrent dispatches are queued, never interleaved.

For single-threaded environments (WASM), disable with `threadSafe: false`.

## Feedback Depth

Interpreter feedback loops are bounded at `MaxFeedbackDepth = 64`. A misconfigured interpreter that produces infinite feedback will fail with a descriptive exception rather than stack overflow.

## The Monadic Left Fold

Formally, the runtime is a **monadic left fold** over a stream of events:

```text
foldM : (State → Event → M State) → State → [Event] → M State
```

Where `M` is the `Result` monad. Each step:
1. Applies the pure transition function
2. Runs the observer (monadic side effect)
3. Runs the interpreter (monadic side effect that may produce more events)
4. Recurses on feedback events

This is Kleisli composition (`>=>`) applied to state machine execution.

## See Also

- [The Kernel](the-kernel.md) — the pure function that the runtime executes
- [The Decider](the-decider.md) — adding command validation
- [Runtime Reference](../reference/runtime.md) — full API
- [Building Custom Runtimes](../guides/building-custom-runtimes.md) — wire your own observer + interpreter
- [ADR 002](../adr/002-shared-runtime-monadic-left-fold.md) — design rationale
