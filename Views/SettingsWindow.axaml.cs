using Avalonia.Controls;
using NetRadio.ViewModels;

namespace NetRadio.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow()
        : this(new SettingsViewModel(new NetRadio.Services.AppSettings()))
    {
    }

    public SettingsWindow(SettingsViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
    }

    private void OnCancelClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnSaveClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _vm.Apply();
        Close(true);
    }
}
