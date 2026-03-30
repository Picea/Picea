📌 Onboarded for **Picea Core** on 2026-03-30. Squad imported from Picea.Abies project; context refreshed.

# DevOps / Infrastructure Engineer — History

## About This File
Pipeline decisions, container configs, deployment patterns, and CI optimization. Bailey owns release automation.

## Picea Release Pipeline

### Workflows
| Workflow | Trigger | Job | Purpose |
|----------|---------|-----|---------|
| **build.yml** | `main`, `develop`, PRs | `dotnet build` | Compile and validate syntax |
| **test.yml** | `main`, `develop`, PRs | `dotnet test` (TUnit) | Run all unit tests in parallel |
| **lint.yml** | `main`, `develop`, PRs | `dotnet format --verify-no-changes` | Code formatting check (Conventional Commits style) |
| **codeql.yml** | `main`, `develop` | CodeQL security scan | SAST security analysis |
| **benchmarks.yml** | `main` (post-merge) | BenchmarkDotNet | Performance regression detection (5% threshold) |
| **cd.yml** | `main` (post-merge) | NuGet publish | Package and publish to NuGet.org |

### CI Required Checks
All PRs must pass:
✅ **Build** — `dotnet build`  
✅ **Test** — `dotnet test`  
✅ **Format** — `dotnet format --verify-no-changes`  
✅ **CodeQL** — Security scanning passes  
✅ **Approval** — At least 1 approval (Reviewer)  
✅ **Conversations resolved** — All review comments addressed  

### Versioning

Uses **Nerdbank.GitVersioning** (`version.json`):
- Version is driven by git tags and the version.json config
- Patch version auto-increments on each commit to `main`
- Release version: `git tag v{X.Y.Z}` on main to "cut" a release
- CI reads the tag and publishes to NuGet with that version

**To cut a release:**
```bash
git checkout main
git pull origin main
git tag v1.2.3
git push origin v1.2.3
```
CI triggers `cd.yml` → builds, runs tests, publishes `Picea` v1.2.3 to NuGet.

### Trunk-Based Workflow

- **Main branch** is always deployable
- **Feature branches** are short-lived (<2 days)
- **Linear history** enforced (squash/rebase merging, no fast-forward merges)
- **No force pushes** allowed
- **Status checks** required before merge

### Environment Configuration

**NuGet Publishing:**
- Token stored as GitHub secret `NUGET_API_KEY`
- Endpoint: `https://api.nuget.org/v3/index.json`
- Package: `Picea`
- Feed includes XML doc comments (`.GenerateDocumentationFile`)

**CodeQL Configuration:**
- Language: C#
- Default queries enabled
- Runs on `main` and `develop` branches
- Post-merge (non-blocking for PRs, but should be checked before merge)

## CI Failures Investigated
*None yet — build/test failures, flakes, and resolutions tracked here.*

## Release History
*None yet — what was released, when, any hotfixes.*

## Performance Baseline Infrastructure

**BenchmarkDotNet Setup:**
- Runs on `main` post-merge as the source of truth
- Results published to `docs/benchmarks/kernel-baseline-*.md`
- 5% regression threshold triggers alert in release notes
- Local runs: `dotnet run -c Release -p Picea.Benchmarks/`

## Secrets & Credentials
- NUGET_API_KEY — GitHub secret, never logged or exposed
- No database credentials needed for Picea Core

## Gotchas & Learnings
*None yet — environment-specific issues, surprising CI behaviors, workarounds.*
