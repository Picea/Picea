# Decider, DecidingRuntime

`namespace Picea`

Command validation layer for Automatons.

---

## Decider&lt;TState, TCommand, TEvent, TEffect, TError, TParameters&gt;

```csharp
public interface Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract Result<TEvent[], TError> Decide(TState state, TCommand command);
    static virtual bool IsTerminal(TState state) => false;
}
```

### Decide

Validates a command against the current state, producing events or an error. **This function must be pure.**

- `Ok(events)` — command accepted; events will be dispatched through Transition.
- `Err(error)` — command rejected; state remains unchanged.
- `Ok([])` — "accepted but nothing happened" (idempotent command).

### IsTerminal

Whether the automaton has reached a terminal state. Defaults to `false`.

---

## Additive Secure Staged APIs

The Decide-only `Decider` API above remains the baseline and stays fully supported.

When you want explicit staged hardening, use the additive secure APIs:

- `Validator` — stage 1, feasibility/invariant checks
- `Policy` — stage 2, authorization checks (optional caller context)
- `GuardedDecider` — staged command contract (`Validate` -> `Authorize` -> `Decide`)
- `GuardedDecidingRuntime` — runtime wrapper that executes the staged pipeline atomically

Pipeline semantics:

1. `Validate` short-circuits on rejection
2. `Authorize` short-circuits on rejection
3. `Decide` produces events
4. Events are dispatched through `Transition`

Return behavior follows the same `Result` pattern as `Decider`:

- `Ok(state)` — all stages accepted and events dispatched
- `Err(error)` — rejected by any stage; state unchanged

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
```

Validates and handles a command: Decide → Dispatch events → return new state or error.

**Atomicity:** The entire Handle operation executes under a single lock acquisition.

---

## GuardedDecidingRuntime

`GuardedDecidingRuntime` is the secure staged companion to `DecidingRuntime`.

- Same runtime guarantees (`State`, `Events`, terminal checks, atomic `Handle`)
- Adds staged command handling via `Validator` and `Policy` before `Decide`
- Preserves the same success/error channel and state-transition behavior

---

## See Also

- [The Decider](../concepts/the-decider.md) — conceptual explanation
- [Upgrading to Decider](../guides/upgrading-to-decider.md) — migration guide
- [Result](result.md) — the return type of Decide
- [Tutorial 05](../tutorials/05-command-validation.md) — full walkthrough
