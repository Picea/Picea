# Observer Composition

Recipes for combining observers into pipelines.

## The Combinators

| Combinator | Signature | Behavior |
| ---------- | --------- | -------- |
| `Then` | `Observer → Observer → Observer` | Sequential, short-circuits on error |
| `Where` | `Observer → Predicate → Observer` | Guards with a predicate |
| `Select` | `Observer → Mappers → Observer'` | Transforms inputs (contramap) |
| `Catch` | `Observer → Handler → Observer` | Handles errors |
| `Combine` | `Observer → Observer → Observer` | Sequential, does NOT short-circuit |

## Then (Sequential Pipeline)

```csharp
var pipeline = logger.Then(metrics).Then(persister);
// Runs: logger → metrics → persister
// If logger fails: metrics and persister are skipped
```

`Then` is [Kleisli composition](https://en.wikipedia.org/wiki/Kleisli_category) — monadic function composition. It's associative: `(a.Then(b)).Then(c)` equals `a.Then(b.Then(c))`.

## Where (Guard)

```csharp
// Only log error events
var errorLogger = logger.Where((state, evt, eff) => evt is ErrorOccurred);

// Only persist non-empty effects
var filteredPersist = persister.Where((_, _, eff) => eff is not NoEffect);
```

## Select (Contramap)

Adapt an observer from one type to another by transforming its inputs:

```csharp
// domainLogger works with (DomainState, DomainEvent, DomainEffect)
// We need it for (AppState, AppEvent, AppEffect)

Observer<AppState, AppEvent, AppEffect> appLogger =
    domainLogger.Select(
        mapState: (AppState s) => s.Domain,
        mapEvent: (AppEvent e) => e.Inner,
        mapEffect: (AppEffect eff) => eff.Inner);
```

This is a [contravariant functor](https://ncatlab.org/nlab/show/contravariant+functor) operation — it transforms the *inputs*, not the output.

## Catch (Error Recovery)

```csharp
// Swallow persistence errors
var resilient = persister.Catch(err =>
{
    log.Warning("Persist failed: {Message}", err.Message);
    return Result<Unit, PipelineError>.Ok(Unit.Value);
});

// Transform errors
var tagged = persister.Catch(err =>
    Result<Unit, PipelineError>.Err(
        new PipelineError($"[persist] {err.Message}", "persist", err.Exception)));
```

## Combine (Best-Effort)

```csharp
// Both always run, even if one fails
var bestEffort = persister.Combine(notifier);
// Returns the first error encountered, or Ok if both succeed
```

Use `Combine` when both side effects must execute regardless of individual failures. Use `Then` when failure should stop the pipeline.

## Real-World Pipelines

### Logging + Metrics + Persistence

```csharp
var observer = logger
    .Then(metrics)
    .Then(persister.Catch(err =>
    {
        alertService.Warn($"Persist failed: {err.Message}");
        return Result<Unit, PipelineError>.Ok(Unit.Value);
    }))
    .Then(notifier.Where((_, evt, _) => evt is OrderConfirmed));
```

### Cross-Domain Bridge

```csharp
Observer<OrderState, OrderEvent, OrderEffect> inventoryBridge =
    (async (state, @event, effect) =>
    {
        if (effect is OrderEffect.ItemReserved(var sku, var qty))
        {
            var result = await inventoryRuntime.Dispatch(
                new InventoryEvent.Reserve(sku, qty));
            return result.Map(_ => Unit.Value);
        }
        return Result<Unit, PipelineError>.Ok(Unit.Value);
    });
```

## Interpreter Composition

Interpreters have the same combinators (`Then`, `Where`, `Select`, `Catch`):

```csharp
var interpreter = localHandler.Then(remoteSync);
// Both run; result events are concatenated
```

## See Also

- [Runtime Reference](../reference/runtime.md) — full combinator API
- [Error Handling Patterns](error-handling-patterns.md) — Map/Bind/Catch recipes
- [Building Custom Runtimes](building-custom-runtimes.md) — putting it all together
