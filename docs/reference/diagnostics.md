# AutomatonDiagnostics

`namespace Picea`

Static class exposing OpenTelemetry-compatible tracing instrumentation.

## Members

| Member | Type | Description |
| ------ | ---- | ----------- |
| `SourceName` | `string` | The activity source name: `"Picea"`. Use this to subscribe to traces. |
| `Source` | `ActivitySource` | The `System.Diagnostics.ActivitySource` instance. **Internal** — used by the runtime to create spans. |

## Span Names

| Span name | Created by | Description |
| --------- | ---------- | ----------- |
| `Automaton.Start` | `AutomatonRuntime.Start` | Runtime initialization. |
| `Automaton.Dispatch` | `AutomatonRuntime.Dispatch` | Full Transition → Observe → Interpret → Feedback cycle. |
| `Automaton.InterpretEffect` | `AutomatonRuntime.InterpretEffect` | Interpretation of one effect. |
| `Automaton.Decider.Start` | `DecidingRuntime.Start` | Decider runtime initialization. |
| `Automaton.Decider.Handle` | `DecidingRuntime.Handle` | Command handling: Decide → Transition → Observe → Interpret. |

## Tags

| Tag | Type | Added to | Description |
| --- | ---- | -------- | ----------- |
| `automaton.type` | `string` | All spans | The automaton/decider type name. |
| `automaton.state.type` | `string` | Start spans | The state type name. |
| `automaton.event.type` | `string` | Dispatch | The event type name. |
| `automaton.effect.type` | `string` | InterpretEffect | The effect type name. |
| `automaton.command.type` | `string` | Decider.Handle | The command type name. |
| `automaton.result` | `string` | Decider.Handle | `"ok"` or `"error"`. |
| `automaton.error.type` | `string` | Decider.Handle | The error type name (only on rejection). |

## Subscribing to Traces

```csharp
using OpenTelemetry;
using OpenTelemetry.Trace;

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(AutomatonDiagnostics.SourceName)  // "Picea"
    .AddConsoleExporter()
    .Build();
```

## See Also

- [Observability Tutorial](../tutorials/06-observability.md) — end-to-end tracing walkthrough
- [Runtime](runtime.md) — where spans are created
- [Decider](decider.md) — `DecidingRuntime.Handle` spans
