using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTaskSplitter.Native;
using WinTaskSplitter.Services;

namespace WinTaskSplitter.ViewModels;

/// <summary>
/// Polls live system status (WLAN signal, volume, battery) every couple of seconds
/// and exposes Segoe Fluent / MDL2 glyphs + tooltips for the system zone.
/// Clicking an icon opens the matching native Windows flyout.
/// Glyph code points are from the Private Use Area of "Segoe Fluent Icons" /
/// "Segoe MDL2 Assets" and are built numerically to avoid embedding PUA chars in source.
/// </summary>
public partial class SystemStatusViewModel : ObservableObject
{
    // ── Glyph code points (Segoe MDL2 / Fluent) ──────────────────────────
    private const int GlyphNetworkOffline = 0xF384;
    private const int GlyphWifiFull       = 0xE701;
    private const int GlyphWifi3          = 0xE874;
    private const int GlyphWifi2          = 0xE873;
    private const int GlyphWifi1          = 0xE872;
    private const int GlyphMute           = 0xE74F;
    private const int GlyphVolume0        = 0xE992;
    private const int GlyphVolume1        = 0xE993;
    private const int GlyphVolume2        = 0xE994;
    private const int GlyphVolume3        = 0xE995;
    private const int GlyphBattery0       = 0xE850; // .. E85A (0..10)
    private const int GlyphBatteryCharge0 = 0xE85B; // .. E865 (0..10)

    private static string G(int code) => char.ConvertFromUtf32(code);

    private readonly DispatcherTimer _timer;

    [ObservableProperty] private string _wifiGlyph;
    [ObservableProperty] private string _wifiTooltip  = "WLAN";

    [ObservableProperty] private string _volumeGlyph;
    [ObservableProperty] private string _volumeTooltip = "Lautstärke";

    [ObservableProperty] private string _batteryGlyph;
    [ObservableProperty] private string _batteryTooltip  = "Akku";
    [ObservableProperty] private bool   _batteryVisible  = true;

    public SystemStatusViewModel()
    {
        _wifiGlyph    = G(GlyphNetworkOffline);
        _volumeGlyph  = G(GlyphMute);
        _batteryGlyph = G(GlyphBattery0);

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Refresh();
        _timer.Start();
        Refresh();
    }

    private void Refresh()
    {
        UpdateWifi();
        UpdateVolume();
        UpdateBattery();
    }

    private void UpdateWifi()
    {
        int? quality = WlanInterop.GetSignalQuality();
        if (quality is null)
        {
            WifiGlyph   = G(GlyphNetworkOffline);
            WifiTooltip = "Nicht verbunden";
            return;
        }

        if (quality == WlanInterop.ConnectedUnknownSignal)
        {
            // Connected, but Windows hides the signal level unless Location Services are on.
            WifiGlyph   = G(GlyphWifiFull);
            WifiTooltip = "WLAN verbunden (Signalstärke benötigt Standortdienste)";
            return;
        }

        int q = quality.Value;
        WifiGlyph = G(q switch
        {
            >= 75 => GlyphWifiFull,
            >= 50 => GlyphWifi3,
            >= 25 => GlyphWifi2,
            > 0   => GlyphWifi1,
            _     => GlyphNetworkOffline
        });
        WifiTooltip = $"WLAN: {q}%";
    }

    private void UpdateVolume()
    {
        var vol = AudioInterop.GetVolume();
        if (vol is null)
        {
            VolumeGlyph   = G(GlyphMute);
            VolumeTooltip = "Kein Audiogerät";
            return;
        }

        var (level, muted) = vol.Value;
        if (muted)
        {
            VolumeGlyph   = G(GlyphMute);
            VolumeTooltip = "Stumm";
            return;
        }

        VolumeGlyph = G(level switch
        {
            0    => GlyphVolume0,
            < 33 => GlyphVolume1,
            < 66 => GlyphVolume2,
            _    => GlyphVolume3
        });
        VolumeTooltip = $"Lautstärke: {level}%";
    }

    private void UpdateBattery()
    {
        if (!NativeMethods.GetSystemPowerStatus(out var status))
        {
            BatteryVisible = false;
            return;
        }

        // BatteryFlag bit 0x80 = no system battery (desktop) → hide.
        bool noBattery = (status.BatteryFlag & 0x80) != 0 || status.BatteryLifePercent == 255;
        if (noBattery)
        {
            BatteryVisible = false;
            return;
        }

        BatteryVisible = true;
        int percent  = Math.Clamp((int)status.BatteryLifePercent, 0, 100);
        bool charging = status.ACLineStatus == 1;

        int index    = Math.Clamp((int)Math.Round(percent / 10.0), 0, 10);
        int baseCode = charging ? GlyphBatteryCharge0 : GlyphBattery0;
        BatteryGlyph   = G(baseCode + index);
        BatteryTooltip = charging ? $"Akku: {percent}% (lädt)" : $"Akku: {percent}%";
    }

    [RelayCommand]
    private void OpenNetworkFlyout() => ShellLauncher.Open("ms-availablenetworks:");

    // Win+A opens the Windows 11 Quick Settings flyout (volume, network, battery).
    [RelayCommand]
    private void OpenSoundFlyout() => NativeMethods.SendWinShortcut(NativeMethods.VK_A);

    [RelayCommand]
    private void OpenBatteryFlyout() => NativeMethods.SendWinShortcut(NativeMethods.VK_A);

    public void Stop() => _timer.Stop();
}
