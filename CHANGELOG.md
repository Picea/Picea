# Changelog

All notable changes to the Picea kernel will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
via [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning).

## [1.0.0-rc.5] — Unreleased

### Changed

- **AutomatonRuntime construction is now friend-assembly only**: The direct constructor of `AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>` is `internal`. Public consumers **must** use `AutomatonRuntime<...>.Start()` to create and initialize runtimes. Only explicitly declared friend assemblies (`Picea.Tests` and `Picea.Benchmarks`) retain direct-construction access for controlled test/benchmark scenarios. See [migration guide](#migration-guide) below.

- **Internal method parameter ordering**: Reordered internal method signatures to place `CancellationToken` as the final parameter, aligning with .NET Framework Design Guidelines. This affects only internal APIs and is NOT a breaking change for public consumers.

### Fixed

- **Dispatch tracing hot path**: Runtime and decider tracing now skip `Activity` creation entirely when no listener is registered, recovering the dispatch benchmark regression introduced by always entering the tracing setup path on hot calls.
- **Runtime initialization docs**: Corrected guidance that implied arbitrary consumer test projects could access the internal `AutomatonRuntime` constructor. Only friend assemblies declared through `InternalsVisibleTo` can do that.
- **Security documentation**: Updated threat-model CI control references to match the workflows actually enforced in this repository, including the current secrets-scan implementation and the benchmark/validation gates that complement the security jobs.

#### Migration Guide

**Before 1.0.0-rc.5** (calling constructor directly):
```csharp
var runtime = new AutomatonRuntime<MyAutomaton, MyState, MyEvent, MyEffect, MyParams>(
    initialState: myState,
    observer: myObserver,
    interpreter: myInterpreter);
```

**After 1.0.0-rc.5** (use `.Start()` factory):
```csharp
var runtime = await AutomatonRuntime<MyAutomaton, MyState, MyEvent, MyEffect, MyParams>
    .Start(
        parameters: myParams,        // Passes through AutomatonInitialize
        observer: myObserver,
        interpreter: myInterpreter);
```

**Why this change?** The `Start()` factory enforces initialization consistency: it calls `TAutomaton.Initialize(parameters)` automatically and interprets startup effects. This eliminates a class of bugs where manually constructed runtimes skipped initialization or startup effect handling.

### Why AutomatonRuntime.Start is now required

The change encodes an invariant: **runtimes are either initialized through `.Start()` or they come from test code.** Public users have no reason to bypass the factory — it does all the work correctly.

---

## [1.0.0-rc.4] — 2026-04-05

### Changed

- **Deciding runtime internals**: Normalized internal helper method signatures in `DecidingRuntime` for consistent parameter ordering and clearer call paths.

### Fixed

- **Formatting gate readiness**: Applied full repository `dotnet format` changes and aligned test code style (`null!` conversions and redundant cast cleanup) to satisfy formatting analyzers.
- **Test analyzer failure**: Removed a constant-value assertion test that triggered non-auto-fixable `TUnitAssertions0005` during formatting verification.

## [1.0.0-rc.3] — 2026-04-04

### Added

- **Decider staged pipeline model**: `Validate -> Authorize(auth-context) -> Decide`
- **Authorization context support**: `Authorize<TAuthorizationContext>(...)` and `Handle(command, authorizationContext, cancellationToken)` overload
- **Composition law coverage**: `DeciderComposition` law/composition tests

### Changed

- **Tracing**: Added explicit `automaton.pipeline.stage = "decide"` tagging on decide rejection branches
- **Documentation**: End-to-end Decider docs alignment across reference, concepts, glossary, testing guide, tutorial, and ADR-004

### Fixed

- **Tracing test stability**: `Dispatch_EmitsTracingSpan` now filters by expected event tag to avoid cross-test span pickup in parallel runs

## [1.0.0-rc.2] — 2026-03-13

### Added

- **InterpreterResult\<TEvent\>**: Pre-allocated `Empty` result for zero-alloc interpreter fast paths (analogous to `PipelineResult.Ok`)
- **Documentation**: [Zero-alloc domain modeling guide](docs/guides/zero-alloc-domain-modeling.md) — eliminating heap allocations on hot paths
- **Benchmarks**: Record-based zero-alloc benchmark domain proving 0 B allocation on lean dispatch

### Changed

- **Benchmarks**: Existing interpreters updated to use `InterpreterResult<TEvent>.Empty`
- **Copyright**: Updated to 2025-2026

### Testing

- Added 64 new tests (119 → 183 total):
  - `OptionTests.cs`: 42 tests covering the full `Option<T>` public API (previously zero coverage)
  - `RuntimeTests.cs`: 22 new combinator tests for Observer and Interpreter pipelines

## [1.0.0-rc.1] — 2026-03-09

### Added

- **Kernel**: `Automaton<TState, TEvent, TEffect, TParameters>` — Mealy machine interface (Initialize + Transition)
- **Runtime**: `AutomatonRuntime<TAutomaton, TState, TEvent, TEffect, TParameters>` — Thread-safe async runtime with dispatch → transition → observe → interpret loop
- **Decider**: `Decider<TState, TCommand, TEvent, TEffect, TError, TParameters>` — Command validation layer (Decide + IsTerminal)
- **DecidingRuntime**: Atomic `Handle(command)` with TOCTOU-safe locking
- **Result type**: `Result<TSuccess, TError>` readonly struct with Map, Bind, MapError, LINQ query syntax
- **Option type**: `Option<TValue>` readonly struct
- **Observer pipeline**: Monadic combinators — `Then`, `Where`, `Select`, `Catch`, `Combine`
- **Interpreter pipeline**: Monadic combinators — `Then`, `Where`, `Select`, `Catch`
- **Diagnostics**: `AutomatonDiagnostics` with OpenTelemetry-compatible `ActivitySource` tracing
- **Production guarantees**: Thread-safe dispatch (SemaphoreSlim), cancellation support, bounded feedback loops (max 64 depth)
- **Documentation**: Concepts, tutorials, how-to guides, API reference, ADRs
- **Benchmarks**: BenchmarkDotNet suite for kernel operations

### Origin

This release is the extraction of the kernel from [MCGPPeters/Automaton](https://github.com/MCGPPeters/Automaton) into the Picea ecosystem. The kernel code is identical — only the root namespace changed from `Automaton` to `Picea`. All type names (`Automaton<>`, `AutomatonRuntime<>`, `AutomatonDiagnostics`, etc.) are preserved.

[1.0.0-rc.2]: https://github.com/picea/picea/releases/tag/v1.0.0-rc.2
[1.0.0-rc.1]: https://github.com/picea/picea/releases/tag/v1.0.0-rc.1
[1.0.0-rc.3]: https://github.com/picea/picea/releases/tag/v1.0.0-rc.3
[1.0.0-rc.4]: https://github.com/Picea/Picea/releases/tag/v1.0.0-rc.4
