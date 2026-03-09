# ADR 013: Command Non-Generic Design

## Status

Accepted

## Context

In the initial Decider design, `Command<TState, TEvent>` was proposed as a generic interface:

```csharp
public interface Command<TState, TEvent>
{
    Result<TEvent[], ???> Execute(TState state);
}
```

The idea was that commands would be self-validating: each command carries its own `Execute` method that validates against the current state.

## Decision

Reject the generic `Command<TState, TEvent>` interface. Instead, commands are plain marker interfaces with no generic parameters:

```csharp
public interface CounterCommand
{
    record struct Add(int Amount) : CounterCommand;
    record struct Reset : CounterCommand;
}
```

Validation lives in the `Decide` function, not in the command itself.

### Rationale

1. **Commands are data, not behavior** — Following the kernel's principle that effects are data (not actions), commands should also be data. They describe intent; the Decide function validates intent.

2. **Decide is the single validation point** — Having validation in both the command and the Decide function creates ambiguity about where validation lives. A single `Decide` function per Decider is clearer.

3. **Exhaustive pattern matching** — A single `Decide` function with a `switch` expression over all command types ensures exhaustive handling. Self-validating commands would scatter validation across multiple classes.

4. **Error type freedom** — With `Command<TState, TEvent>`, the error type (`???`) is unclear. Does each command define its own error type? Is there a shared error type? The Decider pattern solves this cleanly: `Decide` returns `Result<TEvent[], TError>` where `TError` is defined once per Decider.

5. **Simpler type signatures** — `CounterCommand` with no generic parameters is much simpler than `Command<CounterState, CounterEvent>`.

## Consequences

### Positive
- **Commands are just DTOs** — No behavior, no dependencies, trivially serializable
- **Single validation locus** — All validation in one exhaustive `switch` expression
- **Clean error typing** — One `TError` type per Decider
- **Simple signatures** — No generic pollution in command definitions

### Negative
- **All validation in one method** — For Deciders with many commands, the `Decide` function can grow large. Mitigated by extracting helper methods per command case.

## References

- Chassaing, J. (2021). [Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider)
- [Command pattern (Wikipedia)](https://en.wikipedia.org/wiki/Command_pattern)
