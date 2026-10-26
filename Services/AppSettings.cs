using CommunityToolkit.Mvvm.ComponentModel;
using NetRadio.Models;

namespace NetRadio.Services;

public sealed partial class AppSettings : ObservableObject
{
    [ObservableProperty] private int _volume = 80;
    [ObservableProperty] private bool _isMuted;

    /// <summary>Default / Light / Dark</summary>
    [ObservableProperty] private string _theme = "Default";

    [ObservableProperty] private bool _autoPlayLastStation = true;

    [ObservableProperty] private int _networkCachingMs = 3000;

    [ObservableProperty] private string _recordingsFolder = AppPaths.DefaultRecordingsDirectory;

    /// <summary>上次播放的电台，用于启动时续播。</summary>
    [ObservableProperty] private RadioStation? _lastStation;

    public static ThemeVariantKind ParseTheme(string? value) => value switch
    {
        "Light" => ThemeVariantKind.Light,
        "Dark" => ThemeVariantKind.Dark,
        _ => ThemeVariantKind.Default
    };
}

public enum ThemeVariantKind
{
    Default,
    Light,
    Dark
}
