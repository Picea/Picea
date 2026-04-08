using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Picea;

/// <summary>
/// Represents a single immutable hash-chained event-log entry.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <param name="SequenceNumber">The 1-based event sequence number.</param>
/// <param name="Timestamp">The timestamp when the event was appended.</param>
/// <param name="Event">The captured event payload.</param>
/// <param name="PreviousHash">The hash of the previous entry, or the anchor hash for the first entry.</param>
/// <param name="Hash">The hash of this entry.</param>
public readonly record struct HashChainLogEntry<TEvent>(
    long SequenceNumber,
    DateTimeOffset Timestamp,
    TEvent Event,
    string PreviousHash,
    string Hash);

/// <summary>
/// Hashing options for hash-chained event logs.
/// </summary>
/// <param name="ComputeHash">Capability that computes the raw hash bytes for a payload.</param>
/// <param name="EncodeHash">Capability that encodes hash bytes to a stable string representation.</param>
/// <param name="AnchorHash">The chain anchor hash used as previous hash for the first entry.</param>
public readonly record struct HashChainOptions(
    Func<byte[], byte[]> ComputeHash,
    Func<byte[], string> EncodeHash,
    string AnchorHash)
{
    /// <summary>
    /// Creates SHA-256 based hashing options.
    /// </summary>
    /// <param name="anchorHash">Optional anchor hash. Empty string by default.</param>
    /// <returns>SHA-256 hashing options.</returns>
    public static HashChainOptions Sha256(string anchorHash = "") =>
        new(
            ComputeHash: static payload => SHA256.HashData(payload),
            EncodeHash: static hashBytes => Convert.ToHexString(hashBytes),
            AnchorHash: anchorHash ?? string.Empty);
}

/// <summary>
/// Storage capability for persisting and loading hash-chained event-log entries.
/// </summary>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <param name="SaveEntries">Persists the provided hash-chained entry stream.</param>
/// <param name="LoadEntries">Loads a hash-chained entry stream from storage.</param>
public readonly record struct HashChainLogStorage<TEvent>(
    Func<IAsyncEnumerable<HashChainLogEntry<TEvent>>, CancellationToken, ValueTask> SaveEntries,
    Func<CancellationToken, IAsyncEnumerable<HashChainLogEntry<TEvent>>> LoadEntries);

/// <summary>
/// Factory methods for common hash-chained event-log storage adapters.
/// </summary>
public static class HashChainLogStorage
{
    /// <summary>
    /// Creates a JSON Lines file-based storage adapter for hash-chained entries.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="path">The JSONL file path.</param>
    /// <param name="serializer">The serializer capability.</param>
    /// <returns>A storage adapter backed by a JSONL file.</returns>
    public static HashChainLogStorage<TEvent> JsonLinesFile<TEvent>(string path, EventSerializer serializer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(serializer);

        return new HashChainLogStorage<TEvent>(
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

    private static async IAsyncEnumerable<HashChainLogEntry<TEvent>> ReadEntries<TEvent>(
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

            var entry = serializer.Deserialize<HashChainLogEntry<TEvent>>(line);
            yield return entry;
        }
    }
}

/// <summary>
/// Append-only hash-chained event log with replay, verification, and persistence APIs.
/// </summary>
/// <typeparam name="TEvent">The event type captured by the log.</typeparam>
public sealed class HashChainEventLog<TEvent>
{
    private readonly Lock _sync = new();
    private readonly EventSerializer _serializer;
    private readonly HashChainOptions _hashing;
    private readonly List<HashChainLogEntry<TEvent>> _entries;
    private HashChainLogEntry<TEvent>[] _entriesSnapshot = Array.Empty<HashChainLogEntry<TEvent>>();
    private IReadOnlyList<HashChainLogEntry<TEvent>> _entriesView = Array.Empty<HashChainLogEntry<TEvent>>();
    private bool _snapshotDirty = true;
    private string _currentHash;

