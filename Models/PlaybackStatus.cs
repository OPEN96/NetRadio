namespace NetRadio.Models;

/// <summary>播放状态。</summary>
public enum PlaybackStatus
{
    Idle,
    Connecting,
    Playing,
    Paused,
    Error
}

public static class PlaybackStatusText
{
    public static string ToDisplayText(PlaybackStatus status) => status switch
    {
        PlaybackStatus.Idle => "未播放",
        PlaybackStatus.Connecting => "正在连接…",
        PlaybackStatus.Playing => "正在播放",
        PlaybackStatus.Paused => "已暂停",
        PlaybackStatus.Error => "播放失败",
        _ => string.Empty
    };
}
