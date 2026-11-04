# MD Show 1.0.1 — native dialog fix

This maintenance release removes the extra symbols that could appear next to
quotation marks around file names in native Windows dialogs. It covers the
unsaved-changes prompt, open-error messages and unreadable-recovery messages.
Arabic text within file names retains its writing direction.

## Downloads

| File | Edition |
| --- | --- |
| `MDShow-1.0.1-Setup-x64.exe` | Installer, five interface languages |
| `MDShow-1.0.1-portable-x64.zip` | Portable, five interface languages |
| `MDShow-1.0.1-portable-en-x64.zip` | Portable, English interface |

SHA-256 checksums are provided in `SHA256SUMS.txt`. The packages remain unsigned.

Requirements and known limitations are unchanged from [1.0.0](RELEASE-NOTES-v1.0.md):
Windows 11 x64, .NET Framework 4.8 and the system WebView2 Evergreen Runtime.
Close MD Show before updating. Documents and profile locations are unchanged.

## По-русски

Убраны лишние значки рядом с кавычками вокруг имени файла в диалоге сохранения,
сообщениях об ошибке открытия и нечитаемом файле восстановления. Направление
арабского текста в именах файлов сохранено. Перед обновлением закройте MD Show.
Системные требования и известные ограничения версии 1.0.0 остаются прежними.