    /// <summary>
    /// Initializes a new empty hash-chained event log.
    /// </summary>
    /// <param name="serializer">Optional serializer used for hash payload generation and persistence.</param>
    /// <param name="hashing">Optional hashing configuration. SHA-256 is used by default.</param>
    public HashChainEventLog(EventSerializer? serializer = null, HashChainOptions? hashing = null)
    {
        _serializer = serializer ?? new JsonEventSerializer();
        _hashing = ResolveHashing(hashing);
        _entries = [];
        _currentHash = _hashing.AnchorHash;
    }

    private HashChainEventLog(
        List<HashChainLogEntry<TEvent>> entries,
        EventSerializer serializer,
        HashChainOptions hashing)
    {
        _entries = entries;
        _serializer = serializer;
        _hashing = hashing;
        _currentHash = entries.Count is 0 ? hashing.AnchorHash : entries[^1].Hash;
    }

    /// <summary>
    /// Returns a snapshot of all hash-chained entries in the log.
    /// </summary>
    public IReadOnlyList<HashChainLogEntry<TEvent>> Entries
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
    /// Returns the current head hash of the chain.
    /// </summary>
    public string CurrentHash
    {
        get
        {
            lock (_sync)
                return _currentHash;
        }
    }

    /// <summary>
    /// Returns the chain anchor hash.
    /// </summary>
    public string AnchorHash => _hashing.AnchorHash;

    /// <summary>
    /// Returns the entry at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    public HashChainLogEntry<TEvent> this[int index]
    {
        get
        {
            lock (_sync)
                return _entries[index];
        }
    }

    /// <summary>
    /// Appends a new event to the hash-chained log.
    /// </summary>
    /// <param name="event">The event to append.</param>
    /// <param name="timestamp">Optional timestamp. Uses UTC now when omitted.</param>
    /// <returns>The appended hash-chained entry.</returns>
    public HashChainLogEntry<TEvent> Append(TEvent @event, DateTimeOffset? timestamp = null)
    {
        lock (_sync)
        {
            var sequence = _entries.Count is 0 ? 1 : _entries[^1].SequenceNumber + 1;
            var logEntry = new LogEntry<TEvent>(sequence, timestamp ?? DateTimeOffset.UtcNow, @event);
            var previousHash = _currentHash;
            var currentHash = ComputeHash(logEntry, previousHash);

            var entry = new HashChainLogEntry<TEvent>(
                logEntry.SequenceNumber,
                logEntry.Timestamp,
                logEntry.Event,
                previousHash,
                currentHash);

            _entries.Add(entry);
            _currentHash = currentHash;
            _snapshotDirty = true;
            return entry;
        }
    }

    /// <summary>
    /// Creates an append-only hash-chained event log and an observer that appends each transition event.
    /// </summary>
    /// <typeparam name="TState">The observed state type.</typeparam>
    /// <typeparam name="TEffect">The observed effect type.</typeparam>
    /// <param name="serializer">Optional serializer used for hash payload generation and persistence.</param>
    /// <param name="hashing">Optional hashing configuration. SHA-256 is used by default.</param>
    /// <param name="timestampFactory">Optional timestamp factory used for each append.</param>
    /// <returns>A tuple containing the observer and the backing hash-chained event log.</returns>
    public static (Observer<TState, TEvent, TEffect> Observer, HashChainEventLog<TEvent> Log) Create<TState, TEffect>(
        EventSerializer? serializer = null,
        HashChainOptions? hashing = null,
        Func<DateTimeOffset>? timestampFactory = null)
    {
        var log = new HashChainEventLog<TEvent>(serializer, hashing);
        var now = timestampFactory ?? (() => DateTimeOffset.UtcNow);

        Observer<TState, TEvent, TEffect> observer = (_, @event, _) =>
        {
            log.Append(@event, now());
            return PipelineResult.Ok;
        };

        return (observer, log);
    }

    /// <summary>
    /// Converts this hash-chained log into a standard event log snapshot.
    /// </summary>
    /// <returns>A snapshot-compatible event log.</returns>
    public EventLog<TEvent> AsEventLog()
    {
        lock (_sync)
        {
            var entries = _entries.Count is 0
                ? []
                : new List<LogEntry<TEvent>>(_entries.Count);

            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                entries.Add(new LogEntry<TEvent>(entry.SequenceNumber, entry.Timestamp, entry.Event));
            }

            return EventLog<TEvent>.FromEntries(entries);
        }
    }

