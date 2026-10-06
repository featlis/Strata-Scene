using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using StrataScene.Platform;

namespace StrataScene.App.Hud;

public class NoActivateWindow : Window
{
    private HwndSource? _hwndSource;
    private IntPtr _hwnd;

    public IntPtr Handle => _hwnd;

    public NoActivateWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var helper = new WindowInteropHelper(this);
        _hwnd = helper.Handle;

        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(WndProc);

        // Apply Extended Styles: WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST
        WindowStyles.ApplyNoActivateStyles(_hwnd);
    }

    protected virtual IntPtr OnMouseActivate()
    {
        return (IntPtr)Win32Constants.MA_NOACTIVATE;
    }

    public void PositionPhysical(int x, int y, int width, int height)
    {
        if (_hwnd == IntPtr.Zero) return;
        WindowStyles.PositionTopmost(_hwnd, x, y, width, height);
    }

    public void EnsureTopmost()
    {
        if (_hwnd == IntPtr.Zero) return;
        WindowStyles.EnsureTopmost(_hwnd);
    }

    public void ShowNoActivate()
    {
        if (_hwnd == IntPtr.Zero) return;
        WindowStyles.ShowNoActivate(_hwnd);
    }

    public void HideWindow()
    {
        if (_hwnd == IntPtr.Zero) return;
        WindowStyles.Hide(_hwnd);
    }

    protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Constants.WM_MOUSEACTIVATE)
        {
            handled = true;
            return OnMouseActivate();
        }

        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        _hwndSource?.RemoveHook(WndProc);
        base.OnClosed(e);
    }
}
