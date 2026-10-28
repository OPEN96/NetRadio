using System;
using Android.App;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using NetRadio.Droid.Playback;

namespace NetRadio.Droid;

/// <summary>
/// Android 进程入口。在 Avalonia 初始化之前把数据目录指向应用私有目录，
/// 并初始化播放器上下文（前台服务需要 ApplicationContext）。
/// </summary>
[Application(Label = "NetRadio", Icon = "@drawable/Icon")]
public class AndroidApp : AvaloniaAndroidApplication<App>
{
    protected AndroidApp(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    public override void OnCreate()
    {
        var files = FilesDir?.AbsolutePath;
        if (!string.IsNullOrEmpty(files))
        {
            // 配置与收藏放在私有目录，录音目录同理（Android 暂不支持录音，保留占位）
            NetRadio.Services.AppPaths.Override(
                System.IO.Path.Combine(files, "config"),
                System.IO.Path.Combine(files, "recordings"));
        }

        PlaybackHub.Initialize(this);

        base.OnCreate();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .LogToTrace();
    }
}
