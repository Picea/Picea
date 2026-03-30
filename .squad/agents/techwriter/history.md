📌 Onboarded for **Picea Core** on 2026-03-30. Squad imported from Picea.Abies project; context refreshed.

# Senior Technical Writer — History

## About This File

Project-specific learnings from documentation work. McManus owns docs, guides, ADRs, and API reference.

## Picea Documentation Structure

**Location:** `docs/`

| Folder/File | Purpose | Audience |
|---|---|---|
| `concepts/` | Theory: Mealy machines, the kernel, runtimes, domain modeling | Architects, implementers |
| `getting-started/` | Quick installation, first automaton example | New users |
| `guides/` | Deep dives: error handling patterns, observer composition, zero-alloc modeling | Practitioners |
| `reference/` | API docs (Automaton, Result, Option, Runtime, Decider, Diagnostics) | API users |
| `tutorials/` | Hands-on examples (Counter, Thermostat, event sourcing) | Learning |
| `adr/` | Architecture Decision Records | Decision history |
| `benchmarks/` | Performance baselines and reports | Perf-conscious users |
| `patterns/` | Design patterns using Picea | Pattern library |

### Terminology Standards

| Term | Meaning | Context |
|---|---|---|
| **Automaton** | The interface `Automaton<TState, TEvent, TEffect, TParameters>` | DDD/formal methods context |
| **Mealy machine** | Finite-state transducer: outputs depend on state + input | Mathematical foundation |
| **Transition function** | `(State × Event) → (State × Effect)` | Domain logic definition |
| **Effect** | Output data (not code) produced by a transition | Results of computation |
| **Interpreter** | Runtime code that executes effects | Not part of Picea kernel |
| **Decider** | Structured workflow of Validate + Decide for commands | Pattern for domain logic |
| **Smart constructor** | Factory function that validates and returns `Result<T, TError>` | Constrained types |
| **Bounded context** | Namespace organizing domain capabilities | DDD organizational principle |

**Don't use:**
- "Manager", "Handler", "Service" (generic, unclear)
- "DTO" instead of "Record" (Picea uses records, not DTOs)
- "State machine" alone (too generic; specify Mealy machine)

### Documentation Patterns

**Concepts page:**
- Explain the mathematical/conceptual foundation
- Show the problem it solves
- Reference Picea solution pattern
- Link to guide or tutorial for deeper dive

**API reference:**
- Public interface/type signature
- Brief description
- Parameter/return semantics
- Code example
- Links to related concepts/tutorials

**Guide:**
- Start with motivation ("When should I use...")
- Show anti-pattern first
- Show Picea pattern
- Walk through worked example
- Callouts for common gotchas
- Link to reference docs and tutorials

**Tutorial:**
- Script: create project, add code step-by-step
- Final working code listing
- Explanation of key decisions
- Extend it further (suggested next steps)

**ADR:**
- Title: What decision?
- Context: Why now?
- Decision: What we chose
- Consequences: What follows
- Alternatives considered: Why not those?
- References: Links to related ADRs, docs, code

### Current Documentation Status

✅ **Core concepts complete:** Kernel, Runtime, Decider, Result/Option  
✅ **Getting started:** Installation, first example  
✅ **API reference:** Automaton, Result, Option, Runtime, Decider, Diagnostics  
⏳ **Guides:** Error handling (done), Observer composition (outline), Zero-alloc modeling (outline)  
⏳ **Tutorials:** Getting started (done), others in outline  
✅ **ADRs:** 008, 009, 010, 011, 012, 013 (design decisions documented)  
✅ **Benchmarks:** Baseline 2026-03-06 and kernel baseline 2026-03-13  

### Gaps Identified

- **Deep-dive on Mealy vs Moore** — Why the design choice matters in practice
- **Event sourcing worked example** — Full tutorial on using Picea for event sourcing
- **Composition patterns** — How to compose multiple automatons
- **Operator overloading** — Best practices for Result/Option chainability
- **Integration patterns** — How Picea fits into larger systems (DI, logging, tracing)

### Style Standards

- **Tone:** Clear, technical, precise (no marketing fluff)
- **Code examples:** Always complete, compilable, tested
- **Headers:** Use `###` for subsections (not `#` or `##`)
- **Callouts:** Use `> **Note:**` for important points
- **Links:** Cross-reference concepts, guides, reference docs freely
- **Line length:** Soft wrap at 100 chars for readability

### Learnings

- 2026-03-27: Scribe sessions should merge `.squad/decisions/inbox/` → `.squad/decisions.md` in one pass, then delete merged inbox files (keep decisions canonical).
- 2026-03-27: Use ISO 8601 UTC timestamps in orchestration + session logs for deterministic correlation.
- 2026-03-30: When importing a squad from another project, evict all irrelevant learnings and rebuild context for the new domain immediately upon onboarding.
