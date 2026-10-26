using System.Threading.Tasks;
using NetRadio.Models;

namespace NetRadio.Services;

/// <summary>UI 层提供的对话框能力，供 ViewModel 调用，保持 ViewModel 不依赖具体窗口类型。</summary>
public interface IDialogService
{
    /// <summary>打开"添加 / 编辑电台"对话框，返回用户确认后的电台；取消返回 null。</summary>
    Task<RadioStation?> ShowStationEditorAsync(RadioStation? station = null);

    /// <summary>打开设置对话框，返回 true 表示用户保存了设置。</summary>
    Task<bool> ShowSettingsAsync();

    void ShowMessage(string title, string message);
}
