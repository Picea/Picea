# ADR 001: Automaton Kernel as Mealy Machine

## Status

Accepted

## Context

We need a core abstraction that can power multiple runtime patterns — Model-View-Update (MVU), Event Sourcing, and Actor systems — without coupling to any specific pattern.

The kernel must:
- Be purely functional (deterministic, no side effects)
- Support effects as first-class output (not just state)
- Be testable without any runtime infrastructure
- Be composable across different execution models

## Decision

We model the kernel as a **Mealy machine** — a finite-state transducer where outputs depend on both the current state and the input:

```text
transition : (State × Event) → (State × Effect)
```

In C#:

```csharp
public interface Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract (TState State, TEffect Effect) Initialize(TParameters parameters);
    static abstract (TState State, TEffect Effect) Transition(TState state, TEvent @event);
}
```

Key design choices:

1. **Static abstract members** — The implementing type holds no instance state. All state flows through `TState`. This enforces the Mealy machine constraint: the transition function depends only on its inputs.

2. **Effects as output** — Unlike a Moore machine (where output depends only on state), the Mealy machine produces effects that depend on both state and event. This is essential for patterns like MVU where the view depends on the full transition context.

3. **`TParameters` for initialization** — The `Initialize` method accepts parameters and returns both an initial state and an initial effect. This supports patterns like Event Sourcing where the initial state may depend on configuration.

4. **`Unit` type for unused parameters** — When no initialization parameters are needed, use `Unit` to avoid `void` (which can't be a type parameter in C#).

## Consequences

### Positive
- **Universal kernel** — The same interface powers MVU, Event Sourcing, and Actor systems
- **Pure testability** — `Transition` is a pure function: test it with no runtime, no async, no mocking
- **Deterministic replay** — Given the same events, the same state is always produced
- **Effects as data** — Side effects are described, not executed, enabling replay, testing, and runtime substitution

### Negative
- **Static abstract members** require .NET 7+ (we target .NET 10.0)
- **Implementer choice** — Structs are supported, but prefer simple stateless implementers by default; use structs only when profiling demonstrates a clear benefit
- **Single effect per transition** — Each transition produces exactly one effect. Composite effects must be modeled as a single effect type (e.g., `BatchEffect`)

## Alternatives Considered

### Moore Machine
Output depends only on state, not on the input event. This is simpler but less expressive — you can't produce different effects for the same state depending on which event triggered the transition.

### Mealy Machine with Multiple Effects
Return `TEffect[]` instead of `TEffect`. Rejected because it complicates the interpreter contract and encourages large effect arrays. A single composite effect (e.g., `BatchEffect(effects)`) is cleaner.

### No Effects (Pure State Machine)
Return only `TState`. This works for simple FSMs but doesn't support MVU (view rendering), Event Sourcing (persistence), or Actor patterns (message sending) without bolting on side effects through other means.

## References

- [Mealy machine (Wikipedia)](https://en.wikipedia.org/wiki/Mealy_machine)
- [The Elm Architecture](https://guide.elm-lang.org/architecture/)
- Chassaing, J. (2021). [Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider)
