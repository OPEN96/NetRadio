using Avalonia.Media;
using NetRadio.Services;

namespace NetRadio.Models;

public enum LocalSource
{
    None,
    Favorites,
    Custom
}

/// <summary>左侧栏的一个分类。</summary>
public sealed class CategoryItem
{
    public required string Name { get; init; }

    /// <summary>图标路径数据（SVG path）。</summary>
    public string? IconPath { get; init; }

    /// <summary>在线查询条件；本地分类为 null。</summary>
    public StationQuery? Query { get; init; }

    public LocalSource Local { get; init; } = LocalSource.None;

    public string? CacheKey => Local != LocalSource.None ? Local.ToString() : (Query?.ToString() ?? Name);

    public Geometry? Icon => IconPath is null ? null : Geometry.Parse(IconPath);
}
