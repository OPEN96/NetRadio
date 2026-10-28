using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using NetRadio.Services;

namespace NetRadio.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettings _settings;

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        // 设置里存的是 Light/Dark/Default 这类键，界面下拉用的是中文显示名，需先转换
        _theme = ThemeDisplayName(settings.Theme);
        _autoPlayLastStation = settings.AutoPlayLastStation;
        _networkCachingMs = settings.NetworkCachingMs;
        _recordingsFolder = settings.RecordingsFolder;
        _minimizeToTray = settings.MinimizeToTray;
        _closeToTray = settings.CloseToTray;
        _miniPlayerAlwaysOnTop = settings.MiniPlayerAlwaysOnTop;
    }

    public IReadOnlyList<string> Themes { get; } = new[] { "跟随系统", "浅色", "深色" };

    [ObservableProperty] private string _theme = "Default";
    [ObservableProperty] private bool _autoPlayLastStation;
    [ObservableProperty] private int _networkCachingMs = 3000;
    [ObservableProperty] private string _recordingsFolder = string.Empty;
    [ObservableProperty] private bool _minimizeToTray = true;
    [ObservableProperty] private bool _closeToTray = true;
    [ObservableProperty] private bool _miniPlayerAlwaysOnTop = true;

    /// <summary>把界面上的选择写回设置对象。</summary>
    public void Apply()
    {
        _settings.Theme = SelectedThemeKey;
        _settings.AutoPlayLastStation = AutoPlayLastStation;
        _settings.NetworkCachingMs = NetworkCachingMs is < 300 or > 20000 ? 3000 : NetworkCachingMs;
        _settings.RecordingsFolder = string.IsNullOrWhiteSpace(RecordingsFolder)
            ? AppPaths.DefaultRecordingsDirectory
            : RecordingsFolder.Trim();
        _settings.MinimizeToTray = MinimizeToTray;
        _settings.CloseToTray = CloseToTray;
        _settings.MiniPlayerAlwaysOnTop = MiniPlayerAlwaysOnTop;
    }

    public string SelectedThemeKey => Theme switch
    {
        "浅色" => "Light",
        "深色" => "Dark",
        _ => "Default"
    };

    public static string ThemeDisplayName(string key) => key switch
    {
        "Light" => "浅色",
        "Dark" => "深色",
        _ => "跟随系统"
    };
}
