using StrataScene.Core.Config;

namespace StrataScene.Core.Scenes;

public sealed record WindowInfoSnapshot(IntPtr Handle, string ProcessName, string Title);

public interface ISceneHost
{
    int CaptureSnapshot(IReadOnlyCollection<string>? excludeProcesses);
    int RestoreSnapshot();
    bool HasSnapshot { get; }
    IReadOnlyList<WindowInfoSnapshot> GetActiveWindows(IReadOnlyCollection<string>? excludeProcesses);
    void MinimizeWindow(IntPtr handle);
    void CloseWindow(IntPtr handle);
    bool LaunchOrFocus(LaunchItem item);
}
