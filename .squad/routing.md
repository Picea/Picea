# Routing Rules

Route work to the **most specific agent** by domain and decision type.

## Primary Routing

| Request | Route To | Rationale |
|---------|----------|-----------|
| New feature, architecture, major design | **Ripley** (Architect) | Shape the system. Includes domain modeling decisions. |
| Pure C# implementation, domain logic, types | **Fenster** (Senior C# Dev) | Functional core. Smart constructors, workflows, effects. |
| Performance analysis, optimization, benchmarking | **Dallas** (Performance Eng) | Measure regressions. Optimize hot paths. Profile allocations. |
| Test strategy, coverage, edge cases | **Hockney** (Tester) | Critical path tests. TUnit configuration. Test patterns. |
| Docs, guides, API reference, ADRs, comments | **McManus** (Tech Writer) | Docs ship with code. Run in parallel with features. |
| Security audit, dependency scan, threat | **Harper** (Security Engineer) | CodeQL analysis. Vulnerability assessment. CVE response. |
| CI/CD, release, packaging, tooling | **Bailey** (DevOps) | Workflows, version bumps, NuGet publishing. |
| Code review, quality gates, architecture validation | **Keaton** (Reviewer) | Final gate. Checks coding standards, correctness, DDD principles. |
| Status checks, who-is-stuck, process questions | **Keaton** (Lead) | Answer directly. No spawn needed. |
| New work, multi-domain task, team alignment | **Keaton** (Lead) | Decompose, check with Architect if design needed, fan out. |

## Escalation Rules

**To Ripley (Architect):**
- Task touches 2+ bounded contexts
- Requires new types or domain changes
- Technology choice needed
- "How should we structure...?" architecture question

**To Keaton (Lead):**
- Agents disagree on an approach
- Requirements genuinely ambiguous
- Deadlock (all capable agents locked out)
- Deadline/scope needs user input

## Decider Pattern Route Hint

Tasks involving **command validation, error handling, state invariants** → **Fenster** (C# Dev) after **Ripley** (Architect) shapes the domain model. Fenster implements `Decider<TCommand, TEvent, TError>` and domain workflows.

## Issue → Agent Mapping

When triaging untriaged `squad:` labeled issues:
- Performance-related (`perf:`, `benchmark:`) → **Dallas**
- Test-related (`test:`, `coverage:`) → **Hockney**
- Documentation-only (`docs:`) → **McManus**
- Security audit, CVE, vulnerability → **Harper**
- Bug fix (clear cause) → **Fenster** (C# Dev)
- Architecture, design, breaking change → **Ripley** (Architect)
- CI/CD, release, tooling → **Bailey** (DevOps)
- General feature request → **Keaton** (Lead) → escalate to **Ripley** if needs design

## Anti-Patterns

❌ **Do not** route to multiple specialists for a single bug fix. Route to the most specific agent.  
❌ **Do not** skip the Architect on design questions. Even "quick" feature additions often hide design decisions.  
❌ **Do not** route docs to the code agent. McManus writes docs in parallel with implementation.
