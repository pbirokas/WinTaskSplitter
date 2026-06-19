using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using WinTaskSplitter.ViewModels;
using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;

namespace WinTaskSplitter.Views;

public partial class SettingsWindow : Window
{
    private readonly TaskbarViewModel _vm;

    public SettingsWindow(TaskbarViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        UpdateFontSizeLabel();
    }

    private void UpdateFontSizeLabel()
    {
        if (FontSizeLabel is null || FontSizeSlider is null) return;
        FontSizeLabel.Text = $"{(int)FontSizeSlider.Value}px";
    }

    private void FontSizeSlider_ValueChanged(object sender,
        RoutedPropertyChangedEventArgs<double> e)
        => UpdateFontSizeLabel();

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is ZoneViewModel zone)
            _vm.MoveZoneUp(zone);
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is ZoneViewModel zone)
            _vm.MoveZoneDown(zone);
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).Tag is not ZoneViewModel zone) return;
        if (!zone.CanDelete)
        {
            MessageBox.Show("System- und Allgemein-Zonen können nicht gelöscht werden.",
                "WinTaskSplitter", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var result = MessageBox.Show(
            $"Zone \"{zone.Name}\" wirklich löschen?\nAlle Apps dieser Zone werden in die Allgemein-Zone verschoben.",
            "Zone löschen", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
            _vm.DeleteZone(zone);
    }

    private void AddZone_Click(object sender, RoutedEventArgs e)
        => _vm.AddZone("Neue Zone");

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        _vm.SaveSettings();
        base.OnClosing(e);
    }
}
