using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetRadio.Droid.Playback;

/// <summary>
/// 读取 Icecast / Shoutcast 的 ICY 元数据（当前曲目）。
/// Android 的 MediaPlayer 不解析这部分，所以另开一条带 <c>Icy-MetaData: 1</c> 的连接，
/// 只做文本解析、丢弃音频数据；失败不影响正常播放。
/// </summary>
public sealed class IcyMetadataReader : IDisposable
{
    private static readonly HttpClient Http = new() { Timeout = Timeout.InfiniteTimeSpan };

    private CancellationTokenSource? _cts;
    private string? _last;

    public void Start(string streamUrl, Action<string?> onTitle)
    {
        Stop();
        _last = null;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(() => ReadLoopAsync(streamUrl, onTitle, token), token);
    }

    public void Stop()
    {
        var cts = _cts;
        _cts = null;
        cts?.Cancel();
        cts?.Dispose();
    }

    private async Task ReadLoopAsync(string url, Action<string?> onTitle, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("Icy-MetaData", "1");
            request.Headers.TryAddWithoutValidation("User-Agent", "NetRadio/1.0");

            using var response = await Http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var metaint = 0;
            if (response.Headers.TryGetValues("icy-metaint", out var values) &&
                int.TryParse(values.FirstOrDefault(), out var parsed))
            {
                metaint = parsed;
            }

            if (metaint <= 0) return;

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);

            var audio = new byte[Math.Min(metaint, 64 * 1024)];
            var lengthByte = new byte[1];

            while (!ct.IsCancellationRequested)
            {
                // 跳过音频数据，只保留元数据块
                var remaining = metaint;
                while (remaining > 0 && !ct.IsCancellationRequested)
                {
                    var n = await stream.ReadAsync(audio.AsMemory(0, Math.Min(audio.Length, remaining)), ct)
                        .ConfigureAwait(false);
                    if (n <= 0) return;
                    remaining -= n;
                }

                if (await stream.ReadAsync(lengthByte, ct).ConfigureAwait(false) <= 0) return;

                var size = lengthByte[0] * 16;
                if (size == 0) continue;

                var block = new byte[size];
                var read = 0;
                while (read < size)
                {
                    var n = await stream.ReadAsync(block.AsMemory(read, size - read), ct).ConfigureAwait(false);
                    if (n <= 0) return;
                    read += n;
                }

                var title = ExtractTitle(Encoding.UTF8.GetString(block).TrimEnd('\0'));
                if (!string.IsNullOrWhiteSpace(title) && title != _last)
                {
                    _last = title;
                    onTitle(title);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            // 元数据服务不可用是常态（部分电台根本不下发），静默忽略。
        }
    }

    private static string? ExtractTitle(string meta)
    {
        const string key = "StreamTitle='";
        var start = meta.IndexOf(key, StringComparison.Ordinal);
        if (start < 0) return null;
        start += key.Length;
        var end = meta.IndexOf("';", start, StringComparison.Ordinal);
        if (end < 0) end = meta.IndexOf('\'', start);
        if (end <= start) return null;
        return meta[start..end].Trim();
    }

    public void Dispose() => Stop();
}
