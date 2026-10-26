using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NetRadio.Models;

namespace NetRadio.Services;

/// <summary>本地电台库：收藏 + 自定义电台，JSON 持久化。</summary>
public sealed class StationLibrary
{
    public ObservableCollection<RadioStation> Favorites { get; } = new();
    public ObservableCollection<RadioStation> CustomStations { get; } = new();

    public void Load()
    {
        var favorites = JsonStore.Load<List<RadioStation>>(AppPaths.FavoritesFile);
        if (favorites is { Count: > 0 })
        {
            foreach (var s in favorites.Where(s => !string.IsNullOrWhiteSpace(s.Url)))
            {
                s.IsFavorite = true;
                Favorites.Add(s);
            }
        }

        var custom = JsonStore.Load<List<RadioStation>>(AppPaths.CustomStationsFile);
        if (custom is { Count: > 0 })
        {
            foreach (var s in custom.Where(s => !string.IsNullOrWhiteSpace(s.Url)))
            {
                s.IsCustom = true;
                CustomStations.Add(s);
            }
        }
    }

    public void Save()
    {
        JsonStore.Save(AppPaths.FavoritesFile, Favorites.ToList());
        JsonStore.Save(AppPaths.CustomStationsFile, CustomStations.ToList());
    }

    public bool IsFavorite(string? id)
        => !string.IsNullOrEmpty(id) && Favorites.Any(x => string.Equals(x.Id, id, StringComparison.Ordinal));

    /// <summary>切换收藏，返回切换后的状态。</summary>
    public bool ToggleFavorite(RadioStation station)
    {
        if (station is null) return false;

        var existing = Favorites.FirstOrDefault(x => string.Equals(x.Id, station.Id, StringComparison.Ordinal));
        if (existing is not null)
        {
            Favorites.Remove(existing);
            station.IsFavorite = false;
            Save();
            return false;
        }

        var copy = station.Clone();
        copy.IsFavorite = true;
        copy.IsPlaying = false;
        copy.IsBuffering = false;
        copy.NowPlaying = null;
        Favorites.Add(copy);
        station.IsFavorite = true;
        Save();
        return true;
    }

    public void AddOrUpdateCustom(RadioStation station)
    {
        var existing = CustomStations.FirstOrDefault(x => string.Equals(x.Id, station.Id, StringComparison.Ordinal));
        if (existing is not null)
        {
            var index = CustomStations.IndexOf(existing);
            station.IsCustom = true;
            CustomStations[index] = station;
        }
        else
        {
            station.IsCustom = true;
            CustomStations.Add(station);
        }

        Save();
    }

    public void RemoveCustom(RadioStation station)
    {
        var existing = CustomStations.FirstOrDefault(x => string.Equals(x.Id, station.Id, StringComparison.Ordinal));
        if (existing is null) return;

        CustomStations.Remove(existing);
        if (IsFavorite(station.Id))
        {
            ToggleFavorite(station);
        }

        Save();
    }

    public IReadOnlyList<RadioStation> AllLocal => Favorites.Concat(CustomStations).ToList();
}
