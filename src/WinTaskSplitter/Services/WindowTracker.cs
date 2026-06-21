using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinTaskSplitter.Native;

namespace WinTaskSplitter.Services;

public class WindowTracker : IDisposable
{
    public ObservableCollection<TrackedWindow> Windows { get; } = [];

    private IntPtr  _hookWnd;
    private uint    _shellHookMsg;
    private HwndSource? _source;
    private DispatcherTimer? _reconcileTimer;

    private const int HSHELL_WINDOWCREATED   = 1;
    private const int HSHELL_WINDOWDESTROYED = 2;
    private const int HSHELL_WINDOWACTIVATED = 4;
    private const int HSHELL_REDRAW          = 6;
    private const int HSHELL_FLASH           = 8;

    public void Initialize(IntPtr hostHwnd)
    {
        _hookWnd      = hostHwnd;
        _shellHookMsg = NativeMethods.RegisterWindowMessage("SHELLHOOK");

        NativeMethods.RegisterShellHookWindow(_hookWnd);

        _source = HwndSource.FromHwnd(_hookWnd);
        _source?.AddHook(WndProc);

        Reconcile(); // initial population (prune is a no-op on the empty collection)

        // Safety net: shell-hook events can be missed and some windows (cloaked UWP hosts)
        // never raise WINDOWDESTROYED. Periodically reconcile against the real window set.
        _reconcileTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _reconcileTimer.Tick += (_, _) => Reconcile();
        _reconcileTimer.Start();
    }

    // Runs on the UI thread (DispatcherTimer): add windows we missed, drop ghosts.
    private void Reconcile()
    {
        var valid = new HashSet<IntPtr>();
        NativeMethods.EnumWindows((h, _) =>
        {
            if (IsTaskbarWindow(h)) valid.Add(h);
            return true;
        }, IntPtr.Zero);

        for (int i = Windows.Count - 1; i >= 0; i--)
            if (!valid.Contains(Windows[i].Handle))
                Windows.RemoveAt(i);

        foreach (var h in valid)
            AddWindow(h); // dedups internally
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == _shellHookMsg)
        {
            int code = wParam.ToInt32() & 0x7FFF;
            IntPtr wn = lParam;
            switch (code)
            {
                case HSHELL_WINDOWCREATED:
                    if (IsTaskbarWindow(wn)) AddWindow(wn);
                    break;
                case HSHELL_WINDOWDESTROYED:
                    RemoveWindow(wn);
                    break;
                case HSHELL_REDRAW:
                case HSHELL_FLASH:
                    RefreshWindow(wn);
                    break;
                case HSHELL_WINDOWACTIVATED:
                    MarkActive(wn);
                    break;
            }
        }
        return IntPtr.Zero;
    }

    private static bool IsTaskbarWindow(IntPtr hWnd)
    {
        if (!NativeMethods.IsWindowVisible(hWnd)) return false;
        if (NativeMethods.GetWindow(hWnd, NativeMethods.GW_OWNER) != IntPtr.Zero) return false;

        int exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
        if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0) return false;

        // Cloaked windows are invisible UWP/host windows (e.g. "Windows-Eingabeerfahrung",
        // "Windows Shell Experience Host") — locale-independent ghost filter.
        if (IsCloaked(hWnd)) return false;

        var title = GetTitle(hWnd);
        if (string.IsNullOrWhiteSpace(title)) return false;
        if (title is "Program Manager") return false;

        return true;
    }

    private static bool IsCloaked(IntPtr hWnd)
        => NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWA_CLOAKED, out int cloaked, sizeof(int)) == 0
           && cloaked != 0;

    private void AddWindow(IntPtr hWnd)
    {
        if (Windows.Any(w => w.Handle == hWnd)) return;
        var tracked = TrackedWindow.FromHandle(hWnd);
        if (tracked is not null)
            System.Windows.Application.Current.Dispatcher.Invoke(() => Windows.Add(tracked));
    }

    private void RemoveWindow(IntPtr hWnd)
    {
        var existing = Windows.FirstOrDefault(w => w.Handle == hWnd);
        if (existing is not null)
            System.Windows.Application.Current.Dispatcher.Invoke(() => Windows.Remove(existing));
    }

    private void RefreshWindow(IntPtr hWnd)
    {
        var existing = Windows.FirstOrDefault(w => w.Handle == hWnd);
        if (existing is not null) existing.Refresh();
        else if (IsTaskbarWindow(hWnd)) AddWindow(hWnd);
    }

    private void MarkActive(IntPtr hWnd)
    {
        foreach (var w in Windows) w.IsActive = w.Handle == hWnd;
    }

    private static string GetTitle(IntPtr hWnd)
    {
        var sb = new StringBuilder(256);
        NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public void Dispose()
    {
        _reconcileTimer?.Stop();
        if (_hookWnd != IntPtr.Zero)
            NativeMethods.DeregisterShellHookWindow(_hookWnd);
        _source?.RemoveHook(WndProc);
    }
}

