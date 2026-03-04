using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaloniaApplication1.Converters;

public class DiscountToColorConverter : IValueConverter
{
    public static readonly DiscountToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal discount)
        {
            // Ярко-зеленый цвет, когда скидка больше 15%
            if (discount > 15)
                return new SolidColorBrush(Color.Parse("#008000"));
        }

        // Обычный черный цвет для остальных случаев
        return new SolidColorBrush(Colors.Black);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

