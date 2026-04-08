namespace Picea.Tests;

public sealed class EventLogTests
{
    [Test]
    public async Task CreateObserver_AppendsDispatchedEventsInOrder()
    {
        var (observer, log) = EventLog.Create<CounterState, CounterEvent, CounterEffect>();

        var runtime = new AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>(
            new CounterState(0),
            observer,
            _ => InterpreterResult<CounterEvent>.Empty);

        _ = await runtime.Dispatch(new CounterEvent.Increment());
        _ = await runtime.Dispatch(new CounterEvent.Increment());
        _ = await runtime.Dispatch(new CounterEvent.Decrement());

        await Assert.That(log.Count).IsEqualTo(3);
        await Assert.That(log[0].SequenceNumber).IsEqualTo(1);
        await Assert.That(log[1].SequenceNumber).IsEqualTo(2);
        await Assert.That(log[2].SequenceNumber).IsEqualTo(3);
        await Assert.That(log[2].Event is CounterEvent.Decrement).IsTrue();
    }

    [Test]
    public async Task Replay_ReconstructsFinalStateFromCapturedEvents()
    {
        var log = await CreateCounterEventLog();

        var finalState = log.Replay<Counter, CounterState, CounterEffect, Unit>(default);

        await Assert.That(finalState.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ReplayUntil_ReconstructsStateAtRequestedSequence()
    {
        var log = await CreateCounterEventLog();

        var stateAtTwo = log.ReplayUntil<Counter, CounterState, CounterEffect, Unit>(default, sequenceNumber: 2);

        await Assert.That(stateAtTwo.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Replay_WithVisitor_VisitsEveryTransitionInOrder()
    {
        var log = await CreateCounterEventLog();
        var visited = new List<(long SequenceNumber, int Count)>();

        var finalState = log.Replay<Counter, CounterState, CounterEffect, Unit>(
            default,
            (sequenceNumber, state, _) => visited.Add((sequenceNumber, state.Count)));

        await Assert.That(finalState.Count).IsEqualTo(1);
        await Assert.That(visited.Count).IsEqualTo(3);
        await Assert.That(visited[0]).IsEqualTo((1L, 1));
        await Assert.That(visited[1]).IsEqualTo((2L, 2));
        await Assert.That(visited[2]).IsEqualTo((3L, 1));
    }

    [Test]
    public async Task SaveLoad_RoundTripsJsonlAndSupportsReplay()
    {
        var serializer = new JsonEventSerializer();
        var (_, log) = EventLog.Create<Unit, DeltaEvent, Unit>();

        log.Append(new DeltaEvent(2), new DateTimeOffset(2026, 4, 6, 10, 0, 0, TimeSpan.Zero));
        log.Append(new DeltaEvent(-1), new DateTimeOffset(2026, 4, 6, 10, 1, 0, TimeSpan.Zero));
        log.Append(new DeltaEvent(5), new DateTimeOffset(2026, 4, 6, 10, 2, 0, TimeSpan.Zero));

        var path = Path.Join(Path.GetTempPath(), $"picea-event-log-{Guid.NewGuid():N}.jsonl");

        try
        {
            await log.SaveAsync(path, serializer);
            var loaded = await EventLog.LoadAsync<DeltaEvent>(path, serializer);

            await Assert.That(loaded.Count).IsEqualTo(3);
            await Assert.That(loaded[0].Event.Delta).IsEqualTo(2);
            await Assert.That(loaded[1].Event.Delta).IsEqualTo(-1);
            await Assert.That(loaded[2].Event.Delta).IsEqualTo(5);

            var replayed = loaded.Replay<DeltaAutomaton, int, Unit, Unit>(default);
            await Assert.That(replayed).IsEqualTo(6);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task SaveLoad_StorageOverloads_RoundTripsJsonlAndSupportsReplay()
    {
        var serializer = new JsonEventSerializer();
        var (_, log) = EventLog.Create<Unit, DeltaEvent, Unit>();

        log.Append(new DeltaEvent(2), new DateTimeOffset(2026, 4, 6, 10, 0, 0, TimeSpan.Zero));
        log.Append(new DeltaEvent(-1), new DateTimeOffset(2026, 4, 6, 10, 1, 0, TimeSpan.Zero));
        log.Append(new DeltaEvent(5), new DateTimeOffset(2026, 4, 6, 10, 2, 0, TimeSpan.Zero));

        var path = Path.Join(Path.GetTempPath(), $"picea-event-log-storage-{Guid.NewGuid():N}.jsonl");
        var storage = EventLogStorage.JsonLinesFile<DeltaEvent>(path, serializer);

        try
        {
            await log.SaveAsync(storage);
            var loaded = await EventLog<DeltaEvent>.LoadAsync(storage);

            await Assert.That(loaded.Count).IsEqualTo(3);
            await Assert.That(loaded[0].Event.Delta).IsEqualTo(2);
            await Assert.That(loaded[1].Event.Delta).IsEqualTo(-1);
            await Assert.That(loaded[2].Event.Delta).IsEqualTo(5);

            var replayed = loaded.Replay<DeltaAutomaton, int, Unit, Unit>(default);
            await Assert.That(replayed).IsEqualTo(6);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task LoadAsync_StorageOverload_ThrowsOnNonPositiveSequenceNumber()
    {
        var storage = new EventLogStorage<DeltaEvent>(
            SaveEntries: static (_, _) => ValueTask.CompletedTask,
            LoadEntries: static _ => InvalidSequenceEntries());

        await Assert.That(() => EventLog<DeltaEvent>.LoadAsync(storage).AsTask())
            .ThrowsExactly<InvalidDataException>();
    }

    [Test]
    public async Task LoadAsync_StorageOverload_ThrowsOnDuplicateSequenceNumber()
    {
        var storage = new EventLogStorage<DeltaEvent>(
            SaveEntries: static (_, _) => ValueTask.CompletedTask,
            LoadEntries: static _ => DuplicateSequenceEntries());

        await Assert.That(() => EventLog<DeltaEvent>.LoadAsync(storage).AsTask())
            .ThrowsExactly<InvalidDataException>();
    }

    [Test]
    public async Task SaveAsync_StorageOverload_RespectsCancellationToken()
    {
        var (_, log) = EventLog.Create<Unit, DeltaEvent, Unit>();
        log.Append(new DeltaEvent(1));

        var storage = new EventLogStorage<DeltaEvent>(
            SaveEntries: static async (entries, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await foreach (var _ in entries.WithCancellation(cancellationToken))
                {
                    // Intentionally drain the stream to exercise cancellation propagation.
                }
            },
            LoadEntries: static _ => EmptyEntries());

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.That(() => log.SaveAsync(storage, cancellationTokenSource.Token).AsTask())
            .ThrowsExactly<OperationCanceledException>();
    }

    [Test]
    public async Task LoadAsync_StorageOverload_RespectsCancellationToken()
    {
        var storage = new EventLogStorage<DeltaEvent>(
            SaveEntries: static (_, _) => ValueTask.CompletedTask,
            LoadEntries: static cancellationToken => CanceledEntries(cancellationToken));

        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.That(() => EventLog<DeltaEvent>.LoadAsync(storage, cancellationTokenSource.Token).AsTask())
            .ThrowsExactly<OperationCanceledException>();
    }

    [Test]
    public async Task ObserverCombinators_Where_CanFilterLoggedEvents()
    {
        var (logObserver, log) = EventLog.Create<CounterState, CounterEvent, CounterEffect>();
        var filtered = logObserver.Where((_, @event, _) => @event is CounterEvent.Increment);

        var runtime = new AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>(
            new CounterState(0),
            filtered,
            _ => InterpreterResult<CounterEvent>.Empty);

        _ = await runtime.Dispatch(new CounterEvent.Increment());
        _ = await runtime.Dispatch(new CounterEvent.Decrement());

        await Assert.That(log.Count).IsEqualTo(1);
        await Assert.That(log[0].Event is CounterEvent.Increment).IsTrue();
    }

    [Test]
    public async Task HashChain_CreateFactory_ProducesComposableObserverAndTracksCurrentHash()
    {
        var (observer, log) = EventLog.CreateHashChain<CounterState, CounterEvent, CounterEffect>();
        var baselineHash = log.CurrentHash;

        var runtime = new AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>(
            new CounterState(0),
            observer,
            _ => InterpreterResult<CounterEvent>.Empty);

        _ = await runtime.Dispatch(new CounterEvent.Increment());
        _ = await runtime.Dispatch(new CounterEvent.Decrement());

        await Assert.That(log.Count).IsEqualTo(2);
        await Assert.That(log.CurrentHash).IsNotEqualTo(baselineHash);
        await Assert.That(log.VerifyChain()).IsTrue();
    }

    [Test]
    public async Task HashChain_VerifyRangeAndAnchor_WorkForValidLog()
    {
        var log = CreateDeltaHashChainLog();

        await Assert.That(log.VerifyChain()).IsTrue();
        await Assert.That(log.VerifyRange(2, 3)).IsTrue();
        await Assert.That(log.VerifyAnchor(log.AnchorHash)).IsTrue();
        await Assert.That(log.VerifyAnchor("not-the-anchor")).IsFalse();
    }

    [Test]
    public async Task HashChain_EmptyLog_VerifyAnchorRequiresAnchorMatch()
    {
        var (_, log) = EventLog.CreateHashChain<Unit, DeltaEvent, Unit>();

        await Assert.That(log.VerifyChain()).IsTrue();
        await Assert.That(log.VerifyAnchor(log.AnchorHash)).IsTrue();
        await Assert.That(log.VerifyAnchor("not-the-anchor")).IsFalse();
    }

    [Test]
    public async Task HashChain_VerifyRange_RejectsInvalidBoundsAndMissingSequences()
    {
        var log = CreateDeltaHashChainLog();

        await Assert.That(log.VerifyRange(0, 1)).IsFalse();
        await Assert.That(log.VerifyRange(3, 2)).IsFalse();
        await Assert.That(log.VerifyRange(1, 99)).IsFalse();
        await Assert.That(log.VerifyRange(99, 100)).IsFalse();
    }

    [Test]
    public async Task HashChain_TamperDetection_ModifiedEntryFailsVerification()
    {
        var serializer = new JsonEventSerializer();
        var source = CreateDeltaHashChainLog(serializer);

        var entries = source.Entries.ToArray();
        entries[1] = entries[1] with { Event = new DeltaEvent(999) };

        var loaded = await HashChainEventLog<DeltaEvent>.LoadAsync(HashChainStorage(entries), serializer);

        await Assert.That(loaded.VerifyChain()).IsFalse();
    }

    [Test]
    public async Task HashChain_TamperDetection_ModifiedEntryFailsRangeAndAnchorVerification()
    {
        var serializer = new JsonEventSerializer();
        var source = CreateDeltaHashChainLog(serializer);

        var entries = source.Entries.ToArray();
        entries[1] = entries[1] with { Event = new DeltaEvent(999) };

        var loaded = await HashChainEventLog<DeltaEvent>.LoadAsync(HashChainStorage(entries), serializer);

        await Assert.That(loaded.VerifyRange(1, 3)).IsFalse();
        await Assert.That(loaded.VerifyAnchor(source.AnchorHash)).IsFalse();
    }

    [Test]
    public async Task HashChain_TamperDetection_InsertedDeletedAndReorderedEntriesFailVerification()
    {
        var serializer = new JsonEventSerializer();
        var source = CreateDeltaHashChainLog(serializer);
        var original = source.Entries.ToArray();

        var inserted = new[]
        {
            original[0],
            original[1],
            original[2],
            new HashChainLogEntry<DeltaEvent>(99, original[2].Timestamp.AddMinutes(1), new DeltaEvent(7), original[2].Hash, original[2].Hash)
        };

        var deleted = new[]
        {
            original[0],
            original[2]
        };

        var reordered = new[]
        {
            original[0],
            original[2],
            original[1]
        };

        await Assert.That(() => HashChainEventLog<DeltaEvent>.LoadAsync(HashChainStorage(inserted), serializer).AsTask())
            .ThrowsExactly<InvalidDataException>();

        await Assert.That(() => HashChainEventLog<DeltaEvent>.LoadAsync(HashChainStorage(deleted), serializer).AsTask())
            .ThrowsExactly<InvalidDataException>();

        await Assert.That(() => HashChainEventLog<DeltaEvent>.LoadAsync(HashChainStorage(reordered), serializer).AsTask())
            .ThrowsExactly<InvalidDataException>();
    }

    [Test]
    public async Task HashChain_SaveLoad_RoundTripsAndPreservesReplayParity()
    {
        var serializer = new JsonEventSerializer();
        var source = CreateDeltaHashChainLog(serializer);
        var path = Path.Join(Path.GetTempPath(), $"picea-hash-chain-log-{Guid.NewGuid():N}.jsonl");

        try
        {
            await source.SaveAsync(path);

            var loaded = await EventLog.LoadHashChainAsync<DeltaEvent>(path, serializer);
            var sourceAsEventLog = source.AsEventLog();
            var loadedAsEventLog = loaded.AsEventLog();

            await Assert.That(loaded.Count).IsEqualTo(source.Count);
            await Assert.That(loaded.VerifyChain()).IsTrue();
            await Assert.That(loaded.CurrentHash).IsEqualTo(source.CurrentHash);

            var sourceReplay = sourceAsEventLog.Replay<DeltaAutomaton, int, Unit, Unit>(default);
            var loadedReplay = loadedAsEventLog.Replay<DeltaAutomaton, int, Unit, Unit>(default);

            await Assert.That(sourceReplay).IsEqualTo(6);
            await Assert.That(loadedReplay).IsEqualTo(sourceReplay);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static async ValueTask<EventLog<CounterEvent>> CreateCounterEventLog()
    {
        var (observer, log) = EventLog.Create<CounterState, CounterEvent, CounterEffect>();

        var runtime = new AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>(
            new CounterState(0),
            observer,
            _ => InterpreterResult<CounterEvent>.Empty);

        _ = await runtime.Dispatch(new CounterEvent.Increment());
        _ = await runtime.Dispatch(new CounterEvent.Increment());
        _ = await runtime.Dispatch(new CounterEvent.Decrement());

        return log;
    }

    private static HashChainEventLog<DeltaEvent> CreateDeltaHashChainLog(EventSerializer? serializer = null)
    {
        var (_, log) = EventLog.CreateHashChain<Unit, DeltaEvent, Unit>(serializer: serializer);

        log.Append(new DeltaEvent(2), new DateTimeOffset(2026, 4, 6, 10, 0, 0, TimeSpan.Zero));
        log.Append(new DeltaEvent(-1), new DateTimeOffset(2026, 4, 6, 10, 1, 0, TimeSpan.Zero));
        log.Append(new DeltaEvent(5), new DateTimeOffset(2026, 4, 6, 10, 2, 0, TimeSpan.Zero));

        return log;
    }

    private static HashChainLogStorage<DeltaEvent> HashChainStorage(IReadOnlyList<HashChainLogEntry<DeltaEvent>> entries) =>
        new(
            SaveEntries: static (_, _) => ValueTask.CompletedTask,
            LoadEntries: _ => ToAsync(entries));

    private static async IAsyncEnumerable<HashChainLogEntry<DeltaEvent>> ToAsync(IReadOnlyList<HashChainLogEntry<DeltaEvent>> entries)
    {
        await ValueTask.CompletedTask;

        for (var i = 0; i < entries.Count; i++)
            yield return entries[i];
    }

    public readonly record struct DeltaEvent(int Delta);

    public sealed class DeltaAutomaton : Automaton<int, DeltaEvent, Unit, Unit>
    {
        public static (int State, Unit Effect) Initialize(Unit _) => (0, Unit.Value);

        public static (int State, Unit Effect) Transition(int state, DeltaEvent @event) =>
            (state + @event.Delta, Unit.Value);
    }

    private static async IAsyncEnumerable<LogEntry<DeltaEvent>> InvalidSequenceEntries()
    {
        await ValueTask.CompletedTask;
        yield return new LogEntry<DeltaEvent>(0, DateTimeOffset.UtcNow, new DeltaEvent(1));
    }

    private static async IAsyncEnumerable<LogEntry<DeltaEvent>> DuplicateSequenceEntries()
    {
        await ValueTask.CompletedTask;
        yield return new LogEntry<DeltaEvent>(1, DateTimeOffset.UtcNow, new DeltaEvent(1));
        yield return new LogEntry<DeltaEvent>(1, DateTimeOffset.UtcNow, new DeltaEvent(2));
    }

    private static async IAsyncEnumerable<LogEntry<DeltaEvent>> EmptyEntries()
    {
        await ValueTask.CompletedTask;
        yield break;
    }

    private static async IAsyncEnumerable<LogEntry<DeltaEvent>> CanceledEntries(CancellationToken cancellationToken)
    {
        await ValueTask.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        yield break;
    }
}
