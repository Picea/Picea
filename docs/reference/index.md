# API Reference

Complete type and method documentation for the `Picea` package.

## Types

| Type | Namespace | Purpose |
| ---- | --------- | ------- |
| [`Automaton<TState, TEvent, TEffect, TParameters>`](automaton.md) | `Picea` | The kernel interface — Initialize + Transition |
| [`AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>`](runtime.md) | `Picea` | Thread-safe async runtime |
| [`Observer<TState, TEvent, TEffect>`](runtime.md#observer) | `Picea` | Transition observer delegate |
| [`Interpreter<TEffect, TEvent>`](runtime.md#interpreter) | `Picea` | Effect interpreter delegate |
| [`ObserverExtensions`](runtime.md#observerextensions) | `Picea` | Observer composition (`Then`, `Where`, `Select`, `Catch`, `Combine`) |
| [`InterpreterExtensions`](runtime.md#interpreterextensions) | `Picea` | Interpreter composition (`Then`, `Where`, `Select`, `Catch`) |
| [`PipelineError`](runtime.md#pipelineerror) | `Picea` | Structured error from observer/interpreter pipeline |
| [`PipelineResult`](runtime.md#pipelineresult) | `Picea` | Pre-allocated `Ok` result for zero-alloc happy path |
| [`Unit`](runtime.md#unit) | `Picea` | Unit type for `Result<Unit, PipelineError>` |
| [`Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>`](decider.md) | `Picea` | Command validation interface |
| [`DecidingRuntime<...>`](decider.md#decidingruntime) | `Picea` | Command-validating runtime |
| [`GuardedDecider` and `GuardedDecidingRuntime`](guarded-decider.md) | `Picea.Commanding` | Staged authorization + validation + decision runtime |
| [`Result<TSuccess, TError>`](result.md) | `Picea` | Discriminated union for error handling |
| [`AutomatonDiagnostics`](diagnostics.md) | `Picea` | OpenTelemetry tracing |

## Dependency Graph

```text
Automaton<S, E, F, P>          ← kernel interface (pure)
    │
    ├── Decider<S, C, E, F, Err, P>    ← extends with Decide + IsTerminal
    │   └── GuardedDecider<S, C, E, F, P>    ← staged decision contract (proof-token command)
    │
    └── AutomatonRuntime<A, S, E, F, P>    ← executes the loop
            │
            ├── DecidingRuntime<D, S, C, E, F, Err, P>  ← wraps with Handle
            └── GuardedDecidingRuntime<...>              ← wraps with Handle(principal, command)
            │
            ├── Observer<S, E, F>       ← sees transitions, returns Result<Unit, PipelineError>
            │   └── ObserverExtensions  ← Then, Where, Select, Catch, Combine
            │
            └── Interpreter<F, E>       ← converts effects to feedback, returns Result<E[], PipelineError>
                └── InterpreterExtensions ← Then, Where, Select, Catch

Result<T, E>                ← used by Decide, Observer, Interpreter. LINQ monad (Select, SelectMany).
Unit                        ← success type for effectful operations (replaces void in Result)
PipelineError               ← structured error for Observer/Interpreter pipelines
PipelineResult              ← pre-allocated Ok for zero-alloc happy path

AutomatonDiagnostics        ← ActivitySource for tracing
```

## Source Files

| File | Contains |
| ---- | -------- |
| `Automaton.cs` | `Automaton<TState, TEvent, TEffect, TParameters>` |
| `Runtime.cs` | `AutomatonRuntime<...>`, `Observer<...>`, `Interpreter<...>`, `ObserverExtensions`, `InterpreterExtensions`, `PipelineError`, `PipelineResult`, `Unit` |
| `Decider.cs` | `Decider<...>`, `DecidingRuntime<...>` |
| `Commanding/GuardedDecider.cs` | `Validator`, `Policy`, `ValidCommand`, `DenialKind`, `DenialObserver`, `GuardedAuthorization`, `GuardedValidation`, `GuardedDecider`, `GuardedDecidingRuntime` |
| `Result.cs` | `Result<TSuccess, TError>` |
| `Diagnostics.cs` | `AutomatonDiagnostics` |
