using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaloniaApplication1.Converters;

/// <summary>
/// Возвращает цвет текста в зависимости от остатка на складе.
///  - Синий, если товара нет в наличии (кол-во == 0)
///  - Обычный черный цвет во всех остальных случаях
/// </summary>
public class StockToColorConverter : IValueConverter
{
    public static readonly StockToColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int stock && stock <= 0)
        {
            // Синим цветом, когда товара нет в наличии
            return new SolidColorBrush(Color.Parse("#0000FF"));
        }

        // Обычный черный цвет по умолчанию
        return new SolidColorBrush(Colors.Black);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}


