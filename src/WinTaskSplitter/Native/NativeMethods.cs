using System.Runtime.InteropServices;

namespace WinTaskSplitter.Native;

internal static class NativeMethods
{
    // AppBar messages
    public const int ABM_NEW            = 0x00000000;
    public const int ABM_REMOVE         = 0x00000001;
    public const int ABM_QUERYPOS       = 0x00000002;
    public const int ABM_SETPOS         = 0x00000003;
    public const int ABM_GETSTATE       = 0x00000004;
    public const int ABM_SETAUTOHIDEBAR = 0x00000008;
    public const int ABE_LEFT   = 0;
    public const int ABE_TOP    = 1;
    public const int ABE_RIGHT  = 2;
    public const int ABE_BOTTOM = 3;

    // AppBar notification
    public const int ABN_STATECHANGE    = 0;
    public const int ABN_POSCHANGED     = 1;
    public const int WM_USER            = 0x0400;
    public const int WM_APPBARNOTIFY    = WM_USER + 100;

    // ShowWindow
    public const int SW_HIDE  = 0;
    public const int SW_SHOW  = 5;

    // Window styles
    public const int GWL_EXSTYLE        = -20;
    public const int WS_EX_TOOLWINDOW   = 0x00000080;
    public const int WS_EX_APPWINDOW    = 0x00040000;
    public const int WS_EX_NOACTIVATE   = 0x08000000;

    // GetWindow
    public const int GW_OWNER = 4;

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int    cbSize;
        public IntPtr hWnd;
        public uint   uCallbackMessage;
        public uint   uEdge;
        public RECT   rc;
        public IntPtr lParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TITLEBARINFO
    {
        public uint cbSize;
        public RECT rcTitleBar;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public uint[] rgstate;
    }

    [DllImport("shell32.dll", SetLastError = true)]
    public static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern bool GetTitleBarInfo(IntPtr hwnd, ref TITLEBARINFO pti);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool RegisterShellHookWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool DeregisterShellHookWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern uint RegisterWindowMessage(string lpString);

    // Keyboard input for Start Menu
    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public const byte VK_LWIN      = 0x5B;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public const uint WM_GETICON  = 0x007F;
    public static readonly IntPtr ICON_SMALL  = new(0);
    public static readonly IntPtr ICON_BIG    = new(1);
    public static readonly IntPtr ICON_SMALL2 = new(2);

    [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
    private static extern IntPtr GetClassLongPtr64(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll", EntryPoint = "GetClassLongW")]
    private static extern uint GetClassLong32(IntPtr hWnd, int nIndex);

    public const int GCLP_HICON   = -14;
    public const int GCLP_HICONSM = -34;

    public static IntPtr GetClassLongPtrSafe(IntPtr hWnd, int nIndex)
    {
        try
        {
            return Environment.Is64BitProcess
                ? GetClassLongPtr64(hWnd, nIndex)
                : new IntPtr(GetClassLong32(hWnd, nIndex));
        }
        catch { return IntPtr.Zero; }
    }

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int    iIcon;
        public uint   dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    public const uint SHGFI_ICON       = 0x100;
    public const uint SHGFI_SMALLICON  = 0x001;
    public const uint SHGFI_LARGEICON  = 0x000;
    public const uint SHGFI_USEFILEATTRIBUTES = 0x010;

    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SHGetFileInfo(
        string pszPath, uint dwFileAttributes,
        ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    public static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags, int x, int y, IntPtr hwnd, IntPtr lptpm);

    public const uint TPM_RETURNCMD   = 0x0100;
    public const uint TPM_RIGHTBUTTON = 0x0002;
    public const uint TPM_BOTTOMALIGN = 0x0020;
    public const uint WM_SYSCOMMAND   = 0x0112;

    // DWM thumbnail
    [DllImport("dwmapi.dll")]
    public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr phThumbnailId);

    [DllImport("dwmapi.dll")]
    public static extern int DwmUnregisterThumbnail(IntPtr hThumbnailId);

    [DllImport("dwmapi.dll")]
    public static extern int DwmUpdateThumbnailProperties(IntPtr hThumbnailId, ref DWM_THUMBNAIL_PROPERTIES ptnProperties);

    [StructLayout(LayoutKind.Sequential)]
    public struct DWM_THUMBNAIL_PROPERTIES
    {
        public uint dwFlags;
        public RECT rcDestination;
        public RECT rcSource;
        public byte opacity;
        public bool fVisible;
        public bool fSourceClientAreaOnly;
    }

    public const uint DWM_TNP_RECTDESTINATION   = 0x00000001;
    public const uint DWM_TNP_RECTSOURCE        = 0x00000002;
    public const uint DWM_TNP_OPACITY           = 0x00000004;
    public const uint DWM_TNP_VISIBLE           = 0x00000008;
    public const uint DWM_TNP_SOURCECLIENTAREAONLY = 0x00000010;
}
