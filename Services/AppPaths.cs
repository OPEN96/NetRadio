using System;
using System.IO;

namespace NetRadio.Services;

/// <summary>跨平台的数据/配置目录。Windows：%APPDATA%\NetRadio；Linux：~/.config/NetRadio。</summary>
public static class AppPaths
{
    public static string AppDataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NetRadio");

    public static string FavoritesFile => Path.Combine(AppDataDirectory, "favorites.json");

    public static string CustomStationsFile => Path.Combine(AppDataDirectory, "stations.json");

    public static string SettingsFile => Path.Combine(AppDataDirectory, "settings.json");

    /// <summary>默认录音输出目录：~/Music/NetRadio（Windows 为 用户\Music\NetRadio）。</summary>
    public static string DefaultRecordingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "NetRadio");

    public static void EnsureCreated()
    {
        Directory.CreateDirectory(AppDataDirectory);
        Directory.CreateDirectory(DefaultRecordingsDirectory);
    }

    public static bool IsLinux => OperatingSystem.IsLinux();

    public static bool IsWindows => OperatingSystem.IsWindows();
}
