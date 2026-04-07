# Event Log Save, Load, And Replay (JSONL)

This guide shows save/load/replay with the implemented `EventLog<TEvent>` APIs.

It also shows how to switch storage backends via `EventLogStorage<TEvent>` while keeping replay semantics unchanged.

## When To Use This

Use this pattern when you need deterministic session replay now:

- Reproducing bugs from exported sessions
- Running visual regression from fixed event streams
- Loading test harness sessions in CI

## Core vs Storage Boundary

Keep this distinction clear:

- `EventLog<TEvent>`: append and replay semantics.
- `EventLogStorage<TEvent>`: persistence capability (file, memory, cloud, etc.).
- `EventSerializer`: serialization capability.

The default storage adapter is JSONL via `EventLogStorage.JsonLinesFile<TEvent>(...)`.

## 1. Create Observer + Event Log

```csharp
var (logObserver, eventLog) = EventLog.Create<CounterState, CounterEvent, CounterEffect>();

var observer = logObserver.Then(metricsObserver).Then(renderObserver);
```

`logObserver` is a normal observer and composes with existing observer pipelines.

## 2. Save Using Default JSONL Adapter

```csharp
var serializer = new JsonEventSerializer();
await eventLog.SaveAsync("session.jsonl", serializer);
```

Equivalent explicit storage-capability form:

```csharp
var serializer = new JsonEventSerializer();
var storage = EventLogStorage.JsonLinesFile<CounterEvent>("session.jsonl", serializer);

await eventLog.SaveAsync(storage);
```

## 3. Load From JSONL

```csharp
var serializer = new JsonEventSerializer();
var loadedLog = await EventLog.LoadAsync<CounterEvent>("session.jsonl", serializer);
```

You can also load through an explicit storage capability:

```csharp
var serializer = new JsonEventSerializer();
var storage = EventLogStorage.JsonLinesFile<CounterEvent>("session.jsonl", serializer);
var loadedLog = await EventLog<CounterEvent>.LoadAsync(storage);
```

## 4. Replay Through The Automaton

Replay is deterministic because entries are applied in sequence-number order.

```csharp
var finalState = loadedLog.Replay<Counter, CounterState, CounterEffect, Unit>(default);

var stateAtSequence10 = loadedLog.ReplayUntil<Counter, CounterState, CounterEffect, Unit>(
    default,
    sequenceNumber: 10);
```

Optional per-step callback for projections/debug views:

```csharp
var finalState = loadedLog.Replay<Counter, CounterState, CounterEffect, Unit>(
    default,
    (sequenceNumber, state, @event) =>
    {
        // Project replay progress into diagnostics/UI.
    });
```

## 5. Custom Storage Example

The storage abstraction lets you keep replay semantics while swapping persistence backends.

In-memory example:

```csharp
var persisted = new List<LogEntry<CounterEvent>>();

var memoryStorage = new EventLogStorage<CounterEvent>(
    SaveEntries: async (entries, cancellationToken) =>
    {
        persisted.Clear();
        await foreach (var entry in entries.WithCancellation(cancellationToken))
        {
            persisted.Add(entry);
        }
    },
    LoadEntries: cancellationToken => LoadFromMemory(persisted, cancellationToken));

await eventLog.SaveAsync(memoryStorage);
var loadedLog = await EventLog<CounterEvent>.LoadAsync(memoryStorage);

static async IAsyncEnumerable<LogEntry<CounterEvent>> LoadFromMemory(
    IReadOnlyList<LogEntry<CounterEvent>> entries,
    [EnumeratorCancellation] CancellationToken cancellationToken)
{
    await ValueTask.CompletedTask;

    for (var i = 0; i < entries.Count; i++)
    {
        cancellationToken.ThrowIfCancellationRequested();
        yield return entries[i];
    }
}
```

Cloud-style capability follows the same shape: `SaveEntries` writes each `LogEntry<TEvent>` to your service, `LoadEntries` streams entries back for replay.

## JSONL Shape

Default JSONL storage writes one entry per line:

```json
{"sequenceNumber":1,"timestamp":"2026-04-06T12:34:56.0000000+00:00","event":{"kind":"Increment"}}
```

## Common Pitfalls

- Do not couple replay semantics to backend-specific ordering guarantees.
- Replay by `SequenceNumber`, not by storage insertion metadata.
- Keep transition logic pure; non-deterministic transitions break replay determinism.
- Treat serialization failures as data-quality errors and fail fast in CI replay workflows.

## See Also

- [Observer Composition](observer-composition.md)
- [The Runtime](../concepts/the-runtime.md)
- [Event Log Observer And Replay Model](../concepts/event-log-replay-model.md)
