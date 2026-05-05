# Contributing to Picea

Thank you for your interest in contributing to Picea! This document provides guidelines for contributing to the kernel.

## 🌳 Trunk-Based Development

We follow **trunk-based development** practices:

- **Main branch** (`main`) is always deployable and protected
- All changes go through **pull requests**
- Feature branches are **short-lived** (< 2 days)
- Commits are **small and frequent**
- CI/CD validates all changes before merge

## 🔒 Branch Protection Rules

The `main` branch is protected with the following rules:

### Required Status Checks
All PRs must pass:
- ✅ **Build & Test** — `dotnet build`, `dotnet test`
- ✅ **Format** — `dotnet format --verify-no-changes`
- ✅ **CodeQL** — Security and code quality analysis
- ✅ **Benchmark Regression** — `Benchmarks` workflow (`Run benchmarks`), fails on regressions above 5%

### Pull Request Requirements
- ✅ **At least 1 approval** required
- ✅ **Up-to-date branches** — Must be current with main before merge
- ✅ **Conversation resolution** — All comments must be resolved
- ❌ **No force pushes** allowed
- ❌ **No branch deletions** allowed

### Additional Protections
- Administrators **must follow these rules** (no bypass)
- **Linear history** enforced (squash or rebase merging only)

## 🚀 Workflow

### 1. Create a Feature Branch

```bash
# Always start from the latest main
git checkout main
git pull origin main

# Create a short-lived feature branch
git checkout -b feature/your-feature-name
# or
git checkout -b fix/issue-description
```

### 2. Make Small, Incremental Changes

- Keep commits focused and atomic
- Write descriptive commit messages
- Commit frequently (multiple times per day)
- Follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```
feat: add observer composition combinator
fix: resolve feedback loop depth check
docs: update decider API documentation
test: add tracing integration tests
refactor: simplify runtime initialization
perf: optimize result type allocation
```

### 3. Keep Your Branch Up-to-Date

```bash
# Regularly sync with main
git fetch origin
git rebase origin/main
```

### 4. Run Tests Locally

Before pushing, ensure all tests pass:

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Verify formatting
dotnet format --verify-no-changes

# Run benchmarks (optional, for perf changes)
dotnet run --project Picea.Benchmarks -c Release
```

### 5. Push and Create Pull Request

```bash
# Push your branch
git push origin feature/your-feature-name

# Create PR via GitHub UI or CLI
gh pr create --title "feat: your feature description" --body "Description of changes"
```

### 6. Merge

Once approved and all checks pass:
- Use **Squash and Merge** (preferred) — Creates clean history
- Or **Rebase and Merge** — Preserves individual commits
- ❌ **Never use regular merge** — Creates messy history

## 📝 Pull Request Guidelines

### PR Title

Use [Conventional Commits](https://www.conventionalcommits.org/) format:

```
feat: add observer composition combinator
fix: resolve feedback loop depth check
docs: update decider API documentation
test: add tracing integration tests
refactor: simplify runtime initialization
perf: optimize result type allocation
```

### PR Description

Use the provided [PR template](.github/pull_request_template.md). Include:
- **What** — What changes are being made
- **Why** — Why these changes are needed
- **How** — How the changes work
- **Testing** — What testing was performed
- **Related Issues** — Link to any related issues

### PR Size
- Keep PRs **small** (< 400 lines changed)
- Break large features into multiple PRs
- Each PR should be independently reviewable

## 🧪 Testing Requirements

### Unit Tests
- All new public APIs must have unit tests
- Aim for high code coverage (> 80%)
- Tests should be fast (< 1s per test)
- Use xUnit with `[Fact]` and `[Theory]`

### Benchmarks
- Performance-sensitive changes must include benchmark comparisons
- Use BenchmarkDotNet
- No structural regressions allowed (> 5% degradation blocks merge)

## 📚 Code Style

### C# Guidelines
- Follow [C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use pure functions where possible
- Prefer immutability (records, readonly structs)
- Use pattern matching
- Prefer `Result<T, E>` over exceptions for expected failures
- Enable nullable reference types

### Architecture Principles
- **Zero dependencies** — The kernel has no external dependencies
- **Mealy machine semantics** — `(State × Event) → (State × Effect)`
- **Separation of concerns** — Pure domain logic (Automaton) vs. runtime infrastructure
- **Algebraic correctness** — Observer/Interpreter compose as monadic pipelines

## 🐛 Bug Reports

When reporting bugs, include:
- Clear description of the issue
- Steps to reproduce
- Expected vs actual behavior
- .NET version and OS
- Minimal code sample

## 💡 Feature Requests

When requesting features:
- Check existing issues first
- Describe the use case
- Explain why it fits the kernel (not a runtime concern)
- Consider mathematical grounding — does it preserve the Mealy machine abstraction?

## 🔐 Security

- Review [SECURITY.md](SECURITY.md) for security policies
- Never commit secrets or API keys
- Report security vulnerabilities privately to me@mauricepeters.dev

## 📜 License

By contributing, you agree that your contributions will be licensed under the same [Apache 2.0 License](LICENSE) that covers this project.

---

Thank you for contributing to Picea! 🌲
