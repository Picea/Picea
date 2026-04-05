# Getting Started

Get up and running with Picea in 5 minutes.

## Quick Start

### 1. Install

```bash
dotnet add package Picea
```

### 2. Define Your Domain

```csharp
using Picea;

public record CounterState(int Count);

public interface CounterEvent
{
    record struct Increment : CounterEvent;
    record struct Decrement : CounterEvent;
}

public interface CounterEffect
{
    record struct None : CounterEffect;
}
```

### 3. Implement the Automaton

```csharp
public class Counter : Automaton<CounterState, CounterEvent, CounterEffect, Unit>
{
    public static (CounterState, CounterEffect) Initialize(Unit _) =>
        (new CounterState(0), new CounterEffect.None());

    public static (CounterState, CounterEffect) Transition(
        CounterState state, CounterEvent @event) =>
        @event switch
        {
            CounterEvent.Increment =>
                (state with { Count = state.Count + 1 }, new CounterEffect.None()),
            CounterEvent.Decrement =>
                (state with { Count = state.Count - 1 }, new CounterEffect.None()),
            _ => throw new UnreachableException()
        };
}
```

### 4. Run It

```csharp
var runtime = await AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>
    .Start(
        default,
        observer: (state, @event, effect) =>
        {
            Console.WriteLine($"{@event.GetType().Name} → {state}");
            return PipelineResult.Ok;
        },
        interpreter: _ => new ValueTask<Result<CounterEvent[], PipelineError>>(
            Result<CounterEvent[], PipelineError>.Ok([])));

await runtime.Dispatch(new CounterEvent.Increment());
await runtime.Dispatch(new CounterEvent.Increment());
await runtime.Dispatch(new CounterEvent.Decrement());

Console.WriteLine(runtime.State.Count); // 1
```

### 5. Test It (No Runtime Needed)

```csharp
var (state, _) = Counter.Initialize(default);
Assert.Equal(0, state.Count);

var (next, _) = Counter.Transition(state, new CounterEvent.Increment());
Assert.Equal(1, next.Count);
```

## What Just Happened?

1. You defined **state**, **events**, and **effects** as simple C# types
2. You wrote a **pure transition function** — no I/O, no side effects, no dependencies
3. The **runtime** executed the transition function in a loop, calling your observer and interpreter
4. You tested the transition function **directly** — no runtime, no async, no mocking

## Choose a Command Runtime

When your model evolves from event dispatch (`AutomatonRuntime`) to command handling, pick the runtime that matches your needs:

| If you need... | Use | Why |
| -------------- | --- | --- |
| Command handling with domain validation only | `DecidingRuntime` | `Decider.Decide` returns `Result<TEvent[], TError>` in one stage |
| Explicit authorization + validation + decision stages | `GuardedDecidingRuntime` | Runs `Authorize -> Validate -> Decide` atomically and supports denial auditing |

Reference docs:

- [Decider, DecidingRuntime](../reference/decider.md)
- [GuardedDecider, GuardedDecidingRuntime](../reference/guarded-decider.md)

### Explicit Choice: Where Do Command Guards Run?

Choose the runtime path deliberately based on where authorization/validation responsibility belongs.

| If your intent is... | Choose | Consequence |
| -------------------- | ------ | ----------- |
| Runtime-enforced command checks in one atomic command boundary | `GuardedDecidingRuntime` | `Authorize -> Validate -> Decide` always runs inside `Handle(principal, command)` |
| Command validation only (no principal policy stage) | `DecidingRuntime` | Single-stage `Decide`, then dispatch through transitions |
| Transition/event execution where command checks are handled upstream or not needed (for example replay) | `AutomatonRuntime` (even with a guarded model type) | No runtime invocation of `Authorize`/`Validate`; event dispatch + transition loop only |

If you choose `AutomatonRuntime` with a guarded model type, make that choice explicit in architecture docs so the enforcement boundary is clear.

## What's Next

| If you want to… | Read |
| ---------------- | ---- |
| Understand the theory | [The Kernel](../concepts/the-kernel.md) |
| Build a complete system | [Tutorial 01: Getting Started](../tutorials/01-getting-started.md) |
| Add command validation | [The Decider](../concepts/the-decider.md) |
| Choose a runtime pattern | [Runtimes Compared](../concepts/runtimes-compared.md) |
| See the full API | [Reference](../reference/index.md) |
