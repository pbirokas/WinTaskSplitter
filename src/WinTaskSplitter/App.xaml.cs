using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using ManagedShell;
using WinTaskSplitter.Services;
using WinTaskSplitter.ViewModels;
using WinTaskSplitter.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace WinTaskSplitter;

public partial class App : Application
{
    private ShellManager? _shellManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Always restore the native taskbar on any unhandled crash
        DispatcherUnhandledException          += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;

        // IMPORTANT: run at normal integrity (asInvoker — no self-elevation).
        // An elevated window cannot receive Shell_NotifyIcon (WM_COPYDATA) from normal
        // apps due to UIPI, so the embedded notification area would miss most tray icons.
        // Hiding Explorer's taskbar works fine without admin (same user session).

        var config  = new ConfigService();
        var tracker = new WindowTracker();
        var vm      = new TaskbarViewModel(config, tracker);

        _shellManager = CreateShellManager();

        // Order matters: hide Explorer's taskbar FIRST, then start our tray service.
        // The tray service broadcasts "TaskbarCreated", which makes apps re-add their
        // notification icons — they must register with us, not with Explorer's (now
        // hidden) tray, so the embedded notification area actually captures them.
        if (_shellManager is not null)
        {
            _shellManager.ExplorerHelper.HideExplorerTaskbar = true;
            _shellManager.NotificationArea.Initialize();
        }

        var window  = new TaskbarWindow(vm, tracker, _shellManager?.NotificationArea);
        MainWindow = window;
        window.Show();
    }

    private void RestoreTaskbar()
    {
        try
        {
            if (_shellManager is not null)
                _shellManager.ExplorerHelper.HideExplorerTaskbar = false;
        }
        catch { /* ignore */ }
        TaskbarHider.Restore(); // fallback when ManagedShell is unavailable
    }

    // ManagedShell powers the embedded Windows notification area (third-party tray icons).
    // Only the tray service is enabled — window tracking is handled by our own WindowTracker.
    private static ShellManager? CreateShellManager()
    {
        try
        {
            ShellConfig config         = ShellManager.DefaultShellConfig;
            config.EnableTasksService  = false;
            config.EnableTrayService   = true;
            config.AutoStartTrayService = false; // we Initialize() manually after hiding Explorer's taskbar
            return new ShellManager(config);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[WinTaskSplitter] ManagedShell init failed: {ex}");
            return null; // degrade gracefully — bar works without the embedded tray
        }
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        RestoreTaskbar();
        LogError(e.Exception);
        System.Diagnostics.Trace.TraceError(
            $"[WinTaskSplitter] Unhandled exception: {e.Exception}");
        MessageBox.Show(
            $"Unerwarteter Fehler:\n\n{e.Exception.Message}\n\nDetails wurden im Trace-Log festgehalten.",
            "WinTaskSplitter – Fehler",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        RestoreTaskbar();
        if (e.ExceptionObject is Exception ex) LogError(ex);
    }

    // Persist crash details so unexpected errors are diagnosable after the fact.
    private static void LogError(Exception ex)
    {
        try
        {
            var path = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WinTaskSplitter", "error.log");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            System.IO.File.AppendAllText(path, $"{DateTime.Now:o}\n{ex}\n\n");
        }
        catch { /* logging must never throw */ }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        RestoreTaskbar();
        // Hand the notification area back to Explorer so tray icons survive our exit.
        try { _shellManager?.Dispose(); } catch { /* ignore */ }
        base.OnExit(e);
    }
}
