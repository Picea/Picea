# Glossary

Definitions of terms used throughout the Picea documentation.

## Core Terms

**Automaton**
: The kernel interface — a Mealy machine with `Initialize` and `Transition` methods. See [The Kernel](the-kernel.md).

**Mealy Machine**
: A finite-state transducer where outputs depend on both the current state and the input. The mathematical foundation of the Picea kernel. Compare with Moore machine (output depends only on state).

**Transition Function**
: The pure function `(State × Event) → (State × Effect)` that defines state machine behavior. Must be deterministic, side-effect-free, and total.

**State**
: The data that the automaton remembers between transitions. Should be an immutable record.

**Event**
: An input that triggers a transition. Represents something that happened. Typically modeled as an interface with nested record structs.

**Effect**
: An output produced by a transition. Represents something that should happen. Effects are data, not actions — the Interpreter makes them happen.

**Initialize**
: The function that produces the initial state and startup effect from parameters.

## Runtime Terms

**Runtime**
: The `AutomatonRuntime` that executes the transition function in a loop: dispatch → transition → observe → interpret. See [The Runtime](the-runtime.md).

**Observer**
: A delegate `(State, Event, Effect) → Result<Unit, PipelineError>` called after each transition. Used for side effects: rendering, persistence, logging.

**Interpreter**
: A delegate `Effect → Result<Event[], PipelineError>` that converts effects to feedback events. Closes the event loop.

**Feedback Loop**
: When an Interpreter produces events from effects, and those events are dispatched back into the automaton, triggering more transitions.

**Dispatch**
: Sending an event to the runtime for processing through the full cycle (transition → observe → interpret → feedback).

**Pipeline**
: A composed chain of observers or interpreters connected via `Then`, `Where`, `Catch`, or `Combine`.

**PipelineError**
: A structured error record `(Message, Source?, Exception?)` returned by observers and interpreters.

**PipelineResult**
: Pre-allocated `Result<Unit, PipelineError>.Ok` for the zero-allocation happy path.

**Unit**
: A type with exactly one value. Replaces `void` in generic contexts (e.g., `Result<Unit, PipelineError>`).

## Decider Terms

**Decider**
: An Automaton extended with a staged command pipeline. Adds `Validate`, `Authorize<TAuthorizationContext>`, `Decide`, and `IsTerminal` to the kernel interface. See [The Decider](the-decider.md).

**Command**
: User intent — what someone wants to do. First validated by `Validate`, then authorized by `Authorize`, before `Decide` can produce events.

**Validated**
: The output of the feasibility stage: `Validated<TCommand, TError> = Valid(command) | Invalid(error)`.

**Authorize**
: The pure permission stage `(State × Validated<Command, Error> × AuthContext) → Result<Unit, Error>`.

**Decide**
: The pure decision stage `(State × Validated<Command, Error>) → Result<Event[], Error>` that produces events or rejects with error.

**Handle**
: The `DecidingRuntime.Handle(command, ...)` method that runs `Validate → Authorize → Decide`, then dispatches resulting events. Atomic — the full operation runs under a single lock.

**IsTerminal**
: A function `State → bool` that indicates whether the automaton has reached a final state. Defaults to `false`.

**Result**
: A discriminated union `Ok(value) | Err(error)` implemented as a zero-allocation readonly struct. See [Result Reference](../reference/result.md).

## Composition Terms

**Kleisli Composition (`>=>` / `Then`)**
: Sequential composition of effectful functions. `a.Then(b)` runs `a`, then `b` if `a` succeeds. Short-circuits on error.

**Contramap (`Select`)**
: Transforms the *inputs* of an observer or interpreter, adapting it from one type to another. Contravariant functor operation.

**Guard (`Where`)**
: Filters an observer or interpreter with a predicate. Only runs when the predicate returns true.

**Catch**
: Error recovery combinator. Handles errors from an observer or interpreter and can recover or transform them.

**Combine**
: Sequential composition that does NOT short-circuit. Both observers always run, even if the first fails.

## Pattern Terms

**MVU (Model-View-Update)**
: An architecture where state (Model) is rendered to a view (View) and updated by messages (Update). The Elm Architecture.

**Event Sourcing**
: A pattern where state is derived from a sequence of events rather than stored directly. Events are the source of truth.

**Actor**
: A computational entity that processes messages one at a time from a mailbox. Each actor has its own state and lifecycle.

**CQRS (Command Query Responsibility Segregation)**
: Separating read and write models. Commands modify state; queries read from optimized projections.

## Mathematical Terms

**Monadic Left Fold**
: The runtime's execution model — a sequential reduction over events where each step may produce side effects through a monad (Result).

**Sum Type (Coproduct)**
: A type that can be one of several alternatives. `Result<T, E>` is `T + E`. Events and effects are typically sum types (interface with nested records).

**Functor**
: A structure that supports mapping. `Result.Map(f)` applies `f` to the success value.

**Monad**
: A structure that supports chaining. `Result.Bind(f)` chains operations that may fail, short-circuiting on error.

**Bifunctor**
: A structure with two type parameters that both support mapping. `Result.MapError(f)` maps the error side.
