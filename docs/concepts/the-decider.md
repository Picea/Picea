# The Decider

The Decider adds command validation to the Automaton — separating intent (commands) from facts (events).

## The Interface

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
    static abstract Result<TEvent[], TError> Decide(TState state, Validated<TCommand, TError> command);
    static virtual bool IsTerminal(TState state) => false;
}
```

## The Staged Pipeline

```text
Command (intent)
  └─▶ Validate   : Command -> Validated<Command, Error>
        ├─ Invalid(error) -> reject
        └─ Valid(command')
              └─▶ Authorize : (Validated<Command, Error>, AuthContext) -> Result<Unit, Error>
                    ├─ Err(error) -> reject
                    └─ Ok(Unit)
                          └─▶ Decide : Validated<Command, Error> -> Result<Events, Error>
                                ├─ Err(error) -> reject
                                └─ Ok(events) -> dispatch through Transition
```

All stages are pure functions and short-circuit on first rejection.

## The Nine Elements

The decider-shaped automaton in this library exposes nine explicit elements:

| # | Element | Provider | Implementation |
|---|---------|----------|----------------|
| 1 | Command type | Type parameter | `TCommand` |
| 2 | Event type | Type parameter | `TEvent` |
| 3 | State type | Type parameter | `TState` |
| 4 | Initial state | Automaton | `Initialize(parameters)` |
| 5 | **Validate** | **Decider** | `Validate(state, command)` |
| 6 | **Authorize** | **Decider** | `Authorize(state, validated, authorizationContext)` |
| 7 | **Decide** | **Decider** | `Decide(state, validated)` |
| 8 | Evolve | Automaton | `Transition(state, event)` |
| 9 | **Is terminal** | **Decider** | `IsTerminal(state)` |

## Intent vs. Fact

```text
Command (intent)  →  Validate  →  Authorize  →  Decide  →  Events (facts)  →  Transition  →  State
                         │            │            │
                         └────────────┴────────────┴──▶  Error (rejection)
```

- **Commands** describe what the user *wants* to do
- **Events** describe what *actually happened*
- **Errors** describe why a command was *rejected*

## Validate: Feasibility

`Validate` checks domain invariants and returns a typed proof object:

```csharp
public static Validated<CounterCommand, CounterError> Validate(
    CounterState state, CounterCommand command) =>
    command switch
    {
        CounterCommand.Add(var n) when state.Count + n > MaxCount =>
            new Validated<CounterCommand, CounterError>.Invalid(
                new CounterError.Overflow(state.Count, n, MaxCount)),

        _ => new Validated<CounterCommand, CounterError>.Valid(command)
    };
```

## Authorize: Permission

`Authorize` checks whether a validated command is permitted in the current state.

```csharp
public static Result<Unit, CounterError> Authorize<TAuthorizationContext>(
    CounterState state,
    Validated<CounterCommand, CounterError> validated,
    TAuthorizationContext authorizationContext) =>
    Result<Unit, CounterError>.Ok(Unit.Value);
```

- `Ok(Unit.Value)` means permitted.
- `Err(error)` means denied.

### Authorization Context Principles

- Keep the authorization context at the application boundary. Pass it into `Handle(command, authorizationContext, ...)` and `Authorize<TAuthorizationContext>(...)`, but do not persist raw credentials/tokens in domain state.
- Prefer constrained context types over primitives (`UserId`, `Roles`, `TenantId`, claims snapshot) so required auth data is explicit and testable.
- Keep `Authorize` pure and deterministic for a given `(state, validatedCommand, authorizationContext)`.
- Return domain authorization errors in `TError` (`Err(error)`), not exceptions, so denial stays in the typed pipeline.

Example shape:

```csharp
public sealed record UserContext(Guid UserId, bool CanIncrement);

public static Result<Unit, CounterError> Authorize(
    CounterState state,
    Validated<CounterCommand, CounterError> validated,
    UserContext authorizationContext) =>
    authorizationContext.CanIncrement
        ? Result<Unit, CounterError>.Ok(Unit.Value)
        : Result<Unit, CounterError>.Err(new CounterError.Forbidden(authorizationContext.UserId));
```

## Decide: Event Production

`Decide` is pure: given state and a validated command, it returns either events or an error:

```csharp
public static Result<CounterEvent[], CounterError> Decide(
    CounterState state, Validated<CounterCommand, CounterError> validated) =>
    validated is not Validated<CounterCommand, CounterError>.Valid(var command)
        ? throw new UnreachableException()
        : command switch
        {
            CounterCommand.Add(var n) =>
                Result<CounterEvent[], CounterError>
                    .Ok(Enumerable.Repeat<CounterEvent>(
                        new CounterEvent.Increment(), n).ToArray()),

            _ => throw new UnreachableException()
        };
```

- `Ok(events)` — command accepted; events will be dispatched
- `Err(error)` — command rejected; state remains unchanged
- `Ok([])` — "accepted but nothing happened" (idempotent)

## DecidingRuntime

The `DecidingRuntime` wraps `AutomatonRuntime` and adds `Handle(command, ...)`:

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

The entire `Handle` operation (Validate + Authorize + Decide + all Dispatches) executes under a single lock acquisition. This prevents TOCTOU races.

## Formal Composition

The internal `DeciderComposition` helper provides explicit monadic composition for the three stages:

```csharp
var result = DeciderComposition.Compose(
    state,
    command,
    authorizationContext,
    validate: Counter.Validate,
    authorize: Counter.Authorize,
    decide: Counter.Decide);
```

This keeps the short-circuit behavior explicit and law-testable.

## Non-Breaking Upgrade

Because `Decider<...> : Automaton<...>`, adding command validation is a non-breaking upgrade:

1. Define command and error types
2. Change `Automaton<...>` to `Decider<...>`
3. Add `Validate`, optionally override `Authorize`, and add `Decide`
4. All existing code continues to work — the Decider is still a valid Automaton

This follows the [Open/Closed Principle](https://en.wikipedia.org/wiki/Open%E2%80%93closed_principle): open for extension, closed for modification.

## See Also

- [The Kernel](the-kernel.md) — the base Automaton interface
- [Error Handling Patterns](../guides/error-handling-patterns.md) — Map/Bind/MapError recipes
- [Upgrading to Decider](../guides/upgrading-to-decider.md) — migration guide
- [Decider Reference](../reference/decider.md) — full API
- [Tutorial 05](../tutorials/05-command-validation.md) — hands-on walkthrough
- [ADR 004](../adr/004-decider-pattern-command-validation.md) — design rationale
