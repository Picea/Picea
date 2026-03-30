# Team Decisions

**Project:** Picea — Mealy machine kernel for .NET  
**Established:** 2026-03-30

---

## Project Scope

**[Decision]** Picea remains a foundational library — not a full framework. The kernel is intentionally minimal: the `Automaton` interface and supporting types (`Result`, `Option`). Runtime implementations are separate projects or samples.

---

## Language & Paradigm

**[Principle]** Pure functional programming in C#. No OO. Immutable records, pure functions, explicit error handling via `Result<T, TError>` and `Option<T>`. Static abstract members enforce zero instance state.

**[Principle]** C# 14 features mandatory: records as default aggregate shape, pattern matching, switch expressions, required init-only properties, primary constructors for parameter records.

**[Principle]** Smart constructors (private constructor, public `Create`) guard invariants on constrained types (`EmailAddress`, `AggregateId`, `Slug`, etc.).

**[Exception]** Performance-critical hot paths may use imperative style or unsafe code — with extensive comment justification.

---

## Testing & Quality

**[Decision]** TUnit is the test framework — source-generated, parallel by default, async-first, Native AOT compatible. No xUnit, NUnit, or MSTest.

**[Decision]** All critical paths require test coverage. Use the domain examples (Counter, Thermostat) as reference test patterns.

**[Decision]** BenchmarkDotNet for performance regression detection. Allocations and throughput are measured before optimizations.

---

## Documentation

**[Decision]** ADR-driven architecture decisions. Docs ship with code — Tech Writer works in parallel, not after.

**[Decision]** Inline comments explain *why*, not *what*. Code is self-documenting when it models the domain clearly.

**[Principle]** Bounded context names guide namespace structure: `Picea.Commanding.Pipeline`, `Picea.Decider.Validation`, etc. — not `Picea.CommandPipeline` or `Picea.DeciderValidator`.

---

## CI/CD & Release

**[Decision]** Trunk-based development. `main` is always deployable. Feature branches are short-lived (<2 days).

**[Required Checks]**
- ✅ Build & Test (`dotnet build`, `dotnet test`)
- ✅ Format (`dotnet format --verify-no-changes`)
- ✅ CodeQL (security + code quality)
- ✅ At least 1 approval (Keaton/Reviewer)
- ✅ All conversations resolved
- ✅ Branch up-to-date with main

**[Decision]** Linear history enforced. Squash or rebase merging only. No force pushes.

---

## Code Review Gates

**[Decision]** The Reviewer (Keaton) is the final gate. All production code must be reviewed for correctness, adherence to DDD principles, and adherence to the functional paradigm.

**[Decision]** Configuration changes, dependency bumps, documentation-only updates → Lead (Keaton) can approve directly.

**[Decision]** Architectural changes, new types, breaking changes → Architect (Ripley) + Reviewer required.

---

## Nullable Reference Types

**[Principle]** Declare variables non-nullable by default. Check for `null` at entry points (API boundaries, deserialization). Use `is null` and `is not null` for null checks.

**[Principle]** Trust C#'s null annotations. Don't add redundant null guards on types the type system says cannot be null.

---

## Performance & Optimization

**[Decision]** Optimize only on measured evidence. Use BenchmarkDotNet. Track allocations.

**[Principle]** Prefer correctness and clarity over micro-optimization in non-hot paths.

**[Exception]** Hot paths (transition kernel, effect interpretation loop) may use allocations, unsafe code, or imperative patterns — with comprehensive comment justification.

---

## No Active Constraints or Milestones

- Board: Open and clear. See issues via `gh issue list --label squad --state open`.
- Deadlines: None set. Work is capacity-driven.
- Blocked agents: None currently.
