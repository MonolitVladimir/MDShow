# MD Show 1.0

Lightweight offline Markdown viewer/editor for Windows 11 x64.
Requires .NET Framework 4.8 and system Microsoft Edge WebView2 Evergreen.
No browser engine, Node runtime, cloud account or document upload is bundled.

Setup registers Open With, Default Apps and optional classic Explorer commands.
Full portable offers English, Russian, German, Arabic and Simplified Chinese UI.
English-only portable has the same document features and an English app UI;
Windows dialogs can follow the OS language. It still reads Arabic/CJK/Unicode
documents and preserves the full edition's saved language preference.

Extract the entire ZIP and run MDShow.exe. Portable does not register associations.

Settings, recent paths, recovery and WebView2 cache are separate for each edition:

- Installed: `%LOCALAPPDATA%\MDShow\Installed`
- Full portable: `%LOCALAPPDATA%\MDShow\PortableFull`
- English portable: `%LOCALAPPDATA%\MDShow\PortableEnglish`

All three editions may run together. IPC is independent by edition; a profile file
lock prevents concurrent writers across sessions. Portable means no installation,
not a profile beside the EXE; a read-only application folder is supported.

The old shared `%LOCALAPPDATA%\MDShow` files are neither imported nor deleted.
To transfer settings manually: close every edition, back up the old and new
profiles, then copy only `settings.json` to the intended new scope. English UI
stays English. Save recovered drafts as ordinary Markdown files in the old app
before opening them in the new edition; do not overwrite a new recovery file.
Cache does not need migration. If startup is blocked, finish Setup/uninstall or
close the other session using the same edition/profile; a damaged installation
can be repaired by rerunning Setup.

Read/edit/split, tabs, outline, GFM tables/tasks/code, literal search/replace,
undo/redo, theme selection, 50–300% zoom, PDF/HTML export and print are included.
Ctrl+O: open; Ctrl+N: new; Ctrl+S: save; Ctrl+Shift+S: save as; Ctrl+W: close;
Ctrl+Tab: next tab; Ctrl+F: find; Ctrl+1/2/3: view; Ctrl++/−/0: zoom; F1: help.
UTF-8, BOM UTF-16/32 and Windows-1251 plus original line endings are preserved.

Files are limited to 16 MiB. Preview has separate size/complexity/time limits.
External images are blocked; local resources must be below the document folder.
Clicked http/https/mail links open externally. Another local process replacing
paths or writing between conflict-check and save remains a concurrency limit.
Recovery is a local safety net, not an encrypted or independent backup.

Setup installs for all users in C:\Program Files\MD Show with administrator approval.
Close MD Show in every Windows session before uninstalling. Setup uses an
installation file gate and Restart Manager; it never terminates running apps.
Uninstall removes program files/registrations and the confirmed interactive
account's Installed settings, cache and recovery. Cleanup drops administrator
privileges using the verified non-administrator desktop token. If UAC uses a
different account, all profiles are retained with a message. If safe cleanup fails,
uninstall stops before removing program files; some cache may already be removed. Saved Markdown, portable profiles,
legacy shared files and other users' data are retained. Back up needed drafts first.

The header contains File (Alt+F), Save, Find, view modes and zoom; settings are in
File > Preferences.

EXE/Setup are unsigned; Windows may display a warning. Do not bypass protection.
Compare `Get-FileHash .\MDShow-1.0-portable-x64.zip -Algorithm SHA256` with the
separately supplied SHA256SUMS.txt. Use the corresponding name for other packages.

Original code: MIT (LICENSE). Libraries: THIRD-PARTY-NOTICES.txt.
Artwork/name permission: BRANDING.md; the code license does not license the logo.
Developer: MONOLIT Agency LLC (English display translation).
Copyright (c) 2026 ООО «Агентство «МОНОЛИТ».
Report security vulnerabilities using GitHub's private reporting feature:
https://github.com/MonolitVladimir/MDShow/security
Do not post private documents or exploit details publicly.
