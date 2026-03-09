# ADR 012: LINQ Query Syntax — Remove Match

## Status

Accepted

## Context

The initial `Result<TSuccess, TError>` implementation included a `Match` method:

```csharp
public TResult Match<TResult>(Func<TSuccess, TResult> ok, Func<TError, TResult> err)
```

This is the standard catamorphism (fold) for a sum type. However, C# now has excellent pattern matching via `switch` expressions and `is` patterns.

## Decision

Remove `Match` from `Result<TSuccess, TError>`. Instead, provide `Select` and `SelectMany` for LINQ query syntax, which is the idiomatic C# way to compose monadic operations.

### Rationale

1. **LINQ is more powerful than Match** — Match handles one Result. LINQ composes multiple Results:

```csharp
// Match: handles one Result
var text = result.Match(
    ok: v => $"Got {v}",
    err: e => $"Failed: {e}");

// LINQ: composes multiple Results
var combined =
    from a in Parse("21")
    from b in Parse("21")
    select a + b;
```

2. **C# pattern matching replaces Match** — For simple case analysis, `IsOk`/`IsErr` with conditional expressions is equally readable:

```csharp
var text = result.IsOk ? $"Got {result.Value}" : $"Failed: {result.Error}";
```

3. **Smaller API surface** — Fewer methods means less to learn, less to maintain, fewer overloads to conflict.

4. **Map + Bind + MapError cover all use cases** — `Map` (functor), `Bind` (monad), and `MapError` (bifunctor) are the algebraic operations. `Match` is derivable from these.

## Consequences

### Positive
- **Idiomatic C#** — LINQ query syntax is familiar to C# developers
- **Composable** — `from ... in ... from ... in ... select` composes multiple fallible operations
- **Railway-oriented programming** — Errors short-circuit the entire chain

### Negative
- **Loss of explicit fold** — Developers from F#/Rust may expect `Match`. They can use `IsOk`/`IsErr` with pattern matching instead.

## References

- [LINQ query syntax (MSDN)](https://learn.microsoft.com/en-us/dotnet/csharp/linq/get-started/query-expression-basics)
- [Catamorphism (Wikipedia)](https://en.wikipedia.org/wiki/Catamorphism)
