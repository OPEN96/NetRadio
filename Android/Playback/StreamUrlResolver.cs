using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace NetRadio.Droid.Playback;

/// <summary>
/// 把 .pls / .m3u 播放列表解析成真实音频流地址。
/// Android 的 MediaPlayer 只认直连流，遇到播放列表会直接报错。
/// </summary>
public static class StreamUrlResolver
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(8) };
    private static readonly Dictionary<string, string> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<string> ResolveAsync(string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;
        if (!LooksLikePlaylist(url)) return url;

        lock (Cache)
        {
            if (Cache.TryGetValue(url, out var cached)) return cached;
        }

        try
        {
            var text = await Http.GetStringAsync(url, ct).ConfigureAwait(false);
            var resolved = ParseFirstStream(text);
            if (!string.IsNullOrWhiteSpace(resolved))
            {
                lock (Cache)
                {
                    Cache[url] = resolved;
                }

                return resolved;
            }
        }
        catch
        {
            // 解析失败就退回原地址，让播放器自己报错，提示更直观。
        }

        return url;
    }

    private static bool LooksLikePlaylist(string url)
    {
        var clean = url.Split('?')[0].ToLowerInvariant();
        return clean.EndsWith(".pls") || clean.EndsWith(".m3u");
    }

    private static string? ParseFirstStream(string text)
    {
        // .pls：File1=http://...
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("File", StringComparison.OrdinalIgnoreCase))
            {
                var idx = trimmed.IndexOf('=');
                if (idx > 0 && idx + 1 < trimmed.Length)
                {
                    var value = trimmed[(idx + 1)..].Trim();
                    if (IsStream(value)) return value;
                }
            }

            if (IsStream(trimmed)) return trimmed;
        }

        return null;
    }

    private static bool IsStream(string value) =>
        value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
