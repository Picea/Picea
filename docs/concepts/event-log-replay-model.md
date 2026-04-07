# Event Log Observer And Replay Model

A replayable event log turns runtime transitions into a deterministic session artifact. The model is simple: capture every dispatched event in order, and fold those events back through the same automaton to rebuild state.

## Why This Matters

A shared event-log model enables one session format for:

- Test harness session loading and deterministic replay
- Visual regression at intermediate states
- Time-travel debugging across process boundaries
- Production bug reproduction from exported sessions

The runtime hook is `Observer<TState, TEvent, TEffect>`, which receives every transition triple after `Transition` runs.

## Runtime Model

At each dispatch step:

1. Runtime executes `Transition(state, event)`
2. Runtime invokes `Observer(state, event, effect)`
3. Observer appends the event to an append-only sequence
4. Replay folds recorded events through `Transition` in order

This preserves determinism as long as transition logic is pure and replay uses the same automaton semantics.

## Implemented API Surface

These APIs are implemented and available:

- `Observer<TState, TEvent, TEffect>`
- `AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>.Dispatch(...)`
- `ObserverExtensions.Then(...)`, `Where(...)`, `Catch(...)`, `Combine(...)`
- `EventLog.Create<TState, TEvent, TEffect>(...)`
- `EventLog<TEvent>.Replay(...)`
- `EventLog<TEvent>.ReplayUntil(...)`
- `EventLog<TEvent>.SaveAsync(path, EventSerializer, ...)`
- `EventLog.LoadAsync<TEvent>(path, EventSerializer, ...)`
- `EventLog<TEvent>.SaveAsync(EventLogStorage<TEvent>, ...)`
- `EventLog<TEvent>.LoadAsync(EventLogStorage<TEvent>, ...)`
- `EventLogStorage<TEvent>`
- `EventLogStorage.JsonLinesFile<TEvent>(...)`
- `EventSerializer`
- `JsonEventSerializer`

See the practical guide: [Event Log Save, Load, And Replay (JSONL)](../guides/event-log-save-load-replay.md).

## Core Replay Semantics

`EventLog<TEvent>` is the core replay model and owns replay semantics:

- Append-only entries with stable `SequenceNumber`
- Full replay through an automaton via `Replay(...)`
- Point-in-time replay via `ReplayUntil(...)`
- Optional per-step callback for time-travel style projections

Core replay semantics are independent from any specific persistence technology.

## Storage Abstraction

Persistence is modeled as a capability, not built into replay itself:

```csharp
public readonly record struct EventLogStorage<TEvent>(
    Func<IAsyncEnumerable<LogEntry<TEvent>>, CancellationToken, ValueTask> SaveEntries,
    Func<CancellationToken, IAsyncEnumerable<LogEntry<TEvent>>> LoadEntries);
```

This keeps the separation explicit:

- `EventLog<TEvent>` defines replay behavior.
- `EventLogStorage<TEvent>` defines where and how entries are persisted.
- `EventSerializer` defines how entries are serialized.

## Default Storage Adapter: JSONL

The default adapter is `EventLogStorage.JsonLinesFile<TEvent>(path, serializer)`, which persists one `LogEntry<TEvent>` per JSON line.

Example line:

```json
{"sequenceNumber":1,"timestamp":"2026-04-06T12:34:56.0000000+00:00","event":{"kind":"Increment"}}
```

Benefits:

- Stream-friendly and append-friendly
- Human-readable for support/debugging workflows
- Easy to process with existing JSON tooling

## Composition Principle

The logging observer remains a normal observer, not a runtime mode. This preserves existing composition semantics:

- `Then` for short-circuit pipelines
- `Where` for filtered logging
- `Catch` for resilient logging
- `Combine` for best-effort multi-observer execution

## See Also

- [The Runtime](the-runtime.md)
- [Observer Composition](../guides/observer-composition.md)
- [Event Log Save, Load, And Replay (JSONL)](../guides/event-log-save-load-replay.md)
