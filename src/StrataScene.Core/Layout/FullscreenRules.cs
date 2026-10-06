namespace StrataScene.Core.Layout;

public readonly record struct IntRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

public static class FullscreenRules
{
    private static readonly HashSet<string> DesktopClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Progman",
        "WorkerW",
        "Shell_TrayWnd",
        "Shell_SecondaryTrayWnd"
    };

    public static bool IsDesktopOrShellWindow(string? className)
    {
        if (string.IsNullOrWhiteSpace(className)) return false;
        return DesktopClasses.Contains(className.Trim());
    }

    public static bool IsWindowCoveringMonitor(IntRect windowRect, IntRect monitorRect)
    {
        if (windowRect.Width <= 0 || windowRect.Height <= 0) return false;
        if (monitorRect.Width <= 0 || monitorRect.Height <= 0) return false;

        return windowRect.Left <= monitorRect.Left &&
               windowRect.Top <= monitorRect.Top &&
               windowRect.Right >= monitorRect.Right &&
               windowRect.Bottom >= monitorRect.Bottom;
    }

    public static bool Evaluate(string? className, IntRect windowRect, IntRect monitorRect, bool hasD3dFullscreenState)
    {
        if (IsDesktopOrShellWindow(className))
        {
            return false;
        }

        if (hasD3dFullscreenState)
        {
            return true;
        }

        return IsWindowCoveringMonitor(windowRect, monitorRect);
    }
}
