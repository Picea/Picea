# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for the Picea kernel.

ADRs record significant design decisions with their context, alternatives, and consequences. They serve as institutional memory for why the codebase is shaped the way it is.

## Index

| ADR | Title | Status |
| --- | ----- | ------ |
| [001](001-automaton-kernel-mealy-machine.md) | Automaton Kernel as Mealy Machine | Accepted |
| [002](002-shared-runtime-monadic-left-fold.md) | Shared Runtime as Monadic Left Fold | Accepted |
| [003](003-result-type-algebraic-sum.md) | Result Type as Algebraic Sum | Accepted |
| [004](004-decider-pattern-command-validation.md) | Decider Pattern for Command Validation | Accepted |
| [008](008-production-hardening-thread-safety.md) | Production Hardening: Thread Safety & Cancellation | Accepted |
| [008b](008-monadic-observer-interpreter-pipeline.md) | Monadic Observer/Interpreter Pipeline | Accepted |
| [009](009-opentelemetry-tracing-diagnostics.md) | OpenTelemetry Tracing & Diagnostics | Accepted |
| [010](010-example-runtimes-reference-implementations.md) | Example Runtimes as Reference Implementations | Accepted |
| [011](011-performance-optimizations-allocation-reduction.md) | Performance Optimizations & Allocation Reduction | Accepted |
| [012](012-linq-query-syntax-remove-match.md) | LINQ Query Syntax — Remove Match | Accepted |
| [013](013-command-non-generic-design.md) | Command Non-Generic Design | Accepted |

> **Note:** ADRs 005 (MVU Runtime), 006 (Event Sourcing), and 007 (Actor System) are maintained in their respective repositories:
> - ADR 005 → [picea/abies](https://github.com/picea/abies)
> - ADR 006 → [picea/glauca](https://github.com/picea/glauca)
> - ADR 007 → [picea/rubens](https://github.com/picea/rubens)

## Format

Each ADR follows this structure:

1. **Title** — descriptive name
2. **Status** — Proposed, Accepted, Deprecated, Superseded
3. **Context** — what prompted the decision
4. **Decision** — what we chose
5. **Consequences** — what follows from the decision
6. **Alternatives** — what else was considered
