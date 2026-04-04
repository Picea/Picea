# Decider, DecidingRuntime

`namespace Picea`

Staged command-processing layer for Automatons.

---

## Decider&lt;TState, TCommand, TEvent, TEffect, TError, TParameters&gt;

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

## Validated&lt;TCommand, TError&gt;

```csharp
public abstract record Validated<TCommand, TError>
{
    public sealed record Valid(TCommand Value) : Validated<TCommand, TError>;
    public sealed record Invalid(TError InvalidError) : Validated<TCommand, TError>;
}
```

The output of the validation stage.

- `Valid(command)` — command is feasible for the current state.
- `Invalid(error)` — command violates a domain invariant.

### Validate

```csharp
static abstract Validated<TCommand, TError> Validate(TState state, TCommand command);
```

Stage 1 (feasibility). Must be pure.

### Authorize

```csharp
static virtual Result<Unit, TError> Authorize<TAuthorizationContext>(
    TState state,
    Validated<TCommand, TError> command,
    TAuthorizationContext authorizationContext) =>
    Result<Unit, TError>.Ok(Unit.Value);
```

Stage 2 (permission). Must be pure.

- `Ok(Unit.Value)` — authorized.
- `Err(error)` — denied.

The default implementation allows all validated commands.

Authorization-context guidance:

- Provide caller/application context via `TAuthorizationContext` (identity, tenant, roles/claims snapshot).
- Keep secrets and transport artifacts (JWT raw token, headers) out of domain state.
- Model denials as `Err(TError)` with domain meaning (`Forbidden`, `TenantMismatch`, etc.).
- Prefer small immutable context records over primitive bags.

### Decide

```csharp
static abstract Result<TEvent[], TError> Decide(
    TState state, Validated<TCommand, TError> command);
```

Stage 3 (decision). Must be pure.

- `Ok(events)` — command accepted; events will be dispatched through Transition.
- `Err(error)` — command rejected; state remains unchanged.
- `Ok([])` — "accepted but nothing happened" (idempotent command).

### IsTerminal

Whether the automaton has reached a terminal state. Defaults to `false`.

---

## DecidingRuntime&lt;TDecider, TState, TCommand, TEvent, TEffect, TError, TParameters&gt;

```csharp
public sealed class DecidingRuntime<TDecider, TState, TCommand, TEvent, TEffect, TError, TParameters> : IDisposable
    where TDecider : Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
```

### Properties

| Property | Type | Description |
| -------- | ---- | ----------- |
| `State` | `TState` | The current state. |
| `Events` | `IReadOnlyList<TEvent>` | All dispatched events. |
| `IsTerminal` | `bool` | Whether `TDecider.IsTerminal(State)` is `true`. |

### Handle

```csharp
public ValueTask<Result<TState, TError>> Handle(
    TCommand command, CancellationToken cancellationToken = default)

public ValueTask<Result<TState, TError>> Handle<TAuthorizationContext>(
    TCommand command,
    TAuthorizationContext authorizationContext,
    CancellationToken cancellationToken = default)
```

`Handle(command, cancellationToken)` forwards to the generic overload with `Unit.Value`.
Use the generic overload when your decider authorization depends on caller context.

Handles a command using the staged pipeline:

1. `Validate` (short-circuit on `Invalid`)
2. `Authorize` (short-circuit on `Err`)
3. `Decide` (short-circuit on `Err`)
4. Dispatch all produced events through `Transition`

Return value:

- `Ok(state)` — command accepted and all events were dispatched.
- `Err(error)` — command rejected by any stage; state remains unchanged.

**Atomicity:** The entire Handle operation executes under a single lock acquisition.

---

## DeciderComposition (Internal)

Internal helper API for explicit functional composition of staged deciders.

```csharp
internal static class DeciderComposition
{
    public static Result<Validated<TCommand, TError>, TError> ValidateToResult<TCommand, TError>(
        Validated<TCommand, TError> validated);

    public static Result<Validated<TCommand, TError>, TError> AuthorizeToResult<TCommand, TError>(
        Validated<TCommand, TError> validated,
        Result<Unit, TError> authorization);

    public static Result<TEvent[], TError> Compose<TState, TCommand, TEvent, TError, TAuthorizationContext>(
        TState state,
        TCommand command,
        TAuthorizationContext authorizationContext,
        Func<TState, TCommand, Validated<TCommand, TError>> validate,
        Func<TState, Validated<TCommand, TError>, TAuthorizationContext, Result<Unit, TError>> authorize,
        Func<TState, Validated<TCommand, TError>, Result<TEvent[], TError>> decide);
}
```

---

## See Also

- [The Decider](../concepts/the-decider.md) — conceptual explanation
- [Upgrading to Decider](../guides/upgrading-to-decider.md) — migration guide
- [Result](result.md) — the error/success channel used by Authorize and Decide
- [Tutorial 05](../tutorials/05-command-validation.md) — full walkthrough
