# ADR 004: Decider Pattern for Command Validation

## Status

Accepted

## Context

The basic Automaton (ADR 001) accepts any event — there's no validation layer. In real domains, we need to:
- Validate user intent before producing facts
- Reject invalid operations with structured errors
- Separate "what the user wants" (commands) from "what happened" (events)

## Decision

We implement the **Decider pattern** (Chassaing, 2021) as an extension of the Automaton interface:

```csharp
public interface Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract Result<TEvent[], TError> Decide(TState state, TCommand command);
    static virtual bool IsTerminal(TState state) => false;
}
```

Key design choices:

1. **Decider extends Automaton** — `Decider<...> : Automaton<...>`. This means every Decider is a valid Automaton. Existing code that works with `AutomatonRuntime` continues to work unchanged. The Decider is a non-breaking, additive upgrade.

2. **`Decide` returns `Result<TEvent[], TError>`** — On success, returns the events to dispatch. On failure, returns a typed error. This uses the same `Result` type from ADR 003.

3. **`IsTerminal` with default implementation** — Uses `static virtual` (C# 11) to provide a sensible default (`false`) while allowing domain-specific overrides. Terminal states signal that no further commands should be processed.

4. **`DecidingRuntime` wraps `AutomatonRuntime`** — The `Handle(command)` method calls `Decide`, then dispatches the resulting events through the existing runtime. This reuses all existing infrastructure (observer, interpreter, thread safety, tracing).

5. **Atomic Handle** — The entire `Handle` operation (Decide + all Dispatches) executes under a single semaphore acquisition. This prevents TOCTOU races where state changes between Decide reading the state and events being dispatched.

## Consequences

### Positive
- **Non-breaking upgrade** — Adding `Decide` to an existing Automaton doesn't break any existing code
- **Pure validation** — `Decide` is a pure function, testable without runtime infrastructure
- **Typed errors** — Errors carry domain context (`Overflow(current, amount, max)`) not just strings
- **Seven elements** — The Decider provides all seven elements of the Decider pattern: Command, Event, State, Initial State, Decide, Evolve, IsTerminal

### Negative
- **Six type parameters** — `Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>` is verbose. Mitigated by IDE support and the fact that these are all genuinely distinct concerns.
- **Two runtime types** — Users must choose between `AutomatonRuntime` and `DecidingRuntime`. Mitigated by clear documentation: use `DecidingRuntime` when you have commands.

## Alternatives Considered

### Validation as Middleware
Add validation as an observer or interceptor rather than a language-level interface. Rejected because it doesn't provide type-safe error channels and doesn't integrate with the transition lifecycle.

### Command as a Special Event
Treat commands as events that might fail. Rejected because it conflates intent (commands) with facts (events) and makes the type system less expressive.

### Separate Validation Service
Validate commands in a separate service before dispatching events. Rejected because it creates TOCTOU races (state can change between validation and dispatch) and duplicates state access logic.

## References

- Chassaing, J. (2021). [Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider)
- [Open/Closed Principle (Wikipedia)](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle)
