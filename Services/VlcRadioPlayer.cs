using System;
using System.Threading;
using LibVLCSharp.Shared;
using NetRadio.Models;

namespace NetRadio.Services;

/// <summary>
/// 基于 LibVLC 的网络流播放器。支持 http/https/m3u8/ICY 等绝大多数网络电台格式，
/// 并可读取 ICY 元数据（正在播放的曲目）。
/// </summary>
public sealed class VlcRadioPlayer : IRadioPlayer
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _player;
    private readonly Timer _metadataTimer;

    private Media? _media;
    private RadioStation? _current;
    private string? _lastMetadata;
    private bool _recording;
    private string? _recordPath;

    private PlaybackStatus _status = PlaybackStatus.Idle;
    private bool _isRebuffering;

    public VlcRadioPlayer(int networkCachingMs = 3000)
    {
        Core.Initialize();

        _libVlc = new LibVLC(enableDebugLogs: false,
            "--no-video",
            "--no-sout-video",
            "--no-sub-autodetect-file",
            $"--network-caching={Math.Clamp(networkCachingMs, 300, 20000)}",
            "--live-caching=3000",
            "--quiet");

        _player = new MediaPlayer(_libVlc);

        _player.Opening += (_, _) =>
        {
            SetRebuffering(true);
            SetStatus(PlaybackStatus.Connecting);
        };

        // 重要：libvlc 在"已经开始播放之后"仍会持续、密集地上报缓冲进度（0% → 100%）。
        // 因此 Buffering 绝不能无条件把状态改回 Connecting，否则界面会一直显示"正在连接…"。
        _player.Buffering += (_, e) =>
        {
            SetRebuffering(e.Cache < 100f);
            if (_status != PlaybackStatus.Playing)
            {
                SetStatus(PlaybackStatus.Connecting);
            }
        };

        _player.Playing += (_, _) => SetStatus(PlaybackStatus.Playing);
        _player.Paused += (_, _) =>
        {
            SetRebuffering(false);
            SetStatus(PlaybackStatus.Paused);
        };
        _player.Stopped += (_, _) =>
        {
            SetRebuffering(false);
            SetStatus(PlaybackStatus.Idle);
        };
        _player.EndReached += (_, _) =>
        {
            SetRebuffering(false);
            SetStatus(PlaybackStatus.Idle);
        };
        _player.EncounteredError += (_, _) =>
        {
            SetRebuffering(false);
            SetStatus(PlaybackStatus.Error);
            ErrorOccurred?.Invoke(this, "无法播放该电台：网络不可用、流地址失效或格式不受支持。");
        };

        _metadataTimer = new Timer(_ => PollMetadata(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<string?>? NowPlayingChanged;
    public event EventHandler<bool>? BufferingChanged;
    public event EventHandler<string?>? ErrorOccurred;

    public RadioStation? CurrentStation => _current;

    public PlaybackStatus Status => _status;

    public bool IsPlaying => _status is PlaybackStatus.Playing or PlaybackStatus.Connecting;

    public bool IsRecording => _recording;

    public int Volume
    {
        get => _player.Volume;
        set => _player.Volume = Math.Clamp(value, 0, 100);
    }

    public bool IsMuted
    {
        get => _player.Mute;
        set => _player.Mute = value;
    }

    public void Play(RadioStation station)
    {
        ArgumentNullException.ThrowIfNull(station);
        if (string.IsNullOrWhiteSpace(station.Url))
        {
            ErrorOccurred?.Invoke(this, "电台地址为空。");
            return;
        }

        StopCore();

        _current = station;
        _lastMetadata = null;

        Media media;
        try
        {
            media = new Media(_libVlc, new Uri(station.Url));
        }
        catch (Exception ex)
        {
            SetStatus(PlaybackStatus.Error);
            ErrorOccurred?.Invoke(this, $"无效的电台地址：{ex.Message}");
            return;
        }

        media.AddOption(":no-video");
        if (_recording && !string.IsNullOrWhiteSpace(_recordPath))
        {
            media.AddOption(BuildRecordOption(_recordPath));
        }

        media.MetaChanged += OnMediaMetaChanged;
        _media = media;

        _player.Media = media;
        _player.Play();

        SetStatus(PlaybackStatus.Connecting);
        _metadataTimer.Change(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5));
    }

    public void TogglePause()
    {
        if (_current is null)
        {
            return;
        }

        switch (_status)
        {
            case PlaybackStatus.Playing:
                _player.Pause();
                break;
            case PlaybackStatus.Connecting:
                // 还在连接/缓冲，libvlc 此时 pause 不会生效，直接停止更符合预期
                Stop();
                break;
            default:
                _player.Play();
                break;
        }
    }

    public void Stop()
    {
        StopCore();
        _current = null;
        SetRebuffering(false);
        SetStatus(PlaybackStatus.Idle);
    }

    public void SetRecording(bool enabled, string? outputPath)
    {
        var changed = enabled != _recording;
        _recording = enabled;
        _recordPath = enabled ? outputPath : null;

        if (changed && _current is not null && _status is PlaybackStatus.Playing or PlaybackStatus.Connecting)
        {
            // libvlc 的 sout 参数必须在播放前指定，因此重新拉流一次
            var station = _current;
            Play(station);
        }
    }

    private void StopCore()
    {
        SetRebuffering(false);
        _metadataTimer.Change(Timeout.Infinite, Timeout.Infinite);
        try
        {
            _player.Stop();
        }
        catch
        {
            // ignore
        }

        if (_media is not null)
        {
            _media.MetaChanged -= OnMediaMetaChanged;
            _media.Dispose();
            _media = null;
        }

        _player.Media = null;
    }

    private void OnMediaMetaChanged(object? sender, MediaMetaChangedEventArgs e)
    {
        if (e.MetadataType is MetadataType.NowPlaying or MetadataType.Title or MetadataType.Artist)
        {
            PollMetadata();
        }
    }

    private void PollMetadata()
    {
        try
        {
            var media = _media;
            if (media is null)
            {
                return;
            }

            var text = media.Meta(MetadataType.NowPlaying);
            if (string.IsNullOrWhiteSpace(text))
            {
                var title = media.Meta(MetadataType.Title);
                var artist = media.Meta(MetadataType.Artist);
                text = !string.IsNullOrWhiteSpace(artist) && !string.IsNullOrWhiteSpace(title)
                    ? $"{artist} - {title}"
                    : (title ?? string.Empty);
            }

            text = text?.Trim();
            if (string.IsNullOrWhiteSpace(text) || text == _lastMetadata)
            {
                return;
            }

            _lastMetadata = text;
            NowPlayingChanged?.Invoke(this, text);
        }
        catch
        {
            // 元数据读取失败不影响播放
        }
    }

    private void SetStatus(PlaybackStatus status)
    {
        // 去重：libvlc 的 Buffering 事件每秒可达上百次，重复派发会淹没 UI 线程
        if (_status == status)
        {
            return;
        }

        _status = status;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(_current, status));
    }

    /// <summary>只在"是否缓冲"这个布尔值真正翻转时才通知 UI。</summary>
    private void SetRebuffering(bool value)
    {
        if (_isRebuffering == value)
        {
            return;
        }

        _isRebuffering = value;
        BufferingChanged?.Invoke(this, value);
    }

    private static string BuildRecordOption(string path)
    {
        // VLC 的 sout 字符串中反斜杠是转义字符，统一换成正斜杠
        var p = path.Replace("\\", "/");
        return $":sout=#transcode{{acodec=mp3,ab=192,channels=2,samplerate=44100}}:std{{access=file,mux=raw,dst={p}}}";
    }

    public void Dispose()
    {
        _metadataTimer.Dispose();
        StopCore();
        _player.Dispose();
        _libVlc.Dispose();
    }
}
