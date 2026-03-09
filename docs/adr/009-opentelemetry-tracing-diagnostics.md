# ADR 009: OpenTelemetry Tracing & Diagnostics

## Status

Accepted

## Context

Production systems need observability. We need to answer questions like:
- How long does each dispatch take?
- Which commands are being rejected, and why?
- Where in the pipeline are errors occurring?
- How do automaton operations relate to incoming HTTP requests?

## Decision

We instrument the runtime using `System.Diagnostics.ActivitySource` — the .NET standard for distributed tracing, compatible with OpenTelemetry.

### Static Diagnostics Class

```csharp
public static class AutomatonDiagnostics
{
    public const string SourceName = "Picea";

    internal static ActivitySource Source { get; } = new(
        SourceName,
        typeof(AutomatonDiagnostics).Assembly.GetName().Version?.ToString() ?? "0.0.0");
}
```

### Span Design

| Span | Created By | Tags |
| ---- | ---------- | ---- |
| `Automaton.Start` | `AutomatonRuntime.Start` | `automaton.type`, `automaton.state.type` |
| `Automaton.Dispatch` | `AutomatonRuntime.Dispatch` | `automaton.type`, `automaton.event.type` |
| `Automaton.InterpretEffect` | `AutomatonRuntime.InterpretEffect` | `automaton.type`, `automaton.effect.type` |
| `Automaton.Decider.Start` | `DecidingRuntime.Start` | `automaton.type`, `automaton.state.type` |
| `Automaton.Decider.Handle` | `DecidingRuntime.Handle` | `automaton.type`, `automaton.command.type`, `automaton.result`, `automaton.error.type` |

### Key Design Choices

1. **Zero external dependencies** — `System.Diagnostics.ActivitySource` is part of the BCL. No OpenTelemetry SDK package is needed in the library itself. Consumers add the SDK in their host applications.

2. **Zero overhead when disabled** — `ActivitySource.StartActivity()` returns `null` when no listener is registered. All tag-setting uses null-conditional (`?.SetTag`), making the instrumentation a no-op when not observed.

3. **Command rejection is Ok, not Error** — A rejected command is correct business behavior (the system prevented an invalid operation). We set `automaton.result = "error"` as a tag but keep `ActivityStatusCode.Ok`. `ActivityStatusCode.Error` is reserved for infrastructure failures (exceptions, timeouts).

4. **Type names as tags** — We use `typeof(T).Name` for type tags. This is human-readable and stable across refactors (unlike full qualified names). The small cost of `GetType().Name` on events is acceptable because tracing is already opt-in.

## Consequences

### Positive
- **Standard integration** — Works with Jaeger, Zipkin, OTLP, Application Insights, and any OpenTelemetry-compatible collector
- **Zero-cost when off** — No allocation, no string formatting when no listener is registered
- **Testable** — Spans can be captured in-process via `ActivityListener` for assertions
- **Correlatable** — Automaton spans participate in distributed traces alongside HTTP, gRPC, and database spans

### Negative
- **Type name cost** — `GetType().Name` on every dispatch allocates a string. For extremely high-throughput scenarios, this could be cached.
- **Limited context** — We don't include state values in tags (they could be large). This limits trace-based debugging.

## References

- [ActivitySource (MSDN)](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.activitysource)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/)
- [Semantic conventions for general identity attributes](https://opentelemetry.io/docs/specs/semconv/general/attributes/)
