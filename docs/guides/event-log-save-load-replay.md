# Event Log Save, Load, And Replay (JSONL)

This guide shows a practical save/load replay workflow using JSON Lines today, while issue #35 is still in flight.

> **Status:** Transitional guide.
> **Tracking:** TODO(issue #35): replace adapter code with native `EventLog<TEvent>` APIs when shipped.

## When To Use This

Use this pattern when you need deterministic session replay now:

- Reproducing bugs from exported sessions
- Running visual regression from fixed event streams
- Loading test harness sessions in CI

## 1. Define A Log Entry Record

```csharp
public readonly record struct ReplayLogEntry<TEvent>(
    long SequenceNumber,
    DateTimeOffset Timestamp,
    TEvent Event);
```

## 2. Build A Logging Observer

```csharp
var nextSequence = 0L;
var entries = new List<ReplayLogEntry<CounterEvent>>();

Observer<CounterState, CounterEvent, CounterEffect> logObserver =
    (state, @event, effect) =>
    {
        var sequenceNumber = Interlocked.Increment(ref nextSequence);
        entries.Add(new ReplayLogEntry<CounterEvent>(
            sequenceNumber,
            DateTimeOffset.UtcNow,
            @event));

        return PipelineResult.Ok;
    };
```

Compose it with your existing observer pipeline:

```csharp
var observer = logObserver.Then(metricsObserver).Then(renderObserver);
```

## 3. Save As JSONL

```csharp
using System.Text.Json;

static async ValueTask SaveJsonLines<TEvent>(
    string path,
    IReadOnlyList<ReplayLogEntry<TEvent>> entries,
    JsonSerializerOptions? options = null,
    CancellationToken cancellationToken = default)
{
    await using var stream = File.Create(path);
    await using var writer = new StreamWriter(stream);

    foreach (var entry in entries)
    {
        var json = JsonSerializer.Serialize(entry, options);
        await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
    }
}
```

## 4. Load From JSONL

```csharp
using System.Text.Json;

static async ValueTask<IReadOnlyList<ReplayLogEntry<TEvent>>> LoadJsonLines<TEvent>(
    string path,
    JsonSerializerOptions? options = null,
    CancellationToken cancellationToken = default)
{
    var result = new List<ReplayLogEntry<TEvent>>();

    using var stream = File.OpenRead(path);
    using var reader = new StreamReader(stream);

    while (!reader.EndOfStream)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var line = await reader.ReadLineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(line))
            continue;

        var entry = JsonSerializer.Deserialize<ReplayLogEntry<TEvent>>(line, options)
            ?? throw new InvalidOperationException("Unable to deserialize replay log entry.");

        result.Add(entry);
    }

    return result;
}
```

## 5. Replay Through The Automaton

Replay does not need observer side effects, so use a no-op observer/interpreter and dispatch the loaded events in order.

```csharp
var runtime = await AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>
    .Start(
        default,
        observer: (_, _, _) => PipelineResult.Ok,
        interpreter: _ => InterpreterResult<CounterEvent>.Empty);

var loadedEntries = await LoadJsonLines<CounterEvent>("session.jsonl");

foreach (var entry in loadedEntries.OrderBy(x => x.SequenceNumber))
{
    var dispatchResult = await runtime.Dispatch(entry.Event);
    if (dispatchResult.IsErr)
        throw new InvalidOperationException($"Replay failed at sequence #{entry.SequenceNumber}.");
}

var finalState = runtime.State;
```

## Planned Native API Mapping (Issue #35)

When issue #35 lands, this guide should simplify to native methods:

- `EventLog.Create<TState, TEvent, TEffect>()`
- `eventLog.SaveAsync(path, serializer)`
- `EventLog.LoadAsync<TEvent>(path, serializer)`
- `eventLog.Replay<TAutomaton, TParameters>(parameters)`
- `eventLog.ReplayUntil<TAutomaton, TParameters>(parameters, sequenceNumber)`

> **TODO(issue #35):** Replace local helpers (`ReplayLogEntry`, `SaveJsonLines`, `LoadJsonLines`) with final public API examples.

## Common Pitfalls

- Do not depend on local clock ordering alone; replay by `SequenceNumber`.
- Keep transition logic pure; non-deterministic transition code breaks replay determinism.
- Treat serialization failures as data-quality errors and fail fast in CI replay workflows.

## See Also

- [Observer Composition](observer-composition.md)
- [The Runtime](../concepts/the-runtime.md)
- [Event Log Observer And Replay Model](../concepts/event-log-replay-model.md)
- [Issue #35](https://github.com/Picea/Picea/issues/35)
