# Concepts

Deep-dive explanations of the core ideas behind Picea. Read these to understand *why* the library is designed the way it is.

> **New here?** Start with the [Quick Start](../getting-started/index.md) for a hands-on introduction, then come back for the theory.

## Core Concepts

| Concept | What It Explains |
| ------- | ---------------- |
| [The Kernel](the-kernel.md) | The Mealy machine interface — `Initialize` + `Transition` |
| [The Runtime](the-runtime.md) | The monadic left fold — Observer + Interpreter |
| [The Decider](the-decider.md) | Command validation — `Decide` + `IsTerminal` |
| [Composition](composition.md) | How automata compose — product, sum, feedback |
| [Runtimes Compared](runtimes-compared.md) | MVU vs Event Sourcing vs Actor — when to use which |
| [Glossary](glossary.md) | Definitions of terms used throughout the docs |

## Reading Order

```text
The Kernel (start here)
    │
    ├─▶ The Runtime (how the kernel executes)
    │       │
    │       └─▶ Runtimes Compared (which pattern fits your problem)
    │
    └─▶ The Decider (adding command validation)
            │
            └─▶ Composition (combining multiple automata)
```

## The One-Sentence Summary

Picea is a [Mealy machine](https://en.wikipedia.org/wiki/Mealy_machine) kernel where **`Transition` is a pure function**, **effects are data**, and the **runtime is a monadic left fold** with composable Observer and Interpreter pipelines.
