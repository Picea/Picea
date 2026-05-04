# Threat To Regression Test Mapping

This document maps each threat in `docs/security/threat-model.md` to concrete regression checks.

- Last updated: 2026-05-01
- Scope: current repository tests and CI policy checks

## Mapping Matrix

### TM-001

- Threat Summary: Unauthorized command execution
- Regression Objective: Denied authorization must not mutate state and must return typed denial
- Evidence (Test / CI Check): `Picea.Tests/GuardedDeciderTests.cs` -> `Handle_AuthorizationDenial_DoesNotMutateState`; `Picea.Tests/DeciderTests.cs` -> `GuardedHandle_AuthorizationDenied_ReturnsUnauthorizedAndObservesDenial`
- Current Status: Covered

### TM-002

- Threat Summary: Invalid command input drives illegal transitions
- Regression Objective: Validation/decision errors must preserve prior state and block event dispatch
- Evidence (Test / CI Check): `Picea.Tests/GuardedDeciderTests.cs` -> `Handle_ValidationDenial_DoesNotMutateState`; `Picea.Tests/DeciderTests.cs` -> `Handle_SetTarget_AboveMax_ReturnsInvalidTargetError`; `Picea.Tests/DeciderTests.cs` -> `Handle_ErrorDoesNotMutateState`
- Current Status: Covered

### TM-003

- Threat Summary: Runaway feedback loop denial of service
- Regression Objective: Infinite/cyclic feedback must terminate via guard or cancellation
- Evidence (Test / CI Check): `Picea.Tests/RuntimeTests.cs` -> `FeedbackLoop_ThrowsAtMaxDepth`; `Picea.Tests/RuntimeTests.cs` -> `CancellationDuringFeedbackLoop_StopsProcessing`
- Current Status: Covered

### TM-004

- Threat Summary: Concurrent dispatch race conditions
- Regression Objective: Parallel dispatches must remain serialized and deterministic
- Evidence (Test / CI Check): `Picea.Tests/RuntimeTests.cs` -> `ConcurrentDispatches_AreSerializedAndProduceCorrectFinalState`; `Picea.Tests/RuntimeTests.cs` -> `ConcurrentMixedDispatches_ProduceCorrectFinalState`; `Picea.Tests/GuardedDeciderTests.cs` -> `Handle_ConcurrentMixed_DenialsAndSuccess_CompleteWithoutGateStall`
- Current Status: Covered

### TM-005

- Threat Summary: Event-log tampering and integrity loss
- Regression Objective: Tampering, reordering, insertion/deletion, and invalid sequence metadata must fail verification/load
- Evidence (Test / CI Check): `Picea.Tests/EventLogTests.cs` -> `HashChain_TamperDetection_ModifiedEntryFailsVerification`; `Picea.Tests/EventLogTests.cs` -> `HashChain_TamperDetection_InsertedDeletedAndReorderedEntriesFailVerification`; `Picea.Tests/EventLogTests.cs` -> `LoadAsync_StorageOverload_ThrowsOnDuplicateSequenceNumber`; `Picea.Tests/EventLogTests.cs` -> `LoadAsync_StorageOverload_ThrowsOnNonPositiveSequenceNumber`
- Current Status: Covered

### TM-006

- Threat Summary: Supply-chain dependency vulnerabilities
- Regression Objective: PR/release pipelines must fail on critical/high vulnerable packages and run static/security guardrails
- Evidence (Test / CI Check): `.github/workflows/pr-validation.yml` -> job `security-scan` (`dotnet list package --vulnerable --include-transitive` + fail gate); `.github/workflows/cd.yml` -> step `Check for vulnerable packages (SCA)`; `.github/workflows/codeql.yml` -> `CodeQL Security Analysis` job; `.github/workflows/secrets-scan.yml` -> job `gitleaks` (`gitleaks/gitleaks-action@v2`)
- Current Status: Covered (CI policy checks)

## Gaps And Explicit Non-Coverage

The following risks are explicitly tracked as not currently covered by an automated regression check in this repository:

### Secret leakage into git history/repository

- Current Guardrail: Automated secrets scanning in `.github/workflows/secrets-scan.yml` (`gitleaks` job using `gitleaks/gitleaks-action@v2`) plus policy and review discipline via `SECURITY.md` and PR review
- Why It Is Not Fully Covered: Pattern/signature-based scanners can miss novel formats or obfuscated secrets; periodic ruleset tuning is still required

## Maintenance Contract

1. Any PR that introduces or changes a threat in `docs/security/threat-model.md` must update this mapping in the same PR.
2. New regression tests must reference the threat ID in test name or nearby comments where practical.
3. If a threat is accepted without automated regression coverage, it must appear in "Gaps And Explicit Non-Coverage" with concrete rationale.
4. CI policy checks are treated as regression coverage only when they are enforced in repository workflows.

## Additional CI Controls

- `.github/workflows/benchmarks.yml` enforces a 5% benchmark regression threshold for performance-sensitive changes. This is tracked as an availability/release-quality guard, not as direct security coverage for TM-006.
- `.github/workflows/pr-validation.yml` also enforces lint/format, PR metadata, and TODO hygiene checks. These are repository quality controls and are intentionally not counted as security regression evidence unless they directly mitigate a named threat.
