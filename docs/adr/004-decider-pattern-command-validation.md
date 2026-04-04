# ADR 004: Decider Pattern for Command Validation

## Status

Accepted

## Context

The basic Automaton (ADR 001) accepts any event — there's no validation layer. In real domains, we need to:
- Validate user intent before producing facts
- Authorize permitted intents in current state/context
- Reject invalid operations with structured errors
- Separate "what the user wants" (commands) from "what happened" (events)

## Decision

We implement the **Decider pattern** (Chassaing, 2021) as an extension of the Automaton interface:

```csharp
public interface Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract Validated<TCommand, TError> Validate(TState state, TCommand command);
    static virtual Result<Unit, TError> Authorize(TState state, Validated<TCommand, TError> command) =>
        Result<Unit, TError>.Ok(Unit.Value);
    static abstract Result<TEvent[], TError> Decide(TState state, Validated<TCommand, TError> command);
    static virtual bool IsTerminal(TState state) => false;
}
```

Key design choices:

1. **Decider extends Automaton** — `Decider<...> : Automaton<...>`. This means every Decider is a valid Automaton. Existing code that works with `AutomatonRuntime` continues to work unchanged. The Decider is a non-breaking, additive upgrade.

2. **Three-stage command pipeline** —
    - `Validate` returns `Validated<TCommand, TError>` (`Valid` or `Invalid`)
    - `Authorize` returns `Result<Unit, TError>` (`Ok(Unit)` when permitted; `Err(error)` when denied)
    - `Decide` returns `Result<TEvent[], TError>` for event production

    This keeps all reject paths in explicit sum types and allows short-circuit composition.

3. **`IsTerminal` with default implementation** — Uses `static virtual` (C# 11) to provide a sensible default (`false`) while allowing domain-specific overrides. Terminal states signal that no further commands should be processed.

4. **`DecidingRuntime` wraps `AutomatonRuntime`** — The `Handle(command)` method executes `Validate → Authorize → Decide`, then dispatches the resulting events through the existing runtime. This reuses all existing infrastructure (observer, interpreter, thread safety, tracing).

5. **Atomic Handle** — The entire `Handle` operation (Validate + Authorize + Decide + all Dispatches) executes under a single semaphore acquisition. This prevents TOCTOU races where state changes between stage reads and event dispatch.

6. **Formal composition helper** — `DeciderComposition.Compose(...)` is provided to express the staged pipeline as explicit monadic composition and to support law tests.

## Consequences

### Positive
- **Non-breaking upgrade** — Adding staged command processing to an existing Automaton doesn't break existing automaton runtime usage
- **Pure stages** — `Validate`, `Authorize`, and `Decide` are pure functions, testable without runtime infrastructure
- **Typed errors** — Errors carry domain context (`Overflow(current, amount, max)`) not just strings
- **Explicit short-circuit semantics** — First reject wins, with no partial dispatch

### Negative
- **Six type parameters** — `Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>` is verbose. Mitigated by IDE support and the fact that these are all genuinely distinct concerns.
- **Two runtime types** — Users must choose between `AutomatonRuntime` and `DecidingRuntime`. Mitigated by clear documentation: use `DecidingRuntime` when you have commands.
- **Additional API surface** — Decider now includes `Validated` and a staged contract, which increases learning curve versus a single `Decide` method.

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
