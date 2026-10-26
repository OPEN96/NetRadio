using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using NetRadio.Models;
using NetRadio.Services;
using NetRadio.ViewModels;

namespace NetRadio.Views;

/// <summary>
/// 系统托盘图标与菜单。托盘的文案（播放 / 暂停、显示 / 隐藏）跟随播放与窗口状态实时更新。
/// 依赖 <see cref="Application.Current"/> 已完成初始化，因此必须在
/// <c>OnFrameworkInitializationCompleted</c> 之后创建；不支持托盘的平台会由调用方捕获异常降级。
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private const string IconUri = "avares://NetRadio/Assets/avalonia-logo.ico";

    private readonly MainViewModel _viewModel;
    private readonly IShellService _shell;

    private readonly TrayIcons _icons;
    private readonly TrayIcon _tray;

    private readonly NativeMenuItem _toggleWindowItem;
    private readonly NativeMenuItem _miniPlayerItem;
    private readonly NativeMenuItem _playPauseItem;
    private readonly NativeMenuItem _previousItem;
    private readonly NativeMenuItem _nextItem;
    private readonly NativeMenuItem _stopItem;

    private bool _disposed;

    public TrayIconService(MainViewModel viewModel, IShellService shell)
    {
        _viewModel = viewModel;
        _shell = shell;

        _toggleWindowItem = new NativeMenuItem("显示主界面");
        _toggleWindowItem.Click += (_, _) => _shell.ToggleMainWindow();

        _miniPlayerItem = new NativeMenuItem("打开小窗口");
        _miniPlayerItem.Click += (_, _) => _shell.ToggleMiniPlayer();

        _playPauseItem = new NativeMenuItem("播放")
        {
            Command = viewModel.TogglePlayPauseCommand
        };

        _previousItem = new NativeMenuItem("上一曲")
        {
            Command = viewModel.PlayPreviousCommand
        };

        _nextItem = new NativeMenuItem("下一曲")
        {
            Command = viewModel.PlayNextCommand
        };

        _stopItem = new NativeMenuItem("停止")
        {
            Command = viewModel.StopCommand
        };

        var exitItem = new NativeMenuItem("退出");
        exitItem.Click += (_, _) => _shell.ExitApp();

        var menu = new NativeMenu();
        menu.Items.Add(_toggleWindowItem);
        menu.Items.Add(_miniPlayerItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_playPauseItem);
        menu.Items.Add(_previousItem);
        menu.Items.Add(_nextItem);
        menu.Items.Add(_stopItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        _tray = new TrayIcon
        {
            Icon = LoadIcon(),
            ToolTipText = "NetRadio 网络收音机",
            Menu = menu,
            IsVisible = true
        };
        _tray.Clicked += OnTrayClicked;

        _icons = new TrayIcons { _tray };
        if (Application.Current is { } app)
        {
            TrayIcon.SetIcons(app, _icons);
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _shell.MainWindowVisibilityChanged += OnShellStateChanged;
        _shell.MiniPlayerVisibilityChanged += OnShellStateChanged;

        UpdateState();
    }

    /// <summary>托盘创建是否成功（平台不支持时为 false，由 App 记录状态提示）。</summary>
    public bool IsAvailable => !_disposed && Application.Current is not null;

    private static WindowIcon? LoadIcon()
    {
        try
        {
            return new WindowIcon(AssetLoader.Open(new Uri(IconUri)));
        }
        catch
        {
            return null;
        }
    }

    private void OnTrayClicked(object? sender, EventArgs e) => _shell.ToggleMainWindow();

    private void OnShellStateChanged(object? sender, EventArgs e) => UpdateState();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.Status)
            or nameof(MainViewModel.StatusText)
            or nameof(MainViewModel.CurrentStation)
            or nameof(MainViewModel.IsMiniPlayerOpen))
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        if (_disposed) return;

        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(UpdateState);
            return;
        }

        _toggleWindowItem.Header = _shell.IsMainWindowVisible ? "隐藏主界面" : "显示主界面";
        _miniPlayerItem.Header = _shell.IsMiniPlayerOpen ? "关闭小窗口" : "打开小窗口";

        var isPlaying = _viewModel.Status is PlaybackStatus.Playing or PlaybackStatus.Connecting;
        _playPauseItem.Header = isPlaying ? "暂停" : "播放";

        var hasStations = _viewModel.Stations.Count > 0;
        _previousItem.IsEnabled = hasStations;
        _nextItem.IsEnabled = hasStations;
        _stopItem.IsEnabled = _viewModel.CurrentStation is not null;

        var station = _viewModel.CurrentStation?.Name;
        _tray.ToolTipText = string.IsNullOrWhiteSpace(station)
            ? "NetRadio 网络收音机"
            : $"{station} · {_viewModel.StatusText}";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _shell.MainWindowVisibilityChanged -= OnShellStateChanged;
        _shell.MiniPlayerVisibilityChanged -= OnShellStateChanged;
        _tray.Clicked -= OnTrayClicked;

        try
        {
            _tray.IsVisible = false;
            _icons.Remove(_tray);
            _tray.Dispose();
        }
        catch
        {
            // 退出阶段忽略托盘释放异常。
        }
    }
}
