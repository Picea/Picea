# Security Policy

## Supported Versions

We actively support the latest major version of Picea. Security updates are provided for:

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | ✅ Yes             |
| < 1.0   | ❌ No              |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report security vulnerabilities by emailing: **me@mauricepeters.dev**

You should receive a response within 48 hours. If for some reason you do not, please follow up via email to ensure we received your original message.

Please include the following information in your report:

- Type of issue (e.g., buffer overflow, memory safety, denial of service, etc.)
- Full paths of source file(s) related to the manifestation of the issue
- The location of the affected source code (tag/branch/commit or direct URL)
- Any special configuration required to reproduce the issue
- Step-by-step instructions to reproduce the issue
- Proof-of-concept or exploit code (if possible)
- Impact of the issue, including how an attacker might exploit it

This information will help us triage your report more quickly.

## Preferred Languages

We prefer all communications to be in English.

## Security Measures

### Automated Dependency Scanning

We automatically scan for vulnerable dependencies using:

- **NuGet Audit**: Built into .NET SDK, runs on every restore (`all` mode, `low` severity)
- **GitHub Dependabot**: Monitors for security updates weekly
- **CodeQL**: Static analysis on every PR

### Zero-Dependency Architecture

Picea's kernel has **zero external dependencies** — it depends only on the .NET Base Class Library. This significantly reduces the attack surface:

- No transitive dependency vulnerabilities
- No supply chain risks from third-party packages
- Only `System.Diagnostics.DiagnosticSource` for OpenTelemetry tracing (BCL built-in)

### Code Review

All pull requests are reviewed for:

- Secure coding practices
- Thread-safety guarantees (SemaphoreSlim serialization)
- Proper input validation
- Bounded feedback loops (max 64 depth)

## Security Best Practices for Users

When using Picea in your projects:

1. **Keep Picea Updated**: Always use the latest stable version
2. **Enable NuGet Audit**: Ensure `NuGetAudit` is enabled in your projects (enabled by default in .NET 10+)
3. **Validate Commands**: When using the Decider pattern, validate all command inputs before processing
4. **Bound Your Effects**: Ensure Interpreter implementations don't create unbounded feedback loops
5. **Monitor Tracing**: Use the built-in OpenTelemetry tracing to detect anomalies

## Security Updates

Security updates will be released as soon as possible after a vulnerability is confirmed. We will:

1. Publish a GitHub Security Advisory
2. Release a patch version
3. Update this document with mitigation steps if immediate patching is not possible
4. Notify users through GitHub releases and repository notifications

## Acknowledgments

We thank security researchers who responsibly disclose vulnerabilities to us. With your permission, we will acknowledge your contribution in our release notes.

## Contact

For security-related questions that are not vulnerability reports, please open a GitHub discussion or issue.

---

*Last Updated: March 9, 2026*
