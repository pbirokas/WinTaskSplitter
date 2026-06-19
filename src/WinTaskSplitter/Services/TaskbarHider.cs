using WinTaskSplitter.Native;

namespace WinTaskSplitter.Services;

/// <summary>
/// Hides and restores the native Windows taskbar.
/// Must be restored on exit to avoid leaving the user without a taskbar.
/// </summary>
public static class TaskbarHider
{
    private static IntPtr _trayWnd    = IntPtr.Zero;
    private static IntPtr _startBtn   = IntPtr.Zero;
    private static bool   _hidden     = false;

    public static void Hide()
    {
        if (_hidden) return;

        _trayWnd  = NativeMethods.FindWindow("Shell_TrayWnd", null);
        _startBtn = NativeMethods.FindWindow("Button", null);

        if (_trayWnd  != IntPtr.Zero) NativeMethods.ShowWindow(_trayWnd,  NativeMethods.SW_HIDE);
        if (_startBtn != IntPtr.Zero) NativeMethods.ShowWindow(_startBtn, NativeMethods.SW_HIDE);

        _hidden = true;
    }

    public static void Restore()
    {
        if (!_hidden) return;

        if (_trayWnd  != IntPtr.Zero) NativeMethods.ShowWindow(_trayWnd,  NativeMethods.SW_SHOW);
        if (_startBtn != IntPtr.Zero) NativeMethods.ShowWindow(_startBtn, NativeMethods.SW_SHOW);

        _hidden = false;
    }
}
