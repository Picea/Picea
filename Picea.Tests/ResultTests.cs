namespace Picea.Tests;

/// <summary>
/// Tests for the <see cref="Result{TSuccess, TError}"/> discriminated union,
/// covering factory methods, projections, monadic operations, catamorphism,
/// Try-pattern extraction, defaults, and implicit conversion.
/// </summary>
public sealed class ResultTests
{
    // ── Factory methods ───────────────────────────────────────────────

    [Test]
    public async Task Ok_creates_success_result()
    {
        var result = Result<int, string>.Ok(42);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.IsErr).IsFalse();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Err_creates_error_result()
    {
        var result = Result<int, string>.Err("boom");

        await Assert.That(result.IsOk).IsFalse();
        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("boom");
    }

    // ── Throwing accessors ────────────────────────────────────────────

    [Test]
    public async Task Value_on_Err_throws_InvalidOperationException()
    {
        var result = Result<int, string>.Err("nope");

        var ex = await Assert.That(() => result.Value).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("Err");
    }

    [Test]
    public async Task Error_on_Ok_throws_InvalidOperationException()
    {
        var result = Result<int, string>.Ok(1);

        var ex = await Assert.That(() => result.Error).ThrowsExactly<InvalidOperationException>();
        await Assert.That(ex!.Message).Contains("Ok");
    }

    // ── Match (catamorphism) ──────────────────────────────────────────

    [Test]
    public async Task Match_invokes_ok_branch_on_success()
    {
        var result = Result<int, string>.Ok(42);

        var message = result.Match(
            ok: v => $"value={v}",
            err: e => $"error={e}");

        await Assert.That(message).IsEqualTo("value=42");
    }

    [Test]
    public async Task Match_invokes_err_branch_on_failure()
    {
        var result = Result<int, string>.Err("boom");

        var message = result.Match(
            ok: v => $"value={v}",
            err: e => $"error={e}");

        await Assert.That(message).IsEqualTo("error=boom");
    }

    [Test]
    public async Task Match_supports_type_coercion_through_common_base()
    {
        var ok = Result<int, string>.Ok(1);
        var err = Result<int, string>.Err("x");

        // Both branches return object — verifying generic TOut works
        object okResult = ok.Match<object>(ok: v => v, err: e => e);
        object errResult = err.Match<object>(ok: v => v, err: e => e);

        await Assert.That(okResult).IsEqualTo(1);
        await Assert.That(errResult).IsEqualTo("x");
    }

    // ── Switch (void catamorphism) ───────────────────────────────────

    [Test]
    public async Task Switch_invokes_ok_action_on_success()
    {
        var result = Result<int, string>.Ok(42);
        var captured = 0;

        result.Switch(
            ok: v => captured = v,
            err: _ => captured = -1);

        await Assert.That(captured).IsEqualTo(42);
    }

    [Test]
    public async Task Switch_invokes_err_action_on_failure()
    {
        var result = Result<int, string>.Err("fail");
        var captured = "";

        result.Switch(
            ok: _ => captured = "ok",
            err: e => captured = e);

        await Assert.That(captured).IsEqualTo("fail");
    }

    // ── TryGetValue / TryGetError ────────────────────────────────────

