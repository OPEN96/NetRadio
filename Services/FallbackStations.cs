using System.Collections.Generic;
using NetRadio.Models;

namespace NetRadio.Services;

/// <summary>
/// 首次启动或完全离线时使用的内置电台，保证"装上就能听"。
/// 这些地址均为长期稳定的公开流。
/// </summary>
public static class FallbackStations
{
    public static IReadOnlyList<RadioStation> Get()
    {
        return new List<RadioStation>
        {
            new("SomaFM Groove Salad", "https://ice1.somafm.com/groovesalad-128-mp3")
            {
                Id = "url:https://ice1.somafm.com/groovesalad-128-mp3",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "ambient,chillout,electronic"
            },
            new("SomaFM Drone Zone", "https://ice1.somafm.com/dronezone-128-mp3")
            {
                Id = "url:https://ice1.somafm.com/dronezone-128-mp3",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "ambient,drone"
            },
            new("SomaFM Indie Pop Rocks", "https://ice1.somafm.com/indiepop-128-mp3")
            {
                Id = "url:https://ice1.somafm.com/indiepop-128-mp3",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "indie,pop"
            },
            new("SomaFM Underground 80s", "https://ice1.somafm.com/u80s-128-mp3")
            {
                Id = "url:https://ice1.somafm.com/u80s-128-mp3",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "80s,synthpop"
            },
            new("Radio Paradise Main Mix", "https://stream.radioparadise.com/mp3-192")
            {
                Id = "url:https://stream.radioparadise.com/mp3-192",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 192,
                Tags = "rock,eclectic,adult album alternative"
            },
            new("Radio Paradise Mellow Mix", "https://stream.radioparadise.com/mellow-192")
            {
                Id = "url:https://stream.radioparadise.com/mellow-192",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 192,
                Tags = "mellow,acoustic"
            },
            new("Radio Paradise Rock Mix", "https://stream.radioparadise.com/rock-192")
            {
                Id = "url:https://stream.radioparadise.com/rock-192",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 192,
                Tags = "rock"
            },
            new("Radio Paradise Global Mix", "https://stream.radioparadise.com/global-192")
            {
                Id = "url:https://stream.radioparadise.com/global-192",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 192,
                Tags = "world music"
            },
            new("Venice Classic Radio", "https://live.veniceclassicradio.eu/128mp3")
            {
                Id = "url:https://live.veniceclassicradio.eu/128mp3",
                Country = "Italy", CountryCode = "IT", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "classical"
            },
            new("Smooth Jazz Florida", "https://smoothjazz.stream.publicradio.org/smoothjazz.mp3")
            {
                Id = "url:https://smoothjazz.stream.publicradio.org/smoothjazz.mp3",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "jazz,smooth jazz"
            },
            new("Jazz24", "https://live.wostreaming.net/direct/ppm-jazz24aacimptest-ibc1")
            {
                Id = "url:https://live.wostreaming.net/direct/ppm-jazz24aacimptest-ibc1",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "AAC", Bitrate = 64,
                Tags = "jazz"
            },
            new("Classic FM (UK)", "https://media-ice.musicradio.com/ClassicFMMP3")
            {
                Id = "url:https://media-ice.musicradio.com/ClassicFMMP3",
                Country = "United Kingdom", CountryCode = "GB", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "classical"
            },
            new("KEXP 90.3 FM Seattle", "https://kexp-mp3-128.streamguys1.com/kexp128.mp3")
            {
                Id = "url:https://kexp-mp3-128.streamguys1.com/kexp128.mp3",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "indie,alternative,eclectic"
            },
            new("WWOZ New Orleans", "https://wwoz-sc.streamguys1.com/wwoz-hi.mp3")
            {
                Id = "url:https://wwoz-sc.streamguys1.com/wwoz-hi.mp3",
                Country = "USA", CountryCode = "US", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "jazz,blues,world"
            },
            new("Deutschlandfunk", "https://st01.sslstream.dlf.de/dlf/01/128/mp3/stream.mp3")
            {
                Id = "url:https://st01.sslstream.dlf.de/dlf/01/128/mp3/stream.mp3",
                Country = "Germany", CountryCode = "DE", Language = "german", Codec = "MP3", Bitrate = 128,
                Tags = "news,talk"
            },
            new("France Info", "https://stream.radiofrance.fr/franceinfo/franceinfo_hifi.m3u8")
            {
                Id = "url:https://stream.radiofrance.fr/franceinfo/franceinfo_hifi.m3u8",
                Country = "France", CountryCode = "FR", Language = "french", Codec = "AAC", Bitrate = 128,
                Tags = "news"
            },
            new("NHK Radio 1 (Tokyo)", "https://radio-stream.nhk.jp/hls/live/2023509/nhkradiruakr1/master.m3u8")
            {
                Id = "url:https://radio-stream.nhk.jp/hls/live/2023509/nhkradiruakr1/master.m3u8",
                Country = "Japan", CountryCode = "JP", Language = "japanese", Codec = "AAC", Bitrate = 64,
                Tags = "news,talk"
            },
            new("SBS Radio 2 (Australia)", "https://ice60.securenetsystems.net/SBS2")
            {
                Id = "url:https://ice60.securenetsystems.net/SBS2",
                Country = "Australia", CountryCode = "AU", Language = "english", Codec = "MP3", Bitrate = 128,
                Tags = "news,talk"
            }
        };
    }
}
