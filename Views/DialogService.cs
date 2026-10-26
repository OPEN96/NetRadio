using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NetRadio.Models;
using NetRadio.Services;
using NetRadio.ViewModels;

namespace NetRadio.Views;

/// <summary>对话框的 UI 实现。</summary>
public sealed class DialogService(SettingsService settingsService) : IDialogService
{
    private static Window? Owner =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;

    public async Task<RadioStation?> ShowStationEditorAsync(RadioStation? station = null)
    {
        var owner = Owner;
        if (owner is null) return null;

        var window = new StationEditorWindow(new StationEditorViewModel(station));
        return await window.ShowDialog<RadioStation?>(owner).ConfigureAwait(true);
    }

    public async Task<bool> ShowSettingsAsync()
    {
        var owner = Owner;
        if (owner is null) return false;

        var window = new SettingsWindow(new SettingsViewModel(settingsService.Current));
        return await window.ShowDialog<bool>(owner).ConfigureAwait(true);
    }

    public void ShowMessage(string title, string message)
    {
        var owner = Owner;
        if (owner is null) return;

        var dialog = new Window
        {
            Title = title,
            Width = 440,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new TextBlock
            {
                Text = message,
                Margin = new Thickness(24),
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            }
        };

        _ = dialog.ShowDialog(owner);
    }
}
