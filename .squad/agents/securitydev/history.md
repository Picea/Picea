📌 Onboarded for **Picea Core** on 2026-03-30. Squad imported from Picea.Abies project; context refreshed.

# Security Expert & Pentester — History

## About This File
Project-specific security learnings, vulnerability patterns, dependency audits, and threat assessments. Harper owns security hardening.

## Security for Picea Core

Picea is a foundational library with an extremely limited attack surface:
- **No network I/O** — the kernel is pure functions
- **No database** — no data persistence in the library itself
- **No user input** — domain modeling is up to the user
- **No reflection** (LINQ, JSON deserialization) in the hot path — uses static abstract members

The library **encodes invariants in types**, not validation at runtime. This reduces certain categories of vulnerabilities.

### Security Toolchain Status

| Layer | Tool | Status | Last Used |
|---|---|---|---|
| **SAST** | CodeQL (GitHub) | ✅ Active | CI/CD on `main` |
| **SCA** | `dotnet list package --outdated` | ✅ Manual | N/A (no dependencies) |
| **SCA** | `dotnet nuget verify` | ✅ Available | On demand |
| **Secrets** | GitHub Secret Scanning | ✅ Active | All branches |
| **Supply Chain** | Package signing | ⏳ TODO | Milestone: v1.0+ |

### Dependency Audit

**Current dependencies for `Picea` package:**
- None (zero external NuGet dependencies)

**Build/test dependencies:**
- `TUnit` — testing framework (MIT/Apache 2.0 licensed)
- `BenchmarkDotNet` — benchmarking (MIT licensed)
- `.NET 10 SDK` — runtime (MIT licensed)

All transitive dependencies are well-maintained Microsoft and community projects with active security monitoring.

### Known Threats & Mitigations

| Threat | Severity | Mitigation | Status |
|--------|----------|-----------|--------|
| Breaking API change (no SemVer enforcement) | Medium | Use Nerdbank.GitVersioning, tag releases, publish to NuGet only from `main` | ✅ In place |
| Compromised NuGet API key | Critical | Key stored in GitHub secret, never logged, rotated regularly | ✅ In place |
| Unsigned packages shipped | Medium | Plan to code-sign releases after v1.0 | ⏳ TODO |
| Numeric/allocation overflow in Mealy machine | Low | Struct-based implementation, no unbounded allocations in hot path | ✅ By design |

### Vulnerability Patterns (Picea-Specific)

**Not applicable:**
- SQL injection — no SQL in the library
- XSS — no HTML rendering
- Deserialization attacks — domain code owns deserialization, not the library
- Timing attacks — pure functions are deterministic, no crypto in kernel
- Integer overflow — users of constrained types guard their own ranges

**Applicable & how we handle them:**

1. **Type confusion** — Mitigated by discriminated unions (sum types). Illegal states unrepresentable.
2. **State invariant violation** — Mitigated by smart constructors and immutable records
3. **Effect execution bypassed** — Mitigated by returning effects as values; only the Interpreter can execute them

### CodeQL Configuration

Picea uses GitHub's default CodeQL C# queries:
- Standard rules for C# code quality
- Runs on `main` and `develop` branches post-push
- Results reviewed before merge

## False Positive Patterns
*None yet — real findings and how to interpret them tracked here.*

## Scanner Rules Added/Tuned
| Rule | Tool | Reason | Date |
|---|---|---|---|
| *None yet* | | | |

## Pentest & Audit History
*None yet — pentests or security audits and their results.*

## Threat Models
- **User threat model:** Domain code that uses Picea guards its own input validation
- **Library threat model:** Picea kernel is pure; no injection vectors in the transition function
- **Runtime threat model:** The Interpreter (not Picea) interprets effects; interpreter authors own security of effect execution

## Proactive Hardening Roadmap

| Item | Milestone | Status |
|---|---|---|
| Code signing releases | v1.0+ | ⏳ TODO |
| SBOM generation | v1.0+ | ⏳ TODO |
| Signed commits to main | End of Q2 2026 | ⏳ Proposed |
| Dependency audit CI check | End of Q2 2026 | ⏳ Proposed |
| Third-party security audit | Post v1.0 | ⏳ Future |

## Attack Surface Map
*Not yet mapped — all public endpoints, auth flows, data flows, external integrations tracked here.*

## Security Standards
*Refer to charter for baseline. Project-specific additions tracked here.*
