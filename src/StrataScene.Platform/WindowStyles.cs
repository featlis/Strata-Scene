using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace StrataScene.Platform;

public static class WindowStyles
{
    private static readonly HWND HWND_TOPMOST = new(-1);

    public static void ApplyNoActivateStyles(IntPtr hwndHandle)
    {
        var hwnd = (HWND)hwndHandle;
        var exStyle = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
        exStyle |= Win32Constants.WS_EX_NOACTIVATE | Win32Constants.WS_EX_TOOLWINDOW | Win32Constants.WS_EX_TOPMOST;
        PInvoke.SetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, exStyle);
    }

    public static void PositionTopmost(IntPtr hwndHandle, int x, int y, int width, int height)
    {
        var hwnd = (HWND)hwndHandle;
        PInvoke.SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            x,
            y,
            width,
            height,
            (SET_WINDOW_POS_FLAGS)(Win32Constants.SWP_NOACTIVATE | Win32Constants.SWP_SHOWWINDOW));
    }

    public static void EnsureTopmost(IntPtr hwndHandle)
    {
        var hwnd = (HWND)hwndHandle;
        PInvoke.SetWindowPos(
            hwnd,
            HWND_TOPMOST,
            0,
            0,
            0,
            0,
            (SET_WINDOW_POS_FLAGS)(Win32Constants.SWP_NOMOVE | Win32Constants.SWP_NOSIZE | Win32Constants.SWP_NOACTIVATE | Win32Constants.SWP_NOOWNERZORDER));
    }

    public static void ShowNoActivate(IntPtr hwndHandle)
    {
        var hwnd = (HWND)hwndHandle;
        PInvoke.ShowWindowAsync(hwnd, SHOW_WINDOW_CMD.SW_SHOWNOACTIVATE);
    }

    public static void Hide(IntPtr hwndHandle)
    {
        var hwnd = (HWND)hwndHandle;
        PInvoke.ShowWindowAsync(hwnd, SHOW_WINDOW_CMD.SW_HIDE);
    }
}
