# Team Roster

**Project:** Picea — A minimal, production-hardened Mealy machine kernel for .NET  
**Language:** C# 14 (.NET 10)  
**Paradigm:** Pure functional programming (no OO)  
**Owner:** Maurice  
**Established:** 2026-03-30 (imported, refreshed for Picea)

## Members

| Name | Role | Expertise | Badge |
|------|------|-----------|-------|
| Keaton | 🏗️ Lead | Triage, coordination, unblocking, team flow | 🏗️ |
| Ripley | ⚛️ Architect | System design, domain modeling, technology decisions | ⚛️ |
| Fenster | 🔧 Senior C# Dev | Pure functional C#, domain logic, Mealy machine patterns | 🔧 |
| Dallas | 📊 Performance Engineer | Benchmarking, allocations, hot-path optimization | 📊 |
| Hockney | 🧪 Tester | Test strategy, edge cases, quality gates | 🧪 |
| McManus | 📝 Tech Writer | Docs, guides, API reference, ADRs | 📝 |
| Harper | 🔒 Security Engineer | Threat analysis, dependency audits, CVEs | 🔒 |
| Bailey | ⚙️ DevOps | CI/CD pipelines, releases, automation | ⚙️ |
| Keaton | 👤 Reviewer | Deep code review, architecture validation | 👤 |
| Scribe | 📋 Session Logger | Decisions, memories, orchestration logs | 📋 |
| Ralph | 🔄 Work Monitor | Issue triage, PR tracking, backlog | 🔄 |

## Project Context

**What is Picea?**

Picea is a foundational abstraction library for building event-driven systems on .NET. It provides:

- **The Kernel:** An `Automaton<TState, TEvent, TEffect, TParameters>` interface — pure Mealy machine abstraction. Two static methods, zero dependencies.
- **Target Domains:** MVU runtimes, event-sourced aggregates, actor systems, state machines.
- **Philosophy:** Write domain logic once as a pure transition function `(State × Event) → (State × Effect)`. Plug into any runtime.
- **Language:** C# 14, pure functional style (no OO). Smart constructors, constrained types, Result/Option, sum types, pattern matching.
- **Target Framework:** .NET 10 (LTS).
- **Design Pattern:** Functional core + imperative shell. Domain functions pure, effects at the edges.

**Key Bounded Contexts:**

1. **`Picea` (core)** — Kernel interfaces, Result/Option types, Decider pattern, foundational abstractions
2. **`Picea.Tests`** — Test suite using TUnit, domain examples (Counter, Thermostat)
3. **`Picea.Benchmarks`** — BenchmarkDotNet performance regression detection

## Issue Source

No GitHub issues connected yet. Use `gh issue list` or `.squad/decisions/inbox/` for work items.

## Standards

- **Formatting:** `dotnet format` (verified in CI)
- **Testing:** TUnit (source-generated, parallel, async-first, Native AOT compatible)
- **Documentation:** ADR-driven, inline comments for why (not just what)
- **Code Review:** Conventional Commits PR titles, full test coverage for critical paths
- **Performance:** BenchmarkDotNet for hot paths; measure before optimizing

## Notes

This squad was imported from a previous project (Picea.Abies — a WASM UI framework) and has been refreshed for **Picea Core**. Previous context about Abies, templates, server rendering, or E2E browser testing is **NOT applicable here** and should be evicted from agent histories.
