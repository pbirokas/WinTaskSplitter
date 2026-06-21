using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinTaskSplitter.Native;
using WinTaskSplitter.ViewModels;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace WinTaskSplitter.Views;

public partial class ZonePanel : UserControl
{
    private Point _dragStart;

    public ZonePanel()
    {
        InitializeComponent();
        Drop     += ZonePanel_Drop;
        DragOver += ZonePanel_DragOver;
    }

    // ── Drag & Drop ──────────────────────────────────────────────────────────

    private void AppButton_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not Button btn) return;
        if (btn.Tag is not AppItemViewModel item) return;

        var pos  = e.GetPosition(null);
        var diff = _dragStart - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        DragDrop.DoDragDrop(btn, item, DragDropEffects.Move);
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        _dragStart = e.GetPosition(null);
    }

    private void ZonePanel_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(AppItemViewModel))
            ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void ZonePanel_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(AppItemViewModel)) is not AppItemViewModel app) return;
        if (DataContext is not ZoneViewModel targetZone) return;
        GetTaskbarVm()?.MoveAppToZone(app, targetZone);
    }

    // ── Native App-System-Menü ────────────────────────────────────────────────

    private void AppButton_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not AppItemViewModel app) return;
        e.Handled = true;

        ShowNativeSystemMenu(app, btn);
    }

    private void ShowNativeSystemMenu(AppItemViewModel app, Button btn)
    {
        IntPtr hMenu = NativeMethods.GetSystemMenu(app.Handle, false);
        if (hMenu == IntPtr.Zero) return;

        // Remember the owning process before the menu blocks (HWND may be recycled afterward)
        NativeMethods.GetWindowThreadProcessId(app.Handle, out uint expectedPid);

        var screenPos = btn.PointToScreen(new Point(0, 0));
        int cmd = NativeMethods.TrackPopupMenuEx(
            hMenu,
            NativeMethods.TPM_RETURNCMD | NativeMethods.TPM_RIGHTBUTTON | NativeMethods.TPM_BOTTOMALIGN,
            (int)screenPos.X, (int)screenPos.Y,
            new System.Windows.Interop.WindowInteropHelper(
                Application.Current.MainWindow!).Handle,
            IntPtr.Zero);

        if (cmd != 0)
        {
            // Verify HWND still belongs to the same process (guards against recycled handles)
            NativeMethods.GetWindowThreadProcessId(app.Handle, out uint currentPid);
            if (currentPid != 0 && currentPid == expectedPid)
                NativeMethods.PostMessage(app.Handle, NativeMethods.WM_SYSCOMMAND,
                    new IntPtr(cmd), IntPtr.Zero);
        }
    }

    // ── Zone context menu ─────────────────────────────────────────────────────

    private void AddZone_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new InputDialog("Neue Zone", "Name der Zone:", "Neue Zone");
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.Result))
            GetTaskbarVm()?.AddZone(dlg.Result);
    }

    private void DeleteZone_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ZoneViewModel zone) return;
        if (!zone.CanDelete)
        {
            MessageBox.Show("System- und Allgemein-Zonen können nicht gelöscht werden.",
                "WinTaskSplitter", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var result = MessageBox.Show($"Zone \"{zone.Name}\" löschen?", "WinTaskSplitter",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
            GetTaskbarVm()?.DeleteZone(zone);
    }

    // ── Zone verschieben (aus Drag & Drop Context) ────────────────────────────

    private void MoveToZone_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem mi) return;
        if (mi.Tag is not AppItemViewModel app) return;

        var taskbarVm = GetTaskbarVm();
        if (taskbarVm is null) return;

        var menu = new ContextMenu();
        foreach (var zone in taskbarVm.UserZones())
        {
            var item = new MenuItem { Header = zone.Name, Tag = (app, zone) };
            item.Click += (_, _) =>
            {
                var (a, z) = ((AppItemViewModel, ZoneViewModel))item.Tag!;
                taskbarVm.MoveAppToZone(a, z);
            };
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
        => (Window.GetWindow(this) as TaskbarWindow)?.OpenSettings();

    private TaskbarViewModel? GetTaskbarVm()
        => Application.Current.MainWindow?.DataContext as TaskbarViewModel;
}
