using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using NetRadio.Services;
using NetRadio.ViewModels;

namespace NetRadio.Views;

public partial class MainWindow : Window
{
    private readonly IShellService? _shell;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(IShellService shell) : this()
    {
        _shell = shell;
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);

        var list = this.FindControl<ListBox>("StationList");
        if (list is not null)
        {
            list.DoubleTapped += OnStationDoubleTapped;
            list.KeyUp += OnStationKeyUp;
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // 最小化主窗口时隐藏到托盘（可在设置中关闭；托盘不可用时保持普通最小化）。
        if (change.Property == WindowStateProperty &&
            WindowState == WindowState.Minimized &&
            _shell is { IsTrayAvailable: true } &&
            DataContext is MainViewModel { MinimizeToTrayEnabled: true })
        {
            HideToTray();
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OnMainWindowClosing();

            // 关闭按钮默认只收起窗口，真正退出走托盘菜单「退出」。
            if (_shell is { IsExiting: false, IsTrayAvailable: true } && vm.CloseToTrayEnabled)
            {
                e.Cancel = true;
                Dispatcher.UIThread.Post(HideToTray, DispatcherPriority.Background);
            }
        }

        base.OnClosing(e);
    }

    private void HideToTray()
    {
        Hide();

        if (DataContext is MainViewModel vm)
        {
            vm.StatusMessage = "已最小化到系统托盘，点击托盘图标可重新打开";
        }
    }

    private void OnStationDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (vm.SelectedStation is null) return;

        vm.PlayCommand.Execute(vm.SelectedStation);
    }

    private void OnStationKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is not MainViewModel vm) return;
        if (vm.SelectedStation is null) return;

        vm.PlayCommand.Execute(vm.SelectedStation);
        e.Handled = true;
    }
}
