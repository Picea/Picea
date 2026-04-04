// =============================================================================
// Option — Presence or Absence of a Value
// =============================================================================
// A discriminated union for values that may or may not exist.
//
// Unlike nullable reference types, Option<T> makes optionality explicit in the
// type system and forces callers to handle both cases via Match or TryGetValue.
//
// Algebraic structure:
//     Option<T> ≅ 1 + T    (coproduct / sum type)
//     Map/Select  : (T → U) → Option<T> → Option<U>                   (functor)
//     Bind        : (T → Option<U>) → Option<T> → Option<U>           (monad)
//     Match       : (T → R) × (() → R) → Option<T> → R               (catamorphism)
// =============================================================================


using System.Runtime.CompilerServices;

namespace Picea;
/// <summary>
/// A discriminated union representing either a value (<c>Some</c>) or no value (<c>None</c>).
/// </summary>
/// <typeparam name="T">The type of the contained value.</typeparam>
public readonly struct Option<T>
{
    private readonly bool _isSome;
    private readonly T _value;

    private Option(T value)
    {
        _isSome = true;
        _value = RequireNonNull(value);
    }

    /// <summary>
    /// An option containing no value.
    /// </summary>
    public static Option<T> None => default;

    /// <summary>
    /// Creates an option containing a value.
    /// </summary>
    public static Option<T> Some(T value) => new(value);

    /// <summary>
    /// Whether this option contains a value.
    /// </summary>
    public bool IsSome => _isSome;

    /// <summary>
    /// Whether this option is empty.
    /// </summary>
    public bool IsNone => !_isSome;

    /// <summary>
    /// The contained value. Throws if this is None.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessing Value on a None option.</exception>
    public T Value => _isSome
        ? _value
        : throw new InvalidOperationException("Cannot access Value on a None option.");

    /// <summary>
    /// Maps a function over the contained value (functor).
    /// </summary>
    public Option<TNew> Map<TNew>(Func<T, TNew> f) =>
        _isSome
            ? Option<TNew>.Some(f(_value))
            : Option<TNew>.None;

    /// <summary>
    /// Maps a function over the contained value. LINQ-compatible alias for <see cref="Map{TNew}"/>.
    /// </summary>
    public Option<TNew> Select<TNew>(Func<T, TNew> selector) =>
        Map(selector);

    /// <summary>
    /// Chains a function that returns an Option over the contained value (monad bind).
    /// </summary>
    public Option<TNew> Bind<TNew>(Func<T, Option<TNew>> f) =>
        _isSome
            ? f(_value)
            : Option<TNew>.None;

    /// <summary>
    /// Chains a function that returns an Option, then projects the pair.
    /// LINQ-compatible overload that enables multi-<c>from</c> query syntax.
    /// </summary>
    public Option<TNew> SelectMany<TIntermediate, TNew>(
        Func<T, Option<TIntermediate>> bind,
        Func<T, TIntermediate, TNew> project) =>
        _isSome
            ? bind(_value) switch
            {
                { IsSome: true } intermediate => Option<TNew>.Some(project(_value, intermediate.Value)),
                _ => Option<TNew>.None
            }
            : Option<TNew>.None;

    /// <summary>
    /// Exhaustive fold (catamorphism) over both cases.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> some, Func<TOut> none) =>
        _isSome ? some(_value) : none();

    /// <summary>
    /// Exhaustive side-effecting fold over both cases.
    /// </summary>
    public void Switch(Action<T> some, Action none)
    {
        if (_isSome)
            some(_value);
        else
            none();
    }

    /// <summary>
    /// Attempts to extract the contained value using the Try-pattern.
    /// </summary>
    public bool TryGetValue(out T value)
    {
        value = _value;
        return _isSome;
    }

    /// <summary>
    /// Returns the contained value if Some, or the provided fallback if None.
    /// </summary>
    public T DefaultValue(T fallback) =>
        _isSome ? _value : fallback;

    /// <summary>
    /// Returns the contained value if Some, or the lazily-evaluated fallback if None.
    /// </summary>
    public T DefaultWith(Func<T> fallback) =>
        _isSome ? _value : fallback();

    /// <summary>
    /// Converts this Option to a Result, using the provided error for the None case.
    /// </summary>
    public Result<T, TError> ToResult<TError>(TError error) =>
        _isSome
            ? Result<T, TError>.Ok(_value)
            : Result<T, TError>.Err(error);

    /// <summary>
    /// Filters the contained value by a predicate. Returns None if the predicate is false.
    /// </summary>
    public Option<T> Where(Func<T, bool> predicate) =>
        _isSome && predicate(_value) ? this : None;

    /// <summary>
    /// Implicitly converts a value to a Some option.
    /// </summary>
    public static implicit operator Option<T>(T value) =>
        Some(value);

    /// <inheritdoc/>
    public override string ToString() =>
        _isSome ? $"Some({_value})" : "None";

    private static T RequireNonNull(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        return value;
    }
}

/// <summary>
/// Factory methods for creating <see cref="Option{T}"/> values with type inference.
/// </summary>
public static class Option
{
    /// <summary>
    /// Creates an option containing a value.
    /// </summary>
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);

    /// <summary>
    /// Returns an empty option for the specified type.
    /// </summary>
    public static Option<T> None<T>() => Option<T>.None;
}
