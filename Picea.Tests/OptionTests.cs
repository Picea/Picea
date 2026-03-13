// =============================================================================
// Option Tests
// =============================================================================
// Tests for the Option<T> discriminated union, covering factory methods,
// functor (Map/Select), monad (Bind/SelectMany), catamorphism (Match),
// Try-pattern extraction, defaults, filtering, conversion, and implicit ops.
// =============================================================================

namespace Picea.Tests;

/// <summary>
/// Tests for the <see cref="Option{T}"/> discriminated union,
/// covering factory methods, projections, monadic operations, catamorphism,
/// Try-pattern extraction, defaults, filtering, and implicit conversion.
/// </summary>
public sealed class OptionTests
{
    // ── Factory methods ───────────────────────────────────────────────

    [Fact]
    public void Some_creates_option_with_value()
    {
        var option = Option<int>.Some(42);

        Assert.True(option.IsSome);
        Assert.False(option.IsNone);
        Assert.Equal(42, option.Value);
    }

    [Fact]
    public void None_creates_empty_option()
    {
        var option = Option<int>.None;

        Assert.False(option.IsSome);
        Assert.True(option.IsNone);
    }

    [Fact]
    public void Static_Some_creates_option_with_type_inference()
    {
        var option = Option.Some(42);

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Value);
    }

    [Fact]
    public void Static_None_creates_empty_option_with_type_inference()
    {
        var option = Option.None<int>();

        Assert.True(option.IsNone);
    }

    // ── Throwing accessor ─────────────────────────────────────────────

    [Fact]
    public void Value_on_None_throws_InvalidOperationException()
    {
        var option = Option<int>.None;

        var ex = Assert.Throws<InvalidOperationException>(() => option.Value);
        Assert.Contains("None", ex.Message);
    }

    [Fact]
    public void Value_on_Some_returns_contained_value()
    {
        var option = Option<string>.Some("hello");

        Assert.Equal("hello", option.Value);
    }

    // ── Match (catamorphism) ──────────────────────────────────────────

    [Fact]
    public void Match_invokes_some_branch_on_Some()
    {
        var option = Option<int>.Some(42);

        var message = option.Match(
            some: v => $"value={v}",
            none: () => "empty");

        Assert.Equal("value=42", message);
    }

    [Fact]
    public void Match_invokes_none_branch_on_None()
    {
        var option = Option<int>.None;

        var message = option.Match(
            some: v => $"value={v}",
            none: () => "empty");

        Assert.Equal("empty", message);
    }

    [Fact]
    public void Match_supports_type_coercion_through_common_base()
    {
        var some = Option<int>.Some(1);
        var none = Option<int>.None;

        object someResult = some.Match<object>(some: v => v, none: () => "nothing");
        object noneResult = none.Match<object>(some: v => v, none: () => "nothing");

        Assert.Equal(1, someResult);
        Assert.Equal("nothing", noneResult);
    }

    // ── Switch (void catamorphism) ───────────────────────────────────

    [Fact]
    public void Switch_invokes_some_action_on_Some()
    {
        var option = Option<int>.Some(42);
        var captured = 0;

        option.Switch(
            some: v => captured = v,
            none: () => captured = -1);

        Assert.Equal(42, captured);
    }

    [Fact]
    public void Switch_invokes_none_action_on_None()
    {
        var option = Option<int>.None;
        var captured = false;

        option.Switch(
            some: _ => captured = false,
            none: () => captured = true);

        Assert.True(captured);
    }

    // ── TryGetValue ──────────────────────────────────────────────────

    [Fact]
    public void TryGetValue_returns_true_and_value_on_Some()
    {
        var option = Option<int>.Some(42);

        Assert.True(option.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public void TryGetValue_returns_false_on_None()
    {
        var option = Option<int>.None;

        Assert.False(option.TryGetValue(out _));
    }

    // ── DefaultValue ─────────────────────────────────────────────────

    [Fact]
    public void DefaultValue_returns_value_on_Some()
    {
        var option = Option<int>.Some(42);

        Assert.Equal(42, option.DefaultValue(0));
    }

    [Fact]
    public void DefaultValue_returns_fallback_on_None()
    {
        var option = Option<int>.None;

        Assert.Equal(-1, option.DefaultValue(-1));
    }

    // ── DefaultWith ──────────────────────────────────────────────────

    [Fact]
    public void DefaultWith_returns_value_on_Some()
    {
        var option = Option<int>.Some(42);

        Assert.Equal(42, option.DefaultWith(() => 0));
    }

    [Fact]
    public void DefaultWith_returns_lazy_fallback_on_None()
    {
        var option = Option<int>.None;

        Assert.Equal(-1, option.DefaultWith(() => -1));
    }

    [Fact]
    public void DefaultWith_does_not_evaluate_fallback_on_Some()
    {
        var option = Option<int>.Some(42);
        var evaluated = false;

        option.DefaultWith(() =>
        {
            evaluated = true;
            return 0;
        });

        Assert.False(evaluated);
    }

    // ── Map / Select (functor) ───────────────────────────────────────

    [Fact]
    public void Map_transforms_Some_value()
    {
        var option = Option<int>.Some(21).Map(v => v * 2);

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Value);
    }

    [Fact]
    public void Map_propagates_None()
    {
        var option = Option<int>.None.Map(v => v * 2);

        Assert.True(option.IsNone);
    }

    [Fact]
    public void Select_is_LINQ_alias_for_Map()
    {
        var option = from v in Option<int>.Some(21) select v * 2;

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Value);
    }

    [Fact]
    public void Select_propagates_None()
    {
        var option = from v in Option<int>.None select v * 2;

        Assert.True(option.IsNone);
    }

    // ── Bind (monad) ─────────────────────────────────────────────────

    [Fact]
    public void Bind_chains_Some_to_Some()
    {
        var option = Option<int>.Some(21)
            .Bind(v => Option<int>.Some(v * 2));

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Value);
    }

    [Fact]
    public void Bind_chains_Some_to_None()
    {
        var option = Option<int>.Some(21)
            .Bind(_ => Option<int>.None);

        Assert.True(option.IsNone);
    }

    [Fact]
    public void Bind_short_circuits_on_None()
    {
        var called = false;
        var option = Option<int>.None
            .Bind(v =>
            {
                called = true;
                return Option<int>.Some(v);
            });

        Assert.True(option.IsNone);
        Assert.False(called);
    }

    // ── SelectMany (LINQ multi-from) ─────────────────────────────────

    [Fact]
    public void SelectMany_supports_LINQ_query_syntax()
    {
        var option =
            from x in Option<int>.Some(10)
            from y in Option<int>.Some(32)
            select x + y;

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Value);
    }

    [Fact]
    public void SelectMany_short_circuits_on_first_None()
    {
        var secondCalled = false;

        var option =
            from x in Option<int>.None
            from y in Invoke(() => { secondCalled = true; return Option<int>.Some(2); })
            select x + y;

        Assert.True(option.IsNone);
        Assert.False(secondCalled);
    }

    [Fact]
    public void SelectMany_short_circuits_on_second_None()
    {
        var option =
            from x in Option<int>.Some(1)
            from y in Option<int>.None
            select x + y;

        Assert.True(option.IsNone);
    }

    [Fact]
    public void SelectMany_three_from_clauses()
    {
        var option =
            from a in Option<int>.Some(1)
            from b in Option<int>.Some(2)
            from c in Option<int>.Some(3)
            select a + b + c;

        Assert.True(option.IsSome);
        Assert.Equal(6, option.Value);
    }

    // ── Where (filter) ───────────────────────────────────────────────

    [Fact]
    public void Where_retains_Some_when_predicate_passes()
    {
        var option = Option<int>.Some(42).Where(v => v > 10);

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Value);
    }

    [Fact]
    public void Where_returns_None_when_predicate_fails()
    {
        var option = Option<int>.Some(5).Where(v => v > 10);

        Assert.True(option.IsNone);
    }

    [Fact]
    public void Where_returns_None_on_None_input()
    {
        var option = Option<int>.None.Where(v => v > 10);

        Assert.True(option.IsNone);
    }

    // ── ToResult ─────────────────────────────────────────────────────

    [Fact]
    public void ToResult_converts_Some_to_Ok()
    {
        var result = Option<int>.Some(42).ToResult("missing");

        Assert.True(result.IsOk);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void ToResult_converts_None_to_Err()
    {
        var result = Option<int>.None.ToResult("missing");

        Assert.True(result.IsErr);
        Assert.Equal("missing", result.Error);
    }

    // ── Implicit conversion ──────────────────────────────────────────

    [Fact]
    public void Implicit_conversion_from_T_creates_Some()
    {
        Option<int> option = 42;

        Assert.True(option.IsSome);
        Assert.Equal(42, option.Value);
    }

    [Fact]
    public void Implicit_conversion_works_in_method_return()
    {
        static Option<string> GetName() => "Alice";

        var option = GetName();

        Assert.True(option.IsSome);
        Assert.Equal("Alice", option.Value);
    }

    // ── ToString ─────────────────────────────────────────────────────

    [Fact]
    public void ToString_Some_includes_value() =>
        Assert.Equal("Some(42)", Option<int>.Some(42).ToString());

    [Fact]
    public void ToString_None_returns_None() =>
        Assert.Equal("None", Option<int>.None.ToString());

    // ── Composition: Match + Map + Bind ──────────────────────────────

    [Fact]
    public void Full_pipeline_with_Match_extraction()
    {
        var output = Option<int>.Some(10)
            .Map(v => v * 2)
            .Bind(v => v > 15
                ? Option<string>.Some($"big: {v}")
                : Option<string>.None)
            .Match(
                some: v => v,
                none: () => "too small");

        Assert.Equal("big: 20", output);
    }

    [Fact]
    public void Full_pipeline_none_path()
    {
        var output = Option<int>.Some(5)
            .Map(v => v * 2)
            .Bind(v => v > 15
                ? Option<string>.Some($"big: {v}")
                : Option<string>.None)
            .Match(
                some: v => v,
                none: () => "too small");

        Assert.Equal("too small", output);
    }

    [Fact]
    public void Where_into_Map_pipeline()
    {
        var output = Option<int>.Some(42)
            .Where(v => v > 10)
            .Map(v => $"valid: {v}")
            .DefaultValue("invalid");

        Assert.Equal("valid: 42", output);
    }

    [Fact]
    public void Where_filters_then_DefaultValue()
    {
        var output = Option<int>.Some(5)
            .Where(v => v > 10)
            .Map(v => $"valid: {v}")
            .DefaultValue("invalid");

        Assert.Equal("invalid", output);
    }

    [Fact]
    public void ToResult_integrates_with_Result_pipeline()
    {
        var result = Option<int>.Some(42)
            .ToResult("not found")
            .Map(v => v * 2);

        Assert.True(result.IsOk);
        Assert.Equal(84, result.Value);
    }

    [Fact]
    public void ToResult_None_integrates_with_Result_pipeline()
    {
        var result = Option<int>.None
            .ToResult("not found")
            .Map(v => v * 2);

        Assert.True(result.IsErr);
        Assert.Equal("not found", result.Error);
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Helper to wrap a lambda call so we can track whether it was invoked.
    /// </summary>
    private static T Invoke<T>(Func<T> f) => f();
}
