using System;
using Android.Content;
using Avalonia.Threading;
using NetRadio.Models;
using NetRadio.ViewModels;

namespace NetRadio.Droid.Playback;

/// <summary>
/// 进程内唯一的播放器与上下文持有者。
/// Activity 与前台服务共享这里的实例，所以锁屏 / 通知栏操作能直接作用到同一个播放器，
/// Avtivity 重建（旋屏等）后界面也能重新接上正在播放的电台。
/// </summary>
public static class PlaybackHub
{
    private static bool _wired;

    public static AndroidRadioPlayer Player { get; } = new();

    public static Context? AppContext { get; private set; }

    public static MainViewModel? ViewModel { get; set; }

    public static void Initialize(Context context)
    {
        AppContext = context.ApplicationContext ?? context;

        if (_wired) return;
        _wired = true;

        // 播放开始 → 拉起前台服务（保活 + 通知栏控制）；停止 → 收起服务
        Player.StateChanged += (_, e) =>
        {
            switch (e.Status)
            {
                case PlaybackStatus.Connecting:
                case PlaybackStatus.Playing:
                case PlaybackStatus.Paused:
                    StartService();
                    break;
                case PlaybackStatus.Idle:
                case PlaybackStatus.Error:
                    StopService();
                    break;
            }
        };
    }

    public static void StartService()
    {
        var context = AppContext;
        if (context is null) return;

        try
        {
            var intent = new Intent(context, typeof(RadioPlaybackService));
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                context.StartForegroundService(intent);
            }
            else
            {
                context.StartService(intent);
            }
        }
        catch
        {
            // 服务起不来也不该影响播放本身
        }
    }

    public static void StopService()
    {
        var context = AppContext;
        if (context is null) return;

        try
        {
            context.StopService(new Intent(context, typeof(RadioPlaybackService)));
        }
        catch
        {
        }
    }

    public static void TogglePlayPause() => Dispatch(vm => vm.TogglePlayPauseCommand.Execute(null), Player.TogglePause);

    public static void Next() => Dispatch(vm => vm.PlayNextCommand.Execute(null), () => { });

    public static void Previous() => Dispatch(vm => vm.PlayPreviousCommand.Execute(null), () => { });

    public static void Stop() => Dispatch(vm => vm.StopCommand.Execute(null), Player.Stop);

    private static void Dispatch(Action<MainViewModel> action, Action fallback)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ViewModel is { } vm)
            {
                action(vm);
            }
            else
            {
                // 界面已销毁（进程仍被前台服务保活）时退化为播放器级操作
                fallback();
            }
        });
    }
}
