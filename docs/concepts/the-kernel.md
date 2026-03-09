# The Kernel

The Picea kernel is a [Mealy machine](https://en.wikipedia.org/wiki/Mealy_machine) — a finite-state transducer where outputs depend on both the current state and the input.

## The Interface

```csharp
public interface Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract (TState State, TEffect Effect) Initialize(TParameters parameters);
    static abstract (TState State, TEffect Effect) Transition(TState state, TEvent @event);
}
```

Two methods. That's the entire kernel.

## The Transition Function

```text
transition : (State × Event) → (State × Effect)
```

Given the current state and an event, produce the new state and an effect.

**This function must be pure:**

| Property | Meaning |
| -------- | ------- |
| Deterministic | Same inputs → same outputs, always |
| No side effects | No I/O, no mutation, no randomness |
| Total | Handles every possible event (exhaustive `switch`) |

## Effects Are Data

The transition function describes what should happen — it doesn't make it happen:

```csharp
// ✅ Effect as data (describes intent)
(state with { Heating = false }, new ThermostatEffect.TurnOnHeater())

// ❌ Effect as code (performs side effect)
heater.TurnOn(); // impure!
return (state with { Heating = true }, ...);
```

Effects are values. The [Interpreter](the-runtime.md#the-interpreter) makes them happen.

## Why Mealy, Not Moore?

A [Moore machine](https://en.wikipedia.org/wiki/Moore_machine) produces output based only on state. A Mealy machine produces output based on both state and input.

```text
Moore:  output = f(state)
Mealy:  output = f(state, event)
```

Mealy is essential because:
- **MVU**: The view depends on the *transition* (state + event), not just the state
- **Event Sourcing**: The persisted event is the input, not derived from state alone
- **Effects**: Different events in the same state may require different effects

## Static Abstract Members

The `Automaton` interface uses C# static abstract members:

```csharp
static abstract (TState, TEffect) Transition(TState state, TEvent @event);
```

This enforces a critical constraint: **the implementing class holds no instance state**. All state flows through `TState`. The class is just a namespace for pure functions.

```csharp
public class Counter : Automaton<CounterState, CounterEvent, CounterEffect, Unit>
{
    // No fields. No constructor. No instance state.
    // Just pure static methods.

    public static (CounterState, CounterEffect) Initialize(Unit _) => ...;
    public static (CounterState, CounterEffect) Transition(CounterState state, CounterEvent @event) => ...;
}
```

## Testing

Because `Transition` is a pure static method, you can test it with zero infrastructure:

```csharp
var (state, effect) = Counter.Transition(
    new CounterState(5),
    new CounterEvent.Increment());

Assert.Equal(6, state.Count);
Assert.IsType<CounterEffect.None>(effect);
```

No runtime. No async. No mocking. No DI. Just call the function.

## See Also

- [The Runtime](the-runtime.md) — how the kernel executes
- [The Decider](the-decider.md) — adding command validation
- [Automaton Reference](../reference/automaton.md) — full API
- [Tutorial 01](../tutorials/01-getting-started.md) — hands-on walkthrough
- [ADR 001](../adr/001-automaton-kernel-mealy-machine.md) — design rationale
