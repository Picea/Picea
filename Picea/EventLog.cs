using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace Picea;

/// <summary>
/// Represents a single immutable event-log entry.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <param name="SequenceNumber">The 1-based event sequence number.</param>
/// <param name="Timestamp">The timestamp when the event was appended.</param>
/// <param name="Event">The captured event payload.</param>
public readonly record struct LogEntry<TEvent>(
    long SequenceNumber,
    DateTimeOffset Timestamp,
    TEvent Event);

/// <summary>
/// Serializer capability for event log persistence.
/// </summary>
public interface EventSerializer
{
    /// <summary>
    /// Serializes a value to text.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <returns>A serialized representation of <paramref name="value"/>.</returns>
    string Serialize<T>(T value);

    /// <summary>
    /// Deserializes text into a value.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="value">The serialized representation.</param>
    /// <returns>The deserialized value.</returns>
    T Deserialize<T>(string value);
}

/// <summary>
/// Default JSON serializer implementation backed by System.Text.Json.
/// </summary>
/// <param name="options">Optional serializer options.</param>
public sealed class JsonEventSerializer(JsonSerializerOptions? options = null) : EventSerializer
{
    private readonly JsonSerializerOptions _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <inheritdoc/>
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, _options);

    /// <inheritdoc/>
    public T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<T>(value, _options)
        ?? throw new InvalidOperationException($"Could not deserialize {typeof(T).Name} from JSON.");
}

/// <summary>
/// Storage capability for persisting and loading event-log entries.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <param name="SaveEntries">Persists the provided entry stream.</param>
/// <param name="LoadEntries">Loads an entry stream from storage.</param>
public readonly record struct EventLogStorage<TEvent>(
    Func<IAsyncEnumerable<LogEntry<TEvent>>, CancellationToken, ValueTask> SaveEntries,
    Func<CancellationToken, IAsyncEnumerable<LogEntry<TEvent>>> LoadEntries);

/// <summary>
/// Factory methods for common event-log storage adapters.
/// </summary>
public static class EventLogStorage
{
    /// <summary>
    /// Creates a JSON Lines file-based storage adapter.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="path">The JSONL file path.</param>
    /// <param name="serializer">The serializer capability.</param>
    /// <returns>A storage adapter backed by a JSONL file.</returns>
    public static EventLogStorage<TEvent> JsonLinesFile<TEvent>(string path, EventSerializer serializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(serializer);

        return new EventLogStorage<TEvent>(
            SaveEntries: async (entries, cancellationToken) =>
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 16 * 1024, useAsync: true);
                await using var writer = new StreamWriter(stream);

                await foreach (var entry in entries.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await writer.WriteLineAsync(serializer.Serialize(entry)).ConfigureAwait(false);
                }
            },
                LoadEntries: cancellationToken => ReadEntries<TEvent>(path, serializer, cancellationToken));
    }

    private static async IAsyncEnumerable<LogEntry<TEvent>> ReadEntries<TEvent>(
        string path,
        EventSerializer serializer,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, useAsync: true);
        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
                yield break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var entry = serializer.Deserialize<LogEntry<TEvent>>(line);
            yield return entry;
        }
    }
}

/// <summary>
/// Static factory and persistence entry points for event logs.
/// </summary>
public static class EventLog
{
    /// <summary>
    /// Creates an append-only event log together with a composable observer.
    /// </summary>
    /// <typeparam name="TState">The runtime state type.</typeparam>
    /// <typeparam name="TEvent">The runtime event type.</typeparam>
    /// <typeparam name="TEffect">The runtime effect type.</typeparam>
    /// <param name="timestampFactory">Optional timestamp factory used for each append.</param>
    /// <returns>A tuple containing the observer and its backing event log.</returns>
    public static (Observer<TState, TEvent, TEffect> Observer, EventLog<TEvent> Log) Create<TState, TEvent, TEffect>(
        Func<DateTimeOffset>? timestampFactory = null) =>
        EventLog<TEvent>.Create<TState, TEffect>(timestampFactory);

