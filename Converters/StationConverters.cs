using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NetRadio.Converters;

/// <summary>取电台名首字符作为头像文字。</summary>
public sealed class StationInitialConverter : IValueConverter
{
    public static readonly StationInitialConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = (value as string)?.Trim();
        if (string.IsNullOrEmpty(name)) return "#";
        return char.ToUpperInvariant(name[0]).ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>根据电台名生成稳定的头像底色。</summary>
public sealed class StationAvatarBrushConverter : IValueConverter
{
    private static readonly Color[] Palette =
    {
        Color.Parse("#E4572E"),
        Color.Parse("#F4A259"),
        Color.Parse("#3D8BFD"),
        Color.Parse("#17BEBB"),
        Color.Parse("#7A6FF0"),
        Color.Parse("#2E9E5B"),
        Color.Parse("#D64550"),
        Color.Parse("#5C6BC0"),
        Color.Parse("#00897B"),
        Color.Parse("#C2185B")
    };

    public static readonly StationAvatarBrushConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var name = value as string ?? string.Empty;
        var hash = 0;
        foreach (var c in name)
        {
            hash = (hash * 31 + c) & 0x7FFFFFFF;
        }

        return new SolidColorBrush(Palette[hash % Palette.Length]);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>字符串非空 → 可见。</summary>
public sealed class StringNotEmptyConverter : IValueConverter
{
    public static readonly StringNotEmptyConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => !string.IsNullOrWhiteSpace(value as string);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>bool → bool 取反。</summary>
public sealed class BoolNegationConverter : IValueConverter
{
    public static readonly BoolNegationConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not true;
}
