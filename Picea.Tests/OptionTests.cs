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

    [Test]
    public async Task Some_creates_option_with_value()
    {
        var option = Option<int>.Some(42);

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.IsNone).IsFalse();
        await Assert.That(option.Value).IsEqualTo(42);
    }

    [Test]
    public async Task None_creates_empty_option()
    {
        var option = Option<int>.None;

        await Assert.That(option.IsSome).IsFalse();
        await Assert.That(option.IsNone).IsTrue();
    }

    [Test]
    public async Task Static_Some_creates_option_with_type_inference()
    {
        var option = Option.Some(42);

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Static_None_creates_empty_option_with_type_inference()
    {
        var option = Option.None<int>();

        await Assert.That(option.IsNone).IsTrue();
    }

    // ── Throwing accessor ─────────────────────────────────────────────

    [Test]
    public async Task Value_on_None_throws_InvalidOperationException()
    {
        var option = Option<int>.None;

        var ex = await Assert.That(() => option.Value).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex.Message).Contains("None");
    }

    [Test]
    public async Task Value_on_Some_returns_contained_value()
    {
        var option = Option<string>.Some("hello");

        await Assert.That(option.Value).IsEqualTo("hello");
    }

    // ── Match (catamorphism) ──────────────────────────────────────────

    [Test]
    public async Task Match_invokes_some_branch_on_Some()
    {
        var option = Option<int>.Some(42);

        var message = option.Match(
            some: v => $"value={v}",
            none: () => "empty");

        await Assert.That(message).IsEqualTo("value=42");
    }

    [Test]
    public async Task Match_invokes_none_branch_on_None()
    {
        var option = Option<int>.None;

        var message = option.Match(
            some: v => $"value={v}",
            none: () => "empty");

        await Assert.That(message).IsEqualTo("empty");
    }

    [Test]
    public async Task Match_supports_type_coercion_through_common_base()
    {
        var some = Option<int>.Some(1);
        var none = Option<int>.None;

        object someResult = some.Match<object>(some: v => v, none: () => "nothing");
        object noneResult = none.Match<object>(some: v => v, none: () => "nothing");

        await Assert.That(someResult).IsEqualTo(1);
        await Assert.That(noneResult).IsEqualTo("nothing");
    }

    // ── Switch (void catamorphism) ───────────────────────────────────

    [Test]
    public async Task Switch_invokes_some_action_on_Some()
    {
        var option = Option<int>.Some(42);
        var captured = 0;

        option.Switch(
            some: v => captured = v,
            none: () => captured = -1);

        await Assert.That(captured).IsEqualTo(42);
    }

    [Test]
    public async Task Switch_invokes_none_action_on_None()
    {
        var option = Option<int>.None;
        var captured = false;

        option.Switch(
            some: _ => captured = false,
            none: () => captured = true);

        await Assert.That(captured).IsTrue();
    }

    // ── TryGetValue ──────────────────────────────────────────────────

    [Test]
    public async Task TryGetValue_returns_true_and_value_on_Some()
    {
        var option = Option<int>.Some(42);

        await Assert.That(option.TryGetValue(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task TryGetValue_returns_false_on_None()
    {
        var option = Option<int>.None;

        await Assert.That(option.TryGetValue(out _)).IsFalse();
    }

    // ── DefaultValue ─────────────────────────────────────────────────

    [Test]
    public async Task DefaultValue_returns_value_on_Some()
    {
        var option = Option<int>.Some(42);

        await Assert.That(option.DefaultValue(0)).IsEqualTo(42);
    }

    [Test]
    public async Task DefaultValue_returns_fallback_on_None()
    {
        var option = Option<int>.None;

        await Assert.That(option.DefaultValue(-1)).IsEqualTo(-1);
    }

    // ── DefaultWith ──────────────────────────────────────────────────

    [Test]
    public async Task DefaultWith_returns_value_on_Some()
    {
        var option = Option<int>.Some(42);

        await Assert.That(option.DefaultWith(() => 0)).IsEqualTo(42);
    }

    [Test]
    public async Task DefaultWith_returns_lazy_fallback_on_None()
    {
        var option = Option<int>.None;

        await Assert.That(option.DefaultWith(() => -1)).IsEqualTo(-1);
    }

    [Test]
    public async Task DefaultWith_does_not_evaluate_fallback_on_Some()
    {
        var option = Option<int>.Some(42);
        var evaluated = false;

        option.DefaultWith(() =>
        {
            evaluated = true;
            return 0;
        });

        await Assert.That(evaluated).IsFalse();
    }

    // ── Map / Select (functor) ───────────────────────────────────────

    [Test]
    public async Task Map_transforms_Some_value()
    {
        var option = Option<int>.Some(21).Map(v => v * 2);

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Map_propagates_None()
    {
        var option = Option<int>.None.Map(v => v * 2);

        await Assert.That(option.IsNone).IsTrue();
    }

    [Test]
    public async Task Select_is_LINQ_alias_for_Map()
    {
        var option = from v in Option<int>.Some(21) select v * 2;

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Select_propagates_None()
    {
        var option = from v in Option<int>.None select v * 2;

        await Assert.That(option.IsNone).IsTrue();
    }

    // ── Bind (monad) ─────────────────────────────────────────────────

    [Test]
    public async Task Bind_chains_Some_to_Some()
    {
        var option = Option<int>.Some(21)
            .Bind(v => Option<int>.Some(v * 2));

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Bind_chains_Some_to_None()
    {
        var option = Option<int>.Some(21)
            .Bind(_ => Option<int>.None);

        await Assert.That(option.IsNone).IsTrue();
    }

    [Test]
    public async Task Bind_short_circuits_on_None()
    {
        var called = false;
        var option = Option<int>.None
            .Bind(v =>
            {
                called = true;
                return Option<int>.Some(v);
            });

        await Assert.That(option.IsNone).IsTrue();
        await Assert.That(called).IsFalse();
    }

    // ── SelectMany (LINQ multi-from) ─────────────────────────────────

    [Test]
    public async Task SelectMany_supports_LINQ_query_syntax()
    {
        var option =
            from x in Option<int>.Some(10)
            from y in Option<int>.Some(32)
            select x + y;

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo(42);
    }

    [Test]
    public async Task SelectMany_short_circuits_on_first_None()
    {
        var secondCalled = false;

        var option =
            from x in Option<int>.None
            from y in Invoke(() => { secondCalled = true; return Option<int>.Some(2); })
            select x + y;

        await Assert.That(option.IsNone).IsTrue();
        await Assert.That(secondCalled).IsFalse();
    }

    [Test]
    public async Task SelectMany_short_circuits_on_second_None()
    {
        var option =
            from x in Option<int>.Some(1)
            from y in Option<int>.None
            select x + y;

        await Assert.That(option.IsNone).IsTrue();
    }

    [Test]
    public async Task SelectMany_three_from_clauses()
    {
        var option =
            from a in Option<int>.Some(1)
            from b in Option<int>.Some(2)
            from c in Option<int>.Some(3)
            select a + b + c;

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo(6);
    }

    // ── Where (filter) ───────────────────────────────────────────────

    [Test]
    public async Task Where_retains_Some_when_predicate_passes()
    {
        var option = Option<int>.Some(42).Where(v => v > 10);

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Where_returns_None_when_predicate_fails()
    {
        var option = Option<int>.Some(5).Where(v => v > 10);

        await Assert.That(option.IsNone).IsTrue();
    }

    [Test]
    public async Task Where_returns_None_on_None_input()
    {
        var option = Option<int>.None.Where(v => v > 10);

        await Assert.That(option.IsNone).IsTrue();
    }

    // ── ToResult ─────────────────────────────────────────────────────

    [Test]
    public async Task ToResult_converts_Some_to_Ok()
    {
        var result = Option<int>.Some(42).ToResult("missing");

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task ToResult_converts_None_to_Err()
    {
        var result = Option<int>.None.ToResult("missing");

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("missing");
    }

    // ── Implicit conversion ──────────────────────────────────────────

    [Test]
    public async Task Implicit_conversion_from_T_creates_Some()
    {
        Option<int> option = 42;

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Implicit_conversion_works_in_method_return()
    {
        static Option<string> GetName() => "Alice";

        var option = GetName();

        await Assert.That(option.IsSome).IsTrue();
        await Assert.That(option.Value).IsEqualTo("Alice");
    }

    [Test]
    public async Task Some_null_reference_throws_ArgumentNullException()
    {
        var ex = await Assert.That(() => Option<string>.Some(null!)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(ex.ParamName).Contains("value");
    }

    [Test]
    public async Task Implicit_conversion_of_null_reference_throws_ArgumentNullException()
    {
        var ex = await Assert.That(() =>
        {
            Option<string> option = null!;
            return option;
        }).ThrowsExactly<ArgumentNullException>();

        await Assert.That(ex.ParamName).Contains("value");
    }

    // ── ToString ─────────────────────────────────────────────────────

    [Test]
    public async Task ToString_Some_includes_value()
    {
        await Assert.That(Option<int>.Some(42).ToString()).IsEqualTo("Some(42)");
    }

    [Test]
    public async Task ToString_None_returns_None()
    {
        await Assert.That(Option<int>.None.ToString()).IsEqualTo("None");
    }

    // ── Composition: Match + Map + Bind ──────────────────────────────

    [Test]
    public async Task Full_pipeline_with_Match_extraction()
    {
        var output = Option<int>.Some(10)
            .Map(v => v * 2)
            .Bind(v => v > 15
                ? Option<string>.Some($"big: {v}")
                : Option<string>.None)
            .Match(
                some: v => v,
                none: () => "too small");

        await Assert.That(output).IsEqualTo("big: 20");
    }

    [Test]
    public async Task Full_pipeline_none_path()
    {
        var output = Option<int>.Some(5)
            .Map(v => v * 2)
            .Bind(v => v > 15
                ? Option<string>.Some($"big: {v}")
                : Option<string>.None)
            .Match(
                some: v => v,
                none: () => "too small");

        await Assert.That(output).IsEqualTo("too small");
    }

    [Test]
    public async Task Where_into_Map_pipeline()
    {
        var output = Option<int>.Some(42)
            .Where(v => v > 10)
            .Map(v => $"valid: {v}")
            .DefaultValue("invalid");

        await Assert.That(output).IsEqualTo("valid: 42");
    }

    [Test]
    public async Task Where_filters_then_DefaultValue()
    {
        var output = Option<int>.Some(5)
            .Where(v => v > 10)
            .Map(v => $"valid: {v}")
            .DefaultValue("invalid");

        await Assert.That(output).IsEqualTo("invalid");
    }

    [Test]
    public async Task ToResult_integrates_with_Result_pipeline()
    {
        var result = Option<int>.Some(42)
            .ToResult("not found")
            .Map(v => v * 2);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(84);
    }

    [Test]
    public async Task ToResult_None_integrates_with_Result_pipeline()
    {
        var result = Option<int>.None
            .ToResult("not found")
            .Map(v => v * 2);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("not found");
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Helper to wrap a lambda call so we can track whether it was invoked.
    /// </summary>
    private static T Invoke<T>(Func<T> f) => f();
}
