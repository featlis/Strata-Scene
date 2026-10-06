using StrataScene.Core.Layout;
using Xunit;

namespace StrataScene.Core.Tests;

public class FullscreenRulesTests
{
    [Fact]
    public void Evaluate_WindowMatchesMonitor_ReturnsTrue()
    {
        var mon = new IntRect(0, 0, 1920, 1080);
        var win = new IntRect(0, 0, 1920, 1080);

        var result = FullscreenRules.Evaluate("GameWindowClass", win, mon, false);
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_WindowCoversMonitorWithBorders_ReturnsTrue()
    {
        var mon = new IntRect(0, 0, 1920, 1080);
        var win = new IntRect(-5, -5, 1925, 1085);

        var result = FullscreenRules.Evaluate("GameWindowClass", win, mon, false);
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_NormalWindow_ReturnsFalse()
    {
        var mon = new IntRect(0, 0, 1920, 1080);
        var win = new IntRect(100, 100, 1200, 800);

        var result = FullscreenRules.Evaluate("Notepad", win, mon, false);
        Assert.False(result);
    }

    [Theory]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    [InlineData("Shell_TrayWnd")]
    [InlineData("Shell_SecondaryTrayWnd")]
    public void Evaluate_DesktopAndShellWindows_ReturnsFalseEvenIfCoveringMonitor(string shellClass)
    {
        var mon = new IntRect(0, 0, 1920, 1080);
        var win = new IntRect(0, 0, 1920, 1080);

        var result = FullscreenRules.Evaluate(shellClass, win, mon, true);
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_D3dFullscreenState_ReturnsTrue()
    {
        var mon = new IntRect(0, 0, 1920, 1080);
        var win = new IntRect(100, 100, 500, 500);

        var result = FullscreenRules.Evaluate("GameClass", win, mon, true);
        Assert.True(result);
    }
}
