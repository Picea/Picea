# ADR 008: Production Hardening — Thread Safety & Cancellation

## Status

Accepted

## Context

The initial `AutomatonRuntime` prototype (ADR 002) was not production-ready:
- No thread safety — concurrent `Dispatch` calls could interleave transitions
- No cancellation support — long-running operations couldn't be cancelled
- No null validation — null observers/interpreters silently produced `NullReferenceException`
- No feedback depth limiting — a misconfigured interpreter could stack overflow

## Decision

### Thread Safety

All public entry points (`Start`, `Dispatch`, `InterpretEffect`, `Reset`) are serialized via `SemaphoreSlim(1, 1)`:

```csharp
await _semaphore.WaitAsync(cancellationToken);
try
{
    // transition + observe + interpret
}
finally
{
    _semaphore.Release();
}
```

This is configurable via `threadSafe` parameter (default: `true`). Single-threaded environments like WASM should use `threadSafe: false` to avoid the ~1µs overhead per dispatch.

### Cancellation

`CancellationToken` is threaded through `Start`, `Dispatch`, and `InterpretEffect`. The token is checked at the semaphore acquisition point and before each feedback dispatch.

### Null Validation

Constructor validates observer and interpreter are non-null:

```csharp
ArgumentNullException.ThrowIfNull(observer);
ArgumentNullException.ThrowIfNull(interpreter);
```

### Feedback Depth Limiting

`MaxFeedbackDepth = 64`. Exceeded depth throws a descriptive exception. This prevents infinite loops without limiting legitimate use cases (64 levels of feedback is more than any real domain needs).

## Consequences

### Positive
- **Thread-safe by default** — Concurrent sensor readings, user actions, etc. are safely serialized
- **Cancellable operations** — Long-running interpretation can be cancelled
- **Fast failure** — Null delegates fail at construction, not at first dispatch
- **Bounded recursion** — Misconfigured interpreters don't stack overflow

### Negative
- **Semaphore overhead** — ~1µs per dispatch in single-threaded environments (mitigated by `threadSafe: false`)
- **Deadlock risk** — Calling `Reset` from within an observer callback when `threadSafe: true` will deadlock (documented in API reference)

## References

- [SemaphoreSlim (MSDN)](https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim)
- [CancellationToken best practices](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)
