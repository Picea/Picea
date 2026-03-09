# ADR 008b: Monadic Observer/Interpreter Pipeline

## Status

Accepted

## Context

Observers and interpreters need to be composable. Real systems have multiple concerns:
- Logging
- Persistence
- Metrics
- Notifications
- Error recovery

Each concern should be a separate, focused observer or interpreter that can be combined.

## Decision

We design Observer and Interpreter as **monadic pipelines** using extension methods for composition:

### Observer Combinators

| Combinator | Algebraic Name | Behavior |
| ---------- | -------------- | -------- |
| `Then` | Kleisli composition (`>=>`) | Sequential, short-circuits on error |
| `Where` | Guard / filter | Runs observer only when predicate is true |
| `Select` | Contramap (contravariant functor) | Transforms observer inputs |
| `Catch` | Error recovery | Handles errors, can recover or transform |
| `Combine` | Applicative | Sequential, does **not** short-circuit |

### Interpreter Combinators

Same set: `Then`, `Where`, `Select`, `Catch`.

### Key Design Choices

1. **Delegates, not interfaces** — Lambdas compose more naturally than interface implementations. No class boilerplate.

2. **`Result<Unit, PipelineError>` return type** — Errors propagate as values, not exceptions. The `Unit` type replaces `void` in generic contexts.

3. **`PipelineResult.Ok` pre-allocation** — The happy path (`Ok(Unit.Value)`) is pre-allocated as a static field. Combined with `ValueTask`, this means zero allocation for synchronous observers.

4. **Async elision in `Then`** — When both observers complete synchronously (the common case), `Then` avoids allocating an async state machine:

```csharp
var firstResult = first(state, @event, effect);
if (firstResult.IsCompleted && firstResult.Result.IsOk)
    return second(state, @event, effect);  // no async overhead
```

5. **`Combine` vs `Then`** — `Then` short-circuits (if first fails, second doesn't run). `Combine` always runs both (useful when both side effects must execute regardless of individual failures).

## Consequences

### Positive
- **Composable** — `logger.Then(metrics).Then(persister)` reads as a pipeline
- **Algebraically sound** — `Then` is associative, `Where` distributes over `Then`
- **Zero-alloc happy path** — Pre-allocated `PipelineResult.Ok` + `ValueTask` = no heap allocation
- **Error recovery** — `Catch` enables resilient pipelines without try/catch

### Negative
- **Learning curve** — Kleisli composition and contramaps are unfamiliar to many C# developers
- **Delegate allocation** — Each `Then`/`Where`/`Catch` creates a closure. For very hot paths with many combinators, this can add GC pressure (mitigated by the fact that pipelines are constructed once, not per-dispatch)

## References

- [Kleisli category (Wikipedia)](https://en.wikipedia.org/wiki/Kleisli_category)
- [Contravariant functor (nLab)](https://ncatlab.org/nlab/show/contravariant+functor)
- Wlaschin, S. [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