public class TrackedWindow : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    public IntPtr  Handle      { get; }
    public string  ProcessName { get; }
    public string  ExePath     { get; }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    private ImageSource? _icon;
    public ImageSource? Icon
    {
        get => _icon;
        private set => SetProperty(ref _icon, value);
    }

    private TrackedWindow(IntPtr hWnd, string title, string processName, string exePath)
    {
        Handle      = hWnd;
        _title      = title;
        ProcessName = processName;
        ExePath     = exePath;
    }

    public static TrackedWindow? FromHandle(IntPtr hWnd)
    {
        try
        {
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
            var proc  = Process.GetProcessById((int)pid);
            var sb    = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, sb, sb.Capacity);

            string exePath = string.Empty;
            try { exePath = proc.MainModule?.FileName ?? string.Empty; } catch { }

            var tracked = new TrackedWindow(hWnd, sb.ToString(), proc.ProcessName, exePath);
            tracked.LoadIconAsync();
            return tracked;
        }
        catch { return null; }
    }

    private void LoadIconAsync()
    {
        Task.Run(() =>
        {
            try
            {
                // Priority 1: ask the window itself (works for Win32 + UWP + packaged apps)
                IntPtr hIcon = NativeMethods.SendMessage(Handle, NativeMethods.WM_GETICON,
                                   NativeMethods.ICON_BIG, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.SendMessage(Handle, NativeMethods.WM_GETICON,
                                NativeMethods.ICON_SMALL2, IntPtr.Zero);
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.SendMessage(Handle, NativeMethods.WM_GETICON,
                                NativeMethods.ICON_SMALL, IntPtr.Zero);

                // Priority 2: class icon registered for the window class
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.GetClassLongPtrSafe(Handle, NativeMethods.GCLP_HICON);
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.GetClassLongPtrSafe(Handle, NativeMethods.GCLP_HICONSM);

                // Priority 3: SHGetFileInfo from exe path (requires MainModule access)
                bool iconFromShell = false;
                if (hIcon == IntPtr.Zero && !string.IsNullOrEmpty(ExePath))
                {
                    var shfi = new NativeMethods.SHFILEINFO();
                    NativeMethods.SHGetFileInfo(ExePath, 0, ref shfi,
                        (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.SHFILEINFO>(),
                        NativeMethods.SHGFI_ICON | NativeMethods.SHGFI_LARGEICON);
                    hIcon       = shfi.hIcon;
                    iconFromShell = hIcon != IntPtr.Zero;
                }

                if (hIcon == IntPtr.Zero) return;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        var src = Imaging.CreateBitmapSourceFromHIcon(
                            hIcon,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(32, 32));
                        src.Freeze();
                        Icon = src;
                    }
                    finally
                    {
                        // Only destroy icons we obtained via SHGetFileInfo or WM_GETICON copies;
                        // class icons are owned by the window class and must NOT be destroyed.
                        if (iconFromShell)
                            NativeMethods.DestroyIcon(hIcon);
                    }
                });
            }
            catch { }
        });
    }

    public void Refresh()
    {
        var sb = new StringBuilder(256);
        NativeMethods.GetWindowText(Handle, sb, sb.Capacity);
        Title = sb.ToString();
    }

    public void Activate()
    {
        if (NativeMethods.IsIconic(Handle))
            NativeMethods.ShowWindowAsync(Handle, 9); // SW_RESTORE
        NativeMethods.SetForegroundWindow(Handle);
    }

    public void Close()
    {
        const uint WM_CLOSE = 0x0010;
        NativeMethods.PostMessage(Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }
}
