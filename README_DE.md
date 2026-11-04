# MD Show 1.0.1 — Anleitung

[Sprachen](README.md) · [English](README_EN.md) · [Русский](README_RU.md) · [العربية](README_AR.md) · [简体中文](README_ZH-CN.md)

## Funktionen und Voraussetzungen

Kompakter Markdown-Editor und Betrachter für **Windows 11 x64**. Lesen, Editor
und geteilte Ansicht mit Live-Vorschau, Tabs, Gliederung, zuletzt geöffnete Dateien,
Formatierung, Listenfortsetzung, Rückgängig/Wiederholen sowie Textsuche/-ersetzung.
GFM-Tabellen, Aufgabenlisten, Durchstreichen, Code und lokale Bilder werden
unterstützt. Export als HTML/PDF und Druckvorschau sind enthalten; Mermaid,
LaTeX und Plugins nicht. Windows 10 gehört nicht zur unterstützten Umgebung.

Erforderlich: .NET Framework 4.8 und **Microsoft Edge WebView2 Evergreen aus dem
System**. Kein eingebauter Browser, Electron, Node-Laufzeit, Konto oder Dokumenten-
Upload. Fehlendes WebView2 nur über Microsofts offizielle Verteilung installieren.

## Pakete

**Downloads ab nur 394 KB.**

| Datei | Downloadgröße | Zweck |
| --- | ---: | --- |
| `MDShow-1.0.1-Setup-x64.exe` | **2,41 MB** | Installation für alle Benutzer in Program Files mit Administratorrechten, fünf Sprachen, „Öffnen mit“, Standard-Apps und optionale klassische Explorer-Befehle |
| `MDShow-1.0.1-portable-x64.zip` | **410 KB** | Vollständig entpacken und `MDShow.exe` starten; fünf Oberflächensprachen |
| `MDShow-1.0.1-portable-en-x64.zip` | **394 KB** | Gleiche Dokumentfunktionen, nur englische Oberflächenressourcen |

MD Show bleibt kompakt, da es die auf dem System installierte **Microsoft Edge WebView2 Evergreen Runtime** nutzt; die Browser-Engine ist nicht im Paket enthalten. Die Downloadgrößen sind gerundet.

English-only unterstützt weiterhin russische, arabische, chinesische und gemischte
Unicode-Dokumente. Die Spracheinstellung der Vollversion bleibt erhalten.
Systemdialoge können der Windows-Sprache folgen. ZIPs registrieren keine
Dateizuordnungen und enthalten keine WebView2-Laufzeit.

## Installation und Deinstallation

Setup installiert für alle Benutzer unter `C:\Program Files\MD Show` und benötigt
Administratorrechte. ZIPs lassen sich ohne Installation in einen beschreibbaren
Ordner entpacken. MD Show über „Öffnen mit“/Standard-Apps auswählen; geschütztes
`UserChoice` wird nicht erzwungen geändert. Klassische Befehle stehen gegebenenfalls
unter „Weitere Optionen anzeigen“.

MD Show vor der Deinstallation in allen Windows-Sitzungen schließen. Setup prüft
Dateisperre und Restart Manager und beendet keine laufenden Prozesse. Entfernt
werden Programmdateien, Registrierungen und Einstellungen, Cache und Entwürfe des
Installed-Profils des bestätigten Sitzungskontos. Die Bereinigung erfolgt ohne
Administratorrechte. Bei anderem UAC-Konto bleiben alle Profile mit einem Hinweis
erhalten. Schlägt die sichere Bereinigung fehl, stoppt die Deinstallation vor dem
Entfernen der Programmdateien; ein Teil des Caches kann bereits gelöscht sein. Gespeicherte Markdown-Dateien,
Portable-Profile, alte gemeinsame Dateien und andere Benutzer bleiben unberührt.
Benötigte Entwürfe vor der Deinstallation sichern.

Die Kopfzeile zeigt Logo, **Datei**, Speichern, Suchen, Ansichten und Zoom. Datei
gruppiert Neu/Öffnen, Speichern, Export/Druck und Schließen. Einstellungen enthält
Design, Zeilenumbruch, Sprache und Gliederung. Alt+F öffnet das Menü, Pfeile navigieren,
Escape schließt es.

## Profil und Datenschutz

Einstellungen, letzte Pfade, Wiederherstellung und WebView2-Cache sind getrennt:

- Installiert: `%LOCALAPPDATA%\MDShow\Installed`
- Vollständiges Portable: `%LOCALAPPDATA%\MDShow\PortableFull`
- Englisches Portable: `%LOCALAPPDATA%\MDShow\PortableEnglish`

Alle drei Ausgaben dürfen gleichzeitig laufen. IPC ist je Ausgabe getrennt;
eine Dateisperre verhindert gleichzeitiges Schreiben aus mehreren Sitzungen.
Portable bedeutet ohne Installation, nicht ein Profil neben der EXE. Ein
schreibgeschützter Programmordner ist möglich.

Das alte gemeinsame `%LOCALAPPDATA%\MDShow` wird nicht importiert oder gelöscht.
Für eine manuelle Übernahme alle Ausgaben schließen, alte und neue Profile sichern
und nur `settings.json` in das gewünschte neue Profil kopieren. Die englische
Ausgabe bleibt englisch. Wiederhergestellte Entwürfe zuerst mit der alten Ausgabe
als Markdown speichern, dann mit der neuen öffnen; keine neue Wiederherstellungsdatei
überschreiben. Cache nicht übertragen. Bei blockiertem Start Setup/Deinstallation
abschließen oder die andere Sitzung mit diesem Profil schließen. Eine beschädigte
Installation lässt sich durch erneutes Setup reparieren.

