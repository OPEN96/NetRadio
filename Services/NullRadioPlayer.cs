using System;
using NetRadio.Models;

namespace NetRadio.Services;

/// <summary>
/// 当底层音频后端不可用时（例如 Linux 未安装 libvlc）使用的占位播放器，
/// 保证 UI 仍然可用，并给出清晰的安装提示。
/// </summary>
public sealed class NullRadioPlayer(string? reason) : IRadioPlayer
{
    private readonly string _reason = reason ?? "音频后端不可用";

#pragma warning disable CS0067
    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;
    public event EventHandler<string?>? NowPlayingChanged;
    public event EventHandler<bool>? BufferingChanged;
    public event EventHandler<string?>? ErrorOccurred;
#pragma warning restore CS0067

    public RadioStation? CurrentStation { get; private set; }
    public PlaybackStatus Status { get; private set; } = PlaybackStatus.Idle;
    public bool IsPlaying => false;
    public bool IsRecording => false;

    public int Volume { get; set; } = 80;
    public bool IsMuted { get; set; }

    public void Play(RadioStation station)
    {
        CurrentStation = station;
        Status = PlaybackStatus.Error;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(station, PlaybackStatus.Error));
        ErrorOccurred?.Invoke(this, _reason);
    }

    public void TogglePause()
    {
        ErrorOccurred?.Invoke(this, _reason);
    }

    public void Stop()
    {
        Status = PlaybackStatus.Idle;
        CurrentStation = null;
        StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(null, PlaybackStatus.Idle));
    }

    public void SetRecording(bool enabled, string? outputPath)
    {
        ErrorOccurred?.Invoke(this, _reason);
    }

    public void Dispose()
    {
    }
}
