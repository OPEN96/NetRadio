using System;
using CommunityToolkit.Mvvm.ComponentModel;
using NetRadio.Models;

namespace NetRadio.ViewModels;

public sealed partial class StationEditorViewModel : ViewModelBase
{
    public StationEditorViewModel(RadioStation? station = null)
    {
        IsEditing = station is not null;
        if (station is not null)
        {
            _originalId = station.Id;
            Name = station.Name;
            Url = station.Url;
            Country = station.Country;
            Language = station.Language;
            Tags = station.Tags;
            Homepage = station.Homepage;
        }
    }

    private readonly string? _originalId;

    public bool IsEditing { get; }

    public string Title => IsEditing ? "编辑电台" : "添加电台";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _url = string.Empty;

    [ObservableProperty] private string? _country;
    [ObservableProperty] private string? _language;
    [ObservableProperty] private string? _tags;
    [ObservableProperty] private string? _homepage;

    [ObservableProperty] private string? _errorMessage;

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(Name) &&
        Uri.TryCreate(Url?.Trim(), UriKind.Absolute, out var uri) &&
        uri is { Scheme: "http" or "https" or "mms" or "rtsp" or "rtmp" or "file" };

    public RadioStation Build()
    {
        var station = new RadioStation
        {
            Name = Name.Trim(),
            Url = Url.Trim(),
            Country = string.IsNullOrWhiteSpace(Country) ? null : Country.Trim(),
            Language = string.IsNullOrWhiteSpace(Language) ? null : Language.Trim(),
            Tags = string.IsNullOrWhiteSpace(Tags) ? null : Tags.Trim(),
            Homepage = string.IsNullOrWhiteSpace(Homepage) ? null : Homepage.Trim(),
            IsCustom = true
        };

        if (!string.IsNullOrWhiteSpace(_originalId))
        {
            station.Id = _originalId;
        }

        return station;
    }

    public bool TryBuild(out RadioStation? station)
    {
        if (!IsValid)
        {
            ErrorMessage = Uri.TryCreate(Url?.Trim(), UriKind.Absolute, out _)
                ? "请填写电台名称。"
                : "请填写正确的流媒体地址（以 http:// 或 https:// 开头）。";
            station = null;
            return false;
        }

        ErrorMessage = null;
        station = Build();
        return true;
    }
}
