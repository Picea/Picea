namespace Picea.Tests;

/// <summary>
/// Laws and composition checks for <see cref="DeciderComposition"/>.
/// These tests back the staged pipeline claims with executable invariants.
/// </summary>
public sealed class DeciderCompositionTests
{
    [Test]
    public async Task ValidateToResult_identity_for_valid()
    {
        var validated = new Validated<int, string>.Valid(42);

        var result = DeciderComposition.ValidateToResult(validated);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(validated);
    }

    [Test]
    public async Task ValidateToResult_identity_for_invalid()
    {
        var validated = new Validated<int, string>.Invalid("invalid");

        var result = DeciderComposition.ValidateToResult(validated);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("invalid");
    }

    [Test]
    public async Task AuthorizeToResult_ok_means_continue()
    {
        var validated = new Validated<int, string>.Valid(7);

        var result = DeciderComposition.AuthorizeToResult(validated, Result<Unit, string>.Ok(Unit.Value));

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value).IsEqualTo(validated);
    }

    [Test]
    public async Task AuthorizeToResult_err_means_reject()
    {
        var validated = new Validated<int, string>.Valid(7);

        var result = DeciderComposition.AuthorizeToResult(validated, Result<Unit, string>.Err("forbidden"));

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("forbidden");
    }

    [Test]
    public async Task Compose_short_circuits_on_validate_failure()
    {
        var authorizeCalled = false;
        var decideCalled = false;

        static Validated<int, string> Validate(int state, int command) =>
            new Validated<int, string>.Invalid($"bad:{command}");

        Result<Unit, string> Authorize(int state, Validated<int, string> validated, Unit authContext)
        {
            authorizeCalled = true;
            return Result<Unit, string>.Ok(Unit.Value);
        }

        Result<int[], string> Decide(int state, Validated<int, string> validated)
        {
            decideCalled = true;
            return Result<int[], string>.Ok([state + 1]);
        }

        var result = DeciderComposition.Compose(
            state: 10,
            command: 99,
            authorizationContext: Unit.Value,
            validate: Validate,
            authorize: Authorize,
            decide: Decide);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("bad:99");
        await Assert.That(authorizeCalled).IsFalse();
        await Assert.That(decideCalled).IsFalse();
    }

    [Test]
    public async Task Compose_short_circuits_on_authorize_failure()
    {
        var decideCalled = false;

        static Validated<int, string> Validate(int state, int command) =>
            new Validated<int, string>.Valid(command);

        static Result<Unit, string> Authorize(int state, Validated<int, string> validated, Unit authContext) =>
            Result<Unit, string>.Err("not-allowed");

        Result<int[], string> Decide(int state, Validated<int, string> validated)
        {
            decideCalled = true;
            return Result<int[], string>.Ok([state + 1]);
        }

        var result = DeciderComposition.Compose(
            state: 2,
            command: 3,
            authorizationContext: Unit.Value,
            validate: Validate,
            authorize: Authorize,
            decide: Decide);

        await Assert.That(result.IsErr).IsTrue();
        await Assert.That(result.Error).IsEqualTo("not-allowed");
        await Assert.That(decideCalled).IsFalse();
    }

    [Test]
    public async Task Compose_happy_path_returns_decide_result()
    {
        static Validated<int, string> Validate(int state, int command) =>
            new Validated<int, string>.Valid(command);

        static Result<Unit, string> Authorize(int state, Validated<int, string> validated, Unit authContext) =>
            Result<Unit, string>.Ok(Unit.Value);

        static Result<int[], string> Decide(int state, Validated<int, string> validated) =>
            validated is Validated<int, string>.Valid(var command)
                ? Result<int[], string>.Ok([state + command])
                : Result<int[], string>.Err("unexpected");

        var result = DeciderComposition.Compose(
            state: 10,
            command: 32,
            authorizationContext: Unit.Value,
            validate: Validate,
            authorize: Authorize,
            decide: Decide);

        await Assert.That(result.IsOk).IsTrue();
        await Assert.That(result.Value.Length).IsEqualTo(1);
        await Assert.That(result.Value[0]).IsEqualTo(42);
    }

    [Test]
    public async Task Compose_matches_explicit_bind_chain()
    {
        static Validated<int, string> Validate(int state, int command) =>
            command < 0
                ? new Validated<int, string>.Invalid("negative")
                : new Validated<int, string>.Valid(command);

        static Result<Unit, string> Authorize(int state, Validated<int, string> validated, Unit authContext) =>
            state < 0
            ? Result<Unit, string>.Err("state-forbidden")
            : Result<Unit, string>.Ok(Unit.Value);

        static Result<int[], string> Decide(int state, Validated<int, string> validated) =>
            validated is Validated<int, string>.Valid(var command)
                ? Result<int[], string>.Ok([state + command])
                : Result<int[], string>.Err("unexpected");

        var composed = DeciderComposition.Compose(
            state: 10,
            command: 5,
            authorizationContext: Unit.Value,
            validate: Validate,
            authorize: Authorize,
            decide: Decide);

        var explicitChain = DeciderComposition.ValidateToResult(Validate(10, 5))
            .Bind(validated => DeciderComposition.AuthorizeToResult(validated, Authorize(10, validated, Unit.Value)))
            .Bind(validated => Decide(10, validated));

        await Assert.That(composed.IsOk).IsEqualTo(explicitChain.IsOk);
        await Assert.That(composed.IsErr).IsEqualTo(explicitChain.IsErr);
        await Assert.That(composed.Value[0]).IsEqualTo(explicitChain.Value[0]);
    }

    [Test]
    public async Task Result_bind_associativity_holds_for_staged_pipeline()
    {
        static Result<int, string> M() => Result<int, string>.Ok(7);

        static Result<int, string> F(int x) =>
            x > 0 ? Result<int, string>.Ok(x + 1) : Result<int, string>.Err("f");

        static Result<int, string> G(int x) =>
            x % 2 is 0 ? Result<int, string>.Ok(x * 3) : Result<int, string>.Err("g");

        var left = M().Bind(F).Bind(G);
        var right = M().Bind(x => F(x).Bind(G));

        await Assert.That(left.IsOk).IsEqualTo(right.IsOk);
        await Assert.That(left.IsErr).IsEqualTo(right.IsErr);
        await Assert.That(left.Value).IsEqualTo(right.Value);
    }
}
