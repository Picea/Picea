# Event Log Observer And Replay Model

A replayable event log turns runtime transitions into a deterministic session artifact. The model is simple: capture every dispatched event in order, persist those events, and fold them back through the same automaton to rebuild state.

## Why This Matters

A shared event-log model enables one session format for:

- Test harness session loading and deterministic replay
- Visual regression at intermediate states
- Time-travel debugging across process boundaries
- Production bug reproduction from exported sessions

The runtime already provides the core hook: `Observer<TState, TEvent, TEffect>` receives every transition triple after `Transition` runs.

## Runtime Model

At each dispatch step:

1. Runtime executes `Transition(state, event)`
2. Runtime invokes `Observer(state, event, effect)`
3. Observer appends the event to an append-only sequence
4. Replay folds the recorded events through `Transition` in order

This preserves determinism as long as transition logic is pure and replay uses the same automaton semantics.

## Available Today

These APIs exist now and are the current extension points:

- `Observer<TState, TEvent, TEffect>`
- `AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>.Dispatch(...)`
- `ObserverExtensions.Then(...)`, `Where(...)`, `Catch(...)`, `Combine(...)`

You can build a project-local append-only log observer today by composing a custom observer into the pipeline. See the practical guide: [Event Log Save, Load, And Replay (JSONL)](../guides/event-log-save-load-replay.md).

## Planned API Surface (Issue #35)

> **Status:** In flight.
> **Tracking:** TODO(issue #35): implement and align docs when public API lands.

Proposed public API from issue #35:

```csharp
public readonly record struct LogEntry<TEvent>(
    long SequenceNumber,
    DateTimeOffset Timestamp,
    TEvent Event
);

public static class EventLog
{
    public static (Observer<TState, TEvent, TEffect> Observer, EventLog<TEvent> Log)
        Create<TState, TEvent, TEffect>();

    public static ValueTask<EventLog<TEvent>> LoadAsync<TEvent>(
        string path,
        IEventSerializer serializer);
}

public interface IEventSerializer
{
    string Serialize<T>(T value);
    T Deserialize<T>(string value);
}
```

Expected behavior:

- Append-only sequence with stable `SequenceNumber`
- Replay full log to reconstruct final state
- Replay to an intermediate sequence number
- Optional step callback for time-travel style UI
- Save and load with JSON Lines (`.jsonl`)

## JSONL Shape

Issue #35 proposes JSON Lines as default persistence shape: one record per line.

Example line:

```json
{"sequenceNumber":1,"timestamp":"2026-04-06T12:34:56.0000000+00:00","event":{"kind":"Increment"}}
```

Benefits:

- Stream-friendly and append-friendly
- Human-readable for support/debugging workflows
- Easy to process with existing JSON tooling

## Composition Principle

The logging observer should remain a normal observer, not a special runtime mode. That preserves existing composition semantics:

- `Then` for short-circuit pipelines
- `Where` for filtered logging
- `Catch` for resilient logging
- `Combine` for best-effort multi-observer execution

## See Also

- [The Runtime](the-runtime.md)
- [Observer Composition](../guides/observer-composition.md)
- [Event Log Save, Load, And Replay (JSONL)](../guides/event-log-save-load-replay.md)
- [Issue #35](https://github.com/Picea/Picea/issues/35)
