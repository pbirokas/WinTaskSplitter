using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTaskSplitter.Services;

namespace WinTaskSplitter.ViewModels;

public partial class AppItemViewModel : ObservableObject
{
    public TrackedWindow Window { get; }

    [ObservableProperty] private string _title;
    [ObservableProperty] private bool   _isActive;
    [ObservableProperty] private ImageSource? _icon;

    public string ProcessName => Window.ProcessName;
    public IntPtr Handle      => Window.Handle;

    public AppItemViewModel(TrackedWindow window)
    {
        Window    = window;
        _title    = window.Title;
        _isActive = window.IsActive;
        _icon     = window.Icon;

        window.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TrackedWindow.Title))    Title    = window.Title;
            if (e.PropertyName == nameof(TrackedWindow.IsActive)) IsActive = window.IsActive;
            if (e.PropertyName == nameof(TrackedWindow.Icon))     Icon     = window.Icon;
        };
    }

    [RelayCommand]
    private void Activate() => Window.Activate();

    [RelayCommand]
    private void Close() => Window.Close();
}
