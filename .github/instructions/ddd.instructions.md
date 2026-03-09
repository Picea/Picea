---
applyTo: '**'
---

# Functional Domain Modeling (DDD) — Agent Instructions

These instructions capture practical functional domain-driven design and adapt them to this repo's constraints:

* C# (latest features), but **pure functional programming** style (see `csharp.instructions.md`).
* **No OO-first design**: prefer immutable records + pure functions + explicit types.

---

## What "done" looks like

When new domain logic is added/changed, the result should:

* Use **ubiquitous language** names that match the domain.
* Encode invariants using **constrained types** (illegal states unrepresentable).
* Represent workflows as **functions** with explicit inputs/outputs.
* Make errors explicit using **Result/Option**, not exceptions/null.
* Push side effects to the edge via **capability functions**.

---

## Core principles

### 1) Focus on the domain, not the technology

* You are modeling business **capabilities**, not database tables.
* Prefer domain terms over technical terms (no `Manager`, `Helper`, `Util` in the domain).

### 2) Make illegal states unrepresentable

* Replace primitive obsession with constrained types (smart constructors).
* Model mutually exclusive states as **sum types** (discriminated unions).
* Model optional data as **Option**, not null.

### 3) Make workflows explicit

* A workflow is a function from **Command → (Events | Errors)**.
* Use types to make business rules and decision points obvious.
* Workflows should read like a business narrative.

### 4) Push IO to the edges (functional core, imperative shell)

* Domain functions are pure.
* Effects (time, persistence, external services) are supplied as dependencies.

### 5) Errors are part of the domain

* Expected failures are values.
* Use exceptions only for programmer bugs/unrecoverable infrastructure failures.

---

## Modeling with types

### Constrained types (smart constructors)

Use constrained types for domain primitives. Construction validates invariants. If invalid, return error as a value.

### Value objects

Use `record` / `record struct`:

* Immutable
* Equality by value
* No identity

### Entities

Entities have identity + evolving state.

* Use explicit ID types rather than raw `Guid`/`int`.
* Updates return new values (no mutable setters).

### Aggregates

If a set of entities/value objects must be consistent together, model it as an aggregate.

* Only the aggregate enforces its invariants.
* Workflows return updated aggregates and events.

### Domain events

Events are facts in the past tense. Immutable records.

---

## Workflows: command → events (and errors)

### Workflow signature

Prefer signatures that accept explicit capabilities for side effects and return a single meaningful result type.

### Railway-Oriented Programming (ROP)

Use `Result<T, TError>` to short-circuit on errors. Use combinators like `Bind`, `Map`, and `Match`.

---

## Dependencies as capabilities

Pass dependencies as functions ("capabilities"), not via domain-level service objects. No `I*` interface naming.

## Testing practices

- Domain logic is **pure**; tests do not require mocks for pure functions.
- Unit test "smart constructors" heavily (they guard invariants).
- For workflows, fake capabilities and assert on produced events/errors.

## Procedural rules for generated code

1. Start with the domain story: capability → workflow → command/event names.
2. Identify bounded context and keep types inside it.
3. Use constrained types to make illegal states unrepresentable.
4. Model state variants with sum types; pattern match exhaustively.
5. Use `Option<T>` for missing values; avoid null.
6. Use `Result<T, TError>` for expected errors; avoid exceptions.
7. Express behavior as workflows: `Command -> Result<Event(s), Error>` (or updated state + events).
8. Pass IO dependencies as capabilities (functions); wire them at the application edge.
9. Keep DTO mapping at the boundaries; domain types are annotation-free.
10. Add tests (at least one happy path + one edge/invariant case).
