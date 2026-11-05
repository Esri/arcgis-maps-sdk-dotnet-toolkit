using System;
using Microsoft.UI.Xaml.Data;

namespace Toolkit.UITests.App.TestPages;

public sealed class BookmarkTemplateNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return $"{parameter}: {value}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotSupportedException();
    }
}