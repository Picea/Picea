# Picea Documentation

Welcome to the Picea kernel documentation. Picea is a .NET library that implements a universal state machine kernel — a [Mealy machine](https://en.wikipedia.org/wiki/Mealy_machine) — that powers multiple runtime patterns with a single interface.

## Quick Navigation

| Section | What You'll Find |
| ------- | ---------------- |
| [Getting Started](getting-started/index.md) | Installation, first automaton, 5-minute quick start |
| [Concepts](concepts/index.md) | The kernel, the runtime, the Decider, composition |
| [Tutorials](tutorials/README.md) | End-to-end walkthroughs building real systems |
| [How-To Guides](guides/index.md) | Recipes for specific tasks |
| [Reference](reference/index.md) | Complete API documentation |
| [Patterns](patterns/index.md) | Production patterns (Event Sourcing, etc.) |
| [ADRs](adr/) | Architecture decision records |

## The Big Idea

Write your domain logic once as a pure function:

```csharp
public static (TState, TEffect) Transition(TState state, TEvent @event) => ...
```

Then run it on any runtime:

| Pattern | Runtime | Observer Does | Interpreter Does |
| ------- | ------- | ------------- | ---------------- |
| **MVU** | [Abies](https://github.com/picea/abies) | Renders view | Executes commands |
| **Event Sourcing** | [Glauca](https://github.com/picea/glauca) | Persists events | Publishes projections |
| **Actor** | [Rubens](https://github.com/picea/rubens) | Sends messages | Routes replies |

Same `Transition` function. Different wiring. Zero code changes.

## Architecture

```text
Automaton<S, E, F, P>              ← kernel interface (pure)
    │
    ├── Decider<S, C, E, F, Err, P>        ← adds command validation
    │
    └── AutomatonRuntime<A, S, E, F, P>    ← executes the loop
            │
            ├── DecidingRuntime<D, S, C, E, F, Err, P>  ← wraps with Handle
            │
            ├── Observer<S, E, F>       ← sees transitions
            │   └── Then, Where, Catch, Combine, Select
            │
            └── Interpreter<F, E>       ← converts effects to feedback
                └── Then, Where, Catch, Select

Result<T, E>    ← zero-alloc discriminated union
Unit            ← replaces void in generics
```

## Documentation Conventions

- **Concepts** explain *why* — the theory and design rationale
- **Tutorials** show *how* — step-by-step walkthroughs
- **Guides** solve *specific problems* — recipes you can apply immediately
- **Reference** documents *what* — complete API signatures and behavior

This follows the [Diataxis](https://diataxis.fr/) documentation framework.

## Additional Reading

- [Event Log Observer And Replay Model](concepts/event-log-replay-model.md) (plain replay semantics and hash-chain integrity boundaries)
- [Event Log Save, Load, And Replay (JSONL)](guides/event-log-save-load-replay.md) (plain mode and tamper-evidence usage examples)
