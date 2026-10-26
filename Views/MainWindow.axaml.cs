using System;
using Avalonia.Controls;
using Avalonia.Input;
using NetRadio.ViewModels;

namespace NetRadio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.OnMainWindowClosing();
        }

        base.OnClosing(e);
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
