using StrataScene.Core.Config;
using StrataScene.Core.Hotkeys;
using StrataScene.Core.Logging;
using Xunit;

namespace StrataScene.Core.Tests;

public class HotkeyGestureTests
{
    [Theory]
    [InlineData("Ctrl+Alt+W", true, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_ALT, (uint)'W', "Ctrl+Alt+W")]
    [InlineData("Alt+Space", true, VirtualKeys.MOD_ALT, VirtualKeys.VK_SPACE, "Alt+Space")]
    [InlineData("ctrl+shift+e", true, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_SHIFT, (uint)'E', "Ctrl+Shift+E")]
    [InlineData("Ctrl+Alt+Back", true, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_ALT, VirtualKeys.VK_BACK, "Ctrl+Alt+Back")]
    [InlineData("Ctrl+Alt+Backspace", true, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_ALT, VirtualKeys.VK_BACK, "Ctrl+Alt+Back")]
    [InlineData("Win+F1", true, VirtualKeys.MOD_WIN, 0x70, "Win+F1")]
    [InlineData("Ctrl+Alt+Shift+Win+P", true, VirtualKeys.MOD_CONTROL | VirtualKeys.MOD_ALT | VirtualKeys.MOD_SHIFT | VirtualKeys.MOD_WIN, (uint)'P', "Ctrl+Alt+Shift+Win+P")]
    public void TryParse_ValidInputs_ParsesCorrectly(string input, bool expectedSuccess, uint expectedMods, uint expectedVk, string expectedString)
    {
        var success = HotkeyGesture.TryParse(input, out var gesture);
        Assert.Equal(expectedSuccess, success);
        Assert.NotNull(gesture);
        Assert.Equal(expectedMods, gesture.Modifiers);
        Assert.Equal(expectedVk, gesture.VirtualKey);
        Assert.Equal(expectedString, gesture.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    [InlineData("Ctrl")]
    [InlineData("Unknown+Key")]
    [InlineData("Ctrl+Alt+UnknownKey")]
    public void TryParse_InvalidInputs_ReturnsFalse(string input)
    {
        var success = HotkeyGesture.TryParse(input, out var gesture);
        Assert.False(success);
        Assert.Null(gesture);
    }
}

public class ConfigRepositoryTests
{
    [Fact]
    public void LoadOrCreate_CreatesDefaultIfMissing()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "StrataSceneConfigTest_" + Guid.NewGuid());
        try
        {
            var log = new FileLog(tempDir);
            var repo = new ConfigRepository(log, tempDir);

            var (config, wasRecovered) = repo.LoadOrCreate();
            Assert.False(wasRecovered);
            Assert.NotNull(config);
            Assert.Equal("0.1", config.Version);
            Assert.Equal(2, config.Scenes.Count);
            Assert.Equal(3, config.Widgets.Count);
            Assert.True(File.Exists(Path.Combine(tempDir, "config.json")));
            Assert.True(File.Exists(Path.Combine(tempDir, "schema.json")));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ConfigRepository_RoundTrips_CloseOrMinimizeItems()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "StrataSceneConfigTest_" + Guid.NewGuid());
        try
        {
            var log = new FileLog(tempDir);
            var repo = new ConfigRepository(log, tempDir);

            var config = ConfigDefaults.CreateDefault();
            config.Scenes[0].Actions.CloseOrMinimize.Add(new CloseOrMinimizeItem("Steam.exe", WindowCloseMode.Minimize));
            config.Scenes[0].Actions.CloseOrMinimize.Add(new CloseOrMinimizeItem("Discord.exe", WindowCloseMode.Close));

            repo.Save(config);

            var (loaded, wasRecovered) = repo.LoadOrCreate();
            Assert.False(wasRecovered);
            var items = loaded.Scenes[0].Actions.CloseOrMinimize;
            Assert.Equal(2, items.Count);
            Assert.Equal("Steam.exe", items[0].Process);
            Assert.Equal(WindowCloseMode.Minimize, items[0].Mode);
            Assert.Equal("Discord.exe", items[1].Process);
            Assert.Equal(WindowCloseMode.Close, items[1].Mode);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void LoadOrCreate_RecoversFromCorruptJson()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "StrataSceneConfigTest_" + Guid.NewGuid());
        try
        {
            var log = new FileLog(tempDir);
            var configPath = Path.Combine(tempDir, "config.json");
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(configPath, "{ invalid json content !!!");

            var repo = new ConfigRepository(log, tempDir);
            var (config, wasRecovered) = repo.LoadOrCreate();

            Assert.True(wasRecovered);
            Assert.NotNull(config);
            Assert.Equal("0.1", config.Version);

            // Check that invalid file was backed up
            var backupFiles = Directory.GetFiles(tempDir, "config.invalid-*.json");
            Assert.Single(backupFiles);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
