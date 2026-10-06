using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

namespace StrataScene.Platform;

public static class DwmHelper
{
    public static unsafe void ApplyModernWindowDecorations(IntPtr hwndHandle, bool darkMode = true, bool roundCorners = true)
    {
        if (hwndHandle == IntPtr.Zero) return;
        var hwnd = (HWND)hwndHandle;

        if (darkMode)
        {
            uint useDarkMode = 1;
            PInvoke.DwmSetWindowAttribute(
                hwnd,
                (DWMWINDOWATTRIBUTE)Win32Constants.DWMWA_USE_IMMERSIVE_DARK_MODE,
                &useDarkMode,
                sizeof(uint));
        }

        if (roundCorners)
        {
            uint cornerPreference = Win32Constants.DWMWCP_ROUND;
            PInvoke.DwmSetWindowAttribute(
                hwnd,
                (DWMWINDOWATTRIBUTE)Win32Constants.DWMWA_WINDOW_CORNER_PREFERENCE,
                &cornerPreference,
                sizeof(uint));
        }
    }
}
