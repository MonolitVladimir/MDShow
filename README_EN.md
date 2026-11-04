# MD Show 1.0.1 — English quick guide

For features, downloads, requirements and source builds, see the [main README](README.md).

## Download

**Downloads from just 394 KB.** All packages are for **Windows 11 x64**. Packages are available in [GitHub Releases](https://github.com/MonolitVladimir/MDShow/releases/latest).

| File | Download size | Edition |
| --- | ---: | --- |
| `MDShow-1.0.1-Setup-x64.exe` | **2.41 MB** | Installer; five interface languages and Windows file associations |
| `MDShow-1.0.1-portable-x64.zip` | **410 KB** | Portable; five interface languages |
| `MDShow-1.0.1-portable-en-x64.zip` | **394 KB** | Portable; English interface with Unicode document support |

MD Show stays compact by using the **Microsoft Edge WebView2 Evergreen Runtime** installed on your system; the browser engine is not bundled with the app. Download sizes are rounded.

## Keyboard shortcuts

| Shortcut | Action |
| --- | --- |
| `Ctrl+O`, `Ctrl+N` | Open / new document |
| `Ctrl+S`, `Ctrl+Shift+S` | Save / Save as |
| `Ctrl+W`, `Ctrl+Tab`, `Ctrl+Shift+Tab` | Close / next / previous tab |
| `Ctrl+1`, `Ctrl+2`, `Ctrl+3` | Editor / split / reading |
| `Ctrl+F`, `Ctrl+H` | Find / replace |
| `Ctrl+A` | Select text in the active editor, preview or input |
| `Ctrl+Z`, `Ctrl+Y` | Undo / redo |
| `Ctrl++`, `Ctrl+−`, `Ctrl+0`, `Ctrl+wheel` | Document zoom |
| `Alt+F`, `F1`, `Ctrl+P` | File menu / shortcuts / print |

**File → Preferences** contains theme, word wrap, language and outline settings.

The English-only portable edition preserves Unicode/Arabic/CJK document support and all document features. Some native dialogs use the Windows display language. Files with many short lines may slow down the editor; see the [release notes](RELEASE-NOTES-v1.0.1.md).

Recovery drafts and recent paths are unencrypted local data. Save recovered drafts as regular Markdown files and maintain independent backups. See [SECURITY.md](SECURITY.md).
