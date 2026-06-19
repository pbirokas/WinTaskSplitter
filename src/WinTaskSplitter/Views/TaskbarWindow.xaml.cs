using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WinTaskSplitter.Native;
using WinTaskSplitter.Services;
using WinTaskSplitter.ViewModels;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace WinTaskSplitter.Views;

public partial class TaskbarWindow : Window
{
    private readonly AppBarService _appBar;
    private readonly WindowTracker _tracker;
    private SettingsWindow? _settingsWindow;
    private const double BarHeight = 56;

    public TaskbarWindow(TaskbarViewModel viewModel, WindowTracker tracker)
    {
        InitializeComponent();
        DataContext = viewModel;

        _tracker = tracker;
        _appBar  = new AppBarService(this);

        Width  = SystemParameters.PrimaryScreenWidth;
        Height = BarHeight;

        viewModel.Zones.CollectionChanged += OnZonesCollectionChanged;
        foreach (var zone in viewModel.Zones)
            SubscribeZoneVisibility(zone);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;

        _appBar.Register(AppBarEdge.Bottom, BarHeight);
        _tracker.Initialize(hwnd);
        TaskbarHider.Hide();

        BuildZonesGrid();
    }

    private void OnZonesCollectionChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems is not null)
            foreach (ZoneViewModel z in e.NewItems) SubscribeZoneVisibility(z);
        if (e.OldItems is not null)
            foreach (ZoneViewModel z in e.OldItems) z.PropertyChanged -= OnZonePropertyChanged;
        BuildZonesGrid();
    }

    private void SubscribeZoneVisibility(ZoneViewModel zone)
    {
        zone.PropertyChanged -= OnZonePropertyChanged;
        zone.PropertyChanged += OnZonePropertyChanged;
    }

    private void OnZonePropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ZoneViewModel.IsVisible))
            Dispatcher.InvokeAsync(BuildZonesGrid);
    }

    // Builds the user-zones Grid with GridSplitters between each zone.
    // Invisible zones (Allgemein when empty) are skipped — they take no space.
    // Zone widths are stored as Star weights so they always fill the available space.
    public void BuildZonesGrid()
    {
        if (DataContext is not TaskbarViewModel vm) return;

        // Unsubscribe old splitters before discarding them
        foreach (var child in ZonesGrid.Children.OfType<GridSplitter>())
            child.DragCompleted -= OnSplitterDragCompleted;

        ZonesGrid.ColumnDefinitions.Clear();
        ZonesGrid.Children.Clear();

        // Only render visible zones — Allgemein is skipped when no apps are assigned
        var zones = vm.Zones.Where(z => z.IsVisible).ToList();
        for (int i = 0; i < zones.Count; i++)
        {
            var zone = zones[i];

            ZonesGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width    = new GridLength(zone.Width, GridUnitType.Star),
                MinWidth = 60
            });

            var panel = new ZonePanel { DataContext = zone };
            Grid.SetColumn(panel, i * 2);
            ZonesGrid.Children.Add(panel);

            if (i < zones.Count - 1)
            {
                ZonesGrid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(4)
                });

                var splitter = CreateZoneSplitter();
                splitter.DragCompleted += OnSplitterDragCompleted;
                Grid.SetColumn(splitter, i * 2 + 1);
                ZonesGrid.Children.Add(splitter);
            }
        }
    }

    private static GridSplitter CreateZoneSplitter()
    {
        var splitter = new GridSplitter
        {
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
            VerticalAlignment   = System.Windows.VerticalAlignment.Stretch,
            ResizeDirection     = GridResizeDirection.Columns,
            ResizeBehavior      = GridResizeBehavior.PreviousAndNext,
            Cursor              = Cursors.SizeWE,
            ShowsPreview        = false
        };

        var hoverColor = new SolidColorBrush(Color.FromArgb(0x55, 0xFF, 0xFF, 0xFF));
        var style = new Style(typeof(GridSplitter));
        style.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
        var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
        trigger.Setters.Add(new Setter(BackgroundProperty, hoverColor));
        style.Triggers.Add(trigger);
        splitter.Style = style;

        return splitter;
    }

    private void OnSplitterDragCompleted(object sender, DragCompletedEventArgs e)
    {
        if (DataContext is not TaskbarViewModel vm) return;

        var zones = vm.Zones.Where(z => z.IsVisible).ToList();
        for (int i = 0; i < zones.Count; i++)
        {
            // Star weight after drag is the new relative width
            double newWeight = ZonesGrid.ColumnDefinitions[i * 2].Width.Value;
            if (newWeight > 0)
                zones[i].Width = newWeight;
        }
        vm.SaveSettings();
    }

    public void OpenSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            _settingsWindow.Activate();
            return;
        }

        var vm = (TaskbarViewModel)DataContext;
        _settingsWindow = new SettingsWindow(vm);
        _settingsWindow.Owner = this;
        _settingsWindow.Closed += (_, _) => BuildZonesGrid();
        _settingsWindow.Show();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is TaskbarViewModel vm)
        {
            vm.Zones.CollectionChanged -= OnZonesCollectionChanged;
            foreach (var zone in vm.Zones)
                zone.PropertyChanged -= OnZonePropertyChanged;
        }
        TaskbarHider.Restore();
        _appBar.Dispose();
        _tracker.Dispose();
        base.OnClosed(e);
    }
}
