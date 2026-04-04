# Changelog

All notable changes to the Picea kernel will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
via [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning).

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
