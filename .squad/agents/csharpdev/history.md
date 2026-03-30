📌 Onboarded for **Picea Core** on 2026-03-30. Squad imported from Picea.Abies project; context refreshed.

# Senior C# Developer — History

## About This File
Implementation patterns, domain logic approaches, and C# language learnings. Fenster owns the functional core.

## Picea Implementation Patterns

### The Automaton Interface

Always implement as pure, stateless functions:

```csharp
public class Counter : Automaton<CounterState, CounterEvent, CounterEffect, Unit>
{
    public static (CounterState, CounterEffect) Initialize(Unit _) =>
        (new CounterState(0), new CounterEffect.None());

    public static (CounterState, CounterEffect) Transition(CounterState state, CounterEvent @event) =>
        @event switch
        {
            CounterEvent.Increment => 
                (state with { Count = state.Count + 1 }, new CounterEffect.DisplayCount()),
            CounterEvent.Decrement => 
                (state with { Count = state.Count - 1 }, new CounterEffect.DisplayCount()),
            _ => throw new UnreachableException()
        };
}
```

**Key properties:**
- All methods are `static` — no instance state ever
- Use `state with { ... }` for immutable updates (records)
- Returns tuple `(newState, effect)` — both changes together
- Exhaustive `switch` — compile-time guarantee all cases handled
- Result is deterministic: same inputs = same outputs always

### Smart Constructors (Constrained Types)

Guard domain invariants at the type level:

```csharp
public readonly record struct EmailAddress
{
    private const int MaxLength = 255;

    private readonly string? _value;

    private EmailAddress(string value) => _value = value;

    public string Value => _value ?? string.Empty;

    public static Result<EmailAddress, EmailError> Create(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Result.Error(EmailError.Empty);
        if (input.Length > MaxLength)
            return Result.Error(EmailError.TooLong);
        if (!input.Contains("@"))
            return Result.Error(EmailError.InvalidFormat);
        return new EmailAddress(input);
    }
}
```

**When to use:**
- Domain primitives that need validation (Email, Username, Slug, ID types)
- Constraints that are fundamental to the domain (max length, format, minimum value)
- Any time an `input`→`output` check is a business rule, not just data validation

### Railway-Oriented Programming (ROP)

Link operations that can fail using `Result<T, TError>`:

```csharp
public static Result<Order, OrderError> CreateOrder(CreateOrderCommand cmd, InventoryService inventory) =>
    ValidateOrderItems(cmd.Items)
        .Bind(validItems => CheckInventory(inventory, validItems))
        .Bind(inventoryStatus => CalculatePrice(cmd, inventoryStatus))
        .Map(price => new Order(cmd.CustomerId, cmd.Items, price));
```

**Benefits:**
- Short-circuit on first error — no nested if/else pyramids
- Explicit error flow — caller sees all possible errors
- Composable — chain operations naturally
- Testable — mock effects at boundaries

### Workflow Structure (Decider Pattern)

A command-driven workflow is always:

```csharp
public interface ThermostatCommand
{
    record struct SetTarget(decimal Temperature) : ThermostatCommand;
    record struct RecordReading(decimal CurrentTemp) : ThermostatCommand;
}

public interface ThermostatEvent
{
    record struct TargetSet(decimal Temperature) : ThermostatEvent;
    record struct TemperatureRecorded(decimal CurrentTemp) : ThermostatEvent;
}

public interface ThermostatError
{
    record struct OutOfRange(decimal Temperature) : ThermostatError;
}

public static class ThermostatDecider
{
    public static Result<ThermostatCommand, ThermostatError> Validate(
        ThermostatState state, 
        ThermostatCommand cmd) =>
        cmd switch
        {
            SetTarget({ Temperature: >= 5 and <= 35 }) => Result.Ok(cmd),
            SetTarget => Result.Error(new ThermostatError.OutOfRange(...)),
            _ => Result.Ok(cmd)
        };

    public static Result<ThermostatEvent, ThermostatError> Decide(
        ThermostatState state,
        ThermostatCommand cmd) =>
        cmd switch
        {
            SetTarget setCmd => Result.Ok<ThermostatEvent>(
                new TargetSet(setCmd.Temperature)),
            RecordReading readCmd => Result.Ok<ThermostatEvent>(
                new TemperatureRecorded(readCmd.CurrentTemp)),
            _ => Result.Error(...)
        };
}
```

### Option<T> for Optionals

Never use `null`. Use `Option<T>`:

```csharp
public readonly record struct Option<T>
{
    private readonly T? _value;
    private readonly bool _hasValue;

    // ... factory methods
    public static Option<T> Some(T value) => new(value, true);
    public static Option<T> None() => default;

    public Result<TSuccess> Match<TSuccess>(
        Func<T, TSuccess> onSome,
        Func<TSuccess> onNone) =>
        _hasValue ? onSome(_value!) : onNone();
}
```

Use in domain code:
```csharp
public record Order(Guid Id, List<Item> Items, Option<DiscountCode> AppliedDiscount);
```

### Result<T, TError> for Expected Failures

Always return errors as values:

```csharp
public static Result<Order, OrderError> ValidateOrder(Order order) =>
    order.Items.Count == 0
        ? Result.Error<Order>(new OrderError.NoItems())
        : order.TotalPrice < 0
        ? Result.Error<Order>(new OrderError.NegativePrice())
        : Result.Ok(order);
```

**Never throw exceptions for expected domain errors.** Exceptions are for programmer bugs and unrecoverable infrastructure failures.

### Null Annotations

Declare non-nullable by default:

```csharp
// ✅ Non-nullable by default
public string Name { get; } = "Default";

// ✅ Nullable when needed
public string? Description { get; } = null;

// ✅ Check at entry points
public void Process(string? input)
{
    if (input is null) return; // guard
    Use(input); // now non-null
}
```

## Performance Patterns

### Hot Path Optimization (Kernel Transition)

The `Transition(state, @event)` method is called millions of times. Measure with BenchmarkDotNet:

```csharp
[Benchmark]
public (CounterState, CounterEffect) TransitionBenchmark()
{
    return Counter.Transition(new CounterState(42), new CounterEvent.Increment());
}
```

Keep the transition function allocation-free if possible. Allocations in cold paths (initialization, logging) are fine.

### Comment Hot Path Decisions

```csharp
// PERF: Avoid allocation by pattern matching on struct discriminant
// instead of calling GetType(). Measured 10% throughput improvement.
public static (TState State, TEffect Effect) Transition(TState state, TEvent @event) =>
    @event switch { ... };
```

## Integration with TUnit Tests

TUnit generates tests at compile time. Structure tests in the domain:

```csharp
public class CounterTests
{
    [Test]
    public async Task Increment_IncrementsCount()
    {
        var (state, effect) = Counter.Transition(
            new CounterState(0),
            new CounterEvent.Increment());

        await Assert.That(state.Count).IsEqualTo(1);
    }
}
```

No `Arrange`, `Act`, `Assert` comments. TUnit is parallel by default — no shared state.

## C# Language Features

- **Records:** Default for state and events (immutable, value equality)
- **Pattern matching:** Prefer over if/else and traditional switch
- **Switch expressions:** `x switch { ... }` over switch statements
- **Required init-only:** `public required string Name { get; init; }` for aggregate creation
- **Primary constructors (C# 12+):** `public record Counter(int Count);`
- **using declarations:** File-scoped namespaces, single-line imports

## No Implementation Blockers

Fenster is ready to build. Designs are clear; domain patterns established. See `.squad/decisions.md` for constraints.
