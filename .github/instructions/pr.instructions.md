---
applyTo: '**'
---

# Pull Request Guidelines

This document contains all guidelines for creating and maintaining pull requests in the Picea project.

## Code Formatting

### `dotnet format` Must Pass

Before submitting a PR, ensure your code passes the formatting check:

```bash
dotnet format --verify-no-changes
```

If there are formatting issues, fix them with:

```bash
dotnet format
```

**Important:** When fixing formatting issues in a PR branch, use `--include` to target only your changed files:

```bash
dotnet format Picea/Picea.csproj --include path/to/your/file.cs
```

## PR Title Guidelines

### Conventional Commits Format

PR titles MUST follow [Conventional Commits](https://www.conventionalcommits.org/) format:

```
<type>[optional scope]: <description>
```

### Types

| Type | Description |
|------|-------------|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation only changes |
| `style` | Changes that don't affect code meaning (formatting, whitespace) |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `perf` | Performance improvement |
| `test` | Adding or correcting tests |
| `build` | Changes to build system or dependencies |
| `ci` | Changes to CI configuration |
| `chore` | Other changes that don't modify src or test files |

### Examples

✅ Good PR titles:
- `feat: Add observer retry combinator`
- `fix: Resolve feedback loop depth overflow`
- `perf: Optimize Result type allocation`
- `docs: Update Decider API documentation`
- `test: Add tracing integration tests`

❌ Bad PR titles:
- `Update code` (too vague)
- `Fixed bug` (missing type prefix)
- `WIP` (not descriptive)

## CI Requirements

Before a PR can be merged, the following CI checks must pass:

1. **Build** — `dotnet build` must succeed with no errors
2. **Lint** — `dotnet format --verify-no-changes` must pass
3. **Tests** — All unit tests must pass
4. **CodeQL** — Security analysis must pass

## Review Process

1. Request review from at least one team member
2. Address all review comments
3. Re-request review after making changes
4. Ensure all CI checks pass
5. Squash and merge when approved