    /// <summary>
    /// Verifies the full chain from the anchor to the current head.
    /// </summary>
    /// <returns>True when the full chain is valid; otherwise false.</returns>
    public bool VerifyChain()
    {
        lock (_sync)
            return VerifyChainFromAnchor(_hashing.AnchorHash);
    }

    /// <summary>
    /// Verifies the chain segment between two sequence numbers, inclusive.
    /// </summary>
    /// <param name="fromSequenceNumber">The inclusive start sequence number.</param>
    /// <param name="toSequenceNumber">The inclusive end sequence number.</param>
    /// <returns>True when the segment is valid; otherwise false.</returns>
    public bool VerifyRange(long fromSequenceNumber, long toSequenceNumber)
    {
        lock (_sync)
            return VerifyRangeCore(fromSequenceNumber, toSequenceNumber, _hashing.AnchorHash);
    }

    /// <summary>
    /// Verifies the full chain against a provided anchor hash.
    /// </summary>
    /// <param name="expectedAnchorHash">The expected anchor hash.</param>
    /// <returns>True when the chain is valid for the provided anchor; otherwise false.</returns>
    public bool VerifyAnchor(string expectedAnchorHash)
    {
        lock (_sync)
            return VerifyChainFromAnchor(expectedAnchorHash ?? string.Empty);
    }

