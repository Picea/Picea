// =============================================================================
// Result — Success or Error
// =============================================================================
// A discriminated union for computations that can fail. Provides functor (Map/Select),
// monad (Bind/SelectMany), and LINQ query syntax support.
//
// Used by the Decider to represent the outcome of command validation:
//
//     Decide : Command → State → Result<Events, Error>
//
// Also used by Observer and Interpreter pipelines to propagate errors as
// values instead of exceptions (railway-oriented programming).
//
// Algebraic structure:
//     Result<T, E> ≅ T + E    (coproduct / sum type)
//     Map/Select       : (T → U) → Result<T, E> → Result<U, E>        (functor)
//     Bind/SelectMany  : (T → Result<U, E>) → Result<T, E> → Result<U, E>  (monad)
//     Match            : (T → R) × (E → R) → Result<T, E> → R         (catamorphism)
//
// LINQ query syntax (monad comprehension):
//     from x in result
//     from y in f(x)
//     select g(x, y)
//
//   desugars to:
//     result.SelectMany(x => f(x), (x, y) => g(x, y))
//
// Implementation note:
//     Result is a readonly struct to avoid heap allocation. Each Ok/Err
//     is stack-allocated, avoiding per-result heap allocations on every Decide
//     and Handle call. The bool discriminator replaces the virtual dispatch
//     of the previous abstract record hierarchy.
// =============================================================================

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Picea;

