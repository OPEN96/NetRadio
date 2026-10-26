using System;

namespace NetRadio.Services;

/// <summary>
/// 窗口外壳服务：统一管理主窗口与小窗口的显示 / 隐藏 / 切换，以及程序退出。
/// 抽成接口是为了让 ViewModel 不直接依赖具体的 Window 类型，便于测试与替换。
/// </summary>
public interface IShellService
{
    /// <summary>程序是否正在退出（退出过程中不再拦截窗口关闭）。</summary>
    bool IsExiting { get; }

    /// <summary>主窗口当前是否可见。</summary>
    bool IsMainWindowVisible { get; }

    /// <summary>小窗口是否已打开。</summary>
    bool IsMiniPlayerOpen { get; }

    /// <summary>系统托盘是否可用；不可用时不应把窗口隐藏到托盘，否则用户无法找回界面。</summary>
    bool IsTrayAvailable { get; }

    /// <summary>显示主窗口（若小窗口打开会先收起小窗口）。</summary>
    void ShowMainWindow();

    /// <summary>隐藏主窗口（最小化到托盘）。</summary>
    void HideMainWindow();

    /// <summary>在主窗口可见 / 隐藏之间切换，供托盘图标点击与菜单使用。</summary>
    void ToggleMainWindow();

    /// <summary>打开小窗口播放模式（会自动隐藏主窗口）。</summary>
    void ShowMiniPlayer();

    /// <summary>关闭小窗口。</summary>
    void CloseMiniPlayer();

    /// <summary>在小窗口播放模式与主窗口之间切换。</summary>
    void ToggleMiniPlayer();

    /// <summary>真正退出程序（关闭所有窗口并保存状态）。</summary>
    void ExitApp();

    /// <summary>主窗口可见性变化。</summary>
    event EventHandler? MainWindowVisibilityChanged;

    /// <summary>小窗口打开 / 关闭。</summary>
    event EventHandler? MiniPlayerVisibilityChanged;
}
