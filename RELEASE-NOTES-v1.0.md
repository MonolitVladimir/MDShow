# MD Show 1.0.0 — first public release

MD Show is a lightweight offline Markdown viewer and editor for **Windows 11 x64**.

**Requirements:** .NET Framework 4.8 and the system Microsoft Edge WebView2 Evergreen Runtime. The browser runtime is not bundled.

**Download:** [GitHub Releases](https://github.com/MonolitVladimir/MDShow/releases).

## Download options

| File | Description |
| --- | --- |
| `MDShow-1.0-Setup-x64.exe` | Installer for all Windows users; five interface languages and Open with / Default apps integration |
| `MDShow-1.0-portable-x64.zip` | Portable edition with five interface languages |
| `MDShow-1.0-portable-en-x64.zip` | English-only portable interface; Unicode document support retained |

All three distributions are **unsigned**. Windows may show an unknown-publisher warning. Compare SHA-256 checksums with the release's `SHA256SUMS.txt`; do not disable Windows protection to install.

## Highlights

- Read, edit and preview Markdown side by side; tabs, outline and recent files.
- GFM tables/tasks/code and local images, search/replace, undo/redo, zoom and themes.
- English, Russian, German, Arabic (RTL) and Simplified Chinese interfaces.
- Preservation of document encoding and line endings; atomic saves, conflict warnings and local recovery.
- HTML/PDF export and printing.
- Faster application startup, with native Windows window controls and improved keyboard text selection.

## Known limitations

- The maximum file size is **16 MiB**. Preview has additional size (1 million input characters), complexity and rendering limits. Documents with very many short lines may make editing temporarily unresponsive.
- Immediate relaunch can wait for a previous WebView2 process to finish.
- Mermaid, LaTeX, remote images and plugins are not supported.
- Settings and recovery drafts are stored locally without encryption; recovery is not a backup.
- Separate Windows-language IME, high-contrast and system scaling configurations have not all been manually verified. On a 1366×768 display, maximize the window if its lower edge is obscured.
- The installer does not force-close running applications. Close MD Show across Windows sessions and save drafts before uninstalling. Saved documents and other users' profiles are retained.

See [README](README.md) and [SECURITY.md](SECURITY.md) for detailed operating and security information.

## License

Original source code and documentation: [MIT](LICENSE). Artwork is subject to [separate permissions](BRANDING.md); dependencies retain [their licenses](THIRD-PARTY-NOTICES.txt).

Copyright (c) 2026 ООО «Агентство «МОНОЛИТ».

## Кратко по-русски

Первый публичный выпуск MD Show: просмотр и редактирование Markdown в Windows 11, пять языков интерфейса, установка или portable, работа без облачного аккаунта. Установщик и EXE без цифровой подписи. Требуются .NET Framework 4.8 и системный WebView2 Evergreen. Подробнее: [README_RU.md](README_RU.md).
