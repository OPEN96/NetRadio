using System.Threading.Tasks;
using Android.Widget;
using Avalonia.Threading;
using NetRadio.Models;
using NetRadio.Services;

namespace NetRadio.Droid.Services;

/// <summary>
/// 移动端对话框：轻提示用 Toast，设置 / 编辑电台这类桌面式弹窗暂不提供
/// （移动端界面上也不放置对应入口）。
/// </summary>
public sealed class AndroidDialogService : IDialogService
{
    public Task<RadioStation?> ShowStationEditorAsync(RadioStation? station = null)
        => Task.FromResult<RadioStation?>(null);

    public Task<bool> ShowSettingsAsync()
        => Task.FromResult(false);

    public void ShowMessage(string title, string message)
    {
        var context = Playback.PlaybackHub.AppContext;
        if (context is null) return;

        Dispatcher.UIThread.Post(() =>
        {
            var text = string.IsNullOrWhiteSpace(title) ? message : $"{title}：{message}";
            Toast.MakeText(context, text, ToastLength.Short)?.Show();
        });
    }
}
