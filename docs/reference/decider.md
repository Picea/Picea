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

- `Policy` — stage 1, authorization checks (optional caller context)
- `Validator` — stage 2, feasibility/invariant checks
- `GuardedDecider` — staged command contract (`Authorize` -> `Validate` -> `Decide`)
- `GuardedDecidingRuntime` — runtime wrapper that executes the staged pipeline atomically

Pipeline semantics:

1. `Authorize` short-circuits on rejection
2. `Validate` short-circuits on rejection
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
- Adds staged command handling via `Policy` then `Validator` before `Decide`
- Preserves the same success/error channel and state-transition behavior

### Runtime Pairing Warning

`GuardedDecider` also satisfies `Automaton`, so it can technically be hosted by `AutomatonRuntime`.
This can be a valid architecture choice, but it should be an explicit one.

When you host it with `AutomatonRuntime`, guarded command semantics are **not** enforced by the runtime path:

1. `Authorize` is not invoked
2. `Validate` is not invoked
3. `DenialObserver` is never called
4. You are running event dispatch/transition only

This is typically correct/useful when:

1. You are replaying trusted historical events and do not want command checks during replay
2. Authorization and validation are enforced upstream (API/application layer), and runtime should only apply transitions
3. You are building a custom runtime pipeline where command policy is intentionally outside the core event loop

Prefer `GuardedDecidingRuntime` when you want command semantics enforced at the runtime boundary itself (single atomic `Handle(principal, command)` with built-in staged denial behavior).

In short: both can be correct, but choose deliberately and document where authorization/validation responsibility lives.

Full API surface (delegates, contracts, runtime signatures, and examples) is documented in [Guarded Decider](guarded-decider.md).

---

## See Also

- [The Decider](../concepts/the-decider.md) — conceptual explanation
- [Upgrading to Decider](../guides/upgrading-to-decider.md) — migration guide
- [Result](result.md) — the return type of Decide
- [Tutorial 05](../tutorials/05-command-validation.md) — full walkthrough
