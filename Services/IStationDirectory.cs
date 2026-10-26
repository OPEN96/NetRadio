using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetRadio.Models;

namespace NetRadio.Services;

/// <summary>在线电台目录查询条件。</summary>
public sealed record StationQuery(
    string? Keyword = null,
    string? CountryCode = null,
    string? Tag = null,
    int Limit = 120)
{
    public override string ToString() => $"{Keyword}|{CountryCode}|{Tag}|{Limit}";
}

public interface IStationDirectory
{
    Task<IReadOnlyList<RadioStation>> SearchAsync(StationQuery query, CancellationToken ct = default);

    /// <summary>统计一次播放点击，帮助目录维护电台热度（失败静默）。</summary>
    Task RegisterPlayAsync(string? stationUuid, CancellationToken ct = default);
}
