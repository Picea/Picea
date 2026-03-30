📌 Onboarded for **Picea Core** on 2026-03-30. Squad imported from Picea.Abies project; context refreshed.

# Tester — History

## About This File
Test strategy, TUnit patterns, edge case findings, and quality checks. Hockney owns test coverage and reliability.

## Test Framework: TUnit

**Why TUnit?**
- Source-generated test discovery — zero reflection overhead
- Parallel execution by default — tests run concurrently
- Async-first API — `async Task` is standard, no special attributes
- Native AOT compatible — future-proof

**Structure:**
```csharp
namespace Picea.Tests;

public class CounterTests
{
    [Test]
    public async Task Increment_IncrementsByOne()
    {
        var (newState, effect) = Counter.Transition(
            new CounterState(0),
            new CounterEvent.Increment());

        await Assert.That(newState.Count).IsEqualTo(1);
        await Assert.That(effect).IsInstanceOf<CounterEffect.DisplayCount>();
    }

    [Test]
    public async Task Initialize_StartsAtZero()
    {
        var (state, _) = Counter.Initialize(Unit.Default);

        await Assert.That(state.Count).IsEqualTo(0);
    }
}
```

**No `Arrange`, `Act`, `Assert` comments** — the test structure is clear.

## Domain Examples (Reference Tests)

These are the canonical test patterns:

### 1. Counter Automaton
- **File:** `Picea.Tests/CounterAutomaton.cs`
- **Tests:** `Picea.Tests/DeciderTests.cs` (subset)
- **Scope:** Simplest possible domain. Test initialization and state transitions.
- **Patterns:** Basic Mealy machine, effect data, exhaustive pattern matching

### 2. Thermostat Automaton
- **File:** `Picea.Tests/ThermostatAutomaton.cs`
- **Tests:** `Picea.Tests/DeciderTests.cs` (full example)
- **Scope:** Complex domain with validation, state invariants, conditional effects
- **Patterns:** Decider validation, command errors, state guards, optional effects

### 3. Result & Option Types
- **File:** `Picea.Tests/ResultTests.cs`, `Picea.Tests/OptionTests.cs`
- **Scope:** Algebraic operations (Map, Bind, Match), error propagation, chaining
- **Patterns:** Railway-oriented programming, struct implementation, zero allocations

## Test Coverage Strategy

### Critical Paths (Must Test)

1. **Initialization** — Happy path and any validation errors
   ```csharp
   [Test]
   public async Task Initialize_Valid() { ... }
   
   [Test]
   public async Task Initialize_InvalidParameter_ReturnsError() { ... }
   ```

2. **State Transitions** — Every event type, every state variant
   ```csharp
   [Test]
   public async Task Transition_FromStateA_WithEventX_TransitionsToStateB() { ... }
   ```

3. **Invariant Guards** — What should NOT happen
   ```csharp
   [Test]
   public async Task Transition_InvalidEvent_PreservesState() { ... }
   ```

4. **Effect Correctness** — Right effect for right state + event combo
   ```csharp
   [Test]
   public async Task Transition_ProducesCorrectEffect() { ... }
   ```

5. **Error Paths** — Validation failures, constraint violations
   ```csharp
   [Test]
   public async Task Validate_OutOfRange_ReturnsError() { ... }
   ```

### Good Edge Cases to Test

- **Boundary conditions:** Min/max values, empty collections, null inputs
- **State transitions:** Can't transition from state X to Y with event Z
- **Invariant violations:** Negative prices, dates in the past, duplicate IDs
- **Accumulation:** Running the same transition 1000 times doesn't break
- **Chaining:** Multiple events in sequence produce the right final state

### Anti-Patterns ❌

❌ Test implementation details (private methods, internal fields)  
❌ Mock the domain — test the domain, not how it's called  
❌ Duplicate tests — one test per behavior, not per code path  
❌ Skip edge cases because they're "unlikely" — Mealy machines are deterministic; test all paths  

## Parallel-Safe Testing

TUnit runs tests in parallel. Keep tests independent:

```csharp
// ✅ Good — no shared state
[Test]
public async Task Each_Test_IsIndependent()
{
    var state = new CounterState(0); // private, local
    var (newState, _) = Counter.Transition(state, new CounterEvent.Increment());
    await Assert.That(newState.Count).IsEqualTo(1);
}

// ❌ Bad — shared mutable field
private static int _sharedCounter = 0; // DANGER: race condition

[Test]
public async Task Test_That_Modifies_Shared_State()
{
    _sharedCounter++;
    await Assert.That(_sharedCounter).IsGreaterThan(0); // Flaky!
}
```

## Current Test Coverage

- **Counter:** Initialize, Increment, Decrement, reset to zero
- **Thermostat:** SetTarget, RecordReading, heating guard, temperature bounds
- **Result:** Ok, Error, Bind, Map, Match, error propagation
- **Option:** Some, None, Map, Bind, Match, default handling
- **Decider (Thermostat):** Command validation, event generation, error cases

See `Picea.Tests.csproj` for the full test list.

## No Test Blockers

All critical paths have coverage. Ready for feature additions — Hockney will add tests in parallel.
