# Building Custom Runtimes

How to wire your own Observer and Interpreter for domain-specific runtimes.

## The Pattern

Every runtime is just a specific combination of:

1. An **Automaton** (your domain logic)
2. An **Observer** (what to do after each transition)
3. An **Interpreter** (how to convert effects to feedback events)

```csharp
var runtime = await AutomatonRuntime<MyAutomaton, MyState, MyEvent, MyEffect, Unit>
    .Start(default, observer, interpreter);
```

## Building Blocks

### Minimal Runtime (No Side Effects)

```csharp
var runtime = await AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>
    .Start(
        default,
        observer: (_, _, _) => PipelineResult.Ok,
        interpreter: _ => new ValueTask<Result<CounterEvent[], PipelineError>>(
            Result<CounterEvent[], PipelineError>.Ok([])));
```

This is the simplest possible runtime: it transitions on events, produces no side effects, and generates no feedback.

### Logging Runtime

```csharp
Observer<CounterState, CounterEvent, CounterEffect> logger =
    (state, @event, effect) =>
    {
        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {@event.GetType().Name} → {state}");
        return PipelineResult.Ok;
    };

var runtime = await AutomatonRuntime<Counter, CounterState, CounterEvent, CounterEffect, Unit>
    .Start(default, logger, noOpInterpreter);
```

### Event Sourcing Runtime

```csharp
Observer<OrderState, OrderEvent, OrderEffect> persister =
    async (state, @event, effect) =>
    {
        await eventStore.Append(@event);
        return Result<Unit, PipelineError>.Ok(Unit.Value);
    };

Interpreter<OrderEffect, OrderEvent> projector =
    async effect =>
    {
        if (effect is OrderEffect.Confirmed(var orderId))
            await readModel.UpdateOrderStatus(orderId, "confirmed");
        return Result<OrderEvent[], PipelineError>.Ok([]);
    };

var runtime = await AutomatonRuntime<Order, OrderState, OrderEvent, OrderEffect, Unit>
    .Start(default, persister, projector);
```

### Composed Runtime

Combine multiple concerns:

```csharp
var observer = logger
    .Then(persister)
    .Then(metrics)
    .Catch(err =>
    {
        // Log and swallow non-critical errors
        Console.Error.WriteLine($"Pipeline error: {err.Message}");
        return Result<Unit, PipelineError>.Ok(Unit.Value);
    });

var runtime = await AutomatonRuntime<Order, OrderState, OrderEvent, OrderEffect, Unit>
    .Start(default, observer, projector);
```

## Controlling Initialization

The `Start` method calls `Initialize` and interprets the initial effect automatically. For most cases, this is the correct pattern:

```csharp
// Recommended: Use Start() for standard initialization
var runtime = await AutomatonRuntime<MyAutomaton, MyState, MyEvent, MyEffect, MyParams>
    .Start(
        parameters,
        observer,
        interpreter);
```

### Advanced: Custom Initialization Flow (Friend Assembly Only)

If you need precise control over initialization order (e.g., rendering before effects), this pattern is restricted to **friend assemblies explicitly granted `InternalsVisibleTo` access**:

```csharp
// Step 1: Initialize manually
var (initialState, initialEffect) = MyAutomaton.Initialize(parameters);

// Step 2: Render initial view BEFORE interpreting effects
RenderView(initialState);

// Step 3: Create runtime with pre-initialized state (internal constructor)
var runtime = new AutomatonRuntime<MyAutomaton, MyState, MyEvent, MyEffect, MyParams>(
    initialState, observer, interpreter);

// Step 4: Interpret initial effect manually
await runtime.InterpretEffect(initialEffect);
```

> The direct constructor is `internal` to ensure production code always uses the safe `Start()` factory. In this repository, only `Picea.Tests` and `Picea.Benchmarks` are granted friend access. Arbitrary consumer test projects cannot call it.

## Hydration (Event Replay)

For Event Sourcing, hydrate state by replaying events:

```csharp
var runtime = await AutomatonRuntime<Order, OrderState, OrderEvent, OrderEffect, Unit>
    .Start(default, observer, interpreter);

// Replay historical events
var events = await eventStore.ReadStream("order-123");
foreach (var @event in events)
    await runtime.Dispatch(@event);

// Or: Reset to a snapshot
var snapshot = await snapshotStore.Load<OrderState>("order-123");
runtime.Reset(snapshot);
```

## See Also

- [The Runtime](../concepts/the-runtime.md) — how the loop works
- [Observer Composition](observer-composition.md) — combining observers
- [Runtimes Compared](../concepts/runtimes-compared.md) — MVU vs ES vs Actor
