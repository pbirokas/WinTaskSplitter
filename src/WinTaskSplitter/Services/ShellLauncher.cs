using System.Diagnostics;

namespace WinTaskSplitter.Services;

/// <summary>Opens a shell URI / settings page (e.g. "ms-availablenetworks:") via the OS handler.</summary>
public static class ShellLauncher
{
    public static void Open(string target)
    {
        try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
        catch { /* handler unavailable — ignore */ }
    }
}
