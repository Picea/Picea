# Benchmark CI Enforcement

This document defines the enforced regression gate and trigger strategy for benchmark protection.

## Regression Gate

- Workflow: `.github/workflows/benchmarks.yml`
- Gate threshold: `105%`
- Fail behavior: enabled (`fail-on-alert: true`)

Interpretation:

- Let `baseline` be the historical benchmark value used by the benchmark action.
- Let `current` be the benchmark value from the current workflow run.
- Compute the regression ratio:

  `regression_ratio = current / baseline`

- The run fails if:

  `regression_ratio > 1.05`

This enforces a strict 5% slowdown limit per benchmark.

## Trigger Strategy

The benchmark workflow runs in three modes:

- `pull_request` to `main` for performance-sensitive file changes:
  - `Picea/**`
  - `Picea.Benchmarks/**`
  - `Picea.Tests/**`
  - `.github/workflows/benchmarks.yml`
  - `Directory.Build.props`
  - `global.json`
  - `Picea.sln`
- `push` to `main`
- `workflow_dispatch` (manual)

Behavior by event:

- `pull_request`: evaluates regression gate for pre-merge confidence but does not push benchmark history and does not post alert comments.
- `push` on `main`: evaluates regression gate and updates benchmark history (`auto-push`) to keep the baseline stream current.
- `workflow_dispatch`: available for explicit reruns and investigations.

## Risk Controls

- Pre-merge runs block regressions above 5% on performance-sensitive changes.
- Mainline runs remain the source of historical benchmark progression.
- PR runs are read-only against benchmark history to avoid baseline pollution.
