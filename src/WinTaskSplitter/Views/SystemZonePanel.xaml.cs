using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using ManagedShell.Common.Helpers;
using ManagedShell.WindowsTray;
using WinTaskSplitter.Native;
using WinTaskSplitter.Services;
using WinTaskSplitter.ViewModels;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using NotifyIcon = ManagedShell.WindowsTray.NotifyIcon;
using Point = System.Windows.Point;
using TrayRect = ManagedShell.Interop.NativeMethods.Rect;
using UserControl = System.Windows.Controls.UserControl;

namespace WinTaskSplitter.Views;

public partial class SystemZonePanel : UserControl
{
    private readonly DispatcherTimer _clockTimer;
    private readonly SystemStatusViewModel _status = new();
    private readonly TrayPinService _pinState = new();
    private NotificationArea? _notificationArea;
    private ICollectionView? _unpinnedView;

    public SystemZonePanel()
    {
        InitializeComponent();

        StatusPanel.DataContext = _status;

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        Unloaded += OnUnloaded;
    }

    // Binds the ManagedShell notification area to the tray icon lists.
    public void AttachTray(NotificationArea area)
    {
        _notificationArea = area;

        // Split the captured icons by the user's Windows "always show" (IsPromoted) setting:
        // promoted icons stay inline, the rest live behind the overflow chevron.
        var pinned = new CollectionViewSource { Source = area.TrayIcons };
        pinned.Filter += (_, e) =>
            e.Accepted = e.Item is NotifyIcon n && !n.IsHidden && _pinState.IsPromoted(n);
        PinnedTrayIcons.ItemsSource = pinned.View;

        var unpinned = new CollectionViewSource { Source = area.TrayIcons };
        unpinned.Filter += (_, e) =>
            e.Accepted = e.Item is NotifyIcon n && !n.IsHidden && !_pinState.IsPromoted(n);
        _unpinnedView = unpinned.View;
        UnpinnedTrayIcons.ItemsSource = _unpinnedView;

        _unpinnedView.CollectionChanged += (_, _) => UpdateOverflowVisibility();
        UpdateOverflowVisibility();
    }

    // Hide the overflow chevron when there are no hidden icons to reveal.
    private void UpdateOverflowVisibility()
        => OverflowToggle.Visibility =
            _unpinnedView is null || _unpinnedView.IsEmpty ? Visibility.Collapsed : Visibility.Visible;

    private void UpdateClock()
    {
        var now = DateTime.Now;
        ClockTime.Text = now.ToString("HH:mm:ss");
        ClockDate.Text = now.ToString("dd.MM.yyyy");
    }

    // ── Tray icon interaction (forwarded to the owning app via ManagedShell) ──
    private void TrayIcon_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not NotifyIcon icon) return;
        e.Handled = true;
        UpdatePlacement(fe, icon);
        icon.IconMouseDown(e.ChangedButton, MouseHelper.GetCursorPositionParam(),
            System.Windows.Forms.SystemInformation.DoubleClickTime);
    }

    private void TrayIcon_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not NotifyIcon icon) return;
        e.Handled = true;
        icon.IconMouseUp(e.ChangedButton, MouseHelper.GetCursorPositionParam(),
            System.Windows.Forms.SystemInformation.DoubleClickTime);
    }

    private void TrayIcon_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not NotifyIcon icon) return;
        UpdatePlacement(fe, icon);
        icon.IconMouseEnter(MouseHelper.GetCursorPositionParam());
    }

    private void TrayIcon_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not NotifyIcon icon) return;
        icon.IconMouseLeave(MouseHelper.GetCursorPositionParam());
    }

    private void TrayIcon_MouseMove(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not NotifyIcon icon) return;
        icon.IconMouseMove(MouseHelper.GetCursorPositionParam());
    }

    // Reports the icon's on-screen rectangle so the app can position its menus/flyouts
    // (used by Shell_NotifyIconGetRect).
    private static void UpdatePlacement(FrameworkElement fe, NotifyIcon icon)
    {
        try
        {
            var topLeft = fe.PointToScreen(new Point(0, 0));
            var dpi     = VisualTreeHelper.GetDpi(fe);
            icon.Placement = new TrayRect
            {
                Left   = (int)topLeft.X,
                Top    = (int)topLeft.Y,
                Right  = (int)(topLeft.X + fe.ActualWidth  * dpi.DpiScaleX),
                Bottom = (int)(topLeft.Y + fe.ActualHeight * dpi.DpiScaleY)
            };
        }
        catch { /* element not yet connected to a presentation source */ }
    }

    // ── Clock ────────────────────────────────────────────────────────────
    // Win+N opens the Windows notification/calendar flyout.
    private void Clock_Click(object sender, RoutedEventArgs e)
        => NativeMethods.SendWinShortcut(NativeMethods.VK_N);

    private void ClockNotifications_Click(object sender, RoutedEventArgs e)
        => NativeMethods.SendWinShortcut(NativeMethods.VK_N);

    private void ClockDateTime_Click(object sender, RoutedEventArgs e)
        => ShellLauncher.Open("ms-settings:dateandtime");

    private void ClockSettings_Click(object sender, RoutedEventArgs e)
    {
        if (Window.GetWindow(this) is TaskbarWindow tw)
            tw.OpenSettings();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _clockTimer.Stop();
        _status.Stop();
    }
}
