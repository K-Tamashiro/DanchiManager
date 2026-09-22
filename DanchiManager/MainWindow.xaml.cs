using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DanchiManager;

public partial class MainWindow : Window
{
    const int WmMouseHWheel = 0x020E;
    const double HorizontalWheelStep = 48;
    HwndSource? _source;

    public MainWindow()
    {
        InitializeComponent();
        RoomScroll.PreviewMouseWheel += OnRoomMouseWheel;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _source = PresentationSource.FromVisual(this) as HwndSource;
        _source?.AddHook(OnWindowMessage);
    }

    protected override void OnClosed(EventArgs e)
    {
        _source?.RemoveHook(OnWindowMessage);
        _source = null;
        RoomScroll.PreviewMouseWheel -= OnRoomMouseWheel;
        base.OnClosed(e);
    }

    void OnRoomMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Shift) == 0) return;
        ScrollRoomsHorizontally(-e.Delta);
        e.Handled = true;
    }

    nint OnWindowMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message != WmMouseHWheel || !IsEnabled || !RoomScroll.IsVisible) return nint.Zero;
        // WM_MOUSEHWHEELの座標はスクリーン座標。負座標のモニターにも対応する。
        var coordinates = lParam.ToInt64();
        var screenPoint = new Point(unchecked((short)coordinates), unchecked((short)(coordinates >> 16)));
        var point = RoomScroll.PointFromScreen(screenPoint);
        if (!new Rect(RoomScroll.RenderSize).Contains(point)) return nint.Zero;

        // サイドホイールは正が右。120未満の高精度入力も距離に反映する。
        var delta = unchecked((short)(wParam.ToInt64() >> 16));
        ScrollRoomsHorizontally(delta);
        handled = true;
        return nint.Zero;
    }

    void ScrollRoomsHorizontally(int delta)
    {
        if (RoomScroll.ScrollableWidth <= 0) return;
        var offset = RoomScroll.HorizontalOffset + delta / 120d * HorizontalWheelStep;
        RoomScroll.ScrollToHorizontalOffset(Math.Clamp(offset, 0, RoomScroll.ScrollableWidth));
    }
}
