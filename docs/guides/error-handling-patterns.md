# Error Handling Patterns

Recipes for working with `Result<TSuccess, TError>` in pipelines.

## The Basics

### Check and Extract

```csharp
var result = await runtime.Handle(new AddItemCommand(sku, qty));

if (result.TryGetValue(out var order))
    Console.WriteLine($"Order: {order}");
else if (result.TryGetError(out var error))
    Console.WriteLine($"Error: {error}");
```

### Pattern Match

```csharp
var message = result.Match(
    ok: value => $"Success: {value}",
    err: error => error switch
    {
        OutOfStock(var sku) => $"Item {sku} is out of stock",
        InvalidQuantity(var qty) => $"Quantity {qty} is invalid",
        _ => $"Unknown error: {error}"
    });
```

## Map (Functor)

Transform the success value, leaving errors untouched:

```csharp
Result<int, string> parsed = Parse("42");      // Ok(42)
Result<string, string> formatted = parsed.Map(n => $"Value: {n}");  // Ok("Value: 42")
```

Errors pass through unchanged:

```csharp
Result<int, string> failed = Parse("abc");     // Err("invalid")
Result<string, string> still = failed.Map(n => $"Value: {n}");      // Err("invalid")
```

## Bind (Monad)

Chain operations that can themselves fail:

```csharp
var result = Parse("42")                        // Result<int, Error>
    .Bind(n => Validate(n))                     // Result<int, Error>
    .Bind(n => Process(n));                     // Result<string, Error>

// Short-circuits on first Err
```

This is **railway-oriented programming**: the happy path flows through Bind; errors derail to the error track.

## LINQ Query Syntax

Result implements `Select` (Map) and `SelectMany` (Bind), enabling LINQ:

```csharp
var result =
    from user in FindUser(userId)               // Result<User, Error>
    from order in GetOrder(user.OrderId)         // Result<Order, Error>
    from item in GetItem(order.ItemId)           // Result<Item, Error>
    select new Summary(user.Name, item.Name, order.Total);

// result is Result<Summary, Error>
// Short-circuits on first Err
```

## MapError

Transform errors when crossing boundaries:

```csharp
// Domain returns domain errors
Result<Order, OrderError> domainResult = await runtime.Handle(command);

// Map to HTTP errors for the API layer
Result<Order, HttpError> httpResult = domainResult.MapError(err => err switch
{
    OrderError.NotFound(var id) => new HttpError(404, $"Order {id} not found"),
    OrderError.OutOfStock(var sku) => new HttpError(409, $"Item {sku} out of stock"),
    _ => new HttpError(500, "Internal error")
});
```

## Observer Error Patterns

### Catch and Swallow

```csharp
var resilient = persister.Catch(err =>
{
    logger.Warning("Persist failed: {Error}", err.Message);
    return Result<Unit, PipelineError>.Ok(Unit.Value); // continue pipeline
});
```

### Catch and Transform

```csharp
var tagged = persister.Catch(err =>
    Result<Unit, PipelineError>.Err(
        new PipelineError($"Persist stage failed: {err.Message}", "persist", err.Exception)));
```

### Then vs Combine

```csharp
// Then: short-circuits (second doesn't run if first fails)
var failFast = persister.Then(notifier);

// Combine: both always run (returns first error)
var bestEffort = persister.Combine(notifier);
```

## Guarded Staged Rejection Patterns

With `GuardedDecidingRuntime`, errors can come from three stages:

- `Validate` rejection (feasibility/invariants)
- `Authorize` rejection (permission/policy)
- `Decide` rejection (domain decision)

Keep these as typed domain errors and map them at boundaries.

```csharp
var result = await guardedRuntime.Handle(principal, command);

var http = result.MapError(error => error switch
{
    CounterError.InvalidAmount => new HttpError(400, "Invalid command payload"),
    CounterError.Unauthorized => new HttpError(403, "Forbidden"),
    CounterError.Overflow(var current, var amount, var max) =>
        new HttpError(409, $"Overflow: {current} + {amount} exceeds {max}"),
    _ => new HttpError(500, "Unhandled domain error")
});
```

Use `DenialObserver` when you need audit trails without changing domain error types:

```csharp
var runtime = await GuardedDecidingRuntime<
    CounterSecure,
    CounterAuthorizationPolicy,
    CounterValidationPolicy,
    CounterPrincipal,
    CounterState,
    CounterCommand,
    CounterEvent,
    CounterEffect,
    CounterError,
    Unit>.Start(
        default,
        observer,
        interpreter,
        denialObserver: (kind, _, _, _, error) =>
        {
            audit.Write($"Guarded denial at {kind}: {error}");
            return ValueTask.CompletedTask;
        });
```

## See Also

- [Result Reference](../reference/result.md) — complete API
- [The Decider](../concepts/the-decider.md) — where Decide returns Result
- [Observer Composition](observer-composition.md) — Then, Catch, Combine recipes
