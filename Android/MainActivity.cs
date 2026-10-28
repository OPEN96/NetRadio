using System;
using Android.App;
using Android;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace NetRadio.Droid;

[Activity(
    Label = "NetRadio",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/Icon",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode | ConfigChanges.SmallestScreenSize)]
public class MainActivity : AvaloniaMainActivity
{
    private const int NotificationPermissionRequest = 1001;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        RequestNotificationPermission();
    }

    /// <summary>Android 13+ 需要运行时授权，否则媒体通知不会显示。</summary>
    private void RequestNotificationPermission()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33)) return;

        if (CheckSelfPermission(Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
        {
            RequestPermissions(new[] { Manifest.Permission.PostNotifications }, NotificationPermissionRequest);
        }
    }
}
