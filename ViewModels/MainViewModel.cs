using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetRadio.Models;
using NetRadio.Services;

namespace NetRadio.ViewModels;

public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IRadioPlayer _player;
    private readonly IStationDirectory _directory;
    private readonly StationLibrary _library;
    private readonly SettingsService _settingsService;
    private readonly IDialogService _dialogs;

    private readonly Dictionary<string, List<RadioStation>> _cache = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _sleepTimer;

    private CancellationTokenSource? _loadCts;
    private CancellationTokenSource? _saveSettingsCts;
    private DateTime? _sleepUntil;
    private bool _initialized;

    public MainViewModel(
        IRadioPlayer player,
        IStationDirectory directory,
        StationLibrary library,
        SettingsService settingsService,
        IDialogService dialogs)
    {
        _player = player;
        _directory = directory;
        _library = library;
        _settingsService = settingsService;
        _dialogs = dialogs;

        _player.StateChanged += OnPlayerStateChanged;
        _player.NowPlayingChanged += OnNowPlayingChanged;
        _player.BufferingChanged += OnBufferingChanged;
        _player.ErrorOccurred += OnPlayerError;

        Volume = settingsService.Current.Volume;
        IsMuted = settingsService.Current.IsMuted;
        _player.Volume = Volume;
        _player.IsMuted = IsMuted;

        _sleepTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnSleepTimerTick);

        Categories = new ObservableCollection<CategoryItem>(BuildCategories());
        SelectedCategory = Categories.FirstOrDefault();
    }

    // ---------------- 集合与状态 ----------------

    public ObservableCollection<RadioStation> Stations { get; } = new();

    public ObservableCollection<CategoryItem> Categories { get; }

    [ObservableProperty] private CategoryItem? _selectedCategory;

    [ObservableProperty] private RadioStation? _selectedStation;

    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _statusMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCurrentStation))]
    private RadioStation? _currentStation;

    [ObservableProperty] private string? _nowPlaying;

    /// <summary>正在播放但数据不足（网络卡顿），仅作提示，不改变播放状态。</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    private bool _isRebuffering;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPlaying))]
    [NotifyPropertyChangedFor(nameof(StatusText))]
    [NotifyPropertyChangedFor(nameof(CanPause))]
    private PlaybackStatus _status = PlaybackStatus.Idle;

    [ObservableProperty] private int _volume;

    [ObservableProperty] private bool _isMuted;

    [ObservableProperty] private bool _isRecording;

    [ObservableProperty] private string? _recordingFile;

    [ObservableProperty] private bool _isSleepTimerActive;

    [ObservableProperty] private string _sleepTimerText = string.Empty;

    public bool HasCurrentStation => CurrentStation is not null;

    public bool IsPlaying => Status is PlaybackStatus.Playing or PlaybackStatus.Connecting;

    public bool CanPause => Status is PlaybackStatus.Playing or PlaybackStatus.Paused or PlaybackStatus.Connecting;

    public string StatusText =>
        Status == PlaybackStatus.Playing && IsRebuffering
            ? "正在播放（缓冲中…）"
            : PlaybackStatusText.ToDisplayText(Status);

    public int FavoriteCount => _library.Favorites.Count;

    public int CustomCount => _library.CustomStations.Count;

    public string WindowTitle => CurrentStation is null
        ? "NetRadio 网络收音机"
        : $"{CurrentStation.Name} - NetRadio 网络收音机";

    // ---------------- 初始化 ----------------

    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        await LoadStationsAsync(force: true).ConfigureAwait(true);

        var settings = _settingsService.Current;
        if (settings.AutoPlayLastStation && settings.LastStation is { } last &&
            !string.IsNullOrWhiteSpace(last.Url))
        {
            Play(last);
        }
    }

    private static IEnumerable<CategoryItem> BuildCategories()
    {
        const string Star = "M12,17.27L18.18,21l-1.64-7.03L22,9.24l-7.19-.61L12,2 9.19,8.63 2,9.24l5.46,4.73L5.82,21z";
        const string Globe = "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,4A8,8 0 0,1 18.9,7.36C17.7,6.24 15.9,5.5 14,5.5C13.2,5.5 12.4,5.66 11.65,5.95C11.29,6.86 10.78,7.68 10.16,8.38C10.72,9.24 11,10.22 11,11.25V11.5C11,12.33 10.5,13.06 9.78,13.44C9.94,14.5 9.94,15.6 9.72,16.67C10.44,16.88 11.22,17 12,17C16.42,17 20,14.76 20,12C20,7.58 16.42,4 12,4M4,12C4,9.24 5.58,6.85 7.9,5.7C7.97,6.5 8.13,7.28 8.4,8C7.2,8.5 6.5,9.45 6.5,10.5C6.5,11.88 7.62,13 9,13C9.55,13 10.07,12.83 10.5,12.54C10.72,14.12 10.44,15.72 9.74,17.14C6.6,16.71 4,14.6 4,12Z";
        const string Music = "M12,3V13.55A4,4 0 1,0 14,17V7H18V3H12Z";
        const string News = "M20,11H4V8H20M20,15H13V13H20M20,19H13V17H20M11,15H4V13H11M11,19H4V17H11M4,4H20A2,2 0 0,1 22,6V18A2,2 0 0,1 20,20H4A2,2 0 0,1 2,18V6A2,2 0 0,1 4,4Z";
        const string Jazz = "M20.5,9.5C20.5,10.88 19.38,12 18,12C16.62,12 15.5,10.88 15.5,9.5C15.5,8.12 16.62,7 18,7C19.38,7 20.5,8.12 20.5,9.5M5,6.5C5,7.88 3.88,9 2.5,9C1.12,9 0,7.88 0,6.5C0,5.12 1.12,4 2.5,4C3.88,4 5,5.12 5,6.5M9,3V15L12,13V5L15,3H9M16,3V13L19,11V5L22,3H16Z";
        const string Electronic = "M12,3V12.26C11.5,12.09 11,12 10.5,12C8.57,12 7,13.57 7,15.5C7,17.43 8.57,19 10.5,19C12.43,19 14,17.43 14,15.5V6H17.5V3H12Z";
        const string List = "M3,5H9V11H3V5M5,7V9H7V7H5M11,7H21V9H11V7M11,11H21V13H11V11M3,13H9V19H3V13M5,15V17H7V15H5M11,15H21V17H11V15Z";

        yield return new CategoryItem { Name = "热门推荐", IconPath = Star, Query = new StationQuery(Limit: 120) };
        yield return new CategoryItem { Name = "华语电台", IconPath = Globe, Query = new StationQuery(CountryCode: "CN", Limit: 120) };
        yield return new CategoryItem { Name = "音乐", IconPath = Music, Query = new StationQuery(Tag: "music", Limit: 120) };
        yield return new CategoryItem { Name = "新闻 / 资讯", IconPath = News, Query = new StationQuery(Tag: "news", Limit: 120) };
        yield return new CategoryItem { Name = "爵士 / 古典", IconPath = Jazz, Query = new StationQuery(Tag: "jazz", Limit: 120) };
        yield return new CategoryItem { Name = "电子 / 舞曲", IconPath = Electronic, Query = new StationQuery(Tag: "electronic", Limit: 120) };
        yield return new CategoryItem { Name = "我的收藏", IconPath = Star, Local = LocalSource.Favorites };
        yield return new CategoryItem { Name = "我的电台", IconPath = List, Local = LocalSource.Custom };
    }

    // ---------------- 列表加载 ----------------

    partial void OnSearchTextChanged(string value) => _ = DebounceLoadAsync();

    partial void OnSelectedCategoryChanged(CategoryItem? value) => _ = LoadStationsAsync(force: false);

    private async Task DebounceLoadAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        try
        {
            await Task.Delay(450, token).ConfigureAwait(true);
            await LoadStationsAsync(force: true, token).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadStationsAsync(bool force, CancellationToken ct = default)
    {
        var category = SelectedCategory;
        if (category is null) return;

        var keyword = SearchText?.Trim();

        if (category.Local != LocalSource.None)
        {
            var source = category.Local == LocalSource.Favorites ? _library.Favorites : _library.CustomStations;
            var local = source
                .Where(s => string.IsNullOrEmpty(keyword) ||
                            s.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                            (s.Tags?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(CloneForDisplay)
                .ToList();

            ApplyStations(local);
            StatusMessage = local.Count == 0
                ? category.Local == LocalSource.Favorites ? "还没有收藏任何电台" : "还没有添加自定义电台，点击右上角「+」添加"
                : $"共 {local.Count} 个电台";
            return;
        }

        var query = category.Query ?? new StationQuery(Limit: 120);
        if (!string.IsNullOrEmpty(keyword))
        {
            query = query with { Keyword = keyword };
        }

        var key = category.Name + "|" + query;

        if (!force && _cache.TryGetValue(key, out var cached))
        {
            ApplyStations(cached.Select(CloneForDisplay).ToList());
            StatusMessage = $"共 {cached.Count} 个电台";
            return;
        }

        IsBusy = true;
        StatusMessage = "正在获取电台列表…";

        try
        {
            var result = (await _directory.SearchAsync(query, ct).ConfigureAwait(true)).ToList();
            if (result.Count == 0)
            {
                ApplyStations(FallbackStations.Get().ToList());
                StatusMessage = "在线目录暂不可用，已显示内置电台";
            }
            else
            {
                _cache[key] = result;
                ApplyStations(result.Select(CloneForDisplay).ToList());
                StatusMessage = $"共 {result.Count} 个电台";
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusMessage = $"获取电台失败：{ex.Message}";
            ApplyStations(FallbackStations.Get().ToList());
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static RadioStation CloneForDisplay(RadioStation s)
    {
        var copy = s.Clone();
        copy.IsPlaying = false;
        copy.IsBuffering = false;
        copy.NowPlaying = null;
        return copy;
    }

    private void ApplyStations(List<RadioStation> list)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => ApplyStations(list));
            return;
        }

        foreach (var s in list)
        {
            s.IsFavorite = _library.IsFavorite(s.Id);
            s.IsPlaying = s.Id == CurrentStation?.Id;
        }

        Stations.Clear();
        foreach (var s in list)
        {
            Stations.Add(s);
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _cache.Clear();
        await LoadStationsAsync(force: true).ConfigureAwait(true);
    }

    [RelayCommand]
    private void SelectCategory(CategoryItem? category)
    {
        if (category is null || ReferenceEquals(category, SelectedCategory)) return;
        SelectedCategory = category;
    }

    // ---------------- 播放控制 ----------------

    [RelayCommand]
    private void Play(RadioStation? station)
    {
        if (station is null) return;

        foreach (var s in Stations)
        {
            s.IsPlaying = false;
        }

        SelectedStation = station;
        CurrentStation = station;
        station.IsPlaying = true;
        station.IsBuffering = true;
        station.NowPlaying = null;
        NowPlaying = null;

        _settingsService.Current.LastStation = station.Clone();
        ScheduleSaveSettings();

        _player.Play(station);
        _ = _directory.RegisterPlayAsync(station.StationUuid);

        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (CurrentStation is null)
        {
            if (SelectedStation is not null)
            {
                Play(SelectedStation);
            }
            else if (Stations.Count > 0)
            {
                Play(Stations[0]);
            }

            return;
        }

        _player.TogglePause();
    }

    [RelayCommand]
    private void Stop()
    {
        if (IsRecording) ToggleRecording();
        _player.Stop();
        CurrentStation = null;
        NowPlaying = null;
        foreach (var s in Stations) s.IsPlaying = false;
        OnPropertyChanged(nameof(WindowTitle));
    }

    partial void OnVolumeChanged(int value)
    {
        _player.Volume = value;
        _settingsService.Current.Volume = value;
        ScheduleSaveSettings();
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _player.IsMuted = IsMuted;
        _settingsService.Current.IsMuted = IsMuted;
        ScheduleSaveSettings();
    }

    // ---------------- 收藏 / 自定义电台 ----------------

    [RelayCommand]
    private void ToggleFavorite(RadioStation? station)
    {
        if (station is null) return;

        var nowFavorite = _library.ToggleFavorite(station);
        StatusMessage = nowFavorite ? $"已收藏「{station.Name}」" : $"已取消收藏「{station.Name}」";
        OnPropertyChanged(nameof(FavoriteCount));

        if (SelectedCategory?.Local == LocalSource.Favorites && !nowFavorite)
        {
            Stations.Remove(station);
        }
    }

    [RelayCommand]
    private async Task AddStationAsync()
    {
        var station = await _dialogs.ShowStationEditorAsync().ConfigureAwait(true);
        if (station is null) return;

        EnsureId(station);
        _library.AddOrUpdateCustom(station);
        OnPropertyChanged(nameof(CustomCount));
        StatusMessage = $"已添加「{station.Name}」";

        if (SelectedCategory?.Local == LocalSource.Custom)
        {
            await LoadStationsAsync(force: true).ConfigureAwait(true);
        }
    }

    [RelayCommand]
    private async Task EditStationAsync(RadioStation? station)
    {
        if (station is null) return;

        var updated = await _dialogs.ShowStationEditorAsync(station).ConfigureAwait(true);
        if (updated is null) return;

        updated.Id = station.Id;
        _library.AddOrUpdateCustom(updated);
        StatusMessage = "已保存修改";
        await LoadStationsAsync(force: true).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task DeleteStationAsync(RadioStation? station)
    {
        if (station is null) return;

        _library.RemoveCustom(station);
        OnPropertyChanged(nameof(CustomCount));
        OnPropertyChanged(nameof(FavoriteCount));
        StatusMessage = $"已删除「{station.Name}」";
        await LoadStationsAsync(force: true).ConfigureAwait(true);
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var saved = await _dialogs.ShowSettingsAsync().ConfigureAwait(true);
        if (!saved) return;

        var settings = _settingsService.Current;
        Volume = settings.Volume;
        IsMuted = settings.IsMuted;
        _player.Volume = Volume;
        _player.IsMuted = IsMuted;

        // 主题 / 网络缓冲等立即生效
        AppServices.ApplyTheme?.Invoke(settings.Theme);
        StatusMessage = "设置已保存";
    }

    private static void EnsureId(RadioStation station)
    {
        if (string.IsNullOrWhiteSpace(station.Id) || station.Id.StartsWith("url:", StringComparison.Ordinal))
        {
            station.Id = string.IsNullOrWhiteSpace(station.StationUuid)
                ? Guid.NewGuid().ToString("N")
                : "uuid:" + station.StationUuid;
        }
    }

    // ---------------- 录音 ----------------

    [RelayCommand]
    private void ToggleRecording()
    {
        if (CurrentStation is null)
        {
            StatusMessage = "请先播放一个电台，再开始录音";
            return;
        }

        if (IsRecording)
        {
            _player.SetRecording(false, null);
            IsRecording = false;
            StatusMessage = $"录音已保存到：{RecordingFile}";
            RecordingFile = null;
            return;
        }

        var folder = _settingsService.Current.RecordingsFolder;
        try
        {
            Directory.CreateDirectory(folder);
            var safeName = string.Join("_", CurrentStation.Name.Split(Path.GetInvalidFileNameChars()));
            var file = Path.Combine(folder, $"{safeName}_{DateTime.Now:yyyyMMdd_HHmmss}.mp3");
            _player.SetRecording(true, file);
            RecordingFile = file;
            IsRecording = true;
            StatusMessage = $"正在录音：{file}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"无法开始录音：{ex.Message}";
        }
    }

    // ---------------- 睡眠定时器 ----------------

    [RelayCommand]
    private void SetSleepTimer(string? minutes)
    {
        if (!int.TryParse(minutes, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            CancelSleepTimer();
            return;
        }

        _sleepUntil = DateTime.Now.AddMinutes(value);
        IsSleepTimerActive = true;
        UpdateSleepText();
        _sleepTimer.Start();
        StatusMessage = $"睡眠定时器已设置：{value} 分钟后停止播放";
    }

    private void CancelSleepTimer()
    {
        _sleepUntil = null;
        IsSleepTimerActive = false;
        SleepTimerText = string.Empty;
        _sleepTimer.Stop();
    }

    private void OnSleepTimerTick(object? sender, EventArgs e)
    {
        if (_sleepUntil is null) return;

        var left = _sleepUntil.Value - DateTime.Now;
        if (left <= TimeSpan.Zero)
        {
            CancelSleepTimer();
            Stop();
            StatusMessage = "睡眠定时器已停止播放";
            return;
        }

        UpdateSleepText(left);
    }

    private void UpdateSleepText(TimeSpan? remaining = null)
    {
        var left = remaining ?? (_sleepUntil - DateTime.Now) ?? TimeSpan.Zero;
        SleepTimerText = $"{(int)left.TotalMinutes:00}:{left.Seconds:00}";
    }

    // ---------------- 播放器事件 ----------------

    private void OnPlayerStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Status = e.Status;

            if (e.Station is not null && CurrentStation is null)
            {
                CurrentStation = e.Station;
            }

            var current = CurrentStation;
            if (current is not null)
            {
                current.IsBuffering = e.Status == PlaybackStatus.Connecting;
                current.IsPlaying = e.Status == PlaybackStatus.Playing;
            }

            if (e.Status == PlaybackStatus.Idle)
            {
                NowPlaying = null;
            }

            if (e.Status == PlaybackStatus.Error && IsRecording)
            {
                IsRecording = false;
                RecordingFile = null;
            }

            OnPropertyChanged(nameof(WindowTitle));
        });
    }

    private void OnNowPlayingChanged(object? sender, string? text)
    {
        Dispatcher.UIThread.Post(() =>
        {
            NowPlaying = text;
            if (CurrentStation is not null)
            {
                CurrentStation.NowPlaying = text;
            }
        });
    }

    private void OnBufferingChanged(object? sender, bool isBuffering)
    {
        Dispatcher.UIThread.Post(() => IsRebuffering = isBuffering);
    }

    private void OnPlayerError(object? sender, string? message)
    {
        Dispatcher.UIThread.Post(() => StatusMessage = message ?? "播放出错");
    }

    // ---------------- 设置保存去抖 ----------------

    private void ScheduleSaveSettings()
    {
        _saveSettingsCts?.Cancel();
        _saveSettingsCts = new CancellationTokenSource();
        var token = _saveSettingsCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(600, token).ConfigureAwait(false);
                if (token.IsCancellationRequested) return;
                _settingsService.Save();
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    public void OnMainWindowClosing()
    {
        _settingsService.Current.Volume = Volume;
        _settingsService.Current.IsMuted = IsMuted;
        _settingsService.Save();
        _library.Save();
    }

    public void Dispose()
    {
        _player.StateChanged -= OnPlayerStateChanged;
        _player.NowPlayingChanged -= OnNowPlayingChanged;
        _player.BufferingChanged -= OnBufferingChanged;
        _player.ErrorOccurred -= OnPlayerError;

        _sleepTimer.Stop();
        _loadCts?.Cancel();
        _saveSettingsCts?.Cancel();
        _player.Dispose();
    }
}
