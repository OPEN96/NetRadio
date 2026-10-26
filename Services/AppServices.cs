using System;

namespace NetRadio.Services;

/// <summary>少量跨层的运行时钩子（主题切换等），避免在 ViewModel 里直接依赖 Application。</summary>
public static class AppServices
{
    public static Action<string>? ApplyTheme { get; set; }
}
