namespace StrataScene.Core.Layout;

public readonly record struct PhysicalRect(int Left, int Top, int Width, int Height)
{
    public int Right => Left + Width;
    public int Bottom => Top + Height;
}

public readonly record struct DipPoint(double X, double Y);

public static class WidgetLayoutCalculator
{
    public const double DefaultGridSizeDip = 10.0;

    public static double Snap(double value, double gridSize = DefaultGridSizeDip)
    {
        if (gridSize <= 0) return value;
        return Math.Round(value / gridSize, MidpointRounding.AwayFromZero) * gridSize;
    }

    public static DipPoint SnapPoint(DipPoint point, double gridSize = DefaultGridSizeDip)
    {
        return new DipPoint(Snap(point.X, gridSize), Snap(point.Y, gridSize));
    }

    public static PhysicalRect CalculatePhysicalBounds(
        PhysicalRect taskbarBounds,
        double widgetWidthDip,
        double widgetHeightDip,
        double dpiScale,
        string dockAlignment,
        double offsetXDip,
        double offsetYDip)
    {
        var widgetWidthPx = (int)Math.Round(widgetWidthDip * dpiScale);
        var widgetHeightPx = (int)Math.Round(widgetHeightDip * dpiScale);

        var offsetXPx = (int)Math.Round(offsetXDip * dpiScale);
        var offsetYPx = (int)Math.Round(offsetYDip * dpiScale);

        var taskbarLeft = taskbarBounds.Left;
        var taskbarRight = taskbarBounds.Right;
        var taskbarTop = taskbarBounds.Top;
        var taskbarWidth = taskbarBounds.Width;

        var posX = dockAlignment switch
        {
            "TaskbarLeft" => taskbarLeft + offsetXPx,
            "TaskbarCenter" => taskbarLeft + (taskbarWidth / 2) + offsetXPx,
            _ => taskbarRight + offsetXPx // Default "TaskbarRight"
        };

        var posY = taskbarTop + offsetYPx;

        return new PhysicalRect(posX, posY, widgetWidthPx, widgetHeightPx);
    }

    public static DipPoint CalculateOffsetFromPhysicalPosition(
        PhysicalRect taskbarBounds,
        int physicalX,
        int physicalY,
        double dpiScale,
        string dockAlignment,
        bool snapToGrid = true,
        double gridSize = DefaultGridSizeDip)
    {
        if (dpiScale <= 0) dpiScale = 1.0;

        var taskbarLeft = taskbarBounds.Left;
        var taskbarRight = taskbarBounds.Right;
        var taskbarTop = taskbarBounds.Top;
        var taskbarWidth = taskbarBounds.Width;

        double rawOffsetXPx = dockAlignment switch
        {
            "TaskbarLeft" => physicalX - taskbarLeft,
            "TaskbarCenter" => physicalX - (taskbarLeft + (taskbarWidth / 2.0)),
            _ => physicalX - taskbarRight
        };

        double rawOffsetYPx = physicalY - taskbarTop;

        var offsetXDip = rawOffsetXPx / dpiScale;
        var offsetYDip = rawOffsetYPx / dpiScale;

        if (snapToGrid)
        {
            offsetXDip = Snap(offsetXDip, gridSize);
            offsetYDip = Snap(offsetYDip, gridSize);
        }

        return new DipPoint(offsetXDip, offsetYDip);
    }
}
