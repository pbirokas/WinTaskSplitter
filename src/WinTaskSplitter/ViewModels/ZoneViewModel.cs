using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTaskSplitter.Models;

namespace WinTaskSplitter.ViewModels;

public partial class ZoneViewModel : ObservableObject
{
    public Zone Model { get; }

    public Guid   Id        => Model.Id;
    public bool   IsSystem  => Model.IsSystem;
    public bool   IsGeneral => Model.IsGeneral;
    public bool   CanDelete => !IsSystem && !IsGeneral;

    [ObservableProperty] private string _name;
    [ObservableProperty] private bool   _showLabel;
    [ObservableProperty] private string _backgroundColor;
    [ObservableProperty] private string _borderColor;
    [ObservableProperty] private double _borderThickness;
    [ObservableProperty] private double _width;

    [ObservableProperty] private bool _showStartButton;
    [ObservableProperty] private ObservableCollection<AppItemViewModel> _apps = [];

    // A zone with the Start button is always visible, even when empty.
    public bool IsVisible => !IsGeneral || ShowStartButton || Apps.Count > 0;
    private bool _isVisibleCache;

    public ZoneViewModel(Zone model)
    {
        Model            = model;
        _name            = model.Name;
        _showLabel       = model.ShowLabel;
        _backgroundColor = model.BackgroundColor;
        _borderColor     = model.BorderColor;
        _borderThickness = model.BorderThickness;
        _width           = model.Width;

        _isVisibleCache = IsVisible;
        // Only notify when IsVisible actually flips — otherwise routine app churn
        // (windows opening/closing, tracker reconcile) would trigger a full grid
        // rebuild and reset zone widths mid-resize.
        Apps.CollectionChanged += (_, _) => RaiseIsVisibleIfChanged();
    }

    partial void OnShowStartButtonChanged(bool value) => RaiseIsVisibleIfChanged();

    private void RaiseIsVisibleIfChanged()
    {
        bool visible = IsVisible;
        if (visible == _isVisibleCache) return;
        _isVisibleCache = visible;
        OnPropertyChanged(nameof(IsVisible));
    }

    public void AddApp(AppItemViewModel app)
    {
        if (!Apps.Contains(app))
            Apps.Add(app);
    }

    public void RemoveApp(AppItemViewModel app) => Apps.Remove(app);

    public void SyncToModel()
    {
        Model.Name            = Name;
        Model.ShowLabel       = ShowLabel;
        Model.BackgroundColor = BackgroundColor;
        Model.BorderColor     = BorderColor;
        Model.BorderThickness = BorderThickness;
        Model.Width           = Width;
    }
}
