using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaloniaApplication1.Converters;

/// <summary>
/// Возвращает цвет фона карточки товара:
/// - Синий, если товара нет в наличии (StockQuantity == 0)
/// - Зеленый, если скидка > 15%
/// - Белый по умолчанию
/// </summary>
public class ProductCardBackgroundConverter : IMultiValueConverter
{
    public static readonly ProductCardBackgroundConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values == null || values.Count < 2)
            return new SolidColorBrush(Colors.White);

        var stockQuantity = values[0] is int stock ? stock : 0;
        var discount = values[1] is decimal disc ? disc : (decimal?)null;

        // Приоритет: сначала проверяем наличие товара
        if (stockQuantity <= 0)
        {
            // Синий фон, когда товара нет в наличии
            return new SolidColorBrush(Color.Parse("#ADD8E6")); // LightBlue
        }

        // Зеленый фон, когда скидка больше 15%
        if (discount.HasValue && discount.Value > 15)
        {
            return new SolidColorBrush(Color.Parse("#90EE90")); // LightGreen
        }

        // Белый фон по умолчанию
        return new SolidColorBrush(Colors.White);
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

