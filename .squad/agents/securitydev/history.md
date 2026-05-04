# Security Expert & Pentester — History

## About This File
Project-specific security learnings, tool evaluations, vulnerability patterns, and pentest results. Read this before every session.

## Security Toolchain Status
| Layer | Tool | Status | Last Evaluated |
|---|---|---|---|
| SAST | Roslyn Analyzers | Not yet configured | |
| SAST | Semgrep | Not yet configured | |
| SCA | dotnet vuln scan | Not yet configured | |
| SCA | Dependabot | Not yet configured | |
| Secrets | Gitleaks | Not yet configured | |
| DAST | OWASP ZAP | Not yet configured | |
| Container | Trivy | Not yet configured | |

## Vulnerability Patterns Found
*None yet — recurring vulnerability classes tracked here.*

## Scanner Rules Added/Tuned
| Rule | Tool | Reason | Date |
|---|---|---|---|
| *None yet* | | | |

## False Positive Patterns
*None yet — findings that look bad but aren't, so the team doesn't waste time.*

## Tool Evaluations
*None yet — document what was tested, what was chosen, and why.*

## Pentest History
| Date | Scope | Critical | High | Medium | Low | Report |
|---|---|---|---|---|---|---|
| *None yet* | | | | | | |

## Threat Models
*None yet — threat models for features/components tracked here.*

## Threat Intelligence Log
| Date | Threat/CVE | Affects | Severity | Mitigated | Regression Test | Scanner Rule Added |
|---|---|---|---|---|---|---|
| *None yet* | | | | | | |

## Proactive Hardening
| Date | Action | Result |
|---|---|---|
| *None yet — dependency pruning, baseline scans, rule updates tracked here.* | | |

## Attack Surface Map
*Not yet mapped — all public endpoints, auth flows, data flows, external integrations tracked here.*

## Security Standards
*Refer to charter for baseline. Project-specific additions tracked here.*

## Learnings

### 2026-05-01 - Security governance artifacts implemented (threat model + regression mapping)
- Added `docs/security/threat-model.md` as the living threat register with explicit trust boundaries, residual risk statements, and evidence links tied to real tests/workflow checks.
- Added `docs/security/threat-to-tests.md` to map each current threat ID (`TM-001` through `TM-006`) to concrete regression evidence, including CI policy-as-code checks for dependency/CI security gates.
- Captured one explicit uncovered area with concrete wording (automated secrets scanning workflow absent in current `.github/workflows` set) to avoid false assurance.
- Updated `SECURITY.md` to require same-PR maintenance of threat governance artifacts whenever attack surface or controls change, preventing documentation drift.

### 2026-05-01 - Production Security Readiness Assessment (evidence-based)
- CI currently has SAST and SCA signals, but not the full security pipeline required by team principles: `codeql.yml` runs on PR/main, and SCA vulnerable package gates run in `pr-validation.yml` and `cd.yml`.
- Required controls in `.squad/decisions.md` are not fully present in repo workflows today: no secrets scanning workflow, no DAST workflow, and no container image scanning workflow were found under `.github/workflows/`.
- Threat-model policy requires `/docs/security/threat-model.md`, but that path does not exist in the repository. This is a direct production-readiness blocker under the squad's principles-enforcement rules.
- Dependency posture signal is currently clean from project-level checks: `dotnet list package --vulnerable --include-transitive` reported no known vulnerable packages for `Picea`, `Picea.Tests`, `Picea.Benchmarks`, and `Picea.Templates`.
- SECURITY.md claims NuGet audit runs in `all` mode, but `Directory.Build.props` currently sets `NuGetAudit` and `NuGetAuditLevel` without explicitly setting `NuGetAuditMode`; keep policy/docs and build config in sync to avoid assurance drift.

### 2026-04-01 - PR Security Gating Audit (CI workflows)
- Mandatory PR checks already present: gitleaks secrets scan via `secrets-scan.yml` (single source of truth); SCA high/critical gate in `pr-validation.yml`; Trivy high/critical gate in `trivy.yml`; CodeQL on PR in `codeql.yml`.
- Duplicate PR SCA exists in `pr-validation.yml` and `cd.yml`; keep PR gate in `pr-validation.yml` as source of truth and remove PR-triggered SCA from `cd.yml` to reduce PR latency without lowering merge protection.
- Heavy security jobs currently running on every PR: `zap-baseline.yml` (starts services + API + baseline + authenticated profile) and `template-security.yml` (packs templates, scaffolds, restores/builds, Semgrep, Trivy). These are better suited to push/main + nightly and path-filtered PR runs.
- Non-negotiables that should stay PR-blocking: secrets detection, dependency vulnerability gate (HIGH/CRITICAL), and at least one code-level SAST signal (CodeQL or Semgrep) to catch injection/authz patterns before merge.
- Scope-limiting opportunities without blind spots:
	- Path-filter heavy scans: run template security only when template/framework/packaging inputs change.
	- Path-filter ZAP only when API/auth/HTTP middleware/routing changes.
	- Keep full-repo scans on scheduled/nightly as compensating control.
	- For Semgrep PR optimization, diff-based targeting is acceptable only if nightly full scan remains enforced.
