# ADR 010: Example Runtimes as Reference Implementations

## Status

Accepted

## Context

The Picea kernel (Automaton + Runtime + Decider) is pattern-agnostic. To demonstrate its versatility and provide production-quality starting points, we need reference implementations for the three major patterns.

## Decision

We maintain three separate repositories with reference runtime implementations:

| Pattern | Repository | Runtime Type | Observer Does | Interpreter Does |
| ------- | ---------- | ------------ | ------------- | ---------------- |
| **MVU** | [picea/abies](https://github.com/picea/abies) | Model-View-Update | Renders view (HTML/DOM diff) | Executes commands (HTTP, JS interop) |
| **Event Sourcing** | [picea/glauca](https://github.com/picea/glauca) | Command-Sourced | Persists events (append-only store) | Publishes to read models |
| **Actor** | [picea/rubens](https://github.com/picea/rubens) | Message-Driven | Sends messages (mailbox, Channel\<T\>) | Routes replies |

### Key Design Choices

1. **Separate repositories** — Each pattern has its own lifecycle, dependencies, and release cadence. MVU depends on Blazor. Event Sourcing depends on event store clients. Actors depend on System.Threading.Channels. The kernel depends on nothing.

2. **Same kernel, different wiring** — Each runtime is just a specific configuration of `AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>` with different observers and interpreters. No kernel modifications needed.

3. **Production-quality** — These are not toy examples. They include proper error handling, tracing, testing, and documentation.

4. **Tutorials cover all three** — Tutorials 02 (MVU), 03 (Event Sourcing), and 04 (Actor) each build a complete working system using the same kernel.

## Consequences

### Positive
- **Proves the kernel's universality** — Three different patterns, same interface
- **Independent evolution** — MVU can release without affecting Event Sourcing
- **Clear dependency graph** — All runtimes depend on `Picea` (the kernel). Nothing depends on each other.

### Negative
- **Multiple repositories** — More repos to maintain, version, and coordinate
- **Cross-repo breaking changes** — If the kernel interface changes, all runtimes must update

## References

- [The Elm Architecture](https://guide.elm-lang.org/architecture/) (MVU)
- Chassaing, J. (2021). [Functional Event Sourcing Decider](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider)
- [Actor Model (Wikipedia)](https://en.wikipedia.org/wiki/Actor_model)
