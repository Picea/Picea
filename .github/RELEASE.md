# Pre-GA Stable Release Runbook

## Purpose
Use this runbook before tagging any stable pre-GA release candidate on main. It defines the minimum release gates that must pass before a Go decision.

## When To Use
- Before creating a release tag (v*.*.*)
- After the final merge to main for the release candidate
- During release sign-off between DevOps, Security, Performance, and Tech Writer

## Prioritized Checklist (6 Items)

### 1) Release Source Integrity
- Priority: P0
- Owner: DevOps
- Pass criteria: The release tag commit SHA equals origin/main HEAD SHA.
- Fail criteria: Tag points to any commit other than current origin/main HEAD.
- Quick verification:

```bash
git fetch --no-tags origin main
TAG_SHA="$(git rev-list -n 1 vX.Y.Z)"
MAIN_SHA="$(git rev-parse origin/main)"
test "$TAG_SHA" = "$MAIN_SHA"
```

### 2) Mandatory CI Policy Gates On Main
- Priority: P0
- Owner: DevOps
- Pass criteria: Latest runs for PR Validation, Secrets Scan, and DAST on main are successful.
- Fail criteria: Any of these workflows is failing, cancelled, or missing a successful latest run.
- Quick verification:

```bash
gh run list --workflow "PR Validation" --branch main --limit 1
gh run list --workflow "Secrets Scan" --branch main --limit 1
gh run list --workflow "DAST" --branch main --limit 1
```

### 3) Dependency Vulnerability Gate
- Priority: P0
- Owner: Security
- Pass criteria: No critical or high vulnerable NuGet packages are reported.
- Fail criteria: Any critical/high vulnerable package is detected.
- Quick verification:

```bash
dotnet restore
dotnet list package --vulnerable --include-transitive 2>&1 | tee vulnerability-report.txt
! grep -Eiq "critical|high" vulnerability-report.txt
```

### 4) Benchmark Regression Gate
- Priority: P1
- Owner: Performance
- Pass criteria: Benchmarks workflow succeeds; no regression above the enforced 5% threshold.
- Fail criteria: Benchmarks workflow fails due to regression alert (>5% slower).
- Quick verification:

```bash
gh run list --workflow "Benchmarks" --branch main --limit 1
dotnet run --project Picea.Benchmarks/Picea.Benchmarks.csproj -c Release -- --filter '*' --exporters json
```

### 5) Security Governance Artifacts Current
- Priority: P1
- Owner: Security
- Pass criteria: docs/security/threat-model.md and docs/security/threat-to-tests.md are present and include current scope/coverage details.
- Fail criteria: Missing file(s), stale or incomplete threat-to-regression mapping for current attack surface.
- Quick verification:

```bash
test -f docs/security/threat-model.md && test -f docs/security/threat-to-tests.md
rg -n "Last updated|Threat|TM-" docs/security/threat-model.md docs/security/threat-to-tests.md
```

### 6) Release Communication Readiness
- Priority: P2
- Owner: Tech Writer
- Pass criteria: CONTRIBUTING required checks list includes benchmark regression gate, and SECURITY governance links are intact.
- Fail criteria: Missing benchmark gate in contributing guidance or broken/missing security governance references.
- Quick verification:

```bash
rg -n "Required Status Checks|Benchmark|regression" CONTRIBUTING.md
rg -n "threat-model.md|threat-to-tests.md" SECURITY.md
```

## Go/No-Go
- Go: All 6 checklist items are PASS and evidence is attached in the release PR/notes.
- No-Go: Any checklist item is FAIL or unresolved.

Release approvers should record final status as:
- DevOps: Go/No-Go
- Security: Go/No-Go
- Performance: Go/No-Go
- Tech Writer: Go/No-Go
