# Result&lt;TSuccess, TError&gt;

`namespace Picea`

A discriminated union representing either a success value or an error. Implemented as a `readonly struct` for zero heap allocation.

## Construction

| Method | Returns |
| ------ | ------- |
| `Result<T, E>.Ok(value)` | Successful result |
| `Result<T, E>.Err(error)` | Failed result |

## Properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `IsOk` | `bool` | Whether this result is a success. |
| `IsErr` | `bool` | Whether this result is an error. |
| `Value` | `TSuccess` | The success value. **Throws** `InvalidOperationException` if Err. |
| `Error` | `TError` | The error value. **Throws** `InvalidOperationException` if Ok. |

## Methods

| Method | Algebraic Name | Signature |
| ------ | -------------- | --------- |
| `Map` / `Select` | Functor | `(T → U) → Result<T, E> → Result<U, E>` |
| `Bind` / `SelectMany` | Monad | `(T → Result<U, E>) → Result<T, E> → Result<U, E>` |
| `MapError` | Bifunctor (right) | `(E → F) → Result<T, E> → Result<T, F>` |

## LINQ Query Syntax

```csharp
var result =
    from user in FindUser(id)
    from order in GetOrder(user.OrderId)
    select new Summary(user.Name, order.Total);
// Result<Summary, Error> — short-circuits on first Err
```

## Implementation Notes

- `Result` is a `readonly struct` — stack-allocated, zero heap allocation.
- A `bool` discriminator replaces virtual dispatch.
- Prefer `IsOk`/`IsErr`, `Map`/`Bind`, or LINQ query syntax for safe handling.

## See Also

- [The Decider](../concepts/the-decider.md) — where Result is used
- [Error Handling Patterns](../guides/error-handling-patterns.md) — Map/Bind/MapError recipes
- [Decider](decider.md) — `Decide` returns `Result<TEvent[], TError>`
