# Security policy

## Reporting a vulnerability

Please **do not** disclose security vulnerabilities, credentials or private documents in public GitHub issues or discussions.

Private vulnerability reporting is enabled. Use the repository's [Security section](https://github.com/MonolitVladimir/MDShow/security) and select **Report a vulnerability** to submit a private report to the maintainer. Please do not open a public issue for a security vulnerability.

Reports should include the affected version, reproduction steps and expected/actual behavior, while minimizing personal data.

## Supported versions

| Version | Security updates |
| --- | --- |
| 1.0.x | Current public release line |

There is no automatic update service. Install trusted releases manually and verify the accompanying SHA-256 checksums. MD Show's EXE and Setup files are not digitally signed.

## Security model and limitations

- Documents are processed locally; there is no application telemetry or document upload. External images are blocked and external links require user action.
- Markdown preview uses sanitization, a restrictive content security policy, native action validation and resource-path checks. These measures reduce risk but are not a guarantee that arbitrary untrusted files are safe.
- File reads are bounded to 16 MiB; preview uses additional size and complexity controls.
- User settings, recent-file paths and recovery drafts are unencrypted under Windows user-profile access controls. Keep independent backups and protect the Windows account.
- Installed, PortableFull and PortableEnglish maintain separate local profiles. Portable does not store settings beside its EXE.
- Path and file-conflict validation cannot prevent every race with unrelated local processes modifying files concurrently. Existing symlinks/junctions are rejected for protected operations; do not treat conflict warnings as a substitute for backups.
- Setup/uninstall uses Windows safeguards and does not forcibly terminate running applications. Close the application in every session and save drafts before removing it. Safe profile cleanup is limited to the verified user; unrelated profiles and saved documents are retained.
- Windows and the system WebView2 runtime should be kept up to date. Antivirus and dependency scans provide limited evidence, not a guarantee against all vulnerabilities.

Please see [LICENSE](LICENSE), [BRANDING.md](BRANDING.md) and [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt) for licensing and attribution.
