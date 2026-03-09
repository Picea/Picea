# Testing Strategies

How to test automatons at every level — from pure function unit tests to full integration tests.

## The Testing Pyramid for Automatons

```text
    ┌─────────────────┐
    │  Integration     │  Runtime + Observer + Interpreter together
    │  (few, slow)     │
    ├─────────────────┤
    │  Runtime          │  Dispatch events, assert state
    │  (some)           │
    ├─────────────────┤
    │  Pure function    │  Transition & Decide directly
    │  (many, fast)     │
    └─────────────────┘
```

Most of your tests should be at the bottom — pure function tests with no runtime, no async, and no infrastructure.

## Level 1: Pure Function Tests

`Transition` and `Decide` are pure functions. Test them directly:

### Testing Transition

```csharp
[Fact]
public void Transition_Increment_IncreasesCount()
{
    var state = new CounterState(5);

    var (newState, effect) = Counter.Transition(state, new CounterEvent.Increment());

    Assert.Equal(6, newState.Count);
    Assert.IsType<CounterEffect.None>(effect);
}
```

### Testing Decide

```csharp
[Fact]
public void Decide_Overflow_ReturnsError()
{
    var state = new CounterState(95);

    var result = Counter.Decide(state, new CounterCommand.Add(10));

    Assert.True(result.IsErr);
    var error = Assert.IsType<CounterError.Overflow>(result.Error);
    Assert.Equal(95, error.Current);
    Assert.Equal(10, error.Amount);
}
```

### Testing Determinism

```csharp
[Fact]
public void Decide_IsDeterministic()
{
    var state = new CounterState(5);
    var command = new CounterCommand.Add(3);

    var r1 = Counter.Decide(state, command);
    var r2 = Counter.Decide(state, command);

    Assert.Equal(r1.Value.Length, r2.Value.Length);
}
```

## Level 2: Runtime Tests

Test the full dispatch cycle with real Observer and Interpreter:

### Capture Observer

```csharp
[Fact]
public async Task Dispatch_RecordsAllTransitions()
{
    var log = new List<(CounterState State, CounterEvent Event, CounterEffect Effect)>();

    var runtime = await AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>
        .Start(
            default,
            observer: (state, @event, effect) =>
            {
                log.Add((state, @event, effect));
                return PipelineResult.Ok;
            },
            interpreter: _ => new ValueTask<Result<CounterEvent[], PipelineError>>(
                Result<CounterEvent[], PipelineError>.Ok([])));

    await runtime.Dispatch(new CounterEvent.Increment());
    await runtime.Dispatch(new CounterEvent.Increment());
    await runtime.Dispatch(new CounterEvent.Decrement());

    Assert.Equal(3, log.Count);
    Assert.Equal(1, runtime.State.Count);
}
```

## Level 3: Integration Tests

Test the full system including feedback loops:

```csharp
[Fact]
public async Task Interpreter_FeedbackLoop_ProducesMultipleTransitions()
{
    var runtime = await AutomatonRuntime<Thermostat, ThermostatState,
        ThermostatEvent, ThermostatEffect, Unit>.Start(
            default,
            observer: (_, _, _) => PipelineResult.Ok,
            interpreter: effect => new ValueTask<Result<ThermostatEvent[], PipelineError>>(
                Result<ThermostatEvent[], PipelineError>.Ok(effect switch
                {
                    ThermostatEffect.TurnOnHeater => [new ThermostatEvent.HeaterStarted()],
                    ThermostatEffect.TurnOffHeater => [new ThermostatEvent.HeaterStopped()],
                    _ => []
                })));

    await runtime.Dispatch(new ThermostatEvent.TemperatureReading(18.0m));

    Assert.True(runtime.State.Heating);
}
```

## Pattern: Reusable Test Helpers

```csharp
public static class CounterTestHelpers
{
    public static readonly Observer<CounterState, CounterEvent, CounterEffect> NoOp =
        (_, _, _) => PipelineResult.Ok;

    public static readonly Interpreter<CounterEffect, CounterEvent> NoOpInterpreter =
        _ => new ValueTask<Result<CounterEvent[], PipelineError>>(
            Result<CounterEvent[], PipelineError>.Ok([]));
}
```

## See Also

- [Tutorial 01](../tutorials/01-getting-started.md) — the thermostat example
- [Tutorial 05](../tutorials/05-command-validation.md) — testing Decide functions
- [Tutorial 06](../tutorials/06-observability.md) — testing tracing spans
