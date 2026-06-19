# WinTaskSplitter – Projekt-Dokumentation für Claude

## Was ist das Projekt?

WinTaskSplitter ist ein vollständiger Ersatz der Windows 11-Taskleiste (C# + WPF, .NET 9).
Ziel: Die Taskleiste wird in konfigurierbare Zonen unterteilt, die optisch und räumlich getrennt sind.
Entwickler: Pantelis Birokas, 42" 4K Monitor (3840×2160).

## Tech Stack

- **C# + WPF**, .NET 9, `net9.0-windows`
- **CommunityToolkit.Mvvm** — MVVM (ObservableObject, RelayCommand, ObservableProperty)
- **Microsoft.Xaml.Behaviors.Wpf** — XAML Behaviors
- **Hardcodet.NotifyIcon.Wpf** — System-Tray
- `UseWPF=true`, `UseWindowsForms=true` (für `Screen`-Klasse)

## Wichtige Eigenheit: Namespace-Konflikte

Da `UseWindowsForms=true` aktiv ist, gibt es häufige Mehrdeutigkeit zwischen WPF und WinForms.
**In jedem neuen `.cs`-File** das WPF-Typen verwendet, müssen Aliases gesetzt werden:

```csharp
using Application     = System.Windows.Application;
using MessageBox      = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage  = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using Color           = System.Windows.Media.Color;
using ColorConverter  = System.Windows.Media.ColorConverter;
using Brushes         = System.Windows.Media.Brushes;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Button          = System.Windows.Controls.Button;
using UserControl     = System.Windows.Controls.UserControl;
using DragEventArgs   = System.Windows.DragEventArgs;
using DragDropEffects = System.Windows.DragDropEffects;
using MouseEventArgs  = System.Windows.Input.MouseEventArgs;
using Point           = System.Windows.Point;
using KeyEventArgs    = System.Windows.Input.KeyEventArgs;
using Key             = System.Windows.Input.Key;
```

## Build & Run

```powershell
cd f:\Entwicklung\WinTaskSplitter
dotnet build src/WinTaskSplitter/WinTaskSplitter.csproj --configuration Debug
dotnet run --project src/WinTaskSplitter/WinTaskSplitter.csproj
```

- App benötigt Admin-Rechte → UAC-Prompt erscheint automatisch (Self-Elevation in App.xaml.cs)
- Vor einem neuen Build: `WinTaskSplitter.exe` im Task-Manager beenden (sperrt die .exe)

## Taskleiste wiederherstellen (Notfall)

Falls WinTaskSplitter abstürzt und die originale Windows-Taskleiste nicht zurückkommt:
```powershell
Stop-Process -Name explorer -Force; Start-Process explorer
```

## Projektstruktur

```
src/WinTaskSplitter/
├── App.xaml / App.xaml.cs          — Startup, Self-Elevation, globaler Crash-Handler
├── app.manifest                    — asInvoker (Self-Elevation übernimmt Admin-Anfrage)
├── WinTaskSplitter.csproj
│
├── Native/
│   └── NativeMethods.cs            — ALLE P/Invoke Deklarationen
│                                     (AppBar, Shell, DWM, User32, SHGetFileInfo, ...)
│
├── Models/
│   ├── Zone.cs                     — Zone-Datenmodell (Name, Farben, FontSize, ShowLabel)
│   ├── AppAssignment.cs            — ProcessName → ZoneId Mapping
│   └── AppSettings.cs              — Globale Config (Zonen, Assignments, Edge, BarHeight)
│
├── ViewModels/
│   ├── TaskbarViewModel.cs         — Haupt-VM: Zonen verwalten, Apps zuordnen, Start-Menü
│   ├── ZoneViewModel.cs            — Zone-VM mit ObservableProperties für alle Zone-Felder
│   └── AppItemViewModel.cs         — Laufendes Fenster als VM (Icon, Title, IsActive)
│
├── Views/
│   ├── TaskbarWindow.xaml/.cs      — Haupt-AppBar-Fenster (DockPanel mit Zonen)
│   ├── ZonePanel.xaml/.cs          — Zone-UserControl (Label links + Icons + Context-Menü)
│   ├── SystemZonePanel.xaml/.cs    — System-Zone (Start-Button + Uhr)
│   ├── ZoneEditDialog.xaml/.cs     — Zone-Bearbeiten-Dialog (Name, Farbe, Schriftgröße)
│   └── InputDialog.xaml/.cs        — Einfacher Text-Eingabe-Dialog
│
├── Services/
│   ├── AppBarService.cs            — Positioniert Fenster am Bildschirmrand (SetWindowPos)
│   ├── TaskbarHider.cs             — Versteckt/Zeigt Windows Shell_TrayWnd
│   ├── WindowTracker.cs            — ShellHook + EnumWindows → TrackedWindow Collection
│   └── ConfigService.cs            — JSON-Persistenz in %APPDATA%\WinTaskSplitter\
│
└── Converters/
    ├── StringToBrushConverter.cs   — Hex-String → SolidColorBrush
    └── NullToVisibilityConverter.cs — null → Collapsed / nicht-null → Visible (+ Inverse)
```

## Schlüssel-Architektur-Entscheidungen

### AppBar vs. SetWindowPos
Die Windows AppBar API (`SHAppBarMessage`) wurde **nicht** verwendet, weil die Windows-Taskleiste
selbst als AppBar registriert bleibt (auch wenn sie per `ShowWindow` versteckt wird) und uns nur
einen winzigen Reststreifen lässt. Stattdessen verwendet `AppBarService.cs` direkt `SetWindowPos`
mit `HWND_TOPMOST`.

### DPI-Handling
- `GetDpiForWindow(hwnd)` liefert das echte DPI (z.B. 144 für 150%-Skalierung)
- `SystemParameters.PrimaryScreenWidth/Height` sind immer in WPF-Einheiten (96 DPI Basis)
- Umrechnung: `physischePx = wpfEinheit * (dpi / 96.0)`
- **Niemals** `Screen.Bounds` direkt für WPF-Koordinaten verwenden — mehrdeutig je nach DPI-Modus

### Icon-Ladereihenfolge (WindowTracker.cs)
1. `WM_GETICON` (ICON_BIG, dann ICON_SMALL2, dann ICON_SMALL)
2. `GetClassLongPtr(GCLP_HICON)` — Klassen-Icon
3. `SHGetFileInfo` über Exe-Pfad als letzter Fallback
4. Fallback-Anzeige: erster Buchstabe des Prozessnamens

### Namespace-Pattern für neue Views
Jede neue `.cs`-Datei in `Views/` braucht die Aliases (siehe oben).

## Konfiguration

Gespeichert in: `%APPDATA%\WinTaskSplitter\config.json`

Standard-Zonen beim ersten Start:
- **Privat** (#FF1A2433 / #FF2A4A7F)
- **Arbeit** (#FF1A2A1A / #FF2A6A2A)  
- **Dev** (#FF2A1A2A / #FF6A2A6A)
- **System** (fest, nicht löschbar) — Start-Button + Uhr
- **Allgemein** (auto-show/hide wenn unbekannte Apps offen)

## Offene Milestones (Stand 2026-06-19)

### Milestone 2 (nächstes):
- [ ] Autostart-Option (Registry: `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`)
- [ ] Thumbnail-Vorschau beim Hover (DWM `DwmRegisterThumbnail`)
- [ ] Einstellungs-Fenster (Position: unten/oben/links/rechts, BarHeight)

### Milestone 3:
- [ ] Vollständige AppBar-Integration (Work Area anpassen für maximierte Fenster)
- [ ] Multi-Monitor-Unterstützung
- [ ] Jump-List-Menü für App-Icons (ICustomDestinationList)
- [ ] Drag & Drop zwischen Zonen per Maus (funktioniert) + Touch-Support
