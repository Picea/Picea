# AutomatonRuntime, Observer, Interpreter

`namespace Picea`

The shared runtime that executes the automaton loop: dispatch → transition → observe → interpret.

---

## PipelineError

```csharp
public readonly record struct PipelineError(
    string Message,
    string? Source = null,
    Exception? Exception = null)
```

A structured error from an Observer or Interpreter pipeline stage.

| Property | Type | Description |
| -------- | ---- | ----------- |
| `Message` | `string` | Human-readable description of the failure. |
| `Source` | `string?` | The pipeline stage that produced the error (e.g., `"persist"`, `"render"`). |
| `Exception` | `Exception?` | The underlying exception, if the error originated from a caught exception. |

---

## Unit

```csharp
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
```

The unit type — a type with exactly one value. Used where a success type is required but no meaningful value exists.

---

## PipelineResult

```csharp
public static class PipelineResult
{
    public static readonly ValueTask<Result<Unit, PipelineError>> Ok;
}
```

Pre-allocated Result value for the happy path. Zero-alloc fast path.

---

## InterpreterResult&lt;TEvent&gt;

```csharp
public static class InterpreterResult<TEvent>
{
    public static readonly ValueTask<Result<TEvent[], PipelineError>> Empty;
}
```

Pre-allocated empty result for interpreters that produce no feedback events. Zero-alloc fast path — uses `Array.Empty<TEvent>()` internally.

```csharp
// Usage in an interpreter:
var noOp = _ => InterpreterResult<MyEvent>.Empty;
```

---

## Observer

```csharp
public delegate ValueTask<Result<Unit, PipelineError>> Observer<in TState, in TEvent, in TEffect>(
    TState state, TEvent @event, TEffect effect);
```

Observes each transition triple `(state, event, effect)` after the automaton steps.

---

## Interpreter

```csharp
public delegate ValueTask<Result<TEvent[], PipelineError>> Interpreter<in TEffect, TEvent>(TEffect effect);
```

Interprets an effect by converting it into zero or more feedback events.

---

## AutomatonRuntime&lt;TAutomaton, TState, TEvent, TEffect, TParameters&gt;

```csharp
public sealed class AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters> : IDisposable
    where TAutomaton : Automaton<TState, TEvent, TEffect, TParameters>
```

> ⚠️ **Constructor is `internal`** — Use [`Start`](#start) to create a runtime. The factory ensures proper initialization.

### Properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `State` | `TState` | The current state of the automaton. |
| `Events` | `IReadOnlyList<TEvent>` | All dispatched events (including feedback). |

### Start

```csharp
public static async ValueTask<AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>> Start(
    TParameters parameters,
    Observer<TState, TEvent, TEffect> observer,
    Interpreter<TEffect, TEvent> interpreter,
    bool threadSafe = true,
    bool trackEvents = true,
    CancellationToken cancellationToken = default)
```

The **only public way** to create a runtime. This factory:

1. Calls `TAutomaton.Initialize(parameters)` to obtain initial state and effect
2. Constructs the runtime with the initialized state
3. Interprets the startup effect through the interpreter pipeline
4. Returns a fully initialized, ready-to-dispatch runtime

**Always use `Start` for production code.** Only test/internal code may construct directly.

### Dispatch

```csharp
public ValueTask<Result<Unit, PipelineError>> Dispatch(
    TEvent @event, CancellationToken cancellationToken = default)
```

Dispatches an event through the full cycle: transition → observe → interpret effects → dispatch feedback events.

### Reset

```csharp
public void Reset(TState state)
```

Replaces the current state without triggering a transition or observer.

---

## ObserverExtensions

| Method | Algebraic Name | Behavior |
| ------ | -------------- | -------- |
| `Then` | Kleisli composition | Sequential, short-circuits on error |
| `Where` | Guard | Runs only when predicate is true |
| `Select` | Contramap | Transforms observer inputs |
| `Catch` | Error recovery | Handles errors |
| `Combine` | Applicative | Sequential, does NOT short-circuit |

## InterpreterExtensions

| Method | Behavior |
| ------ | -------- |
| `Then` | Sequential, result events concatenated, short-circuits on error |
| `Where` | Guards with predicate |
| `Select` | Contramaps input type |
| `Catch` | Error recovery |

---

## See Also

- [The Runtime](../concepts/the-runtime.md) — conceptual explanation
- [Observer Composition](../guides/observer-composition.md) — recipes
- [Building Custom Runtimes](../guides/building-custom-runtimes.md) — how to wire your own
- [Zero-Alloc Domain Modeling](../guides/zero-alloc-domain-modeling.md) — eliminating heap allocations
