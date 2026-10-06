using StrataScene.Core.Layout;
using Xunit;

namespace StrataScene.Core.Tests;

public class WidgetLayoutCalculatorTests
{
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(4.0, 0.0)]
    [InlineData(5.0, 10.0)]
    [InlineData(14.0, 10.0)]
    [InlineData(16.0, 20.0)]
    [InlineData(-12.0, -10.0)]
    [InlineData(-16.0, -20.0)]
    public void Snap_RoundsToNearestGridSize(double input, double expected)
    {
        var result = WidgetLayoutCalculator.Snap(input, 10.0);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculatePhysicalBounds_TaskbarRight_ComputesCorrectCoordinates()
    {
        var taskbar = new PhysicalRect(0, 1040, 1920, 40);
        var widgetWidthDip = 110.0;
        var widgetHeightDip = 28.0;
        var dpiScale = 1.0;
        var dockAlignment = "TaskbarRight";
        var offsetXDip = -180.0;
        var offsetYDip = 6.0;

        var result = WidgetLayoutCalculator.CalculatePhysicalBounds(
            taskbar,
            widgetWidthDip,
            widgetHeightDip,
            dpiScale,
            dockAlignment,
            offsetXDip,
            offsetYDip);

        // Right is 1920, offset is -180 -> posX = 1740
        Assert.Equal(1740, result.Left);
        Assert.Equal(1046, result.Top);
        Assert.Equal(110, result.Width);
        Assert.Equal(28, result.Height);
    }

    [Fact]
    public void CalculatePhysicalBounds_TaskbarLeft_ComputesCorrectCoordinates()
    {
        var taskbar = new PhysicalRect(0, 1040, 1920, 40);
        var result = WidgetLayoutCalculator.CalculatePhysicalBounds(
            taskbar,
            widgetWidthDip: 100.0,
            widgetHeightDip: 28.0,
            dpiScale: 1.5,
            dockAlignment: "TaskbarLeft",
            offsetXDip: 50.0,
            offsetYDip: 10.0);

        // Left is 0, offset is 50 * 1.5 = 75 -> posX = 75
        Assert.Equal(75, result.Left);
        // Top is 1040, offset is 10 * 1.5 = 15 -> posY = 1055
        Assert.Equal(1055, result.Top);
        Assert.Equal(150, result.Width);
        Assert.Equal(42, result.Height);
    }

    [Fact]
    public void CalculateOffsetFromPhysicalPosition_TaskbarRight_SnapsCorrectly()
    {
        var taskbar = new PhysicalRect(0, 1040, 1920, 40);
        var result = WidgetLayoutCalculator.CalculateOffsetFromPhysicalPosition(
            taskbar,
            physicalX: 1743, // 1743 - 1920 = -177 -> snaps to -180
            physicalY: 1047, // 1047 - 1040 = 7 -> snaps to 10
            dpiScale: 1.0,
            dockAlignment: "TaskbarRight",
            snapToGrid: true);

        Assert.Equal(-180.0, result.X);
        Assert.Equal(10.0, result.Y);
    }
}
