# GuardedDecider, GuardedDecidingRuntime

`namespace Picea.Commanding`

Staged command-processing APIs for authorization + validation + decision on top of the baseline `Decider` model.

---

## Core Types

### ValidCommand<TCommand>

```csharp
public readonly record struct ValidCommand<TCommand>(TCommand Command)
```

Proof token carried between stages. `GuardedDecider.Decide` only receives a `ValidCommand<TCommand>`, making stage assumptions explicit in the type system.

### Validator

```csharp
public delegate Result<ValidCommand<TCommand>, TError>
    Validator<in TState, TCommand, TError>(TState state, TCommand command);
```

### Policy

```csharp
public delegate Result<ValidCommand<TCommand>, TError>
    Policy<in TPrincipal, in TState, TCommand, TError>(
        TPrincipal principal,
        TState state,
        TCommand command);
```

### DenialKind

```csharp
public enum DenialKind
{
    None = 0,
    Authorization = 1,
    Validation = 2
}
```

### DenialObserver

```csharp
public delegate ValueTask DenialObserver<in TPrincipal, in TState, in TCommand, in TError>(
    DenialKind kind,
    TPrincipal principal,
    TState state,
    TCommand command,
    TError error);
```

---

## Stage Contracts

### GuardedAuthorization

```csharp
public interface GuardedAuthorization<TPrincipal, TState, TCommand, TError>
{
    static abstract Policy<TPrincipal, TState, TCommand, TError> Authorize { get; }
}
```

### GuardedValidation

```csharp
public interface GuardedValidation<TState, TCommand, TError>
{
    static abstract Validator<TState, TCommand, TError> Validate { get; }
}
```

### GuardedDecider

```csharp
public interface GuardedDecider<TState, TCommand, TEvent, TEffect, TParameters>
    : Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract TEvent[] Decide(TState state, ValidCommand<TCommand> command);
    static virtual bool IsTerminal(TState state) => false;
}
```

---

## GuardedDecidingRuntime

```csharp
public sealed class GuardedDecidingRuntime<
    TGuardedDecider,
    TGuardedPolicy,
    TValidation,
    TPrincipal,
    TState,
    TCommand,
    TEvent,
    TEffect,
    TError,
    TParameters> : IDisposable
```

Pipeline order in `Handle(principal, command)` is:

1. `Authorize`
2. `Validate`
3. `Decide`
4. Dispatch events via `Transition`

The entire operation executes atomically under a single runtime gate.

### Start

```csharp
public static ValueTask<GuardedDecidingRuntime<...>> Start(
    TParameters parameters,
    Observer<TState, TEvent, TEffect> observer,
    Interpreter<TEffect, TEvent> interpreter,
    DenialObserver<TPrincipal, TState, TCommand, TError>? denialObserver = null,
    bool threadSafe = true,
    bool trackEvents = true,
    CancellationToken cancellationToken = default)
```

### Handle

```csharp
public ValueTask<Result<TState, TError>> Handle(
    TPrincipal principal,
    TCommand command,
    CancellationToken cancellationToken = default)
```

---

## Example

```csharp
var runtime = await GuardedDecidingRuntime<
    CounterSecure,
    CounterAuthorizationPolicy,
    CounterValidationPolicy,
    CounterPrincipal,
    CounterState,
    CounterCommand,
    CounterEvent,
    CounterEffect,
    CounterError,
    Unit>.Start(
        default,
        observer,
        interpreter,
        denialObserver: (kind, principal, state, command, error) =>
        {
            audit.Write($"Denied at {kind} for {principal}: {error}");
            return ValueTask.CompletedTask;
        });
```

## See Also

- [Decider, DecidingRuntime](decider.md)
- [Error Handling Patterns](../guides/error-handling-patterns.md)
- [Tutorial 05: Command Validation](../tutorials/05-command-validation.md)
- [ADR 004](../adr/004-decider-pattern-command-validation.md)
