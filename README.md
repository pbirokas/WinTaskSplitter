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
- **Einstellungsfenster** — alle Zonen in einer Übersicht bearbeiten

## Voraussetzungen

- Windows 11
- .NET 9 Runtime
- Administratorrechte (werden beim Start automatisch per UAC angefragt)

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

## Lizenz

MIT — siehe [LICENSE](LICENSE)
