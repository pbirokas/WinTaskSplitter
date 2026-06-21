# WinTaskSplitter

Ein vollständiger Ersatz der Windows 11-Taskleiste, der die Taskleiste in farbige, konfigurierbare Zonen aufteilt.

## Features

- **Farbige Zonen** — beliebig viele Zonen nebeneinander, jede mit eigenem Namen, Hintergrund- und Rahmenfarbe
- **Zonenbreite per Maus** — Resize-Handle zwischen Zonen (↔-Cursor), Breiten werden persistent gespeichert
- **Zonen-Reihenfolge** — im Einstellungsfenster per ↑/↓ verschiebbar
- **Allgemein-Zone** — blendet sich automatisch aus, wenn keine Apps zugewiesen sind
- **Start-Schaltfläche** — ⊞-Button ist frei in jede Zone verschiebbar (Standard: Allgemein)
- **App-Verwaltung** — Drag & Drop zwischen Zonen; Zuweisung wird dauerhaft gespeichert
- **Natives Systemmenü** — Rechtsklick auf App-Icon öffnet das Windows-Kontextmenü
- **System-Bereich** — WLAN-, Lautstärke- und Akku-Status live; Klick öffnet das jeweilige native Flyout
- **Tray-Icons** — Drittanbieter-Infobereichssymbole (z. B. iCUE, Steam) werden eingebettet, inkl. „^"-Überlauf; immer-sichtbare Icons werden aus den Windows-Einstellungen übernommen
- **Klickbare Uhr** — Linksklick öffnet Benachrichtigungen/Kalender, Rechtsklick das Kontextmenü
- **Transparenz** — Hintergrund-Deckkraft der Zonen per Regler einstellbar
- **Einstellungsfenster** — alle Zonen in einer Übersicht bearbeiten

## Voraussetzungen

- Windows 11
- .NET 9 Runtime
- Keine Administratorrechte nötig (läuft als normaler Benutzer)

## Build

```powershell
dotnet build src/WinTaskSplitter/WinTaskSplitter.csproj --configuration Release
```

## Starten

```powershell
dotnet run --project src/WinTaskSplitter/WinTaskSplitter.csproj
```

Die App ersetzt die Windows-Taskleiste automatisch. Beim Beenden wird die originale Taskleiste wiederhergestellt.

**Notfall-Wiederherstellung** (falls die App abstürzt):
```powershell
Stop-Process -Name explorer -Force; Start-Process explorer
```

## Konfiguration

Gespeichert in `%APPDATA%\WinTaskSplitter\config.json`.

## Tech Stack

- C# / WPF / .NET 9
- CommunityToolkit.Mvvm
- Hardcodet.NotifyIcon.Wpf
- ManagedShell (eingebetteter Windows-Infobereich / Tray)

## Lizenz

MIT — siehe [LICENSE](LICENSE)