Entwürfe werden ungefähr alle zehn Sekunden als **unverschlüsseltes JSON** unter
den Zugriffsrechten des Windows-Kontos gespeichert. Nach einem Absturz wird die
Wiederherstellung angeboten; dies ersetzt keine Sicherung. Bei extern geänderten
Dateien erscheint eine Konfliktabfrage. Abbrechen erhält Entwurf und externe Datei.
Atomare Speicherung schützt vor fehlgeschlagenem Ersetzen, bildet aber keine
gemeinsame Transaktion mit anderen Schreibern.

## Ansicht, Sprachen und Zeichenkodierung

Vollversion: Englisch, Russisch, Deutsch, Arabisch, vereinfachtes Chinesisch.
Automatik folgt Windows, sonst Englisch; manuelle Wahl bleibt gespeichert.
Arabische Oberfläche RTL, Dokumentrichtung unabhängig; Code, URLs und Pfade LTR.
Chinesisch verwendet System-Ersatzschriften. System-/helles/dunkles Design,
Dokumentzoom **50–300%** über Werkzeugleiste oder Strg+Mausrad. Die Teilungsbreite
bleibt erhalten. Echte chinesische IME und zusätzliche Windows-DPI benötigen die
manuellen Prüfungen der Versionshinweise.

UTF-8 mit/ohne BOM, UTF-16/32 mit BOM und Windows-1251 werden unterstützt.
Originalkodierung, BOM und LF/CRLF/CR bleiben erhalten. Nicht darstellbare Zeichen
erfordern ausdrücklich bestätigte Umstellung auf UTF-8.

| Tastenkürzel | Aktion |
|---|---|
| Strg+O / N; Strg+S / Strg+Umschalt+S | Öffnen / neu; speichern / speichern unter |
| Strg+W / Tab / Umschalt+Tab | Tab schließen / weiter / zurück |
| Strg+1 / 2 / 3 | Editor / geteilt / lesen |
| Strg+F / H; Strg+B / I / K | Suchen / ersetzen; fett / kursiv / Link |
| Strg+Z / Y; Strg+P; F1 | Rückgängig / wiederholen; drucken; Hilfe |
| Strg++ / − / 0; Strg+Mausrad | Zoom größer / kleiner / zurücksetzen |

## Grenzen und Sicherheit

Sehr große Dokumente mit sehr vielen kurzen Zeilen können die Editor- und Barrierefreiheitsverarbeitung von WebView2 lange beschäftigen. Bitte warten Sie auf das Ende des Ladevorgangs. Dies ist eine akzeptierte Einschränkung der ersten Version; siehe RELEASE-NOTES-v1.0.1.md.

Dateien bis **16 MiB**, Vorschau bis **1 Mi Zeichen** Eingabe und **2 Mi Zeichen**
HTML, begrenzte Komplexität und etwa **1,5 s** Worker-Zeitlimit. Bei zu komplexen
Dokumenten kann die Vorschau abgelehnt werden; der Editor bleibt nutzbar.
Externe Bilder sind gesperrt. Lokale Bilder/Markdown-Links müssen im Dokumentordner
oder darunter liegen. Vorhandene untergeordnete Junctions/Symlinks und unsichere
Schemata werden abgelehnt. Bewusst geöffnete http/https/mail-Links starten extern.

Ein anderer lokaler Prozess kann einen Pfad zwischen Prüfung und Öffnen ersetzen
(TOCTOU) oder zwischen Konfliktprüfung und Speichern schreiben. Diese Grenzen
bleiben bestehen. Windows/WebView2 aktuell halten; siehe [SECURITY.md](SECURITY.md).
Sicherheitslücken bitte ausschließlich über den privaten Meldeweg im
[GitHub-Sicherheitsbereich](https://github.com/MonolitVladimir/MDShow/security) melden.
Keine privaten Dokumente oder Exploit-Details in öffentlichen Issues teilen.

EXE und Setup sind **nicht signiert**; SmartScreen kann warnen. Schutzmechanismen
nicht umgehen. SHA-256 mit der separat gelieferten Prüfsummendatei vergleichen:

```powershell
Get-FileHash .\MDShow-1.0.1-portable-x64.zip -Algorithm SHA256
```

## Aus Quellcode erstellen

Benötigt werden Windows 11 x64, .NET Framework 4.8 und PowerShell 7.
Das Bootstrap-Skript lädt überprüfte Abhängigkeiten für WebView2 und Inno Setup.

```powershell
pwsh -File scripts/bootstrap.ps1 -Installer
pwsh -File scripts/build.ps1 -Installer
```

Die Pakete werden in `dist/` erstellt. Ein bitgenau reproduzierbarer Build wird
nicht garantiert. Optional können die Icon-Dateien mit
`scripts/make-icon.ps1` und Python/Pillow 12.2.0 neu erzeugt werden.

## Lizenz und Entwickler

Eigener Code und Dokumentation: [MIT](LICENSE).
Bibliotheken behalten [ihre Lizenzen](THIRD-PARTY-NOTICES.txt). Das Logo hat eine
[separate Erlaubnis](BRANDING.md); MIT gewährt keine Markenrechte.
Copyright (c) 2026 **ООО «Агентство «МОНОЛИТ»**.
**MONOLIT Agency LLC** ist die englische Anzeigeübersetzung, kein bestätigter
separater Registername. [Eigentumserklärung](COPYRIGHT.md).

Aktuelle Anforderungen und Einschränkungen stehen in den [Versionshinweisen](RELEASE-NOTES-v1.0.1.md).
