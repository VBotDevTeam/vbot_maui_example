using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace VBotMauiApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? Color.FromArgb("#43A047") : Color.FromArgb("#757575");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => false;
}

public class BoolToStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "Đã kết nối" : "Chưa kết nối";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => false;
}

public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b ? !b : true;
}

public class StringNotEmptyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => false;
}

public class BoolToMuteTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "Bật mic" : "Tắt mic";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => false;
}

public class BoolToMuteColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? Color.FromArgb("#E65100") : Color.FromArgb("#757575");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => false;
}

public class BoolToSpeakerTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? "Loa ngoài" : "Loa trong";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => false;
}

public class BoolToSpeakerColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        (value is bool b && b) ? Color.FromArgb("#1E88E5") : Color.FromArgb("#757575");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => false;
}
