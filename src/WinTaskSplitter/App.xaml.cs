using System.Diagnostics;
using System.Security.Principal;
using System.Windows;
using System.Windows.Threading;
using WinTaskSplitter.Services;
using WinTaskSplitter.ViewModels;
using WinTaskSplitter.Views;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace WinTaskSplitter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Always restore the native taskbar on any unhandled crash
        DispatcherUnhandledException          += OnDispatcherException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;

        if (!IsRunningAsAdmin())
        {
            RestartAsAdmin();
            return;
        }

        var config  = new ConfigService();
        var tracker = new WindowTracker();
        var vm      = new TaskbarViewModel(config, tracker);
        var window  = new TaskbarWindow(vm, tracker);

        MainWindow = window;
        window.Show();
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        TaskbarHider.Restore();
        System.Diagnostics.Trace.TraceError(
            $"[WinTaskSplitter] Unhandled exception: {e.Exception}");
        MessageBox.Show(
            $"Unerwarteter Fehler:\n\n{e.Exception.Message}\n\nDetails wurden im Trace-Log festgehalten.",
            "WinTaskSplitter – Fehler",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        TaskbarHider.Restore();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TaskbarHider.Restore();
        base.OnExit(e);
    }

    private static bool IsRunningAsAdmin()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void RestartAsAdmin()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath is null)
            {
                MessageBox.Show("Pfad zur Anwendung konnte nicht ermittelt werden.",
                    "Erhöhte Rechte erforderlich", MessageBoxButton.OK, MessageBoxImage.Warning);
                Shutdown();
                return;
            }
            Process.Start(new ProcessStartInfo(exePath)
            {
                Verb            = "runas",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"WinTaskSplitter benötigt Administrator-Rechte.\n\n{ex.Message}",
                "Erhöhte Rechte erforderlich",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        Shutdown();
    }
}