    /// <summary>
    /// Saves the hash-chained log to a JSON Lines file.
    /// </summary>
    /// <param name="path">The destination path.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous saving.</param>
    public async ValueTask SaveAsync(string path, CancellationToken cancellationToken = default) =>
        await SaveAsync(HashChainLogStorage.JsonLinesFile<TEvent>(path, _serializer), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Saves the hash-chained log using the provided storage capability.
    /// </summary>
    /// <param name="storage">The storage capability.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous saving.</param>
    public async ValueTask SaveAsync(
        HashChainLogStorage<TEvent> storage,
        CancellationToken cancellationToken = default)
    {
        if (storage.SaveEntries is null)
            throw new ArgumentException($"{nameof(HashChainLogStorage<TEvent>.SaveEntries)} cannot be null.", nameof(storage));

        var snapshot = SnapshotEntries();
        await storage.SaveEntries(AsAsyncEnumerable(snapshot, cancellationToken), cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Loads a hash-chained event log from a JSON Lines file.
    /// </summary>
    /// <param name="path">The source path.</param>
    /// <param name="serializer">The serializer capability.</param>
    /// <param name="hashing">Optional hashing configuration. SHA-256 is used by default.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous loading.</param>
    /// <returns>The loaded hash-chained event log.</returns>
    public static async ValueTask<HashChainEventLog<TEvent>> LoadAsync(
        string path,
        EventSerializer serializer,
        HashChainOptions? hashing = null,
        CancellationToken cancellationToken = default) =>
        await LoadAsync(HashChainLogStorage.JsonLinesFile<TEvent>(path, serializer), serializer, hashing, cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// Loads a hash-chained event log using the provided storage capability.
    /// </summary>
    /// <param name="storage">The storage capability.</param>
    /// <param name="serializer">The serializer capability.</param>
    /// <param name="hashing">Optional hashing configuration. SHA-256 is used by default.</param>
    /// <param name="cancellationToken">Cancellation token for asynchronous loading.</param>
    /// <returns>The loaded hash-chained event log.</returns>
    /// <exception cref="InvalidDataException">Thrown when sequence numbers are invalid.</exception>
    public static async ValueTask<HashChainEventLog<TEvent>> LoadAsync(
        HashChainLogStorage<TEvent> storage,
        EventSerializer serializer,
        HashChainOptions? hashing = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        if (storage.LoadEntries is null)
            throw new ArgumentException($"{nameof(HashChainLogStorage<TEvent>.LoadEntries)} cannot be null.", nameof(storage));

        var entries = new List<HashChainLogEntry<TEvent>>();
        var sequenceNumbers = new HashSet<long>();

        await foreach (var entry in storage.LoadEntries(cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.SequenceNumber <= 0)
                throw new InvalidDataException($"Invalid sequence number '{entry.SequenceNumber}'. Sequence numbers must be greater than zero.");

            if (!sequenceNumbers.Add(entry.SequenceNumber))
                throw new InvalidDataException($"Duplicate sequence number '{entry.SequenceNumber}' detected while loading the hash-chained event log.");

            entries.Add(entry);
        }

        return new HashChainEventLog<TEvent>(entries, serializer, ResolveHashing(hashing));
    }

    private static HashChainOptions ResolveHashing(HashChainOptions? hashing)
    {
        var resolved = hashing ?? HashChainOptions.Sha256();

        if (resolved.ComputeHash is null)
            throw new ArgumentException($"{nameof(HashChainOptions.ComputeHash)} cannot be null.", nameof(hashing));

        if (resolved.EncodeHash is null)
            throw new ArgumentException($"{nameof(HashChainOptions.EncodeHash)} cannot be null.", nameof(hashing));

        return resolved with { AnchorHash = resolved.AnchorHash ?? string.Empty };
    }

    private bool VerifyChainFromAnchor(string anchorHash)
    {
        if (_entries.Count is 0)
            return string.Equals(anchorHash, _hashing.AnchorHash, StringComparison.Ordinal);

        if (_entries[0].SequenceNumber != 1)
            return false;

        return VerifyRangeCore(1, _entries[^1].SequenceNumber, anchorHash);
    }

    private bool VerifyRangeCore(long fromSequenceNumber, long toSequenceNumber, string anchorHash)
    {
        if (fromSequenceNumber <= 0 || toSequenceNumber < fromSequenceNumber)
            return false;

        if (_entries.Count is 0)
            return false;

        var startIndex = IndexOfSequence(fromSequenceNumber);
        var endIndex = IndexOfSequence(toSequenceNumber);

        if (startIndex < 0 || endIndex < 0 || endIndex < startIndex)
            return false;

        string previousHash;
        if (fromSequenceNumber is 1)
        {
            previousHash = anchorHash;
        }
        else
        {
            if (startIndex is 0)
                return false;

            var previousEntry = _entries[startIndex - 1];
            if (previousEntry.SequenceNumber != fromSequenceNumber - 1)
                return false;

            previousHash = previousEntry.Hash;
        }

        var expectedSequence = fromSequenceNumber;
        for (var i = startIndex; i <= endIndex; i++)
        {
            var entry = _entries[i];
            if (entry.SequenceNumber != expectedSequence)
                return false;

            if (!string.Equals(entry.PreviousHash, previousHash, StringComparison.Ordinal))
                return false;

            var expectedHash = ComputeHash(
                new LogEntry<TEvent>(entry.SequenceNumber, entry.Timestamp, entry.Event),
                previousHash);

            if (!string.Equals(entry.Hash, expectedHash, StringComparison.Ordinal))
                return false;

            previousHash = entry.Hash;
            expectedSequence++;
        }

        return true;
    }

    private string ComputeHash(LogEntry<TEvent> entry, string previousHash)
    {
        var eventPayload = _serializer.Serialize(entry.Event);
        var payload = string.Create(
            CultureInfo.InvariantCulture,
            $"{entry.SequenceNumber}\n{entry.Timestamp:O}\n{previousHash}\n{eventPayload}");

        var bytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = _hashing.ComputeHash(bytes);
        return _hashing.EncodeHash(hashBytes);
    }

    private int IndexOfSequence(long sequenceNumber)
    {
        for (var i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].SequenceNumber == sequenceNumber)
                return i;
        }

        return -1;
    }

    private HashChainLogEntry<TEvent>[] SnapshotEntries()
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

        _entriesSnapshot = _entries.Count is 0 ? Array.Empty<HashChainLogEntry<TEvent>>() : [.. _entries];
        _entriesView = new ReadOnlyCollection<HashChainLogEntry<TEvent>>(_entriesSnapshot);
        _snapshotDirty = false;
    }

    private static async IAsyncEnumerable<HashChainLogEntry<TEvent>> AsAsyncEnumerable(
        IReadOnlyList<HashChainLogEntry<TEvent>> entries,
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
