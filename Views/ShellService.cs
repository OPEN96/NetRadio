using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NetRadio.Services;
using NetRadio.ViewModels;

namespace NetRadio.Views;

/// <summary>
/// <see cref="IShellService"/> 的实现：持有主窗口 / 小窗口引用，负责两者的互斥切换与退出。
/// 由 <see cref="App"/> 在启动时创建并注入到 <see cref="MainViewModel"/>。
/// </summary>
public sealed class ShellService : IShellService
{
    private MainWindow? _main;
    private MiniPlayerWindow? _mini;
    private IClassicDesktopStyleApplicationLifetime? _lifetime;

    /// <summary>打开小窗口前主窗口是否可见；关闭小窗口时据此决定是否恢复主界面。</summary>
    private bool _mainWasVisibleBeforeMini = true;

    public bool IsExiting { get; private set; }

    public bool IsMainWindowVisible => _main?.IsVisible == true;

    public bool IsMiniPlayerOpen => _mini is not null;

    /// <summary>由 App 在托盘图标创建成功后置为 true。</summary>
    public bool IsTrayAvailable { get; set; }

    public event EventHandler? MainWindowVisibilityChanged;
    public event EventHandler? MiniPlayerVisibilityChanged;

    /// <summary>绑定主窗口，并监听其可见性变化以同步托盘菜单文案。</summary>
    public void Attach(MainWindow main)
    {
        _main = main;
        _main.Opened += (_, _) => RaiseMainVisibilityChanged();
        _main.PropertyChanged += (_, e) =>
        {
            if (e.Property == Visual.IsVisibleProperty)
            {
                RaiseMainVisibilityChanged();
            }
        };
    }

    public void AttachLifetime(IClassicDesktopStyleApplicationLifetime lifetime) => _lifetime = lifetime;

    public void ShowMainWindow()
    {
        // 从小窗口返回主界面：先收起小窗口，避免两个窗口同时出现。
        if (_mini is not null)
        {
            _mainWasVisibleBeforeMini = false;
            CloseMiniPlayer();
        }

        if (_main is null) return;

        if (_main.WindowState == WindowState.Minimized)
        {
            _main.WindowState = WindowState.Normal;
        }

        if (!_main.IsVisible)
        {
            _main.Show();
        }

        _main.Activate();
        RaiseMainVisibilityChanged();
    }

    public void HideMainWindow()
    {
        _main?.Hide();
        RaiseMainVisibilityChanged();
    }

    public void ToggleMainWindow()
    {
        if (IsMainWindowVisible)
        {
            HideMainWindow();
        }
        else
        {
            ShowMainWindow();
        }
    }

    public void ShowMiniPlayer()
    {
        if (_mini is not null)
        {
            _mini.Activate();
            return;
        }

        if (_main is null) return;

        _mainWasVisibleBeforeMini = _main.IsVisible;

        var mini = new MiniPlayerWindow
        {
            DataContext = _main.DataContext,
            WindowStartupLocation = WindowStartupLocation.Manual
        };

        // 让小窗口出现在原主窗口的位置附近，避免跳到屏幕另一角。
        try
        {
            mini.Position = _main.Position;
        }
        catch
        {
            // 某些平台在窗口未显示前不支持设置位置，忽略即可。
        }

        mini.Closed += OnMiniClosed;
        _mini = mini;

        if (_mainWasVisibleBeforeMini)
        {
            _main.Hide();
        }

        mini.Show();
        mini.Activate();
        RaiseMiniVisibilityChanged();
    }

    public void CloseMiniPlayer()
    {
        _mini?.Close();
    }

    public void ToggleMiniPlayer()
    {
        if (IsMiniPlayerOpen)
        {
            CloseMiniPlayer();
        }
        else
        {
            ShowMiniPlayer();
        }
    }

    public void ExitApp()
    {
        if (IsExiting) return;
        IsExiting = true;

        if (_main?.DataContext is MainViewModel vm)
        {
            vm.OnMainWindowClosing();
        }

        try
        {
            _mini?.Close();
        }
        catch
        {
            // 关闭过程中的异常不应阻止退出。
        }

        try
        {
            _main?.Close();
        }
        catch
        {
            // 同上。
        }

        _lifetime?.Shutdown();
    }

    private void OnMiniClosed(object? sender, EventArgs e)
    {
        if (_mini is not null)
        {
            _mini.Closed -= OnMiniClosed;
            _mini = null;
        }

        RaiseMiniVisibilityChanged();

        // 用户主动关闭小窗口时回到主界面；退出流程或原本主界面就隐藏时保持隐藏。
        if (!IsExiting && _mainWasVisibleBeforeMini)
        {
            ShowMainWindow();
        }
    }

    private void RaiseMainVisibilityChanged() =>
        MainWindowVisibilityChanged?.Invoke(this, EventArgs.Empty);

    private void RaiseMiniVisibilityChanged() =>
        MiniPlayerVisibilityChanged?.Invoke(this, EventArgs.Empty);
}
