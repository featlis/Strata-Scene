using StrataScene.Core.Config;
using StrataScene.Core.Logging;
using StrataScene.Core.Scenes;
using Xunit;

namespace StrataScene.Core.Tests;

public class SceneManagerTests
{
    private class FakeSceneHost : ISceneHost
    {
        public bool SnapshotCaptured { get; set; }
        public bool SnapshotRestored { get; set; }
        public List<WindowInfoSnapshot> ActiveWindows { get; set; } = [];
        public List<IntPtr> MinimizedWindows { get; set; } = [];
        public List<IntPtr> ClosedWindows { get; set; } = [];
        public List<LaunchItem> LaunchedItems { get; set; } = [];
        public bool ShouldFailLaunch { get; set; }

        public bool HasSnapshot => SnapshotCaptured;

        public int CaptureSnapshot(IReadOnlyCollection<string>? excludeProcesses)
        {
            SnapshotCaptured = true;
            return ActiveWindows.Count;
        }

        public int RestoreSnapshot()
        {
            SnapshotRestored = true;
            return ActiveWindows.Count;
        }

        public IReadOnlyList<WindowInfoSnapshot> GetActiveWindows(IReadOnlyCollection<string>? excludeProcesses) =>
            ActiveWindows;

        public void MinimizeWindow(IntPtr handle) => MinimizedWindows.Add(handle);

        public void CloseWindow(IntPtr handle) => ClosedWindows.Add(handle);

        public bool LaunchOrFocus(LaunchItem item)
        {
            if (ShouldFailLaunch) return false;
            LaunchedItems.Add(item);
            return true;
        }
    }

    [Fact]
    public async Task ExecuteSceneAsync_CapturesSnapshot_MinimizesOthers_AndLaunchesTargets()
    {
        var log = new FileLog(Path.Combine(Path.GetTempPath(), "StrataSceneTest_" + Guid.NewGuid()));
        var host = new FakeSceneHost
        {
            ActiveWindows =
            [
                new WindowInfoSnapshot(new IntPtr(101), "notepad", "Notes"),
                new WindowInfoSnapshot(new IntPtr(102), "steam", "Steam"),
                new WindowInfoSnapshot(new IntPtr(103), "discord", "Discord")
            ]
        };

        var manager = new SceneManager(log, host);

        var scene = new SceneConfig
        {
            Id = "scene_work",
            Name = "Work",
            Actions = new SceneActions
            {
                MinimizeOthers = true,
                CloseOrMinimize =
                [
                    new CloseOrMinimizeItem("steam", WindowCloseMode.Minimize),
                    new CloseOrMinimizeItem("discord", WindowCloseMode.Close)
                ],
                Launch =
                [
                    new LaunchItem { Path = "notepad.exe" }
                ]
            }
        };

        var result = await manager.ExecuteSceneAsync(scene);

        Assert.True(result.Success);
        Assert.True(host.SnapshotCaptured);
        Assert.Contains(new IntPtr(102), host.MinimizedWindows);
        Assert.Contains(new IntPtr(103), host.ClosedWindows);
        Assert.DoesNotContain(new IntPtr(101), host.MinimizedWindows); // notepad is launch target, should not minimize
        Assert.Single(host.LaunchedItems);
        Assert.Equal("scene_work", manager.CurrentSceneId);
    }

    [Fact]
    public async Task RestoreSceneAsync_CallsHostRestoreSnapshot()
    {
        var log = new FileLog(Path.Combine(Path.GetTempPath(), "StrataSceneTest_" + Guid.NewGuid()));
        var host = new FakeSceneHost
        {
            ActiveWindows = [new WindowInfoSnapshot(new IntPtr(101), "app", "App")]
        };

        var manager = new SceneManager(log, host);
        var restored = await manager.RestoreSceneAsync();

        Assert.Equal(1, restored);
        Assert.True(host.SnapshotRestored);
    }
}
