using System;
using Android.Content;
using NetRadio.Services;

namespace NetRadio.Droid.Services;

/// <summary>
/// 移动端没有多窗口 / 托盘概念，外壳服务只保留"退出"这一个真实行为，
/// 其余接口保持空实现，好让共享的 MainViewModel 原样复用。
/// </summary>
public sealed class AndroidShellService : IShellService
{
    public bool IsExiting { get; private set; }

    public bool IsMainWindowVisible => true;

    public bool IsMiniPlayerOpen => false;

    public bool IsTrayAvailable => false;

    public event EventHandler? MainWindowVisibilityChanged;

    public event EventHandler? MiniPlayerVisibilityChanged;

    public void ShowMainWindow()
    {
    }

    public void HideMainWindow()
    {
    }

    public void ToggleMainWindow()
    {
    }

    public void ShowMiniPlayer()
    {
    }

    public void CloseMiniPlayer()
    {
    }

    public void ToggleMiniPlayer()
    {
    }

    public void ExitApp()
    {
        IsExiting = true;

        Playback.PlaybackHub.Stop();
        Playback.PlaybackHub.StopService();

        if (Avalonia.Application.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime)
        {
            // 移动端由系统管理 Activity 生命周期，这里只停播放；用户可用返回键退出界面
        }

        MainWindowVisibilityChanged?.Invoke(this, EventArgs.Empty);
        MiniPlayerVisibilityChanged?.Invoke(this, EventArgs.Empty);
    }
}