/// <summary>
/// A discriminated union representing either a success value or an error.
/// </summary>
/// <remarks>
/// <para>
/// Result is the standard functional approach to error handling without exceptions.
/// Callers handle both cases via <see cref="IsOk"/>/<see cref="IsErr"/> properties,
/// C# pattern matching, or LINQ query syntax.
/// </para>
/// <para>
/// Prefer <c>Result</c> over exceptions for expected failures (validation errors,
/// business rule violations). Reserve exceptions for programmer bugs and
/// unrecoverable infrastructure failures.
/// </para>
/// <para>
/// Result is a <c>readonly struct</c> to avoid heap allocation on every Decide
/// and Handle call. Use the static factory methods <see cref="Ok"/> and
/// <see cref="Err"/> to create instances.
/// </para>
/// <para>
/// Supports LINQ query syntax (monad comprehension) via <see cref="Select{TNew}"/>
/// and <see cref="SelectMany{TIntermediate,TNew}"/>. Errors short-circuit the chain.
/// </para>
/// <example>
/// <code>
/// // Exhaustive match (catamorphism) — the recommended approach
/// var message = result.Match(
///     ok: value => $"Got {value}",
///     err: error => $"Failed: {error}");
///
/// // Try-pattern extraction
/// if (result.TryGetValue(out var value))
///     Console.WriteLine(value);
///
/// // LINQ query syntax (railway-oriented programming)
/// var final =
///     from x in parseInput(raw)
///     from y in validate(x)
///     select x + y;
///
/// // Implicit conversion for concise construction
/// Result&lt;int, string&gt; ok = 42;
///
/// // Fluent API
/// result.Map(v =&gt; v * 2)
///       .Bind(v =&gt; validate(v));
/// </code>
/// </example>
/// </remarks>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TError">The type of the error value.</typeparam>
public readonly struct Result<TSuccess, TError>
{
    private readonly bool _isOk;
    private readonly TSuccess _value;
    private readonly TError _error;

    private Result(bool isOk, TSuccess value, TError error)
    {
        _isOk = isOk;
        _value = value;
        _error = error;
    }

    /// <summary>
    /// Creates a successful result containing a value.
    /// </summary>
    public static Result<TSuccess, TError> Ok(TSuccess value) =>
        new(true, RequireNonNull(value), default!);

    /// <summary>
    /// Creates a failed result containing an error.
    /// </summary>
    public static Result<TSuccess, TError> Err(TError error) =>
        new(false, default!, error);

    /// <summary>
    /// Whether this result is a success.
    /// </summary>
    public bool IsOk => _isOk;

    /// <summary>
    /// Whether this result is an error.
    /// </summary>
    public bool IsErr => !_isOk;

    /// <summary>
    /// Legacy success accessor. Prefer <see cref="Match{TOut}(Func{TSuccess, TOut}, Func{TError, TOut})"/>
    /// or <see cref="TryGetValue(out TSuccess)"/> for explicit error-safe handling.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on an Err result.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TSuccess Value => _isOk
        ? _value
        : throw new InvalidOperationException("Cannot access Value on an Err result.");

    /// <summary>
    /// Legacy error accessor. Prefer <see cref="Match{TOut}(Func{TSuccess, TOut}, Func{TError, TOut})"/>
    /// or <see cref="TryGetError(out TError)"/> for explicit error-safe handling.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Error on an Ok result.</exception>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public TError Error => !_isOk
        ? _error
        : throw new InvalidOperationException("Cannot access Error on an Ok result.");

    /// <summary>
    /// Maps a function over the success value (functor).
    /// </summary>
    public Result<TNew, TError> Map<TNew>(Func<TSuccess, TNew> f) =>
        _isOk
            ? Result<TNew, TError>.Ok(f(_value))
            : Result<TNew, TError>.Err(_error);

    /// <summary>
    /// Maps a function over the success value. LINQ-compatible alias for <see cref="Map{TNew}"/>.
    /// </summary>
    public Result<TNew, TError> Select<TNew>(Func<TSuccess, TNew> selector) =>
        Map(selector);

    /// <summary>
    /// Chains a function that returns a Result over the success value (monad bind).
    /// </summary>
    public Result<TNew, TError> Bind<TNew>(Func<TSuccess, Result<TNew, TError>> f) =>
        _isOk
            ? f(_value)
            : Result<TNew, TError>.Err(_error);

    /// <summary>
    /// Chains a function that returns a Result, then projects the pair.
    /// LINQ-compatible overload that enables multi-<c>from</c> query syntax.
    /// </summary>
    public Result<TNew, TError> SelectMany<TIntermediate, TNew>(
        Func<TSuccess, Result<TIntermediate, TError>> bind,
        Func<TSuccess, TIntermediate, TNew> project)
    {
        if (!_isOk)
            return Result<TNew, TError>.Err(_error);

        var value = _value;
        return bind(value).Match(
            intermediate => Result<TNew, TError>.Ok(project(value, intermediate)),
            err => Result<TNew, TError>.Err(err));
    }

    /// <summary>
    /// Maps a function over the error value.
    /// </summary>
    public Result<TSuccess, TNew> MapError<TNew>(Func<TError, TNew> f) =>
        _isOk
            ? Result<TSuccess, TNew>.Ok(_value)
            : Result<TSuccess, TNew>.Err(f(_error));

    /// <summary>
    /// Exhaustive fold (catamorphism) over both cases.
    /// </summary>
    public TOut Match<TOut>(Func<TSuccess, TOut> ok, Func<TError, TOut> err) =>
        _isOk ? ok(_value) : err(_error);

    /// <summary>
    /// Exhaustive side-effecting fold over both cases.
    /// </summary>
    public void Switch(Action<TSuccess> ok, Action<TError> err)
    {
        if (_isOk)
            ok(_value);
        else
            err(_error);
    }

    /// <summary>
    /// Attempts to extract the success value using the Try-pattern.
    /// </summary>
    public bool TryGetValue(out TSuccess value)
    {
        value = _value;
        return _isOk;
    }

    /// <summary>
    /// Attempts to extract the error value using the Try-pattern.
    /// </summary>
    public bool TryGetError(out TError error)
    {
        error = _error;
        return !_isOk;
    }

    /// <summary>
    /// Returns the success value if Ok, or the provided fallback if Err.
    /// </summary>
    public TSuccess DefaultValue(TSuccess fallback) =>
        _isOk ? _value : fallback;

    /// <summary>
    /// Returns the error value if Err, or the provided fallback if Ok.
    /// </summary>
    public TError DefaultError(TError fallback) =>
        _isOk ? fallback : _error;

    /// <summary>
    /// Implicitly converts a success value to an Ok result.
    /// </summary>
    public static implicit operator Result<TSuccess, TError>(TSuccess value) =>
        Ok(value);

    /// <inheritdoc/>
    public override string ToString() =>
        _isOk ? $"Ok({_value})" : $"Err({_error})";

    private static TSuccess RequireNonNull(TSuccess value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }
}

/// <summary>
/// The unit type — a type with exactly one value, used where a success type
/// is required but no meaningful value exists.
/// </summary>
public readonly record struct Unit
{
    /// <summary>
    /// The singleton value of the unit type.
    /// </summary>
    public static readonly Unit Value = default;

    /// <inheritdoc/>
    public override string ToString() => "()";
}
