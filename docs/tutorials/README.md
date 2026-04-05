# Tutorials

End-to-end walkthroughs that build complete working systems on the Picea kernel. Each tutorial starts from scratch and ends with running, tested code.

> **New to Picea?** Start with the [Quick Start](../getting-started/index.md) for a 5-minute introduction, then come back here for deeper dives.

## Prerequisites

- .NET 10.0 SDK ([installation guide](../getting-started/installation.md))
- `dotnet add package Picea`

## Tutorials

| # | Tutorial | What You'll Build | Concepts Used |
|---|----------|-------------------|---------------|
| 01 | [Getting Started](01-getting-started.md) | A smart thermostat with feedback loop | [Kernel](../concepts/the-kernel.md), [Runtime](../concepts/the-runtime.md) |
| 05 | [Command Validation](05-command-validation.md) | Domain validation with the Decider and Result | [Decider](../concepts/the-decider.md), [Result](../reference/result.md) |
| 06 | [Observability](06-observability.md) | Distributed tracing with zero dependencies | [Diagnostics](../reference/diagnostics.md) |

## Planned Tutorials

The following tutorial topics are planned and currently documented in concepts/guides only:

- 02: MVU Runtime
- 03: Event-Sourced Aggregate
- 04: Actor System

## The Big Idea

Every tutorial builds on the **same kernel** — a [Mealy machine](../concepts/the-kernel.md):

```text
transition : (State × Event) → (State × Effect)
```

You write your domain logic once as a pure transition function. Each tutorial shows a different runtime that executes that function — MVU, Event Sourcing, or Actors — without changing a single line of domain code. See [Runtimes Compared](../concepts/runtimes-compared.md) for help choosing.

## Recommended Reading Order

```text
Getting Started (01)
        │
        ├──► Command Validation (05)
        └──► Observability (06)
```

1. **[Getting Started](01-getting-started.md)** — the kernel and the shared runtime.
2. **[Command Validation](05-command-validation.md)** — baseline Decider plus guarded staged command handling.
3. **[Observability](06-observability.md)** — production tracing across all runtimes.
4. For MVU/Event-Sourced/Actor choices today, read [Runtimes Compared](../concepts/runtimes-compared.md) and [Building Custom Runtimes](../guides/building-custom-runtimes.md).

## What to Read Next

| If you want to… | Read |
| ---------------- | ---- |
| Understand the theory | [Concepts](../concepts/index.md) |
| Combine multiple automata | [Composition](../concepts/composition.md) |
| Solve a specific problem | [How-To Guides](../guides/index.md) |
| Look up an API | [Reference](../reference/index.md) |
| See design rationale | [Architecture Decision Records](../adr/) |
