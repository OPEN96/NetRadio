using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NetRadio.Models;

/// <summary>
/// 一个网络电台条目。既可以来自在线电台目录（radio-browser），也可以是用户手动添加的自定义电台。
/// </summary>
public sealed partial class RadioStation : ObservableObject
{
    public RadioStation()
    {
    }

    public RadioStation(string name, string url)
    {
        Name = name;
        Url = url;
    }

    [ObservableProperty] private string _id = Guid.NewGuid().ToString("N");
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _url = string.Empty;

    [ObservableProperty] private string? _favicon;
    [ObservableProperty] private string? _homepage;
    [ObservableProperty] private string? _tags;
    [ObservableProperty] private string? _country;
    [ObservableProperty] private string? _countryCode;
    [ObservableProperty] private string? _language;
    [ObservableProperty] private string? _codec;

    [ObservableProperty] private int _bitrate;
    [ObservableProperty] private int _votes;

    /// <summary>在线目录中的唯一 ID（radio-browser 的 stationuuid），本地电台为空。</summary>
    [ObservableProperty] private string? _stationUuid;

    /// <summary>用户手动添加的电台。</summary>
    [ObservableProperty] private bool _isCustom;

    [ObservableProperty] private bool _isFavorite;
    [ObservableProperty] private bool _isPlaying;
    [ObservableProperty] private bool _isBuffering;

    /// <summary>正在播放的曲目（ICY 元数据），每次播放时重置。</summary>
    [ObservableProperty] private string? _nowPlaying;

    /// <summary>
    /// 稳定标识：优先使用在线目录 uuid，否则使用流地址。
    /// 收藏等本地数据以该值为准，避免每次拉取列表后 ID 漂移。
    /// </summary>
    public string StableKey =>
        !string.IsNullOrWhiteSpace(StationUuid) ? "uuid:" + StationUuid : "url:" + Url;

    /// <summary>卡片上显示的副标题：国家 · 语言 · 编码 · 码率。</summary>
    public string DisplaySubtitle
    {
        get
        {
            var parts = new List<string>(4);
            if (!string.IsNullOrWhiteSpace(Country)) parts.Add(Country!);
            if (!string.IsNullOrWhiteSpace(Language)) parts.Add(Language!);
            if (!string.IsNullOrWhiteSpace(Codec) &&
                !Codec!.Equals("UNKNOWN", StringComparison.OrdinalIgnoreCase)) parts.Add(Codec);
            if (Bitrate > 0) parts.Add($"{Bitrate}k");
            return string.Join(" · ", parts);
        }
    }

    /// <summary>标签列表，最多展示 3 个。</summary>
    public string DisplayTags => string.Join(" / ", TagList);

    private string? _tagCacheSource;
    private IReadOnlyList<string> _tagCache = Array.Empty<string>();

    /// <summary>拆成独立标签，供界面渲染成一个个小胶囊。结果按 Tags 缓存，避免每次属性通知都重建集合。</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<string> TagList
    {
        get
        {
            if (ReferenceEquals(_tagCacheSource, Tags)) return _tagCache;

            _tagCacheSource = Tags;
            _tagCache = string.IsNullOrWhiteSpace(Tags)
                ? Array.Empty<string>()
                : Tags!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Where(t => t.Length > 0)
                       .Take(3)
                       .ToArray();
            return _tagCache;
        }
    }

    partial void OnTagsChanged(string? value) => OnPropertyChanged(nameof(TagList));

    public RadioStation Clone() => (RadioStation)MemberwiseClone();
}
