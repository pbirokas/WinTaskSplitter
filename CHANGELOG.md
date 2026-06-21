# Changelog

## 2026-06-21

### System-Bereich (Tray)
- Windows-Infobereich-Symbole (WLAN, Lautstärke, Akku) werden jetzt direkt in der Bar angezeigt
- Live-Status: WLAN zeigt Verbindung (mit Standortdiensten auch Signalstärke), Lautstärke und Akku aktualisieren sich automatisch
- Klick auf ein Symbol öffnet das jeweilige native Windows-Flyout
- Drittanbieter-Tray-Icons (z. B. iCUE, Steam, Google Drive) erscheinen in der Bar, inkl. „^"-Überlauf für ausgeblendete Symbole
- Welche Icons immer sichtbar sind, wird automatisch aus den Windows-Einstellungen übernommen
- Uhr ist klickbar: Linksklick öffnet Benachrichtigungen/Kalender, Rechtsklick bietet Datum/Uhrzeit- und App-Einstellungen

### Darstellung
- Neue Transparenz-Option: Hintergrund-Deckkraft der Zonen per Regler einstellbar
- Start-Schaltfläche zeigt jetzt das originale Windows-11-Logo

### Zonen & Stabilität
- Zonenbreite lässt sich jetzt ruckelfrei anpassen – kein Zurückschnappen mehr
- Breite des Spalts zwischen den Zonen per Regler einstellbar (2–12 px)
- Icons beendeter Apps verschwinden zuverlässig (keine „Geister-Icons" mehr)
- Aufgeräumtes Zonen-Kontextmenü: „Zone bearbeiten" entfernt (alles in den Einstellungen)

### Unter der Haube
- App läuft ohne Administrator-Rechte – kein UAC-Prompt mehr beim Start
- Unerwartete Fehler werden in `%APPDATA%\WinTaskSplitter\error.log` protokolliert

## 2026-06-19 — Erster Release

### Taskleiste
- Windows 11-Taskleiste wird durch WinTaskSplitter ersetzt (Originale wird ausgeblendet)
- Taskleiste dockt am unteren Bildschirmrand an (Topmost, kein Work-Area-Eingriff)
- System-Zone rechts mit Uhr und Datum (Sekundenanzeige)
- Allgemein-Zone blendet sich automatisch aus wenn keine Apps zugewiesen sind

### Zonen
- Beliebig viele farbige Zonen nebeneinander (Name, Hintergrundfarbe, Rahmenfarbe)
- Zonenbreite per Maus ziehen (Resize-Handle zwischen Zonen, ↔-Cursor)
- Reihenfolge der Zonen im Einstellungsfenster änderbar (↑/↓)
- Zone-Label pro Zone ein-/ausblendbar; globale Schriftgröße für alle Labels
- Position der Allgemein-Zone frei konfigurierbar (nicht mehr fix rechts)

### Start-Schaltfläche
- Start-Schaltfläche (⊞) ist in jeder Zone platzierbar
- Standard: Allgemein-Zone; änderbar im Einstellungsfenster per Dropdown

### App-Verwaltung
- Laufende Fenster erscheinen als Icons in der zugewiesenen Zone
- Drag & Drop zwischen Zonen; Zuweisung wird persistent gespeichert
- Klick auf Icon: Fenster in den Vordergrund holen
- Rechtsklick auf Icon: natives Windows-Systemmenü (Minimieren, Maximieren, Schließen …)
- Icon-Auflösung: WM_GETICON → Klassen-Icon → Shell-Dateiinfo-Fallback

### Einstellungsfenster
- Alle Zonen in einer Übersicht bearbeitbar (Name, Label, Farben, Breite)
- Neue Zone hinzufügen / Zone löschen direkt in der Liste
- Globale Label-Schriftgröße per Slider
- Start-Schaltfläche-Zone per Dropdown wählbar
- Rechtsklick auf Zone → „Einstellungen…" öffnet das Fenster

### Konfiguration
- Einstellungen in `%APPDATA%\WinTaskSplitter\config.json` (atomarer Schreibvorgang)
- Automatischer Fallback auf Standardkonfiguration wenn Config fehlt oder beschädigt ist

### Stabilität
- UAC-Selbstelevierung: App fragt bei Bedarf nach Adminrechten
- Crash-Recovery: Native Taskleiste wird bei jedem unbehandelten Fehler automatisch wiederhergestellt
- ShellHook für Echtzeit-Erkennung von Fenster-Öffnen/-Schließen
