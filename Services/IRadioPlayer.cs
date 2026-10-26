using System;
using NetRadio.Models;

namespace NetRadio.Services;

public sealed class PlaybackStateChangedEventArgs(RadioStation? station, PlaybackStatus status) : EventArgs
{
    public RadioStation? Station { get; } = station;
    public PlaybackStatus Status { get; } = status;
}

/// <summary>音频播放抽象，便于将来替换解码后端。</summary>
public interface IRadioPlayer : IDisposable
{
    event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    /// <summary>当前曲目变化（ICY / Shoutcast 元数据）。</summary>
    event EventHandler<string?>? NowPlayingChanged;

    /// <summary>
    /// 缓冲状态变化（true = 正在缓冲）。
    /// 注意：libvlc 在整个播放期间都会周期性上报缓冲进度，因此该事件只表示"数据不足"，
    /// 不代表还没开始播放——播放状态请以 <see cref="Status"/> 为准。
    /// </summary>
    event EventHandler<bool>? BufferingChanged;

    event EventHandler<string?>? ErrorOccurred;

    RadioStation? CurrentStation { get; }
    PlaybackStatus Status { get; }
    bool IsPlaying { get; }
    bool IsRecording { get; }

    int Volume { get; set; }
    bool IsMuted { get; set; }

    void Play(RadioStation station);
    void TogglePause();
    void Stop();

    /// <summary>开启 / 关闭录制。开启时若正在播放会自动重启流以应用录制参数。</summary>
    void SetRecording(bool enabled, string? outputPath);
}
