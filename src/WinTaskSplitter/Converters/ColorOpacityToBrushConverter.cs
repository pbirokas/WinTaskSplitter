using System.Globalization;
using System.Windows.Data;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace WinTaskSplitter.Converters;

/// <summary>
/// Combines a hex color string (values[0]) with a global opacity (values[1], 0.0–1.0)
/// into a SolidColorBrush. Only the brush's Opacity is affected, so a Border using it
/// becomes see-through while its child content (icons, text) stays fully opaque.
/// </summary>
public class ColorOpacityToBrushConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var hex     = values.Length > 0 ? values[0] as string : null;
        var opacity = values.Length > 1 && values[1] is double o ? o : 1.0;

        if (hex is null) return Brushes.Transparent;

        try
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex))
            {
                Opacity = Math.Clamp(opacity, 0.0, 1.0)
            };
            return brush;
        }
        catch { return Brushes.Transparent; }
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
