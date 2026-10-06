using StrataScene.Core.Config;
using StrataScene.Core.Logging;
using StrataScene.Core.Scenes;

namespace StrataScene.Platform;

public sealed class SceneHost : ISceneHost
{
    private readonly WindowSnapshotService _snapshotService;
    private readonly WindowEnumerator _enumerator;
    private readonly ProcessLauncher _launcher;

    public SceneHost(ILog log)
    {
        _launcher = new ProcessLauncher(log);
        _enumerator = new WindowEnumerator(log);
        _snapshotService = new WindowSnapshotService(log, _enumerator);
    }

    public bool HasSnapshot => _snapshotService.HasSnapshot;

    public int CaptureSnapshot(IReadOnlyCollection<string>? excludeProcesses) =>
        _snapshotService.CaptureSnapshot(excludeProcesses);

    public int RestoreSnapshot() =>
        _snapshotService.RestoreSnapshot();

    public IReadOnlyList<WindowInfoSnapshot> GetActiveWindows(IReadOnlyCollection<string>? excludeProcesses)
    {
        var windows = _enumerator.EnumerateCandidateWindows(excludeProcesses);
        return windows.Select(w => new WindowInfoSnapshot(w.Handle, w.ProcessName, w.Title)).ToList();
    }

    public void MinimizeWindow(IntPtr handle) => _launcher.MinimizeWindow(handle);

    public void CloseWindow(IntPtr handle) => _launcher.CloseWindow(handle);

    public bool LaunchOrFocus(LaunchItem item) => _launcher.LaunchOrFocus(item);
}
