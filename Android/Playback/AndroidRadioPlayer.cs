using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Avalonia.Threading;
using NetRadio.Models;
using NetRadio.Services;

namespace NetRadio.Droid.Playback;

/// <summary>
/// Android 播放后端：用系统 <see cref="MediaPlayer"/> 播放网络流，
/// ICY 元数据由 <see cref="IcyMetadataReader"/> 单独读取。
/// 桌面端的那套 libvlc 实现（VlcRadioPlayer）不参与 Android 构建。
/// </summary>
public sealed class AndroidRadioPlayer : IRadioPlayer
{
    private const int BufferingStart = 701;
    private const int BufferingEnd = 702;

    private readonly MediaPlayer _player;
    private readonly AudioAttributes? _attributes;
    private readonly IcyMetadataReader _icy = new();

    private RadioStation? _current;
    private PlaybackStatus _status = PlaybackStatus.Idle;
    private int _volume = 80;
    private bool _muted;
    private int _playToken;

    public AndroidRadioPlayer()
    {
        _attributes = new AudioAttributes.Builder()
            .SetUsage(AudioUsageKind.Media)!
            .SetContentType(AudioContentType.Music)!
            .Build()!;

        _player = new MediaPlayer();
        _player.SetAudioAttributes(_attributes);
        _player.Prepared += OnPrepared;
        _player.Info += OnInfo;
        _player.Error += OnError;
        _player.Completion += OnCompletion;
    }

    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<string?>? NowPlayingChanged;
    public event EventHandler<bool>? BufferingChanged;
    public event EventHandler<string?>? ErrorOccurred;

    public RadioStation? CurrentStation => _current;

    public PlaybackStatus Status => _status;

    public bool IsPlaying => _status == PlaybackStatus.Playing;

    public bool IsRecording => false;

    public int Volume
    {
        get => _volume;
        set
        {
            _volume = Math.Clamp(value, 0, 100);
            ApplyVolume();
        }
    }

    public bool IsMuted
    {
        get => _muted;
        set
        {
            _muted = value;
            ApplyVolume();
        }
    }

    public void Play(RadioStation station)
    {
        if (station is null || string.IsNullOrWhiteSpace(station.Url)) return;

        var token = Interlocked.Increment(ref _playToken);
        _icy.Stop();
        SafeReset();

        _current = station;
        SetStatus(PlaybackStatus.Connecting);
        RaiseBuffering(true);

        _ = OpenStreamAsync(station.Url, token);
    }

    private async Task OpenStreamAsync(string url, int token)
    {
        try
        {
            var direct = await StreamUrlResolver.ResolveAsync(url, CancellationToken.None).ConfigureAwait(true);
            if (token != Volatile.Read(ref _playToken)) return;

            _player.SetDataSource(direct);
            ApplyVolume();
            _player.PrepareAsync();

            _icy.Start(direct, text => Dispatcher.UIThread.Post(() => NowPlayingChanged?.Invoke(this, text)));
        }
        catch (Exception ex)
        {
            if (token != Volatile.Read(ref _playToken)) return;
            SetStatus(PlaybackStatus.Error);
            RaiseBuffering(false);
            ErrorOccurred?.Invoke(this, $"无法播放：{ex.Message}");
        }
    }

    public void TogglePause()
    {
        switch (_status)
        {
            case PlaybackStatus.Playing:
                try
                {
                    _player.Pause();
                    SetStatus(PlaybackStatus.Paused);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex.Message);
                }

                break;

            case PlaybackStatus.Paused:
                try
                {
                    _player.Start();
                    SetStatus(PlaybackStatus.Playing);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, ex.Message);
                }

                break;

            case PlaybackStatus.Connecting:
                Stop();
                break;

            default:
                if (_current is not null) Play(_current);
                break;
        }
    }

    public void Stop()
    {
        Interlocked.Increment(ref _playToken);
        _icy.Stop();
        SafeReset();
        _current = null;
        RaiseBuffering(false);
        SetStatus(PlaybackStatus.Idle);
        NowPlayingChanged?.Invoke(this, null);
    }

    /// <summary>Android 端暂不支持录音（桌面版用 libvlc 的 sout 转码）。</summary>
    public void SetRecording(bool enabled, string? outputPath)
    {
        if (enabled)
        {
            ErrorOccurred?.Invoke(this, "Android 版本暂不支持录音");
        }
    }

    private void OnPrepared(object? sender, EventArgs e)
    {
        try
        {
            _player.Start();
            RaiseBuffering(false);
            SetStatus(PlaybackStatus.Playing);
        }
        catch (Exception ex)
        {
            SetStatus(PlaybackStatus.Error);
            ErrorOccurred?.Invoke(this, $"播放失败：{ex.Message}");
        }
    }

    private void OnInfo(object? sender, MediaPlayer.InfoEventArgs e)
    {
        // 701 = MEDIA_INFO_BUFFERING_START，702 = MEDIA_INFO_BUFFERING_END
        switch ((int)e.What)
        {
            case BufferingStart:
                if (_status == PlaybackStatus.Playing) RaiseBuffering(true);
                break;

            case BufferingEnd:
                RaiseBuffering(false);
                break;
        }
    }

    private void OnError(object? sender, MediaPlayer.ErrorEventArgs e)
    {
        RaiseBuffering(false);
        SetStatus(PlaybackStatus.Error);
        ErrorOccurred?.Invoke(this, $"播放出错（{e.What}），可尝试其它电台");
    }

    private void OnCompletion(object? sender, EventArgs e)
    {
        // 网络流断开时也会走到这里，保持在暂停态便于用户重新点播放。
        if (_status == PlaybackStatus.Playing)
        {
            SetStatus(PlaybackStatus.Paused);
            RaiseBuffering(false);
        }
    }

    private void SafeReset()
    {
        try
        {
            if (_player.Handle != IntPtr.Zero)
            {
                _player.Reset();
            }
        }
        catch
        {
            // 重置失败不影响后续重新 SetDataSource
        }
    }

    private void ApplyVolume()
    {
        var v = _muted ? 0f : _volume / 100f;
        try
        {
            _player.SetVolume(v, v);
        }
        catch
        {
            // 尚未准备完成时忽略
        }
    }

    private void SetStatus(PlaybackStatus status)
    {
        if (_status == status) return;
        _status = status;
        var station = _current;
        Dispatcher.UIThread.Post(() => StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(station, status)));
    }

    private void RaiseBuffering(bool buffering)
    {
        Dispatcher.UIThread.Post(() => BufferingChanged?.Invoke(this, buffering));
    }

    public void Dispose()
    {
        _icy.Dispose();
        try
        {
            _player.Release();
            _player.Dispose();
        }
        catch
        {
            // 退出阶段忽略
        }
    }
}
