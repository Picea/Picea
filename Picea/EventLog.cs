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
}

/// <summary>
/// Append-only event log with replay and persistence APIs.
/// </summary>
/// <typeparam name="TEvent">The event type captured by the log.</typeparam>
public sealed class EventLog<TEvent>
{
    private readonly object _sync = new();
    private readonly List<LogEntry<TEvent>> _entries;
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
                return _entries.Count == 0 ? Array.Empty<LogEntry<TEvent>>() : _entries.ToArray();
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
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(serializer);

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var snapshot = SnapshotOrderedBySequence();

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 16 * 1024, useAsync: true);
        await using var writer = new StreamWriter(stream);

        for (var i = 0; i < snapshot.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(serializer.Serialize(snapshot[i])).ConfigureAwait(false);
        }
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
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(serializer);

        var entries = new List<LogEntry<TEvent>>();

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 16 * 1024, useAsync: true);
        using var reader = new StreamReader(stream);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (line is null)
                break;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var entry = serializer.Deserialize<LogEntry<TEvent>>(line);
            entries.Add(entry);
        }

        var nextSequence = entries.Count is 0
            ? 1
            : entries.Max(static entry => entry.SequenceNumber) + 1;

        return new EventLog<TEvent>(entries, nextSequence);
    }

    private LogEntry<TEvent>[] SnapshotOrderedBySequence()
    {
        lock (_sync)
        {
            if (_entries.Count is 0)
                return Array.Empty<LogEntry<TEvent>>();

            return [.. _entries.OrderBy(static entry => entry.SequenceNumber)];
        }
    }
}
