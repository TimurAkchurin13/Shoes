using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaloniaApplication1.Converters;

public class CountToVisibilityConverter : IMultiValueConverter
{
    public static readonly CountToVisibilityConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2)
        {
            var orderCount = values[0] as int? ?? 0;
            var isLoading = values[1] as bool? ?? false;
            
            // Показывать сообщение если не загружается И count == 0
            return !isLoading && orderCount == 0;
        }
        
        if (values.Count >= 1 && values[0] is int singleCount)
        {
            return singleCount == 0;
        }
        
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

