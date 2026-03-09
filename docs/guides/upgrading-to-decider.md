# Upgrading to Decider

How to add command validation to an existing Automaton — a non-breaking upgrade.

## Why Upgrade?

The basic Automaton accepts any event:

```csharp
await runtime.Dispatch(new CounterEvent.Increment()); // always succeeds
```

The Decider adds a validation layer between user intent (commands) and facts (events):

```csharp
var result = await runtime.Handle(new CounterCommand.Add(200));
// result is Err(Overflow) — state unchanged, no events dispatched
```

## Step 1: Define Commands and Errors

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

## Step 2: Change the Interface

```csharp
// Before
public class Counter : Automaton<CounterState, CounterEvent, CounterEffect, Unit>

// After
public class Counter
    : Decider<CounterState, CounterCommand, CounterEvent, CounterEffect, CounterError, Unit>
```

Because `Decider<...> : Automaton<...>`, your existing `Initialize` and `Transition` methods are still valid.

## Step 3: Add the Decide Function

```csharp
public static Result<CounterEvent[], CounterError> Decide(
    CounterState state, CounterCommand command) =>
    command switch
    {
        CounterCommand.Add(var n) when state.Count + n > MaxCount =>
            Result<CounterEvent[], CounterError>
                .Err(new CounterError.Overflow(state.Count, n, MaxCount)),
        // ... other cases
        _ => throw new UnreachableException()
    };
```

## Step 4: Choose Your Runtime

**Option A: DecidingRuntime** (recommended)

```csharp
var runtime = await DecidingRuntime<Counter, CounterState, CounterCommand,
    CounterEvent, CounterEffect, CounterError, Unit>.Start(default, observer, interpreter);

var result = await runtime.Handle(new CounterCommand.Add(5));
```

**Option B: Keep AutomatonRuntime** (bypasses validation)

```csharp
var runtime = await AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>
    .Start(default, observer, interpreter);

await runtime.Dispatch(new CounterEvent.Increment()); // bypasses Decide
```

## What Didn't Break

- All existing `AutomatonRuntime` usage continues to work
- All existing tests that call `Transition` directly still pass
- All existing observers and interpreters are compatible
- The `DecidingRuntime` is a new addition, not a replacement

The Decider follows the [Open/Closed Principle](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle).

## See Also

- [The Decider](../concepts/the-decider.md) — conceptual explanation
- [Tutorial 05](../tutorials/05-command-validation.md) — full walkthrough
- [Error Handling Patterns](error-handling-patterns.md) — Result pipelines
