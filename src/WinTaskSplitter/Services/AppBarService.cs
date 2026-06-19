using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WinTaskSplitter.Native;

namespace WinTaskSplitter.Services;

/// <summary>
/// Positions a WPF window at a screen edge.
/// Uses SetWindowPos instead of the AppBar API to avoid conflicts with
/// the native Windows taskbar which stays registered in the system even when hidden.
/// The AppBar API (SHAppBarMessage) will be re-introduced once we properly
/// remove the native taskbar from the shell AppBar list.
/// </summary>
public class AppBarService : IDisposable
{
    private readonly Window _window;
    private IntPtr    _hWnd;
    private AppBarEdge _currentEdge      = AppBarEdge.Bottom;
    private double     _currentThickness = 56;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int x, int y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    public AppBarService(Window window)
    {
        _window = window;
    }

    public void Register(AppBarEdge edge, double thickness)
    {
        _hWnd             = new WindowInteropHelper(_window).Handle;
        _currentEdge      = edge;
        _currentThickness = thickness;
        SetPosition(edge, thickness);
    }

    public void SetPosition(AppBarEdge edge, double thickness)
    {
        _currentEdge      = edge;
        _currentThickness = thickness;

        uint rawDpi = NativeMethods.GetDpiForWindow(_hWnd);
        double dpi  = rawDpi > 0 ? rawDpi / 96.0 : 1.0;

        // SystemParameters are always in WPF logical units
        double screenW = SystemParameters.PrimaryScreenWidth;
        double screenH = SystemParameters.PrimaryScreenHeight;

        // Compute WPF window bounds (logical units)
        double winX, winY, winW, winH;
        switch (edge)
        {
            case AppBarEdge.Bottom:
                winX = 0; winY = screenH - thickness;
                winW = screenW; winH = thickness;
                break;
            case AppBarEdge.Top:
                winX = 0; winY = 0;
                winW = screenW; winH = thickness;
                break;
            case AppBarEdge.Left:
                winX = 0; winY = 0;
                winW = thickness; winH = screenH;
                break;
            case AppBarEdge.Right:
                winX = screenW - thickness; winY = 0;
                winW = thickness; winH = screenH;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(edge));
        }

        // Apply to WPF window (logical units)
        _window.Left   = winX;
        _window.Top    = winY;
        _window.Width  = winW;
        _window.Height = winH;

        // Also pin via SetWindowPos in physical pixels to guarantee position
        int px = (int)(winX * dpi);
        int py = (int)(winY * dpi);
        int pw = (int)(winW * dpi);
        int ph = (int)(winH * dpi);

        SetWindowPos(_hWnd, HWND_TOPMOST, px, py, pw, ph,
                     SWP_NOACTIVATE | SWP_SHOWWINDOW);
    }

    public void Unregister() { /* AppBar API not used yet */ }

    public void Dispose() => Unregister();
}

public enum AppBarEdge
{
    Left   = NativeMethods.ABE_LEFT,
    Top    = NativeMethods.ABE_TOP,
    Right  = NativeMethods.ABE_RIGHT,
    Bottom = NativeMethods.ABE_BOTTOM
}
