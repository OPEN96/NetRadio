using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Avalonia.Threading;
using NetRadio.Models;
using NetRadio.Services;

namespace NetRadio.Droid.Playback;

/// <summary>
/// 前台播放服务：保证退到后台后继续播放，并提供通知栏 / 锁屏的媒体控制。
/// 播放状态本身由 <see cref="PlaybackHub.Player"/> 维护，这里只做通知与媒体会话的展示与转发。
/// </summary>
[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public sealed class RadioPlaybackService : Service
{
    public const string ActionPlayPause = "com.open96.netradio.PLAY_PAUSE";
    public const string ActionNext = "com.open96.netradio.NEXT";
    public const string ActionPrevious = "com.open96.netradio.PREVIOUS";
    public const string ActionStop = "com.open96.netradio.STOP";

    private const string ChannelId = "netradio.playback";
    private const int NotificationId = 1001;

    private NotificationManager? _notifications;
    private MediaSession? _session;

    public override void OnCreate()
    {
        base.OnCreate();

        _notifications = GetSystemService(NotificationService) as NotificationManager;
        CreateChannel();

        _session = new MediaSession(this, "NetRadio");
        _session.SetCallback(new SessionCallback());
        _session.SetFlags(MediaSessionFlags.HandlesMediaButtons | MediaSessionFlags.HandlesTransportControls);
        _session.Active = true;

        PlaybackHub.Player.StateChanged += OnPlayerChanged;
        PlaybackHub.Player.NowPlayingChanged += OnNowPlayingChanged;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        switch (intent?.Action)
        {
            case ActionPlayPause:
                PlaybackHub.TogglePlayPause();
                break;
            case ActionNext:
                PlaybackHub.Next();
                break;
            case ActionPrevious:
                PlaybackHub.Previous();
                break;
            case ActionStop:
                PlaybackHub.Stop();
                break;
        }

        StartForeground(NotificationId, BuildNotification());
        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        PlaybackHub.Player.StateChanged -= OnPlayerChanged;
        PlaybackHub.Player.NowPlayingChanged -= OnNowPlayingChanged;

        if (_session is not null)
        {
            _session.Active = false;
            _session.Release();
            _session.Dispose();
            _session = null;
        }

        base.OnDestroy();
    }

    private void OnPlayerChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                UpdateSessionPlaybackState(e.Status);
                if (e.Status == PlaybackStatus.Idle)
                {
                    StopForeground(StopForegroundFlags.Remove);
                    StopSelf();
                    return;
                }

                _notifications?.Notify(NotificationId, BuildNotification());
            }
            catch
            {
                // 通知更新失败不影响播放
            }
        });
    }

    private void OnNowPlayingChanged(object? sender, string? text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                UpdateSessionMetadata();
                _notifications?.Notify(NotificationId, BuildNotification());
            }
            catch
            {
            }
        });
    }

    private void UpdateSessionPlaybackState(PlaybackStatus status)
    {
        if (_session is null) return;

        var state = status switch
        {
            PlaybackStatus.Playing => PlaybackStateCode.Playing,
            PlaybackStatus.Paused => PlaybackStateCode.Paused,
            PlaybackStatus.Connecting => PlaybackStateCode.Buffering,
            PlaybackStatus.Error => PlaybackStateCode.Error,
            _ => PlaybackStateCode.Stopped
        };

        var actions = PlaybackState.ActionPlay | PlaybackState.ActionPause |
                      PlaybackState.ActionPlayPause | PlaybackState.ActionStop |
                      PlaybackState.ActionSkipToNext | PlaybackState.ActionSkipToPrevious;

        var builder = new PlaybackState.Builder()
            .SetActions(actions)!
            .SetState(state, PlaybackState.PlaybackPositionUnknown, 1.0f)!;

        _session.SetPlaybackState(builder.Build());
        UpdateSessionMetadata();
    }

    private void UpdateSessionMetadata()
    {
        if (_session is null) return;

        var station = PlaybackHub.Player.CurrentStation;
        if (station is null) return;

        var metadata = new MediaMetadata.Builder()
            .PutString(MediaMetadata.MetadataKeyTitle, station.Name)!
            .PutString(MediaMetadata.MetadataKeyArtist, PlaybackHub.Player.CurrentStation?.NowPlaying ?? station.Country)!
            .PutString(MediaMetadata.MetadataKeyAlbum, "NetRadio")!
            .Build();

        _session.SetMetadata(metadata);
    }

    private Notification BuildNotification()
    {
        var station = PlaybackHub.Player.CurrentStation;
        var playing = PlaybackHub.Player.Status == PlaybackStatus.Playing;

        var title = station?.Name ?? "NetRadio";
        var text = PlaybackHub.Player.Status switch
        {
            PlaybackStatus.Connecting => "正在连接…",
            PlaybackStatus.Playing => station?.NowPlaying ?? "正在播放",
            PlaybackStatus.Paused => "已暂停",
            PlaybackStatus.Error => "播放出错",
            _ => "已停止"
        };

        var builder = new Notification.Builder(this, ChannelId)
            .SetContentTitle(title)
            .SetContentText(text)
            .SetSmallIcon(Resource.Drawable.ic_stat_radio)
            .SetContentIntent(OpenAppIntent())
            .SetOngoing(playing)
            .SetShowWhen(false)
            .SetVisibility(NotificationVisibility.Public)
            .AddAction(Action(Android.Resource.Drawable.IcMediaPrevious, "上一曲", ActionPrevious))
            .AddAction(Action(
                playing ? Android.Resource.Drawable.IcMediaPause : Android.Resource.Drawable.IcMediaPlay,
                playing ? "暂停" : "播放",
                ActionPlayPause))
            .AddAction(Action(Android.Resource.Drawable.IcMediaNext, "下一曲", ActionNext))
            .AddAction(Action(Android.Resource.Drawable.IcMenuCloseClearCancel, "停止", ActionStop));

        var style = new Notification.MediaStyle()!;
        style.SetMediaSession(_session?.SessionToken);
        style.SetShowActionsInCompactView(0, 1, 2);

        builder.SetStyle(style);

        return builder.Build();
    }

    private PendingIntent? OpenAppIntent()
    {
        var launch = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
        if (launch is null) return null;

        launch.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        return PendingIntent.GetActivity(this, 0, launch, PendingIntentFlags.Immutable);
    }

    private Notification.Action Action(int icon, string title, string action)
    {
        var intent = new Intent(this, typeof(RadioPlaybackService)).SetAction(action);
        var pending = PendingIntent.GetService(this, action.GetHashCode(), intent, PendingIntentFlags.Immutable);
        var drawable = Android.Graphics.Drawables.Icon.CreateWithResource(this, icon);
        return new Notification.Action.Builder(drawable, new Java.Lang.String(title), pending).Build();
    }

    private void CreateChannel()
    {
        if (_notifications is null) return;
        if (!OperatingSystem.IsAndroidVersionAtLeast(26)) return;

        var channel = new NotificationChannel(ChannelId, "播放控制", NotificationImportance.Low)
        {
            Description = "显示正在播放的电台，并提供播放控制"
        };
        channel.SetShowBadge(false);
        _notifications.CreateNotificationChannel(channel);
    }

    private sealed class SessionCallback : MediaSession.Callback
    {
        public override void OnPlay() => PlaybackHub.TogglePlayPause();

        public override void OnPause() => PlaybackHub.TogglePlayPause();

        public override void OnSkipToNext() => PlaybackHub.Next();

        public override void OnSkipToPrevious() => PlaybackHub.Previous();

        public override void OnStop() => PlaybackHub.Stop();
    }
}
