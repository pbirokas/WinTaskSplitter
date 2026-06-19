using System.Windows;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using WinTaskSplitter.ViewModels;

namespace WinTaskSplitter.Views;

public partial class ZoneEditDialog : Window
{
    private readonly ZoneViewModel _zone;

    public ZoneEditDialog(ZoneViewModel zone)
    {
        InitializeComponent();
        _zone = zone;

        NameBox.Text           = zone.Name;
        ShowLabelBox.IsChecked = zone.ShowLabel;
        BgColorBox.Text        = zone.BackgroundColor;
        BorderColorBox.Text    = zone.BorderColor;
        WidthBox.Text          = ((int)zone.Width).ToString();

        UpdatePreview(BgColorBox.Text, BgPreview);
        UpdatePreview(BorderColorBox.Text, BorderPreview);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        _zone.Name            = NameBox.Text;
        _zone.ShowLabel       = ShowLabelBox.IsChecked ?? true;
        _zone.BackgroundColor = BgColorBox.Text;
        _zone.BorderColor     = BorderColorBox.Text;

        if (double.TryParse(WidthBox.Text, out double w) && w >= 20)
            _zone.Width = w;

        _zone.SyncToModel();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    private void ColorBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdatePreview(BgColorBox.Text, BgPreview);
        UpdatePreview(BorderColorBox.Text, BorderPreview);
    }

    private static void UpdatePreview(string hex, System.Windows.Controls.Border? preview)
    {
        if (preview is null) return;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            preview.Background = new SolidColorBrush(color);
        }
        catch
        {
            preview.Background = Brushes.Transparent;
        }
    }
}
