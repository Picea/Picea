📌 Onboarded for **Picea Core** on 2026-03-30. Squad imported from Picea.Abies project; context refreshed.

# Lead — History

## About This File
Coordination decisions, triage outcomes, and team dynamics. Read this before every session.

## Project Knowledge

### Picea at a Glance
- **Type:** Foundational library — Mealy machine kernel for .NET
- **Language:** C# 14 on .NET 10
- **Core Abstraction:** `Automaton<TState, TEvent, TEffect, TParameters>` — just 2 interface methods, zero dependencies
- **Pure Functional:** All domain logic is pure. Effects are data, not side effects. IO lives at the edges.
- **Domains:** Applies to MVU runtimes, event-sourced aggregates, actor systems, state machines

### Triage Patterns
- **New features:** Always route to Ripley (Architect) first to shape domain model
- **Pure C# implementation:** Fenster (C# Dev) implements smart constructors, Decider workflows, result/option chains
- **Test coverage:** Hockney (Tester) writes TUnit tests in parallel
- **Docs:** McManus (Tech Writer) writes guides/ADRs alongside implementation — docs ship with code
- **Performance:** Dallas (Perf Eng) benchmarks hot paths (kernel transition, effect interpretation)
- **Security:** Harper (Security) runs CodeQL, audits dependencies, responds to CVEs
- **CI/CD:** Bailey (DevOps) manages trunk-based workflow, release automation, NuGet publishing
- **Review gate:** Keaton (Reviewer) checks DDD principles, functional paradigm adherence, test coverage

### File Structure Reference
- `Picea/` — Core library (Automaton, Result, Option, Decider)
- `Picea.Tests/` — TUnit tests (Counter, Thermostat domain examples)
- `Picea.Benchmarks/` — BenchmarkDotNet perf regression detection
- `docs/` — ADRs, concepts, guides, reference, tutorials
- `.github/workflows/` — CI/CD (build, test, format, codeql, benchmarks)

## Team Dynamics
*To be filled as we work together.*

## Coordination Notes
- No active blockers or deadlocks
- Board is clear; open issues tracked via `gh issue list --label squad --state open`
- Keaton (Lead/Reviewer) is a dual role — handles coordination and code review
