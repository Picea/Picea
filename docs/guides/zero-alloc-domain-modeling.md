# Zero-Alloc Domain Modeling

How to eliminate heap allocations on Picea hot paths by choosing the right domain type strategy.

## The Problem

Picea's runtime is generic over your domain types (`TState`, `TEvent`, `TEffect`, etc.). The framework itself is allocation-free on the happy path — but **your domain type choices determine whether boxing occurs at generic boundaries**.

The most common C# pattern for discriminated unions — `interface` + `record struct` — causes boxing every time a value-type variant crosses a generic parameter typed as the interface.

## Why Boxing Happens

Consider a typical domain:

```csharp
public interface MyEvent
{
    record struct OrderPlaced(Guid Id) : MyEvent;  // 16 bytes on the stack
    record struct ItemAdded(string Sku) : MyEvent;
}
```

When `Transition` returns `(TState, TEffect)` and `TEffect` is typed as `MyEffect` (an interface), the `record struct` variant must be **boxed** to satisfy the interface reference — costing **24 bytes** of heap allocation per crossing.

In a lean dispatch cycle, boxing happens at three points:

| Operation | Source | Cost |
| --- | --- | --- |
| `Transition` returns `TEffect` | `record struct` → interface | 24 B |
| `Transition` returns `TState` | `record` class allocation | 24 B |
| Observer receives `TEvent` | `record struct` → interface | 24 B |
| **Total** | | **72 B** |

These aren't framework allocations — they're CLR boxing operations caused by the value-type-behind-interface pattern.

## The Solution: Abstract Record Hierarchies

Replace `interface` + `record struct` with `abstract record` + `sealed record`:

```csharp
// ❌ Interface + record struct (boxes at generic boundaries)
public interface MyEvent
{
    record struct OrderPlaced(Guid Id) : MyEvent;
}

// ✅ Abstract record hierarchy (no boxing — already a reference type)
public abstract record MyEvent
{
    public sealed record OrderPlaced(Guid Id) : MyEvent;
    public sealed record ItemAdded(string Sku) : MyEvent;
}
```

Since `abstract record` is a reference type, passing a `sealed record` variant through a generic `TEvent` parameter requires no boxing — it's already on the heap (or cached).

## Three Techniques Combined

For true zero-alloc hot paths, combine three techniques:

### 1. Abstract Record DUs (No Boxing)

```csharp
public abstract record MyEffect
{
    public sealed record None : MyEffect;
    public sealed record SendEmail(string To) : MyEffect;
}
```

### 2. Record Struct State (Stack-Allocated)

```csharp
// ✅ Value type — stays on the stack through generic TState
public record struct CounterState(int Value);

// ❌ Record class — 24 B heap allocation per transition
public record CounterState(int Value);
```

### 3. Cached Singletons (Reuse Common Instances)

```csharp
public abstract record MyEffect
{
    /// <summary>Cached singleton — 0 B per use after first allocation.</summary>
    public static readonly MyEffect NoneInstance = new None();

    public sealed record None : MyEffect;
    public sealed record SendEmail(string To) : MyEffect;
}

public abstract record MyError
{
    public static readonly MyError NotFoundInstance = new NotFound();

    public sealed record NotFound : MyError;
    public sealed record InvalidQuantity(int Qty) : MyError;
}
```

Use the singleton in `Transition` and `Decide`:

```csharp
public static (MyState State, MyEffect Effect) Transition(MyState state, MyEvent @event) =>
    @event switch
    {
        MyEvent.OrderPlaced(var id) =>
            (state with { OrderId = id }, MyEffect.NoneInstance),  // 0 B
        // ...
    };
```

### 4. InterpreterResult\<TEvent\>.Empty (Cached No-Op Return)

For interpreters that produce no feedback events:

```csharp
// ❌ Allocates a new array + wraps in ValueTask every call
_ => new ValueTask<Result<MyEvent[], PipelineError>>(
    Result<MyEvent[], PipelineError>.Ok([]));

// ✅ Pre-allocated, cached, zero-alloc
_ => InterpreterResult<MyEvent>.Empty;
```

## Complete Example

