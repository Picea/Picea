# The Decider

The Decider adds command validation to the Automaton — separating intent (commands) from facts (events).

## The Interface

```csharp
public interface Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract Result<TEvent[], TError> Decide(TState state, TCommand command);
    static virtual bool IsTerminal(TState state) => false;
}
```

## The Seven Elements

The Decider pattern (Chassaing, 2021) has seven elements. The Automaton kernel provides four, the Decider adds three:

| # | Element | Provider | Implementation |
|---|---------|----------|----------------|
| 1 | Command type | Type parameter | `TCommand` |
| 2 | Event type | Type parameter | `TEvent` |
| 3 | State type | Type parameter | `TState` |
| 4 | Initial state | Automaton | `Initialize(parameters)` |
| 5 | **Decide** | **Decider** | `Decide(state, command)` |
| 6 | Evolve | Automaton | `Transition(state, event)` |
| 7 | **Is terminal** | **Decider** | `IsTerminal(state)` |

## Intent vs. Fact

```text
Command (intent)  →  Decide  →  Events (facts)  →  Transition  →  State
                         │
                         └──▶  Error (rejection)
```

- **Commands** describe what the user *wants* to do
- **Events** describe what *actually happened*
- **Errors** describe why a command was *rejected*

## The Decide Function

`Decide` is pure: given state and command, it returns either events or an error:

```csharp
public static Result<CounterEvent[], CounterError> Decide(
    CounterState state, CounterCommand command) =>
    command switch
    {
        CounterCommand.Add(var n) when state.Count + n > MaxCount =>
            Result<CounterEvent[], CounterError>
                .Err(new CounterError.Overflow(state.Count, n, MaxCount)),

        CounterCommand.Add(var n) =>
            Result<CounterEvent[], CounterError>
                .Ok(Enumerable.Repeat<CounterEvent>(
                    new CounterEvent.Increment(), n).ToArray()),

        // ... other cases
        _ => throw new UnreachableException()
    };
```

- `Ok(events)` — command accepted; events will be dispatched
- `Err(error)` — command rejected; state remains unchanged
- `Ok([])` — "accepted but nothing happened" (idempotent)

## Additive Secure Staging

The Decide-only `Decider` remains the default baseline.

For stricter command hardening, an additive secure staged model introduces:

- `Validator` — state-based feasibility and invariant checks
- `Policy` — authorization checks (optionally using caller context)
- `GuardedDecider` — explicit staged contract (`Authorize` -> `Validate` -> `Decide`)
- `GuardedDecidingRuntime` — executes the staged flow atomically

Conceptual flow:

```text
Command -> Authorize -> Validate -> Decide -> Events -> Transition -> State
                      \-> Error
                                  \-> Error
```

The first rejecting stage returns `Err(error)` and stops processing. If all stages pass, events are dispatched exactly like `Decider`.

## DecidingRuntime

The `DecidingRuntime` wraps `AutomatonRuntime` and adds `Handle(command)`:

```csharp
var runtime = await DecidingRuntime<Counter, CounterState, CounterCommand,
    CounterEvent, CounterEffect, CounterError, Unit>.Start(default, observer, interpreter);

var result = await runtime.Handle(new CounterCommand.Add(5));
// result is Ok(CounterState { Count = 5 })

var overflow = await runtime.Handle(new CounterCommand.Add(200));
// overflow is Err(CounterError.Overflow { Current = 5, Amount = 200, Max = 100 })
// State is unchanged — still 5
```

### Atomicity

The entire `Handle` operation (Decide + all Dispatches) executes under a single lock acquisition. This prevents TOCTOU races.

## Non-Breaking Upgrade

Because `Decider<...> : Automaton<...>`, adding command validation is a non-breaking upgrade:

1. Define command and error types
2. Change `Automaton<...>` to `Decider<...>`
3. Add the `Decide` function
4. All existing code continues to work — the Decider is still a valid Automaton

This follows the [Open/Closed Principle](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle): open for extension, closed for modification.

## See Also

- [The Kernel](the-kernel.md) — the base Automaton interface
- [Error Handling Patterns](../guides/error-handling-patterns.md) — Map/Bind/MapError recipes
- [Upgrading to Decider](../guides/upgrading-to-decider.md) — migration guide
- [Decider Reference](../reference/decider.md) — full API
- [Tutorial 05](../tutorials/05-command-validation.md) — hands-on walkthrough
- [ADR 004](../adr/004-decider-pattern-command-validation.md) — design rationale
