using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using NetRadio.Droid.Playback;
using NetRadio.Droid.Services;
using NetRadio.Droid.Views;
using NetRadio.Services;
using NetRadio.ViewModels;

namespace NetRadio.Droid;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppPaths.EnsureCreated();

        var settingsService = new SettingsService();
        settingsService.Load();

        var library = new StationLibrary();
        library.Load();

        var directory = new RadioBrowserService();
        var dialogs = new AndroidDialogService();
        var shell = new AndroidShellService();
        var player = PlaybackHub.Player;

        var viewModel = new MainViewModel(player, directory, library, settingsService, dialogs, shell);
        PlaybackHub.ViewModel = viewModel;

        AppServices.ApplyTheme = ApplyTheme;
        ApplyTheme(settingsService.Current.Theme);

        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
        {
            single.MainView = new MobileMainView { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();

        // 首屏渲染完成后再拉取电台列表，避免启动卡顿
        Dispatcher.UIThread.Post(
            async () =>
            {
                try
                {
                    await viewModel.InitializeAsync();
                }
                catch (Exception ex)
                {
                    viewModel.StatusMessage = $"初始化失败：{ex.Message}";
                }
            },
            DispatcherPriority.Background);
    }

    private static void ApplyTheme(string themeKey)
    {
        if (Current is null) return;

        Current.RequestedThemeVariant = AppSettings.ParseTheme(themeKey) switch
        {
            ThemeVariantKind.Light => ThemeVariant.Light,
            ThemeVariantKind.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };
    }
}
