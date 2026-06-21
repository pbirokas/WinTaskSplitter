using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using NotifyIcon = ManagedShell.WindowsTray.NotifyIcon;

namespace WinTaskSplitter.Services;

/// <summary>
/// Reads which notification icons Windows has set to "always show" (pinned to the
/// taskbar) from HKCU\Control Panel\NotifyIconSettings (the IsPromoted flag), so the
/// embedded tray can mirror the user's existing Windows configuration.
/// </summary>
public class TrayPinService
{
    private readonly HashSet<Guid>   _promotedGuids    = [];
    private readonly HashSet<string> _promotedPathUids = new(StringComparer.OrdinalIgnoreCase);

    public TrayPinService() => Load();

    public void Load()
    {
        _promotedGuids.Clear();
        _promotedPathUids.Clear();
        try
        {
            using var root = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings");
            if (root is null) return;

            foreach (var name in root.GetSubKeyNames())
            {
                using var key = root.OpenSubKey(name);
                if (key is null) continue;
                if (key.GetValue("IsPromoted") is not int promoted || promoted != 1) continue;

                if (key.GetValue("IconGuid") is string guidStr
                    && Guid.TryParse(guidStr, out var guid))
                {
                    _promotedGuids.Add(guid);
                    continue;
                }

                if (key.GetValue("ExecutablePath") is string exe && exe.Length > 0)
                {
                    exe = ExpandKnownFolder(exe);
                    int uid = key.GetValue("UID") is int u ? u : 0;
                    _promotedPathUids.Add($"{exe}|{uid}");
                }
            }
        }
        catch { /* registry unavailable — nothing pinned */ }
    }

    public bool IsPromoted(NotifyIcon icon)
    {
        if (icon.GUID != default && _promotedGuids.Contains(icon.GUID))
            return true;
        if (!string.IsNullOrEmpty(icon.Path)
            && _promotedPathUids.Contains($"{icon.Path}|{icon.UID}"))
            return true;
        return false;
    }

    // ExecutablePath may be stored as "{KnownFolderGuid}\relative\app.exe".
    private static string ExpandKnownFolder(string path)
    {
        if (path.Length > 2 && path[0] == '{')
        {
            int close = path.IndexOf('}');
            if (close > 0 && Guid.TryParse(path.AsSpan(1, close - 1), out var folderId))
            {
                var basePath = GetKnownFolderPath(folderId);
                if (!string.IsNullOrEmpty(basePath))
                    return Path.Combine(basePath, path[(close + 1)..].TrimStart('\\'));
            }
        }
        return path;
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

    private static string? GetKnownFolderPath(Guid id)
    {
        IntPtr p = IntPtr.Zero;
        try
        {
            return SHGetKnownFolderPath(id, 0, IntPtr.Zero, out p) == 0
                ? Marshal.PtrToStringUni(p)
                : null;
        }
        catch { return null; }
        finally { if (p != IntPtr.Zero) Marshal.FreeCoTaskMem(p); }
    }
}
