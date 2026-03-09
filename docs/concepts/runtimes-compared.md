# Runtimes Compared

The Picea kernel is pattern-agnostic. The same `Automaton` interface powers three major runtime patterns. This guide helps you choose.

## Overview

| Aspect | MVU | Event Sourcing | Actor |
| ------ | --- | -------------- | ----- |
| **Repository** | [picea/abies](https://github.com/picea/abies) | [picea/glauca](https://github.com/picea/glauca) | [picea/rubens](https://github.com/picea/rubens) |
| **Observer does** | Renders view (HTML diff) | Persists events | Sends messages |
| **Interpreter does** | Executes commands (HTTP, JS) | Publishes to read models | Routes replies |
| **State lives** | In memory (single tab) | Event store (reconstructed) | In memory (per actor) |
| **Concurrency model** | Single-threaded (UI) | Optimistic concurrency | Mailbox (one-at-a-time) |
| **Use Decider?** | Optional | Yes (core pattern) | Optional |
| **Best for** | Interactive UIs | Domain-driven backends | Distributed systems |

## Decision Matrix

### Use MVU When

- You're building an interactive UI (web, desktop, mobile)
- State is local to a single user session
- You want Elm-style architecture in C#
- You need virtual DOM diffing for efficient rendering

### Use Event Sourcing When

- You need an audit trail of everything that happened
- You need to reconstruct state at any point in time
- You're implementing CQRS (Command Query Responsibility Segregation)
- Your domain has complex business rules that benefit from Decide

### Use Actor When

- You have many independent entities with their own lifecycle
- You need location transparency (local or remote actors)
- You want fire-and-forget message passing
- You're building a distributed system with supervision

## Same Kernel, Different Wiring

The key insight: all three patterns use the same kernel interface. Only the Observer and Interpreter change:

```text
                    ┌─────────────────────────────┐
                    │     AutomatonRuntime          │
                    │                               │
                    │  Transition (pure, shared)     │
                    │                               │
    ┌────────────┬┴───────────────┬────────────┐
    │            │               │            │
    │  MVU       │  Event Source  │  Actor     │
    │  Observer: │  Observer:     │  Observer: │
    │  render    │  persist       │  send      │
    │  view      │  event         │  message   │
    │            │               │            │
    │  Interp:   │  Interp:       │  Interp:   │
    │  execute   │  publish       │  route     │
    │  commands  │  projections   │  replies   │
    └────────────┴───────────────┴────────────┘
```

You can even run the same automaton on different runtimes in the same application — for example, MVU for the UI and Event Sourcing for the backend, sharing the same Transition function.

## Mixing Patterns

Patterns are not mutually exclusive:

- **MVU + Event Sourcing**: The observer persists events AND renders a view
- **Actor + Event Sourcing**: Each actor persists its own event stream
- **MVU + Actor**: UI automaton communicates with backend actors

The Observer's `Then` combinator makes this natural:

```csharp
var observer = renderView.Then(persistEvent).Then(publishMetrics);
```

## See Also

- [The Runtime](the-runtime.md) — how Observer and Interpreter work
- [Building Custom Runtimes](../guides/building-custom-runtimes.md) — create your own
- [Tutorials](../tutorials/README.md) — build each pattern step-by-step
