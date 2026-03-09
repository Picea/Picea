# Changelog

All notable changes to the Picea kernel will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
via [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning).

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

[1.0.0-rc.1]: https://github.com/picea/picea/releases/tag/v1.0.0-rc.1
