# Threat Model (Living)

This document is the living security threat model for Picea.

- Last updated: 2026-05-01
- Owners: Security Expert + maintainers of affected components
- Primary scope: `Picea` kernel/runtime library, `Picea.Commanding` guarded pipeline, event-log persistence and replay, and repository CI security gates.

## 1. Context And Scope

Picea is a .NET library for deterministic state machines (automata/deciders), with optional guarded command handling and optional hash-chain tamper-evidence for event logs.

### In scope

- Runtime execution safety (`AutomatonRuntime`, `DecidingRuntime`, `GuardedDecidingRuntime`)
- Command authorization/validation staging in guarded pipelines
- Event log integrity (plain and hash-chain modes)
- Security-relevant CI controls currently present in `.github/workflows`

### Out of scope

- Downstream application authentication implementation details outside this repository
- Infrastructure/cloud runtime hardening for consumer applications

## 2. Security Objectives

1. Preserve state integrity under concurrent/hostile workloads.
2. Prevent unauthorized or invalid command execution when guarded pipelines are chosen.
3. Bound interpreter-driven feedback loops to prevent runaway resource consumption.
4. Detect event-log tampering when tamper-evidence mode is used.
5. Detect known vulnerable dependencies before merge/release.

## 3. Assets

- Domain state managed by runtimes
- Event streams (`EventLog<TEvent>`, `HashChainEventLog<TEvent>`)
- Integrity metadata (`previousHash`, `hash`, `AnchorHash`, `CurrentHash`)
- CI trust signal (PR and release gates)

## 4. Trust Boundaries

1. Application caller -> runtime command/event entry points
2. Runtime -> observer/interpreter delegates (host-provided capabilities)
3. In-memory event stream -> persisted JSONL/hash-chain storage
4. Source/dependencies -> CI pipeline gates

## 5. Threat Register

## TM-001 Unauthorized command execution (when command boundary is exposed)

- Category: Authorization bypass
- Attack path: Caller issues privileged command without sufficient role/policy.
- Impact: State mutation not allowed by business policy.
- Current controls:
  - Guarded pipeline (`Authorize -> Validate -> Decide`) in `GuardedDecidingRuntime`.
  - Denial events can be audited through `DenialObserver`.
- Residual risk:
  - Baseline `DecidingRuntime` does not enforce authorization by itself.
  - Consumers must deliberately choose guarded runtime when authorization belongs in the runtime boundary.
- Verification evidence:
  - `Picea.Tests/GuardedDeciderTests.cs` (`Handle_AuthorizationDenial_DoesNotMutateState`)
  - `Picea.Tests/DeciderTests.cs` (`GuardedHandle_AuthorizationDenied_ReturnsUnauthorizedAndObservesDenial`)

## TM-002 Invalid command input drives illegal transitions

- Category: Input validation / invariant violation
- Attack path: Caller sends out-of-range or malformed command values.
- Impact: Corrupted state or policy violation.
- Current controls:
  - Guarded validation stage (`Validate`) with short-circuit denial.
  - Decider returns typed errors for invalid commands without mutating state.
- Verification evidence:
  - `Picea.Tests/GuardedDeciderTests.cs` (`Handle_ValidationDenial_DoesNotMutateState`)
  - `Picea.Tests/DeciderTests.cs` (`Handle_SetTarget_AboveMax_ReturnsInvalidTargetError`)
  - `Picea.Tests/DeciderTests.cs` (`Handle_ErrorDoesNotMutateState`)

## TM-003 Runaway feedback loop denial of service

- Category: Availability / resource exhaustion
- Attack path: Interpreter emits cyclic feedback events indefinitely.
- Impact: CPU exhaustion, unbounded recursion/looping, service disruption.
- Current controls:
  - Hard feedback depth guard (`MaxFeedbackDepth = 64`).
  - Cancellation token propagation through dispatch and effect interpretation paths.
- Verification evidence:
  - `Picea.Tests/RuntimeTests.cs` (`FeedbackLoop_ThrowsAtMaxDepth`)
  - `Picea.Tests/RuntimeTests.cs` (`CancellationDuringFeedbackLoop_StopsProcessing`)

## TM-004 Concurrent command/event dispatch race conditions

- Category: Integrity / concurrency safety
- Attack path: Parallel callers attempt interleaving state mutations.
- Impact: Lost updates, inconsistent state, non-deterministic behavior.
- Current controls:
  - Serialized public runtime entry points via semaphore when `threadSafe: true` (default).
- Verification evidence:
  - `Picea.Tests/RuntimeTests.cs` (`ConcurrentDispatches_AreSerializedAndProduceCorrectFinalState`)
  - `Picea.Tests/RuntimeTests.cs` (`ConcurrentMixedDispatches_ProduceCorrectFinalState`)
  - `Picea.Tests/GuardedDeciderTests.cs` (`Handle_ConcurrentMixed_DenialsAndSuccess_CompleteWithoutGateStall`)

## TM-005 Event log tampering (integrity compromise)

- Category: Data integrity / tampering
- Attack path: Modify, insert, delete, reorder persisted event-log entries.
- Impact: Corrupted replay history, misleading audit/reconstruction.
- Current controls:
  - Hash-chain event log mode and explicit verification (`VerifyChain`, `VerifyRange`, `VerifyAnchor`).
  - Strict sequence validation on load.
- Verification evidence:
  - `Picea.Tests/EventLogTests.cs` (`HashChain_TamperDetection_ModifiedEntryFailsVerification`)
  - `Picea.Tests/EventLogTests.cs` (`HashChain_TamperDetection_InsertedDeletedAndReorderedEntriesFailVerification`)
  - `Picea.Tests/EventLogTests.cs` (`LoadAsync_StorageOverload_ThrowsOnDuplicateSequenceNumber`)

## TM-006 Supply-chain dependency vulnerabilities

- Category: Software composition risk
- Attack path: Introduce direct/transitive package with known CVE.
- Impact: Compromise through known vulnerable component.
- Current controls:
  - CI vulnerable-package checks in:
    - `.github/workflows/pr-validation.yml` (`security-scan` job)
    - `.github/workflows/cd.yml` (`Check for vulnerable packages (SCA)` step)
  - Dedicated secrets scanning workflow in `.github/workflows/secrets-scan.yml` (`gitleaks` job using `gitleaks/gitleaks-action@v2` on `pull_request` and `push` to `main`).
  - CodeQL static analysis in `.github/workflows/codeql.yml`.
- Residual risk:
  - CI signals remain constrained by scanner/ruleset coverage and may produce false negatives for novel credential formats.
- Verification evidence:
  - Workflow policy-as-code checks above (CI-enforced; not a unit-test path).

## 6. Monitoring And Response

- CI failures in security jobs are treated as release/merge blockers for critical and high vulnerable dependency findings.
- Runtime/decider tracing (`Picea.Tests/TracingTests.cs`) provides operational signal paths for dispatch/start/handle failures.
- Reported vulnerabilities follow `SECURITY.md` coordinated disclosure path.

## 7. Maintenance Rules

1. Update this document in the same PR when attack surface changes (new public command boundary, persistence mode, runtime behavior, or CI security control).
2. Every new threat entry must include concrete verification evidence (test name or CI policy gate).
3. Every removed/changed mitigation must update residual risk text.
4. Keep threat IDs stable once published; add new IDs instead of renumbering.