```csharp
// ── State (value type) ───────────────────────────
public record struct OrderState(int ItemCount, decimal Total);

// ── Events (abstract record, no boxing) ──────────
public abstract record OrderEvent
{
    public sealed record ItemAdded(string Sku, decimal Price) : OrderEvent;
    public sealed record OrderConfirmed : OrderEvent;
}

// ── Effects (cached singleton for None) ──────────
public abstract record OrderEffect
{
    public static readonly OrderEffect NoneInstance = new None();

    public sealed record None : OrderEffect;
    public sealed record SendConfirmation(string Email) : OrderEffect;
}

// ── Commands ─────────────────────────────────────
public abstract record OrderCommand
{
    public sealed record AddItem(string Sku, decimal Price) : OrderCommand;
    public sealed record Confirm(string Email) : OrderCommand;
}

// ── Errors (cached singleton) ────────────────────
public abstract record OrderError
{
    public static readonly OrderError EmptyOrderInstance = new EmptyOrder();

    public sealed record EmptyOrder : OrderError;
}

// ── Automaton ────────────────────────────────────
public class OrderAutomaton
    : Automaton<OrderState, OrderEvent, OrderEffect, Unit>
{
    public static (OrderState, OrderEffect) Initialize(Unit _) =>
        (new OrderState(0, 0m), OrderEffect.NoneInstance);

    public static (OrderState, OrderEffect) Transition(
        OrderState state, OrderEvent @event) =>
        @event switch
        {
            OrderEvent.ItemAdded(_, var price) =>
                (new OrderState(state.ItemCount + 1, state.Total + price),
                 OrderEffect.NoneInstance),

            OrderEvent.OrderConfirmed =>
                (state, new OrderEffect.SendConfirmation("customer@example.com")),

            _ => throw new UnreachableException()
        };
}

// ── Interpreter (zero-alloc no-op path) ──────────
var interpreter = effect => effect switch
{
    OrderEffect.SendConfirmation(var email) =>
        new ValueTask<Result<OrderEvent[], PipelineError>>(
            Result<OrderEvent[], PipelineError>.Ok([new OrderEvent.OrderConfirmed()])),

    _ => InterpreterResult<OrderEvent>.Empty  // 0 B
};
```

## Benchmark Evidence

Measured on Apple M4 Pro, .NET 10.0.3, BenchmarkDotNet 0.15.8:

| Benchmark | Interface + Record Struct | Abstract Record + Singletons | Δ Alloc | Δ Speed |
| --- | ---: | ---: | --- | --- |
| Lean Dispatch | 694 ns / **72 B** | 236 ns / **0 B** | −100% | 2.9× faster |
| Lean Dispatch + feedback | 1,080 ns / 176 B | 876 ns / 80 B | −55% | 1.2× faster |
| Handle — accept | 1,545 ns / 128 B | 764 ns / 56 B | −56% | 2.0× faster |
| Handle — reject | 571 ns / **48 B** | 216 ns / **0 B** | −100% | 2.6× faster |

The two zero-alloc paths (**Lean Dispatch** and **Handle — reject**) achieve **literally 0 bytes allocated**. The remaining allocations in "accept" and "feedback" come from the event array (`TEvent[]`) that `Decide` and interpreters must construct — these are inherent to producing new events, not framework overhead.

## When to Use Which Pattern

| Pattern | Allocations | Ergonomics | Best For |
| --- | --- | --- | --- |
| `interface` + `record struct` | Moderate (boxing) | Natural C# DU idiom | Prototyping, low-throughput paths |
| `abstract record` + singletons | Minimal to zero | Slightly more boilerplate | Hot paths, high-throughput systems |

Both patterns work with all Picea APIs. The framework is generic and allocation-free — the choice is yours.

> **Guideline:** Start with `interface` + `record struct` for simplicity. Profile with BenchmarkDotNet. Switch to `abstract record` on paths where allocation pressure matters.

## Trade-Offs

### Abstract Record Advantages

- No boxing at generic boundaries
- Pattern matching works identically
- Cached singletons eliminate repeat allocations
- Measurably faster (2–3× on lean dispatch)

### Abstract Record Costs

- Each variant is a heap object (even if small) — but this is the same cost as boxing, paid once instead of per-call when singletons are used
- Parameterless variants (e.g. `None`, `Rejected`) should be cached as singletons to avoid repeated allocation
- Slightly more boilerplate than the interface pattern

### Record Struct State Advantages

- Zero heap allocation — lives on the stack
- Copied by value through generic `TState` without boxing

### Record Struct State Costs

- Large state records (many fields) increase copy cost
- Reference semantics (`record class`) may be preferable for large state

## See Also

- [ADR 011: Performance Optimizations](../adr/011-performance-optimizations-allocation-reduction.md) — Framework-level allocation elimination
- [Building Custom Runtimes](building-custom-runtimes.md) — Wiring observers and interpreters
- [The Kernel](../concepts/the-kernel.md) — Understanding the Mealy machine
- [Runtime Reference](../reference/runtime.md) — `InterpreterResult<TEvent>.Empty` API
