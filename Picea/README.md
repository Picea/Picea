# Picea

A minimal, production-hardened Mealy machine kernel for building event-driven systems.

## Overview

Picea provides the foundational abstraction underlying three major patterns:

- **Model-View-Update (MVU)** — UI rendering via [Picea.Abies](https://github.com/picea/abies)
- **Event Sourcing** — Event persistence via [Picea.Glauca](https://github.com/picea/glauca)
- **Actor Model** — Message-passing concurrency via [Picea.Rubens](https://github.com/picea/rubens)

All three are instances of a **Mealy machine** — a finite-state transducer where outputs (effects) depend on both the current state and the input (event):

```
transition : (State × Event) → (State × Effect)
```

## The Kernel

```csharp
public interface Automaton<TState, TEvent, TEffect, TParameters>
{
    static abstract (TState State, TEffect Effect) Initialize(TParameters parameters);
    static abstract (TState State, TEffect Effect) Transition(TState state, TEvent @event);
}
```

## The Runtime

The runtime is a monadic left fold over an event stream, parameterized by:

- **Observer** — sees each `(State, Event, Effect)` triple after transition
- **Interpreter** — converts effects into feedback events

Both compose via standard FP combinators: `Then`, `Where`, `Select`, `Catch`, `Combine`.

## Installation

```bash
dotnet add package Picea
```

## Quick Start

```csharp
using Picea;

public record CounterState(int Count);
public record Increment : CounterEvent;
public interface CounterEvent;
public record struct NoEffect;

public class Counter : Automaton<CounterState, CounterEvent, NoEffect, Unit>
{
    public static (CounterState, NoEffect) Initialize(Unit _) => (new(0), default);
    public static (CounterState, NoEffect) Transition(CounterState state, CounterEvent @event) =>
        @event switch
        {
            Increment => (state with { Count = state.Count + 1 }, default),
            _ => (state, default)
        };
}

// Run it
var runtime = await AutomatonRuntime<Counter, CounterState, CounterEvent, NoEffect, Unit>
    .Start(default, (s, e, eff) => PipelineResult.Ok, _ => new(Result<CounterEvent[], PipelineError>.Ok([])));

await runtime.Dispatch(new Increment());
Console.WriteLine(runtime.State.Count); // 1
```

## License

[Apache 2.0](LICENSE) — Copyright 2025 Maurice Peters
