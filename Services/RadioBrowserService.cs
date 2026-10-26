using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NetRadio.Models;

namespace NetRadio.Services;

/// <summary>
/// radio-browser.info 社区电台目录（免费、无需 API Key，收录全球 5 万+ 网络电台）。
/// 内置多个镜像，任一可用即返回结果。
/// </summary>
public sealed class RadioBrowserService : IStationDirectory, IDisposable
{
    private static readonly string[] Mirrors =
    {
        "https://de1.api.radio-browser.info",
        "https://fi1.api.radio-browser.info",
        "https://nl1.api.radio-browser.info",
        "https://at1.api.radio-browser.info"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly HttpClient _http;
    private int _mirrorIndex;

    public RadioBrowserService()
    {
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(25)
        };
        // radio-browser 要求标识 User-Agent
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("NetRadio/1.0 (+https://www.radio-browser.info)");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public async Task<IReadOnlyList<RadioStation>> SearchAsync(StationQuery query, CancellationToken ct = default)
    {
        var path = BuildSearchPath(query);

        for (var attempt = 0; attempt < Mirrors.Length; attempt++)
        {
            var index = (_mirrorIndex + attempt) % Mirrors.Length;
            try
            {
                var json = await _http.GetStringAsync(Mirrors[index] + path, ct).ConfigureAwait(false);
                var items = JsonSerializer.Deserialize<List<StationDto>>(json, JsonOptions) ?? new List<StationDto>();
                _mirrorIndex = index;
                return items.Select(ToStation).Where(s => !string.IsNullOrWhiteSpace(s.Url)).ToList();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // 尝试下一个镜像
            }
        }

        return Array.Empty<RadioStation>();
    }

    public async Task RegisterPlayAsync(string? stationUuid, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(stationUuid)) return;

        try
        {
            await _http.GetAsync($"{Mirrors[_mirrorIndex]}/json/url/{Uri.EscapeDataString(stationUuid)}", ct)
                       .ConfigureAwait(false);
        }
        catch (Exception)
        {
            // 统计失败不影响播放
        }
    }

    private static string BuildSearchPath(StationQuery q)
    {
        var sb = new StringBuilder("/json/stations/search?");
        sb.Append("hidebroken=true");
        sb.Append("&order=votes").Append("&reverse=true");
        sb.Append("&limit=").Append(Math.Clamp(q.Limit, 1, 500));

        if (!string.IsNullOrWhiteSpace(q.Keyword))
            sb.Append("&name=").Append(Uri.EscapeDataString(q.Keyword.Trim()));
        if (!string.IsNullOrWhiteSpace(q.CountryCode))
            sb.Append("&countrycode=").Append(Uri.EscapeDataString(q.CountryCode.Trim()));
        if (!string.IsNullOrWhiteSpace(q.Tag))
            sb.Append("&tag=").Append(Uri.EscapeDataString(q.Tag.Trim()));

        return sb.ToString();
    }

    private static RadioStation ToStation(StationDto dto)
    {
        var name = string.IsNullOrWhiteSpace(dto.Name) ? "(未命名电台)" : dto.Name!.Trim();
        var url = !string.IsNullOrWhiteSpace(dto.UrlResolved) ? dto.UrlResolved! : dto.Url ?? string.Empty;

        return new RadioStation
        {
            Id = string.IsNullOrWhiteSpace(dto.StationUuid) ? "url:" + url : "uuid:" + dto.StationUuid,
            StationUuid = dto.StationUuid,
            Name = name,
            Url = url,
            Favicon = string.IsNullOrWhiteSpace(dto.Favicon) ? null : dto.Favicon,
            Homepage = string.IsNullOrWhiteSpace(dto.Homepage) ? null : dto.Homepage,
            Tags = dto.Tags,
            Country = dto.Country,
            CountryCode = dto.CountryCode,
            Language = dto.Language,
            Codec = dto.Codec,
            Bitrate = dto.Bitrate,
            Votes = dto.Votes
        };
    }

    public void Dispose() => _http.Dispose();

    private sealed class StationDto
    {
        [JsonPropertyName("stationuuid")] public string? StationUuid { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("url")] public string? Url { get; set; }
        [JsonPropertyName("url_resolved")] public string? UrlResolved { get; set; }
        [JsonPropertyName("homepage")] public string? Homepage { get; set; }
        [JsonPropertyName("favicon")] public string? Favicon { get; set; }
        [JsonPropertyName("tags")] public string? Tags { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("countrycode")] public string? CountryCode { get; set; }
        [JsonPropertyName("language")] public string? Language { get; set; }
        [JsonPropertyName("codec")] public string? Codec { get; set; }
        [JsonPropertyName("bitrate")] public int Bitrate { get; set; }
        [JsonPropertyName("votes")] public int Votes { get; set; }
    }
}
