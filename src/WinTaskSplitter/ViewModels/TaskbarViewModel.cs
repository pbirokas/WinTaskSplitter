using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinTaskSplitter.Models;
using WinTaskSplitter.Native;
using WinTaskSplitter.Services;
using Application = System.Windows.Application;

namespace WinTaskSplitter.ViewModels;

public partial class TaskbarViewModel : ObservableObject
{
    private readonly ConfigService _config;
    private readonly WindowTracker _tracker;
    private          AppSettings   _settings;

    public ObservableCollection<ZoneViewModel> Zones { get; } = [];

    [ObservableProperty] private ZoneViewModel? _systemZone;
    [ObservableProperty] private ZoneViewModel? _generalZone;
    [ObservableProperty] private double         _labelFontSize;
    [ObservableProperty] private double         _backgroundOpacity;
    [ObservableProperty] private double         _splitterWidth;
    [ObservableProperty] private ZoneViewModel? _startButtonZone;

    public TaskbarViewModel(ConfigService config, WindowTracker tracker)
    {
        _config   = config;
        _tracker  = tracker;
        _settings = _config.Load();

        _labelFontSize     = _settings.LabelFontSize;
        _backgroundOpacity = _settings.BackgroundOpacity;
        _splitterWidth     = _settings.SplitterWidth;

        BuildZones();
        InitStartButtonZone();
        SubscribeToTracker();
    }

    partial void OnLabelFontSizeChanged(double value)
        => _settings.LabelFontSize = value;

    partial void OnBackgroundOpacityChanged(double value)
        => _settings.BackgroundOpacity = value;

    partial void OnSplitterWidthChanged(double value)
        => _settings.SplitterWidth = value;

    partial void OnStartButtonZoneChanged(ZoneViewModel? oldValue, ZoneViewModel? newValue)
    {
        if (oldValue is not null) oldValue.ShowStartButton = false;
        if (newValue is not null) newValue.ShowStartButton = true;
        _settings.StartButtonZoneId = newValue?.Id ?? Guid.Empty;
    }

    private void InitStartButtonZone()
    {
        var target = Zones.FirstOrDefault(z => z.Id == _settings.StartButtonZoneId)
                  ?? GeneralZone
                  ?? Zones.FirstOrDefault();

        StartButtonZone = target;
    }

    private void BuildZones()
    {
        Zones.Clear();
        foreach (var zone in _settings.Zones.OrderBy(z => z.Order))
        {
            var vm = new ZoneViewModel(zone);
            if (zone.IsSystem) { SystemZone = vm; continue; }
            Zones.Add(vm);
            if (zone.IsGeneral) GeneralZone = vm;
        }
    }

    private void SubscribeToTracker()
    {
        foreach (var win in _tracker.Windows)
            OnWindowAdded(win);

        // CollectionChanged can fire on a non-UI thread (ShellHook messages).
        // Marshal to the UI thread before touching ObservableCollections.
        _tracker.Windows.CollectionChanged += (_, e) =>
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (e.NewItems is not null)
                    foreach (TrackedWindow w in e.NewItems) OnWindowAdded(w);
                if (e.OldItems is not null)
                    foreach (TrackedWindow w in e.OldItems) OnWindowRemoved(w);
            });
    }

    private void OnWindowAdded(TrackedWindow win)
    {
        var item = new AppItemViewModel(win);
        var targetZone = FindZoneForProcess(win.ProcessName);
        if (targetZone is null) return;
        targetZone.AddApp(item);
    }

    private void OnWindowRemoved(TrackedWindow win)
    {
        foreach (var zone in AllZones())
        {
            var item = zone.Apps.FirstOrDefault(a => a.Handle == win.Handle);
            if (item is not null) { zone.RemoveApp(item); return; }
        }
    }

    private ZoneViewModel? FindZoneForProcess(string processName)
    {
        var assignment = FindAssignment(processName);
        if (assignment is not null)
        {
            var zone = AllZones().FirstOrDefault(z => z.Id == assignment.ZoneId);
            if (zone is not null) return zone;
        }
        return GeneralZone ?? Zones.FirstOrDefault();
    }

    private AppAssignment? FindAssignment(string processName)
        => _settings.Assignments.FirstOrDefault(
            a => string.Equals(a.ProcessName, processName, StringComparison.OrdinalIgnoreCase));

    public void MoveAppToZone(AppItemViewModel app, ZoneViewModel targetZone)
    {
        foreach (var zone in AllZones()) zone.RemoveApp(app);
        targetZone.AddApp(app);

        var existing = FindAssignment(app.ProcessName);
        if (existing is not null) existing.ZoneId = targetZone.Id;
        else _settings.Assignments.Add(new AppAssignment { ProcessName = app.ProcessName, ZoneId = targetZone.Id });

        _config.Save(_settings);
    }

    public void AddZone(string name)
    {
        var zone = new Zone
        {
            Name  = name,
            Width = 200,
            Order = _settings.Zones.Count > 0 ? _settings.Zones.Max(z => z.Order) + 1 : 0
        };
        _settings.Zones.Add(zone);
        Zones.Add(new ZoneViewModel(zone));
        SaveSettings();
    }

    public void DeleteZone(ZoneViewModel vm)
    {
        if (GeneralZone is not null)
            foreach (var app in vm.Apps.ToList())
                MoveAppToZone(app, GeneralZone);

        _settings.Zones.Remove(vm.Model);
        Zones.Remove(vm);
        SaveSettings();
    }

    public void MoveZoneUp(ZoneViewModel vm)
    {
        int idx = Zones.IndexOf(vm);
        if (idx <= 0) return;
        Zones.Move(idx, idx - 1);
        UpdateZoneOrder();
        SaveSettings();
    }

    public void MoveZoneDown(ZoneViewModel vm)
    {
        int idx = Zones.IndexOf(vm);
        if (idx < 0 || idx >= Zones.Count - 1) return;
        Zones.Move(idx, idx + 1);
        UpdateZoneOrder();
        SaveSettings();
    }

    private void UpdateZoneOrder()
    {
        for (int i = 0; i < Zones.Count; i++)
            Zones[i].Model.Order = i;
    }

    public void SaveSettings()
    {
        foreach (var zone in AllZones())
            zone.SyncToModel();
        _settings.LabelFontSize     = LabelFontSize;
        _settings.BackgroundOpacity = BackgroundOpacity;
        _settings.SplitterWidth     = SplitterWidth;
        _config.Save(_settings);
    }

    // Returns only user-configurable zones (excludes System zone).
    public IEnumerable<ZoneViewModel> UserZones() => Zones;

    private IEnumerable<ZoneViewModel> AllZones()
    {
        foreach (var z in Zones) yield return z; // includes GeneralZone
        if (SystemZone is not null) yield return SystemZone;
    }

    [RelayCommand]
    private void OpenStartMenu()
    {
        NativeMethods.keybd_event(NativeMethods.VK_LWIN, 0, 0, UIntPtr.Zero);
        NativeMethods.keybd_event(NativeMethods.VK_LWIN, 0, NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
    }
}
