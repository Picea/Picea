# Performance Engineer — History

## About This File
Benchmark results, load test reports, profiling findings, and performance budgets. Read this before every session.

## Performance Budgets
*None yet — budgets per service will be established with the Architect.*

## Benchmark Baselines
| Benchmark | Metric | Baseline Value | Date |
|---|---|---|---|
| *None yet* | | | |

## Load Test History
| Date | Scenario | VUs | p50 | p95 | p99 | Throughput | Breaking Point |
|---|---|---|---|---|---|---|---|
| *None yet* | | | | | | | |

## Profiling Findings
*None yet — hot spots, allocation patterns, GC pressure observations tracked here.*

## Optimization Log
| Date | What | Before | After | Technique |
|---|---|---|---|---|
| *None yet* | | | | |

## Bottleneck Patterns
*None yet — reusable performance knowledge tracked here.*

## Learnings

### 2026-04-01: CI time-to-feedback bottlenecks and lane design
- Current PR critical path is dominated by `E2E` and `Benchmark`; recent runs show `Benchmark` often ~10-11 min and `E2E` ranging ~9-29 min.
- `Benchmark` workflow is required and currently starts on all PRs, but its heavy js-framework-benchmark execution is conditionally skipped unless perf-related triggers fire. This preserves branch protection while reducing unnecessary load.
- `PR Validation` has grown into a mixed lane (policy checks plus expensive template smoke tests, bundle publish, and security scans). It is the biggest opportunity for fast-lane extraction.
- There is duplicate security/template scanning between `pr-validation.yml` and standalone workflows (`template-security.yml`, `secrets-scan.yml`, `trivy.yml`, `semgrep.yml`, `zap-baseline.yml`), which increases runner spend and queue pressure.
- For regression signal quality, keep js-framework-benchmark threshold at 5% and maintain mainline E2E benchmark runs for baseline updates; do not replace E2E gating with micro-benchmarks.
- To avoid false confidence when moving checks off PR, use strict path-aware required checks, merge queue, nightly full-suite sweeps, and an auto-revert/escalation policy for post-merge failures.

### 2026-05-01: Production-readiness performance audit (baseline/guardrails/budgets)
- Verdict from available evidence: conditionally not production-ready from a performance governance perspective until budget and gating gaps are closed.
- Baseline drift detected: docs baselines are dated 2026-03-06 and 2026-03-13 while benchmark suite changed later (record-based benchmarks added), so current baselines are incomplete for present hot paths.
- Artifact freshness risk: repository benchmark outputs are from March while benchmark source changed in April, which weakens confidence in current regression detection.
- Regression guard mismatch: team decision targets a 5% js-framework regression gate, but active BenchmarkDotNet gate is configured at 150% (effectively allowing large slowdowns before failing).
- Pre-merge protection gap: benchmark workflow runs on push to main only, so regressions can merge before benchmark guard evaluates.
- Load test evidence missing: no k6/NBomber scripts or load-test reports found, so p95/p99/throughput and breaking-point claims are unsupported.
- Performance budget gap remains: no service-level p50/p95/p99/throughput/memory/startup budgets are recorded in team decisions or perf history.

### 2026-05-01: Benchmark enforcement aligned to team policy (5% gate + pre-merge checks)
- Benchmark regression gate is now enforced at 105% in CI, which fails any benchmark exceeding a 5% slowdown versus baseline (`current / baseline > 1.05`).
- Benchmark workflow now runs on pull requests to `main` for performance-sensitive paths so regressions are detected before merge.
- PR benchmark runs are read-only for benchmark history (`auto-push: false` outside push events) to prevent baseline pollution.
- Push-to-main benchmark runs remain authoritative for baseline progression and alert comments.
