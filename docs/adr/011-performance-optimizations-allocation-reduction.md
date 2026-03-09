# ADR 011: Performance Optimizations & Allocation Reduction

## Status

Accepted

## Context

The Picea runtime sits in hot paths — every user interaction, every event, every command goes through it. We need to minimize allocation and maximize throughput without sacrificing correctness or readability.

## Decision

We apply targeted optimizations at specific bottleneck points, validated by benchmarks:

### 1. Pre-allocated PipelineResult.Ok

```csharp
public static class PipelineResult
{
    public static readonly ValueTask<Result<Unit, PipelineError>> Ok =
        new(Result<Unit, PipelineError>.Ok(Unit.Value));
}
```

The happy path (observer succeeds) returns a pre-allocated, cached `ValueTask` wrapping a `Result.Ok`. Since both `Result` and `Unit` are readonly structs, and `ValueTask` can wrap synchronous results without heap allocation, this is the zero-alloc fast path.

### 2. Result as Readonly Struct

`Result<TSuccess, TError>` is a `readonly struct` with a boolean discriminator. Each instance avoids 24 bytes of heap allocation compared to a class-based discriminated union. See ADR 003 for full analysis.

### 3. ValueTask Return Types

All async contracts use `ValueTask<T>` instead of `Task<T>`. For synchronous completions (the common case in observers and interpreters), `ValueTask` avoids allocating a `Task` object on the heap.

### 4. Async Elision in Then

The `Then` combinator checks whether the first observer completed synchronously before allocating an async state machine:

```csharp
var firstResult = first(state, @event, effect);
if (firstResult.IsCompleted && firstResult.Result.IsOk)
    return second(state, @event, effect);
```

This avoids the async state machine allocation for the common case where both observers are synchronous.

### 5. ConcatEvents Optimization

The `Then` combinator for interpreters avoids array allocation when one side returns empty:

```csharp
static TEvent[] ConcatEvents(TEvent[] a, TEvent[] b) =>
    a.Length == 0 ? b :
    b.Length == 0 ? a :
    [..a, ..b];
```

## Consequences

### Positive
- **Zero-alloc happy path** — The common case (synchronous observer returning Ok) allocates nothing
- **Measurable improvement** — BenchmarkDotNet confirms elimination of allocations in Observer and Interpreter hot paths
- **No API complexity** — All optimizations are internal; the public API is unchanged

### Negative
- **`ValueTask` constraints** — `ValueTask` should not be awaited multiple times or cached. This is a standard .NET constraint.
- **Complexity in combinators** — Async elision makes `Then` harder to read. Justified by the hot-path allocation savings.

## References

- Toub, S. (2020). [How Async/Await Really Works in C#](https://devblogs.microsoft.com/dotnet/how-async-await-really-works-in-csharp/)
- [ValueTask (MSDN)](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.valuetask-1)
- [BenchmarkDotNet](https://benchmarkdotnet.org/)
