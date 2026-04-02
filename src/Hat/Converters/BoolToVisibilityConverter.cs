using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Hat.Converters;

/// <summary>
/// Converts bool to Visibility. Supports inversion via ConverterParameter="invert".
/// Also supports count-based: ConverterParameter="empty" shows when count is 0.
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var param = parameter as string ?? "";

        if (param == "empty")
        {
            // Show when collection count is 0
            if (value is int count)
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        var boolValue = value is bool b && b;
        var invert = param == "invert";

        if (invert) boolValue = !boolValue;

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