    [Test]
    public async Task TryGetValue_returns_true_and_value_on_Ok()
    {
        var result = Result<int, string>.Ok(42);

        await Assert.That(result.TryGetValue(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task TryGetValue_returns_false_on_Err()
    {
        var result = Result<int, string>.Err("nope");

        await Assert.That(result.TryGetValue(out _)).IsFalse();
    }

    [Test]
    public async Task TryGetError_returns_true_and_error_on_Err()
    {
        var result = Result<int, string>.Err("boom");

        await Assert.That(result.TryGetError(out var error)).IsTrue();
        await Assert.That(error).IsEqualTo("boom");
    }

    [Test]
    public async Task TryGetError_returns_false_on_Ok()
    {
        var result = Result<int, string>.Ok(1);

        await Assert.That(result.TryGetError(out _)).IsFalse();
    }

    // ── DefaultValue / DefaultError ──────────────────────────────────

    [Test]
    public async Task DefaultValue_returns_value_on_Ok()
    {
        var result = Result<int, string>.Ok(42);

        await Assert.That(result.DefaultValue(0)).IsEqualTo(42);
    }

    [Test]
    public async Task DefaultValue_returns_fallback_on_Err()
    {
        var result = Result<int, string>.Err("nope");

        await Assert.That(result.DefaultValue(-1)).IsEqualTo(-1);
    }

    [Test]
    public async Task DefaultError_returns_error_on_Err()
    {
        var result = Result<int, string>.Err("boom");

        await Assert.That(result.DefaultError("fallback")).IsEqualTo("boom");
    }

    [Test]
    public async Task DefaultError_returns_fallback_on_Ok()
    {
        var result = Result<int, string>.Ok(1);

        await Assert.That(result.DefaultError("fallback")).IsEqualTo("fallback");
    }

    // ── Implicit conversion ──────────────────────────────────────────

    [Test]
    public async Task Implicit_conversion_from_TSuccess_creates_Ok()
    {
        Result<int, string> result = 42;

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Implicit_conversion_works_in_method_return()
    {
        static Result<string, int> GetName() => "Alice";

        var result = GetName();

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo("Alice");
    }

    [Test]
    public async Task Ok_null_reference_throws_ArgumentNullException()
    {
        var ex = await Assert.That(() => Result<string, int>.Ok(null!)).ThrowsExactly<ArgumentNullException>();
        await Assert.That(ex!.ParamName).Contains("value");
    }

    [Test]
    public async Task Implicit_conversion_of_null_success_throws_ArgumentNullException()
    {
        var ex = await Assert.That(() =>
        {
            Result<string, int> result = null!;
            return result;
        }).ThrowsExactly<ArgumentNullException>();

        await Assert.That(ex!.ParamName).Contains("value");
    }

    // ── Map / Select (functor) ───────────────────────────────────────

    [Test]
    public async Task Map_transforms_Ok_value()
    {
        var result = Result<int, string>.Ok(21).Map(v => v * 2);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Map_propagates_Err()
    {
        var result = Result<int, string>.Err("fail").Map(v => v * 2);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("fail");
    }

    [Test]
    public async Task Select_is_LINQ_alias_for_Map()
    {
        var result = from v in Result<int, string>.Ok(21) select v * 2;

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    // ── Bind (monad) ─────────────────────────────────────────────────

    [Test]
    public async Task Bind_chains_Ok_to_Ok()
    {
        var result = Result<int, string>.Ok(21)
            .Bind(v => Result<int, string>.Ok(v * 2));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task Bind_chains_Ok_to_Err()
    {
        var result = Result<int, string>.Ok(21)
            .Bind(_ => Result<int, string>.Err("failed"));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("failed");
    }

    [Test]
    public async Task Bind_short_circuits_on_Err()
    {
        var called = false;
        var result = Result<int, string>.Err("first")
            .Bind(v =>
            {
                called = true;
                return Result<int, string>.Ok(v);
            });

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("first");
        await Assert.That(called).IsFalse();
    }

    // ── SelectMany (LINQ multi-from) ─────────────────────────────────

    [Test]
    public async Task SelectMany_supports_LINQ_query_syntax()
    {
        static Result<int, string> Parse(string s) =>
            int.TryParse(s, out var n)
                ? Result<int, string>.Ok(n)
                : Result<int, string>.Err($"Cannot parse '{s}'");

        var result =
            from x in Parse("10")
            from y in Parse("32")
            select x + y;

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    [Test]
    public async Task SelectMany_short_circuits_on_first_Err()
    {
        var secondCalled = false;

        var result =
            from x in Result<int, string>.Err("first failed")
            from y in Invoke(() => { secondCalled = true; return Result<int, string>.Ok(2); })
            select x + y;

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("first failed");
        await Assert.That(secondCalled).IsFalse();
    }

    [Test]
    public async Task SelectMany_short_circuits_on_second_Err()
    {
        var result =
            from x in Result<int, string>.Ok(1)
            from y in Result<int, string>.Err("second failed")
            select x + y;

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("second failed");
    }

    // ── MapError ─────────────────────────────────────────────────────

    [Test]
    public async Task MapError_transforms_Err_value()
    {
        var result = Result<int, string>.Err("boom")
            .MapError(e => e.Length);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo(4);
    }

    [Test]
    public async Task MapError_preserves_Ok()
    {
        var result = Result<int, string>.Ok(42)
            .MapError(e => e.Length);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(42);
    }

    // ── ToString ─────────────────────────────────────────────────────

    [Test]
    public async Task ToString_Ok_includes_value()
    {
        await Assert.That(Result<int, string>.Ok(42).ToString()).IsEqualTo("Ok(42)");
    }

    [Test]
    public async Task ToString_Err_includes_error()
    {
        await Assert.That(Result<int, string>.Err("boom").ToString()).IsEqualTo("Err(boom)");
    }

    // ── Unit type ────────────────────────────────────────────────────

    [Test]
    public async Task Unit_Value_is_default()
    {
        await Assert.That(Unit.Value).IsEqualTo(default(Unit));
    }

    [Test]
    public async Task Unit_ToString_returns_parentheses()
    {
        await Assert.That(Unit.Value.ToString()).IsEqualTo("()");
    }

    [Test]
    public async Task Result_Unit_represents_void_success()
    {
        var result = Result<Unit, string>.Ok(Unit.Value);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(Unit.Value);
    }

    // ── Composition: Match + Map + Bind ──────────────────────────────

    [Test]
    public async Task Full_pipeline_with_Match_extraction()
    {
        var output = Result<int, string>.Ok(10)
            .Map(v => v * 2)
            .Bind(v => v > 15
                ? Result<string, string>.Ok($"big: {v}")
                : Result<string, string>.Err("too small"))
            .Match(
                ok: v => v,
                err: e => $"error: {e}");

        await Assert.That(output).IsEqualTo("big: 20");
    }

    [Test]
    public async Task Full_pipeline_error_path()
    {
        var output = Result<int, string>.Ok(5)
            .Map(v => v * 2)
            .Bind(v => v > 15
                ? Result<string, string>.Ok($"big: {v}")
                : Result<string, string>.Err("too small"))
            .Match(
                ok: v => v,
                err: e => $"error: {e}");

        await Assert.That(output).IsEqualTo("error: too small");
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Helper to wrap a lambda call so we can track whether it was invoked.
    /// </summary>
    private static T Invoke<T>(Func<T> f) => f();
}