    /// <summary>
    /// Creates an append-only hash-chained event log together with a composable observer.
    /// </summary>
    /// <typeparam name="TState">The runtime state type.</typeparam>
    /// <typeparam name="TEvent">The runtime event type.</typeparam>
    /// <typeparam name="TEffect">The runtime effect type.</typeparam>
    /// <param name="serializer">Optional serializer used for hash payload generation and persistence.</param>
    /// <param name="hashing">Optional hashing configuration. SHA-256 is used by default.</param>
    /// <param name="timestampFactory">Optional timestamp factory used for each append.</param>
    /// <returns>A tuple containing the observer and its backing hash-chained event log.</returns>
    public static (Observer<TState, TEvent, TEffect> Observer, HashChainEventLog<TEvent> Log) CreateHashChain<TState, TEvent, TEffect>(
        EventSerializer? serializer = null,
        HashChainOptions? hashing = null,
        Func<DateTimeOffset>? timestampFactory = null) =>
        HashChainEventLog<TEvent>.Create<TState, TEffect>(serializer, hashing, timestampFactory);

    /// <summary>
    /// Loads an event log from a JSON Lines file.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="path">The path to the JSONL file.</param>
    /// <param name="serializer">The serializer capability.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous loading.</param>
    /// <returns>The loaded event log.</returns>
    public static ValueTask<EventLog<TEvent>> LoadAsync<TEvent>(
        string path,
        EventSerializer serializer,
        CancellationToken cancellationToken = default) =>
        EventLog<TEvent>.LoadAsync(path, serializer, cancellationToken);

    /// <summary>
    /// Loads a hash-chained event log from a JSON Lines file.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="path">The path to the JSONL file.</param>
    /// <param name="serializer">The serializer capability.</param>
    /// <param name="hashing">Optional hashing configuration. SHA-256 is used by default.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous loading.</param>
    /// <returns>The loaded hash-chained event log.</returns>
    public static ValueTask<HashChainEventLog<TEvent>> LoadHashChainAsync<TEvent>(
        string path,
        EventSerializer serializer,
        HashChainOptions? hashing = null,
        CancellationToken cancellationToken = default) =>
        HashChainEventLog<TEvent>.LoadAsync(path, serializer, hashing, cancellationToken);
}

/// <summary>
/// Append-only event log with replay and persistence APIs.
/// </summary>
/// <typeparam name="TEvent">The event type captured by the log.</typeparam>
public sealed class EventLog<TEvent>
{
    private readonly Lock _sync = new();
    private readonly List<LogEntry<TEvent>> _entries;
    private LogEntry<TEvent>[] _entriesSnapshot = Array.Empty<LogEntry<TEvent>>();
    private IReadOnlyList<LogEntry<TEvent>> _entriesView = Array.Empty<LogEntry<TEvent>>();
    private bool _snapshotDirty = true;
    private long _nextSequenceNumber;

    /// <summary>
    /// Initializes a new empty event log.
    /// </summary>
    public EventLog() : this([], 1)
    {
    }

    private EventLog(List<LogEntry<TEvent>> entries, long nextSequenceNumber)
    {
        _entries = entries;
        _nextSequenceNumber = nextSequenceNumber;
    }

    /// <summary>
    /// Returns a snapshot of all entries in the log.
    /// </summary>
    public IReadOnlyList<LogEntry<TEvent>> Entries
    {
        get
        {
            lock (_sync)
            {
                RefreshSnapshotIfDirty();
                return _entriesView;
            }
        }
    }

