using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace NetRadio.Converters;

/// <summary>把布尔状态映射为图标几何体，避免在 XAML 里写复杂触发器。</summary>
public abstract class BoolGeometryConverter : IValueConverter
{
    private readonly Lazy<Geometry> _true;
    private readonly Lazy<Geometry> _false;

    protected BoolGeometryConverter(string truePath, string falsePath)
    {
        _true = new Lazy<Geometry>(() => Geometry.Parse(truePath));
        _false = new Lazy<Geometry>(() => Geometry.Parse(falsePath));
    }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? _true.Value : _false.Value;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class PlayPauseIconConverter : BoolGeometryConverter
{
    public PlayPauseIconConverter()
        : base("M14,19H18V5H14V19M6,19H10V5H6V19Z", "M8,5V19L19,12L8,5Z")
    {
    }
}

public sealed class VolumeIconConverter : BoolGeometryConverter
{
    public VolumeIconConverter()
        : base(
            "M12,4L9.91,6.09L12,8.18M4.27,3L3,4.27L7.73,9H3V15H7L12,20V13.27L16.25,17.53C15.58,18.04 14.83,18.46 14,18.7V20.77C15.38,20.45 16.63,19.82 17.68,18.96L19.73,21L21,19.73L12,10.73M19,12C19,12.94 18.8,13.82 18.46,14.64L19.97,16.15C20.62,14.91 21,13.5 21,12C21,7.72 18.01,4.14 14,3.23V5.29C16.89,6.15 19,8.83 19,12M16.5,12C16.5,10.23 15.5,8.71 14,7.97V10.18L16.45,12.63C16.5,12.43 16.5,12.21 16.5,12Z",
            "M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 16.89,17.85 14,18.71V20.77C18.01,19.86 21,16.28 21,12C21,7.72 18.01,4.14 14,3.23M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16C15.5,15.29 16.5,13.76 16.5,12M3,9V15H7L12,20V4L7,9H3Z")
    {
    }
}

public sealed class RecordIconConverter : BoolGeometryConverter
{
    public RecordIconConverter()
        : base("M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M9,9H15V15H9V9Z",
               "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z")
    {
    }
}

public sealed class FavoriteIconConverter : BoolGeometryConverter
{
    public FavoriteIconConverter()
        : base(
            "M12,17.27L18.18,21l-1.64-7.03L22,9.24l-7.19-.61L12,2 9.19,8.63 2,9.24l5.46,4.73L5.82,21z",
            "M22,9.24l-7.19-.62L12,2 9.19,8.63 2,9.24l5.46,4.73L5.82,21 12,17.27 18.18,21l-1.63-7.03L22,9.24M12,15.4l-3.76,2.27 1-4.28-3.32-2.88 4.38-.38L12,6.1l1.71,4.04 4.38.38-3.32,2.88 1,4.28L12,15.4z")
    {
    }
}
