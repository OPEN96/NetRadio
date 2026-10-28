using System;
using System.IO;

namespace NetRadio.Services;

/// <summary>跨平台的数据/配置目录。Windows：%APPDATA%\NetRadio；Linux：~/.config/NetRadio。</summary>
public static class AppPaths
{
    private static string? _appDataOverride;
    private static string? _recordingsOverride;

    /// <summary>
    /// 覆盖数据目录。Android 等平台在启动时调用（<c>Context.FilesDir</c>），
    /// 桌面端保持默认值（用户配置目录）。
    /// </summary>
    public static void Override(string? appDataDirectory, string? recordingsDirectory = null)
    {
        _appDataOverride = appDataDirectory;
        _recordingsOverride = recordingsDirectory;
    }

    public static string AppDataDirectory { get; private set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetRadio");

    public static string FavoritesFile => Path.Combine(AppDataDirectory, "favorites.json");

    public static string CustomStationsFile => Path.Combine(AppDataDirectory, "stations.json");

    public static string SettingsFile => Path.Combine(AppDataDirectory, "settings.json");

    /// <summary>默认录音输出目录：~/Music/NetRadio（Windows 为 用户\Music\NetRadio）。</summary>
    public static string DefaultRecordingsDirectory { get; private set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "NetRadio");

    /// <summary>把 <see cref="Override"/> 指定的目录真正应用上去（保持属性可读且只算一次）。</summary>
    public static void EnsureCreated()
    {
        if (_appDataOverride is { Length: > 0 })
        {
            AppDataDirectory = _appDataOverride;
        }

        if (_recordingsOverride is { Length: > 0 })
        {
            DefaultRecordingsDirectory = _recordingsOverride;
        }

        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(DefaultRecordingsDirectory);
    }

    public static bool IsLinux => OperatingSystem.IsLinux();

    public static bool IsWindows => OperatingSystem.IsWindows();
}
