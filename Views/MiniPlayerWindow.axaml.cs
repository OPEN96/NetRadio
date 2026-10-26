using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NetRadio.ViewModels;

namespace NetRadio.Views;

/// <summary>
/// 小窗口播放模式。与主窗口共享同一个 <see cref="MainViewModel"/> 实例，
/// 因此播放状态、音量、当前曲目与主界面天然同步。
/// </summary>
public partial class MiniPlayerWindow : Window
{
    public MiniPlayerWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        if (DataContext is MainViewModel vm)
        {
            Topmost = vm.MiniPlayerAlwaysOnTop;
        }

        UpdatePinVisual();
    }

    /// <summary>无系统标题栏时自行实现窗口拖动。</summary>
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnPinClick(object? sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        UpdatePinVisual();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();

    private void UpdatePinVisual()
    {
        var pin = this.FindControl<Button>("PinButton");
        if (pin is not null)
        {
            pin.Opacity = Topmost ? 1d : 0.4d;
        }
    }
}
