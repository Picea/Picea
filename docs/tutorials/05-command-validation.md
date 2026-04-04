# Tutorial 05: Command Validation with the Decider

Validate commands before producing events using the Decider pattern and the Result type.

> **Concept reference:** This tutorial implements the theory from [The Decider](../concepts/the-decider.md). For Result type recipes, see [Error Handling Patterns](../guides/error-handling-patterns.md).

## What You'll Learn

- What the [Decider pattern](../concepts/the-decider.md) is and why it matters
- How to separate intent (commands) from facts (events)
- How to use the [`Result<TSuccess, TError>`](../reference/result.md) type for error handling
- How to use [`DecidingRuntime`](../reference/decider.md) for command validation in async runtimes
- How to compose Result values with Map, Bind, and MapError

## Prerequisites

Complete [Tutorial 01: Getting Started](01-getting-started.md). Understanding [Event Sourcing](03-event-sourced-aggregate.md) is helpful but not required.

## The Problem

The basic Automaton accepts any event — there's no validation:

```csharp
// This always succeeds, even if the result doesn't make business sense
await runtime.Dispatch(new CounterEvent.Increment());
```

In real systems, you need to validate *intent* before producing *facts*:

- Can this user place an order? (inventory check)
- Can this account be debited? (balance check)
- Can this counter go above 100? (bounds check)

## The Decider Pattern

