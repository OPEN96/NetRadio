namespace NetRadio.Services;

public sealed class SettingsService
{
    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        Current = JsonStore.Load<AppSettings>(AppPaths.SettingsFile) ?? new AppSettings();
        if (Current.Volume <= 0) Current.Volume = 80;
        if (Current.NetworkCachingMs <= 0) Current.NetworkCachingMs = 3000;
        if (string.IsNullOrWhiteSpace(Current.RecordingsFolder))
            Current.RecordingsFolder = AppPaths.DefaultRecordingsDirectory;
    }

    public void Save() => JsonStore.Save(AppPaths.SettingsFile, Current);
}