    /// <summary>
    /// Returns the number of entries in the log.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_sync)
                return _entries.Count;
        }
    }

    /// <summary>
    /// Returns the entry at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    public LogEntry<TEvent> this[int index]
    {
        get
        {
            lock (_sync)
                return _entries[index];
        }
    }

    /// <summary>
    /// Appends a new event to the log.
    /// </summary>
    /// <param name="event">The event to append.</param>
    /// <param name="timestamp">Optional timestamp. Uses UTC now when omitted.</param>
    /// <returns>The appended entry.</returns>
    public LogEntry<TEvent> Append(TEvent @event, DateTimeOffset? timestamp = null)
    {
        lock (_sync)
        {
            var entry = new LogEntry<TEvent>(_nextSequenceNumber, timestamp ?? DateTimeOffset.UtcNow, @event);
            _entries.Add(entry);
            _nextSequenceNumber++;
            _snapshotDirty = true;
            return entry;
        }
    }

    /// <summary>
    /// Creates an append-only event log and an observer that appends each transition event.
    /// </summary>
    /// <typeparam name="TState">The observed state type.</typeparam>
    /// <typeparam name="TEffect">The observed effect type.</typeparam>
    /// <param name="timestampFactory">Optional timestamp factory used for each append.</param>
    /// <returns>A tuple containing the observer and the backing event log.</returns>
    public static (Observer<TState, TEvent, TEffect> Observer, EventLog<TEvent> Log) Create<TState, TEffect>(
        Func<DateTimeOffset>? timestampFactory = null)
    {
        var log = new EventLog<TEvent>();
        var now = timestampFactory ?? (() => DateTimeOffset.UtcNow);

        Observer<TState, TEvent, TEffect> observer = (_, @event, _) =>
        {
            log.Append(@event, now());
            return PipelineResult.Ok;
        };

        return (observer, log);
    }

    /// <summary>
    /// Replays the entire log through an automaton.
    /// </summary>
    /// <typeparam name="TAutomaton">The automaton type.</typeparam>
    /// <typeparam name="TState">The automaton state type.</typeparam>
    /// <typeparam name="TEffect">The automaton effect type.</typeparam>
    /// <typeparam name="TParameters">The automaton initialization parameter type.</typeparam>
    /// <param name="parameters">Initialization parameters.</param>
    /// <returns>The reconstructed final state.</returns>
    public TState Replay<TAutomaton, TState, TEffect, TParameters>(TParameters parameters)
        where TAutomaton : Automaton<TState, TEvent, TEffect, TParameters> =>
        Replay<TAutomaton, TState, TEffect, TParameters>(parameters, static (_, _, _) => { });

    /// <summary>
    /// Replays the entire log through an automaton and invokes a visitor at each step.
    /// </summary>
    /// <typeparam name="TAutomaton">The automaton type.</typeparam>
    /// <typeparam name="TState">The automaton state type.</typeparam>
    /// <typeparam name="TEffect">The automaton effect type.</typeparam>
    /// <typeparam name="TParameters">The automaton initialization parameter type.</typeparam>
    /// <param name="parameters">Initialization parameters.</param>
    /// <param name="step">Visitor invoked after each transition.</param>
    /// <returns>The reconstructed final state.</returns>
    public TState Replay<TAutomaton, TState, TEffect, TParameters>(
        TParameters parameters,
        Action<long, TState, TEvent> step)
        where TAutomaton : Automaton<TState, TEvent, TEffect, TParameters>
    {
        var orderedEntries = SnapshotOrderedBySequence();
        var (state, _) = TAutomaton.Initialize(parameters);

        for (var i = 0; i < orderedEntries.Length; i++)
        {
            var entry = orderedEntries[i];
            (state, _) = TAutomaton.Transition(state, entry.Event);
            step(entry.SequenceNumber, state, entry.Event);
        }

        return state;
    }

    /// <summary>
    /// Replays the log until the provided sequence number.
    /// </summary>
    /// <typeparam name="TAutomaton">The automaton type.</typeparam>
    /// <typeparam name="TState">The automaton state type.</typeparam>
    /// <typeparam name="TEffect">The automaton effect type.</typeparam>
    /// <typeparam name="TParameters">The automaton initialization parameter type.</typeparam>
    /// <param name="parameters">Initialization parameters.</param>
    /// <param name="sequenceNumber">Inclusive sequence number upper bound.</param>
    /// <returns>The reconstructed state at the requested point in time.</returns>
    public TState ReplayUntil<TAutomaton, TState, TEffect, TParameters>(
        TParameters parameters,
        long sequenceNumber)
        where TAutomaton : Automaton<TState, TEvent, TEffect, TParameters>
    {
        var orderedEntries = SnapshotOrderedBySequence();
        var (state, _) = TAutomaton.Initialize(parameters);

        for (var i = 0; i < orderedEntries.Length; i++)
        {
            var entry = orderedEntries[i];
            if (entry.SequenceNumber > sequenceNumber)
                break;

            (state, _) = TAutomaton.Transition(state, entry.Event);
        }

        return state;
    }

    /// <summary>
    /// Saves the log to a JSON Lines file.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <param name="serializer">The serializer capability.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous saving.</param>
    public async ValueTask SaveAsync(
        string path,
        EventSerializer serializer,
        CancellationToken cancellationToken = default) =>
        await SaveAsync(EventLogStorage.JsonLinesFile<TEvent>(path, serializer), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Saves the log using the provided storage capability.
    /// </summary>
    /// <param name="storage">The storage capability.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous saving.</param>
    public async ValueTask SaveAsync(
        EventLogStorage<TEvent> storage,
        CancellationToken cancellationToken = default)
    {
        if (storage.SaveEntries is null)
            throw new ArgumentException($"{nameof(EventLogStorage<TEvent>.SaveEntries)} cannot be null.", nameof(storage));

        var snapshot = SnapshotOrderedBySequence();
        await storage.SaveEntries(AsAsyncEnumerable(snapshot, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads an event log from a JSON Lines file.
    /// </summary>
    /// <param name="path">The source path.</param>
    /// <param name="serializer">The serializer capability.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous loading.</param>
    /// <returns>The loaded event log.</returns>
    public static async ValueTask<EventLog<TEvent>> LoadAsync(
        string path,
        EventSerializer serializer,
        CancellationToken cancellationToken = default) =>
        await LoadAsync(EventLogStorage.JsonLinesFile<TEvent>(path, serializer), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Loads an event log using the provided storage capability.
    /// </summary>
    /// <param name="storage">The storage capability.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous loading.</param>
    /// <returns>The loaded event log.</returns>
    /// <exception cref="InvalidDataException">Thrown when sequence numbers are invalid.</exception>
    public static async ValueTask<EventLog<TEvent>> LoadAsync(
        EventLogStorage<TEvent> storage,
        CancellationToken cancellationToken = default)
    {
        if (storage.LoadEntries is null)
            throw new ArgumentException($"{nameof(EventLogStorage<TEvent>.LoadEntries)} cannot be null.", nameof(storage));

        var entries = new List<LogEntry<TEvent>>();
        var sequenceNumbers = new HashSet<long>();

        await foreach (var entry in storage.LoadEntries(cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.SequenceNumber <= 0)
                throw new InvalidDataException($"Invalid sequence number '{entry.SequenceNumber}'. Sequence numbers must be greater than zero.");

            if (!sequenceNumbers.Add(entry.SequenceNumber))
                throw new InvalidDataException($"Duplicate sequence number '{entry.SequenceNumber}' detected while loading the event log.");

            entries.Add(entry);
        }

        var nextSequence = entries.Count is 0
            ? 1
            : entries.Max(static entry => entry.SequenceNumber) + 1;

        entries.Sort(static (left, right) => left.SequenceNumber.CompareTo(right.SequenceNumber));

        return new EventLog<TEvent>(entries, nextSequence);
    }

    internal static EventLog<TEvent> FromEntries(IReadOnlyList<LogEntry<TEvent>> orderedEntries)
    {
        var entries = orderedEntries.Count is 0 ? [] : new List<LogEntry<TEvent>>(orderedEntries.Count);
        var nextSequenceNumber = 1L;

        for (var i = 0; i < orderedEntries.Count; i++)
        {
            var entry = orderedEntries[i];
            entries.Add(entry);

            if (entry.SequenceNumber >= nextSequenceNumber)
                nextSequenceNumber = entry.SequenceNumber + 1;
        }

        return new EventLog<TEvent>(entries, nextSequenceNumber);
    }

    private LogEntry<TEvent>[] SnapshotOrderedBySequence()
    {
        lock (_sync)
        {
            RefreshSnapshotIfDirty();
            return _entriesSnapshot;
        }
    }

    private void RefreshSnapshotIfDirty()
    {
        if (!_snapshotDirty)
            return;

        _entriesSnapshot = _entries.Count is 0 ? Array.Empty<LogEntry<TEvent>>() : [.. _entries];
        _entriesView = new ReadOnlyCollection<LogEntry<TEvent>>(_entriesSnapshot);
        _snapshotDirty = false;
    }

    private static async IAsyncEnumerable<LogEntry<TEvent>> AsAsyncEnumerable(
        IReadOnlyList<LogEntry<TEvent>> entries,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await ValueTask.CompletedTask;

        for (var i = 0; i < entries.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return entries[i];
        }
    }
}