The [Decider pattern](https://thinkbeforecoding.com/post/2021/12/17/functional-event-sourcing-decider) (Jérémie Chassaing, 2021) adds a staged command layer:

```text
Command
    -> Validate(state, command)
    -> Authorize(state, validated, authorizationContext)
    -> Decide(state, validated)
    -> Result<Events, Error>
    -> Transition(state, event)
```

It separates:

| Concept | Type | Purpose |
| ------- | ---- | ------- |
| Intent | Command | What the user wants to do |
| Feasibility | Validate | Checks whether command can be processed |
| Permission | Authorize | Checks whether caller is allowed |
| Decision | Decide | Produces events from validated + authorized intent |
| Fact | Event | What actually happened (immutable) |
| Evolution | Transition | How the state changes |

## Step 1: Define Commands and Errors

Commands represent user intent. Errors represent why a command was rejected:

```csharp
public interface CounterCommand
{
    record struct Add(int Amount) : CounterCommand;
    record struct Reset : CounterCommand;
}

public interface CounterError
{
    record struct Overflow(int Current, int Amount, int Max) : CounterError;
    record struct Underflow(int Current, int Amount) : CounterError;
    record struct AlreadyAtZero : CounterError;
}
```

Notice how errors carry *context* — not just "invalid" but *why* it's invalid.

## Step 2: Implement the Decider

A Decider extends the Automaton interface:

```csharp
public interface Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract Validated<TCommand, TError> Validate(TState state, TCommand command);

    static virtual Result<Unit, TError> Authorize<TAuthorizationContext>(
        TState state,
        Validated<TCommand, TError> command,
        TAuthorizationContext authorizationContext) =>
        Result<Unit, TError>.Ok(Unit.Value);

    static abstract Result<TEvent[], TError> Decide(
        TState state, Validated<TCommand, TError> command);

    static virtual bool IsTerminal(TState state) => false;
}
```

All three stages are pure. `Validate` and `Authorize` can reject early; `Decide` produces events only for validated commands.

```csharp
public class Counter
    : Decider<CounterState, CounterCommand, CounterEvent, CounterEffect, CounterError, Unit>
{
    public const int MaxCount = 100;

    public static (CounterState, CounterEffect) Initialize(Unit _) =>
        (new CounterState(0), new CounterEffect.None());

    public static Validated<CounterCommand, CounterError> Validate(
        CounterState state, CounterCommand command) =>
        command switch
        {
            // Overflow: would exceed max
            CounterCommand.Add(var n) when state.Count + n > MaxCount =>
                new Validated<CounterCommand, CounterError>.Invalid(
                    new CounterError.Overflow(state.Count, n, MaxCount)),

            // Underflow: would go below zero
            CounterCommand.Add(var n) when state.Count + n < 0 =>
                new Validated<CounterCommand, CounterError>.Invalid(
                    new CounterError.Underflow(state.Count, n)),

            // Reset at zero: nothing to reset
            CounterCommand.Reset when state.Count is 0 =>
                new Validated<CounterCommand, CounterError>.Invalid(
                    new CounterError.AlreadyAtZero()),

            _ => new Validated<CounterCommand, CounterError>.Valid(command)
        };

    public static Result<Unit, CounterError> Authorize<TAuthorizationContext>(
        CounterState state,
        Validated<CounterCommand, CounterError> validated,
        TAuthorizationContext authorizationContext) =>
        Result<Unit, CounterError>.Ok(Unit.Value);

    public static Result<CounterEvent[], CounterError> Decide(
        CounterState state, Validated<CounterCommand, CounterError> validated) =>
        validated is not Validated<CounterCommand, CounterError>.Valid(var command)
            ? throw new UnreachableException()
            : command switch
            {
                // Valid positive: produce N increment events
                CounterCommand.Add(var n) when n >= 0 =>
                    Result<CounterEvent[], CounterError>
                        .Ok(Enumerable.Repeat<CounterEvent>(
                            new CounterEvent.Increment(), n).ToArray()),

                // Valid negative: produce |N| decrement events
                CounterCommand.Add(var n) =>
                    Result<CounterEvent[], CounterError>
                        .Ok(Enumerable.Repeat<CounterEvent>(
                            new CounterEvent.Decrement(), Math.Abs(n)).ToArray()),

                // Valid reset
                CounterCommand.Reset =>
                    Result<CounterEvent[], CounterError>
                        .Ok([new CounterEvent.Reset()]),

                _ => throw new UnreachableException()
            };

    public static (CounterState, CounterEffect) Transition(
        CounterState state, CounterEvent @event) =>
        @event switch
        {
            CounterEvent.Increment =>
                (state with { Count = state.Count + 1 }, new CounterEffect.None()),
            CounterEvent.Decrement =>
                (state with { Count = state.Count - 1 }, new CounterEffect.None()),
            CounterEvent.Reset =>
                (new CounterState(0),
                 new CounterEffect.Log($"Counter reset from {state.Count}")),
            _ => throw new UnreachableException()
        };
}
```

Because `Decider<...> : Automaton<...>`, the Counter is still a valid automaton. All existing runtimes continue to work — the Decider is a **non-breaking upgrade**.

## Step 3: Use DecidingRuntime

The `DecidingRuntime` wraps `AutomatonRuntime` and adds `Handle(command, ...)` with an optional authorization context overload:

```csharp
using Picea;

Observer<CounterState, CounterEvent, CounterEffect> observer =
    (state, @event, effect) =>
    {
        Console.WriteLine($"{@event.GetType().Name} → {state}");
        return PipelineResult.Ok;
    };

Interpreter<CounterEffect, CounterEvent> interpreter =
    _ => new ValueTask<Result<CounterEvent[], PipelineError>>(
        Result<CounterEvent[], PipelineError>.Ok([]));

var runtime = await DecidingRuntime<Counter, CounterState, CounterCommand,
    CounterEvent, CounterEffect, CounterError, Unit>.Start(default, observer, interpreter);

var authorizationContext = Unit.Value;
```

### Successful Commands

```csharp
var result = await runtime.Handle(new CounterCommand.Add(5), authorizationContext);
// result is Ok(CounterState { Count = 5 })

Console.WriteLine(result.Value.Count); // 5
```

### Rejected Commands

```csharp
var overflow = await runtime.Handle(new CounterCommand.Add(200), authorizationContext);
// overflow is Err(CounterError.Overflow { Current = 5, Amount = 200, Max = 100 })

// State is unchanged
Console.WriteLine(runtime.State.Count); // still 5
Console.WriteLine(runtime.Events.Count); // still 5 (no new events dispatched)
```

### Pattern Matching on Results

```csharp
var result = await runtime.Handle(new CounterCommand.Add(10), authorizationContext);

var message = result.IsOk
    ? $"Success! Count is now {result.Value.Count}"
    : result.Error switch
    {
        CounterError.Overflow o =>
            $"Overflow: {o.Current} + {o.Amount} exceeds max {o.Max}",
        CounterError.Underflow u =>
            $"Underflow: {u.Current} + {u.Amount} would go below zero",
        CounterError.AlreadyAtZero =>
            "Counter is already at zero",
        _ => $"Unknown error: {result.Error}"
    };

Console.WriteLine(message);
```

## The Result Type

`Result<TSuccess, TError>` is a discriminated union — either `Ok(value)` or `Err(error)`, implemented as a zero-allocation readonly struct:

```csharp
public readonly struct Result<TSuccess, TError>
{
    public static Result<TSuccess, TError> Ok(TSuccess value) => ...;
    public static Result<TSuccess, TError> Err(TError error) => ...;

    public bool IsOk { get; }
    public bool IsErr { get; }
    public TSuccess Value { get; }   // throws on Err
    public TError Error { get; }     // throws on Ok
}
```

### Pattern Matching

Use `IsOk`/`IsErr` with C# conditional expressions or `switch`:

```csharp
var text = result.IsOk
    ? $"Got {result.Value}"
    : $"Failed: {result.Error}";
```

### Map / Select (Functor)

Transform the success value, leaving errors untouched. `Select` is the LINQ alias for `Map`:

```csharp
Result<int, string> ok = Result<int, string>.Ok(21);
Result<int, string> mapped = ok.Map(v => v * 2);
// mapped is Ok(42)

// Or using LINQ query syntax:
var doubled = from v in ok select v * 2;
// doubled is Ok(42)

Result<int, string> err = Result<int, string>.Err("fail");
Result<int, string> mappedErr = err.Map(v => v * 2);
// mappedErr is still Err("fail")
```

### Bind / SelectMany (Monad)

Chain operations that can themselves fail. `SelectMany` is the LINQ alias for `Bind`:

```csharp
Result<int, string> ok = Result<int, string>.Ok(21);

var result = ok.Bind(v =>
    v > 50
        ? Result<string, string>.Err("too large")
        : Result<string, string>.Ok($"value: {v * 2}"));
// result is Ok("value: 42")
```

This enables **railway-oriented programming** — errors short-circuit the entire chain:

```csharp
// Fluent API
var final = parseInput(raw)         // Result<int, ParseError>
    .Map(n => n * 2)                // Result<int, ParseError>
    .Bind(n => validate(n))         // Result<int, ValidationError> — ⚠️ error types must match
    .Map(n => $"Result: {n}");      // Result<string, ValidationError>

// LINQ query syntax (equivalent to the above)
var final =
    from n in parseInput(raw)
    let doubled = n * 2
    from valid in validate(doubled)
    select $"Result: {valid}";
```

### MapError

Transform the error value:

```csharp
Result<int, string> err = Result<int, string>.Err("fail");
var mapped = err.MapError(e => e.Length);
// mapped is Err(4)
```

## Testing the Decide Function

Because `Decide` is pure, you can test it directly — no runtime, no async, no infrastructure:

```csharp
[Fact]
public void Decide_Overflow_ReturnsError()
{
    var state = new CounterState(95);
    var command = new CounterCommand.Add(10);
    var validated = Counter.Validate(state, command);

    var result = Counter.Decide(state, validated);

    Assert.True(result.IsErr);
    var overflow = Assert.IsType<CounterError.Overflow>(result.Error);
    Assert.Equal(95, overflow.Current);
    Assert.Equal(10, overflow.Amount);
    Assert.Equal(100, overflow.Max);
}

[Fact]
public void Decide_IsPure_SameInputSameOutput()
{
    var state = new CounterState(5);
    var command = new CounterCommand.Add(3);
    var validated = Counter.Validate(state, command);

    var r1 = Counter.Decide(state, validated);
    var r2 = Counter.Decide(state, validated);

    // Pure function: identical inputs always produce identical outputs
    Assert.True(r1.IsOk);
    Assert.True(r2.IsOk);
    Assert.Equal(r1.Value.ToList(), r2.Value.ToList());
}
```

## The Nine Elements

The decider-shaped automaton in this library exposes nine explicit elements:

| # | Element | Provider | Implementation |
|---|---------|----------|----------------|
| 1 | Command type | Type parameter | `TCommand` |
| 2 | Event type | Type parameter | `TEvent` |
| 3 | State type | Type parameter | `TState` |
| 4 | Initial state | Automaton | `Initialize(parameters)` |
| 5 | Validate | **Decider** | `Validate(state, command)` |
| 6 | Authorize | **Decider** | `Authorize(state, validated, authorizationContext)` |
| 7 | Decide | **Decider** | `Decide(state, validated)` |
| 8 | Evolve | Automaton | `Transition(state, event)` |
| 9 | Is terminal | **Decider** | `IsTerminal(state)` |

`IsTerminal` defaults to `false`. Override it when your domain has a natural end-of-life:

```csharp
public static bool IsTerminal(OrderState state) =>
    state.Status is OrderStatus.Shipped or OrderStatus.Cancelled;
```

## What's Next

- **[Observability](06-observability.md)** — The DecidingRuntime emits tracing spans for every `Handle` call

### Deepen Your Understanding

| Topic | Link |
| ----- | ---- |
| The decider elements explained | [The Decider](../concepts/the-decider.md) |
| Map, Bind, MapError pipelines | [Error Handling Patterns](../guides/error-handling-patterns.md) |
| Migrating from Automaton to Decider | [Upgrading to Decider](../guides/upgrading-to-decider.md) |
| Result API signatures | [Result Reference](../reference/result.md) |
| DecidingRuntime API | [Decider Reference](../reference/decider.md) |
| Production ES with concurrency | [Picea.Patterns](../patterns/index.md) |
