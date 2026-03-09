# ADR 003: Result Type as Algebraic Sum

## Status

Accepted

## Context

The Picea runtime uses `Result<TSuccess, TError>` extensively:
- `Decide` returns `Result<TEvent[], TError>`
- Observer returns `Result<Unit, PipelineError>`
- Interpreter returns `Result<TEvent[], PipelineError>`

We need a Result type that:
- Is zero-allocation (no heap pressure in hot paths)
- Supports functional composition (Map, Bind, MapError)
- Works with C# LINQ query syntax
- Is exhaustive (forces handling both cases)

## Decision

Implement `Result<TSuccess, TError>` as a **readonly struct** with a boolean discriminator:

```csharp
public readonly struct Result<TSuccess, TError>
{
    private readonly bool _isOk;
    private readonly TSuccess _value;
    private readonly TError _error;

    public static Result<TSuccess, TError> Ok(TSuccess value) => ...;
    public static Result<TSuccess, TError> Err(TError error) => ...;

    public bool IsOk => _isOk;
    public bool IsErr => !_isOk;
    public TSuccess Value => _isOk ? _value : throw ...;
    public TError Error => !_isOk ? _error : throw ...;
}
```

Key design choices:

1. **Readonly struct** — Stack-allocated. Each `Result` avoids 24 bytes of heap allocation (object header + method table + field) compared to a class-based discriminated union.

2. **Boolean discriminator** — Not an enum. A `bool` is the smallest possible discriminator for a two-case sum type. No virtual dispatch overhead.

3. **LINQ support via Select and SelectMany** — `Select` = functor map, `SelectMany` = monadic bind. This enables `from ... in ... select` query syntax for composing multiple Result-returning operations.

4. **Throwing Value/Error accessors** — Accessing `Value` on an `Err` throws `InvalidOperationException`. This is intentional: it makes unsafe access explicit and encourages Map/Bind/LINQ patterns instead.

5. **MapError for error transformation** — Enables changing error types across pipeline boundaries (e.g., domain errors → HTTP errors).

## Consequences

### Positive
- **Zero allocation** — `Result` is a value type; no GC pressure
- **LINQ monadic composition** — `from a in x from b in y select a + b` short-circuits on first error
- **Type-safe error handling** — Errors are values, not exceptions; they compose through the type system
- **Railway-oriented programming** — Map/Bind chains provide clear, linear error pipelines

### Negative
- **No exhaustive matching** — C# doesn't enforce exhaustive `switch` on struct properties. You can forget to check `IsErr`.
- **Boxing risk** — If `TSuccess` or `TError` are value types, they're stored inline. But if used as `object` or through interfaces, boxing occurs.
- **Two unused fields** — Every `Result` carries both `_value` and `_error` fields, even though only one is valid. For large value types, this wastes stack space.

## Alternatives Considered

### OneOf / LanguageExt
Use an existing library. Rejected because:
- OneOf doesn't support LINQ syntax
- LanguageExt is a large dependency with many opinions
- Both allocate (class-based)

### C# Discriminated Unions (Language Feature)
Wait for native DU support in C#. Rejected because the timeline is uncertain and we need Result now.

### Exception-Based Error Handling
Use `try/catch` instead of Result. Rejected because:
- Exceptions are expensive (stack unwinding)
- Exceptions don't compose through LINQ
- Exceptions are invisible in type signatures

## References

- [Result type (Rust)](https://doc.rust-lang.org/std/result/)
- [Either monad (Haskell)](https://hackage.haskell.org/package/base/docs/Data-Either.html)
- Wlaschin, S. [Railway-Oriented Programming](https://fsharpforfunandprofit.com/rop/)
