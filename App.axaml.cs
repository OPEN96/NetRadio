using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using NetRadio.Services;
using NetRadio.ViewModels;
using NetRadio.Views;

namespace NetRadio;

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

        IRadioPlayer player;
        try
        {
            player = new VlcRadioPlayer(settingsService.Current.NetworkCachingMs);
        }
        catch (Exception ex)
        {
            player = new NullRadioPlayer(BuildMissingBackendMessage(ex));
        }

        var directory = new RadioBrowserService();
        var dialogs = new DialogService(settingsService);
        var viewModel = new MainViewModel(player, directory, library, settingsService, dialogs);

        AppServices.ApplyTheme = ApplyTheme;
        ApplyTheme(settingsService.Current.Theme);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow { DataContext = viewModel };
            window.Opened += OnWindowOpened;
            desktop.MainWindow = window;

            desktop.ShutdownRequested += (_, _) =>
            {
                viewModel.Dispose();
                directory.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
        return;

        async void OnWindowOpened(object? sender, EventArgs e)
        {
            try
            {
                await viewModel.InitializeAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                viewModel.StatusMessage = $"初始化失败：{ex.Message}";
            }
        }
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

    private static string BuildMissingBackendMessage(Exception ex)
    {
        var hint = AppPaths.IsLinux
            ? "请先安装系统解码库：sudo apt install vlc libvlc-dev （Fedora：sudo dnf install vlc-devel）"
            : AppPaths.IsWindows
                ? "请确认应用目录下的 libvlc 原生文件完整。"
                : "请安装 libvlc 后重试。";

        return $"音频引擎初始化失败：{ex.Message}\n{hint}";
    }
}
