using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.VisualTree;

namespace NetRadio.Droid.Views;

public partial class MobileMainView : UserControl
{
    // 基准内边距：状态栏 / 导航栏的真实高度由安全区动态叠加。
    private static readonly Thickness HeaderBasePadding = new(16, 12, 16, 10);
    private static readonly Thickness PlayerBarBasePadding = new(12, 9, 12, 12);

    private IInsetsManager? _insets;

    public MobileMainView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Android 15（API 35）起强制沉浸式布局，内容会绘制到状态栏与导航栏之下，
    /// 需要按平台上报的安全区自行留出内边距。
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _insets = TopLevel.GetTopLevel(this)?.InsetsManager;
        if (_insets is null)
        {
            ApplySafeArea(default);
            return;
        }

        _insets.SafeAreaChanged += OnSafeAreaChanged;
        ApplySafeArea(_insets.SafeAreaPadding);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_insets is not null)
        {
            _insets.SafeAreaChanged -= OnSafeAreaChanged;
            _insets = null;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => ApplySafeArea(e.SafeAreaPadding);

    private void ApplySafeArea(Thickness safe)
    {
        RootGrid.Margin = new Thickness(safe.Left, 0, safe.Right, 0);

        HeaderBar.Padding = new Thickness(
            HeaderBasePadding.Left + safe.Left,
            HeaderBasePadding.Top + safe.Top,
            HeaderBasePadding.Right + safe.Right,
            HeaderBasePadding.Bottom);

        PlayerBar.Padding = new Thickness(
            PlayerBarBasePadding.Left + safe.Left,
            PlayerBarBasePadding.Top,
            PlayerBarBasePadding.Right + safe.Right,
            PlayerBarBasePadding.Bottom + safe.Bottom);
    }
}
