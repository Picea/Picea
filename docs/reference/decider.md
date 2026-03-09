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

## See Also

- [The Decider](../concepts/the-decider.md) — conceptual explanation
- [Upgrading to Decider](../guides/upgrading-to-decider.md) — migration guide
- [Result](result.md) — the return type of Decide
- [Tutorial 05](../tutorials/05-command-validation.md) — full walkthrough
