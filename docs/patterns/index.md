# Patterns

Production-ready patterns built on top of the Picea kernel.

## Available Patterns

| Pattern | Package | Repository | Description |
| ------- | ------- | ---------- | ----------- |
| **Event Sourcing** | `Picea.Glauca` | [picea/glauca](https://github.com/picea/glauca) | Command-driven aggregates with event persistence |
| **MVU** | `Picea.Abies` | [picea/abies](https://github.com/picea/abies) | Model-View-Update for Blazor |
| **Actor** | `Picea.Rubens` | [picea/rubens](https://github.com/picea/rubens) | Message-driven actors with mailbox |
| **Resilience** | `Picea.Mariana` | [picea/mariana](https://github.com/picea/mariana) | Circuit breakers, retry, bulkhead |

## How Patterns Relate to the Kernel

Each pattern is a specific wiring of the kernel's Observer and Interpreter:

```text
Picea Kernel (Automaton + Runtime + Decider)
    │
    ├── Picea.Glauca (Event Sourcing)
    │   Observer: persist events to store
    │   Interpreter: publish to read models
    │
    ├── Picea.Abies (MVU)
    │   Observer: render view (HTML diff)
    │   Interpreter: execute commands (HTTP, JS interop)
    │
    ├── Picea.Rubens (Actor)
    │   Observer: send messages
    │   Interpreter: route replies
    │
    └── Picea.Mariana (Resilience)
        Middleware patterns for any runtime
```

## See Also

- [Runtimes Compared](../concepts/runtimes-compared.md) — choosing the right pattern
- [The Runtime](../concepts/the-runtime.md) — how Observer and Interpreter work
