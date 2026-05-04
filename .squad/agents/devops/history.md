# DevOps / Infrastructure Engineer — History

## About This File
Pipeline decisions, container configs, deployment patterns, and CI optimization. Read this before every session.

## Pipeline Configuration
*None yet — workflow structure, caching strategy, stage ordering tracked here.*

## Container Images
| Image | Base | Size | Last Optimized |
|---|---|---|---|
| *None yet* | | | |

## Deployment Topology
*None yet — environment descriptions, deployment targets, parity notes.*

## CI Failures Investigated
| Date | Failure | Root Cause | Fix |
|---|---|---|---|
| *None yet* | | | |

## Release Process
*None yet — versioning strategy, release flow, automation status.*

## Environment Gotchas
*None yet — environment-specific issues and workarounds.*

## 2026-05-01 — Production Deployment/Release Readiness Assessment

### Verdict Snapshot
- Status assessed as CONDITIONALLY READY for package publishing, NOT READY for full production deployment operations.

### Key Findings
- CI quality gates exist for build, tests, format, SCA, CodeQL, and benchmark regression.
- Release automation is tied to push on main in `.github/workflows/cd.yml`, not to immutable version tags or staged promotion.
- No containerization or deployment topology assets were found (`Dockerfile`, `.dockerignore`, compose files absent), so deployability to runtime environments is undefined from repo state.
- Branch protection requirements are documented in `CONTRIBUTING.md` but cannot be verified as enforced from repository code alone.

### Operational Risks Observed
- No explicit staged rollout/smoke-test/promotion pipeline.
- No artifact attestation/signing or provenance gate visible in workflows.
- Security vulnerability gate parses command text for "critical|high" and may be brittle compared to machine-readable severity enforcement.

### Recommended Next Actions (Shortest Path)
- Split CI (PR gate) and Release (tag-triggered) workflows; publish only from signed `v*` tags.
- Add deploy workflow with environment protection rules (`staging` -> smoke test -> manual approval -> `production`).
- Add container build/scan/sign path and pin runtime artifact digest for promotion.
- Align documented branch protections with actual required status checks and keep names stable.

## 2026-05-01 — CI/CD Hardening Implementation (Minimal Safe Change Set)

### Changes Applied
- Added PR-blocking secrets scanning workflow at `.github/workflows/secrets-scan.yml` using Gitleaks on PRs to main (and pushes to main).
- Added DAST workflow at `.github/workflows/dast.yml` with repository-context-aware behavior:
- DAST behavior detail: Runs OWASP ZAP baseline only when both web attack surface and `DAST_TARGET_URL` are present.
- DAST behavior detail: Otherwise runs an explicit guard job that documents why DAST is skipped and how to enable it safely.
- Added container scan guard workflow at `.github/workflows/container-scan-guard.yml`:
- Container guard detail: If container artifacts are absent, it passes and documents status.
- Container guard detail: If container artifacts appear, it blocks merge unless `.github/workflows/container-scan.yml` exists.
- Split CI and release concerns:
- CI/release split detail: Kept `.github/workflows/cd.yml` as CI gate (restore, SCA, build, test).
- CI/release split detail: Moved package pack/publish into new tag-driven `.github/workflows/release.yml` on `v*.*.*` tags.

### Learnings
- Tag-driven release publication reduces accidental package pushes and aligns better with immutable release intent.
- Guard workflows are a low-risk bridge when the repository does not yet have a deployable web/container surface.
- Keeping workflow names/triggers stable where possible avoids branch-protection drift while improving security posture.

## 2026-05-04 — Workflow Truthfulness And Release Provenance Hardening

### Workflow Changes

- Removed the PR title uppercase subject rule from `.github/workflows/pr-validation.yml` to align the check with Conventional Commits usage documented elsewhere in the repo.
- Replaced `gitleaks/gitleaks-action@v2` in `.github/workflows/secrets-scan.yml` with a pinned OSS Gitleaks CLI install-and-run path so the secrets gate remains enforceable without org licensing.
- Hardened `.github/workflows/release.yml` by removing manual dispatch and blocking package publication unless the pushed `v*.*.*` tag resolves to the current `origin/main` HEAD.
- Reduced `.github/workflows/dast.yml` to a truthful readiness guard:
- DAST guard detail: If no HTTP attack surface is present in the checked-out code, the job passes and explicitly states that no dynamic scan ran.
- DAST guard detail: If an HTTP attack surface appears, the job fails until CI is updated to start and scan the checked-out application itself.

### Operational Learnings

- Release provenance should be enforced in workflow code, not left to operator discipline or trigger choice alone.
- External URL scans are not valid PR DAST evidence unless the workflow can prove they exercised the code under review.
