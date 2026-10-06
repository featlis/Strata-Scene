using System.Diagnostics;
using StrataScene.Core.Config;
using StrataScene.Core.Logging;

namespace StrataScene.Core.Scenes;

public sealed class SceneManager
{
    private readonly ILog _log;
    private readonly ISceneHost _host;
    private readonly Lock _syncLock = new();
    private string? _currentSceneId;

    public event Action<string?>? CurrentSceneChanged;

    public string? CurrentSceneId
    {
        get
        {
            lock (_syncLock) return _currentSceneId;
        }
    }

    public SceneManager(ILog log, ISceneHost host)
    {
        _log = log;
        _host = host;
    }

    public void SetCurrentSceneId(string? sceneId)
    {
        lock (_syncLock)
        {
            _currentSceneId = sceneId;
        }
        CurrentSceneChanged?.Invoke(sceneId);
    }

    public async Task<SceneExecutionResult> ExecuteSceneAsync(SceneConfig scene, IReadOnlyCollection<string>? globalExcludeList = null)
    {
        return await Task.Run(() =>
        {
            var sw = Stopwatch.StartNew();
            _log.Info($"Executing scene '{scene.Name}' (ID: {scene.Id})...");

            var failures = new List<string>();
            var launchedCount = 0;
            var minimizedCount = 0;
            var closedCount = 0;

            // 1. Snapshot prior window states
            try
            {
                _host.CaptureSnapshot(globalExcludeList);
            }
            catch (Exception ex)
            {
                _log.Error("Failed to capture window snapshot", ex);
                failures.Add("ウィンドウ状態のスナップショット保存に失敗しました。");
            }

            // 2. Identify active windows
            var activeWindows = _host.GetActiveWindows(globalExcludeList);

            // Determine launch process names to avoid minimizing them
            var launchTargetProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var l in scene.Actions.Launch)
            {
                var targetProc = !string.IsNullOrWhiteSpace(l.ProcessName)
                    ? l.ProcessName
                    : Path.GetFileNameWithoutExtension(l.Path);

                if (targetProc.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    targetProc = targetProc[..^4];
                }

                if (!string.IsNullOrWhiteSpace(targetProc))
                {
                    launchTargetProcesses.Add(targetProc);
                }
            }

            // 3. Handle explicit close_or_minimize list
            var explicitActions = new Dictionary<string, WindowCloseMode>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in scene.Actions.CloseOrMinimize)
            {
                var proc = item.Process;
                if (proc.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    proc = proc[..^4];
                }
                explicitActions[proc] = item.Mode;
            }

            var handledHandles = new HashSet<IntPtr>();

            foreach (var win in activeWindows)
            {
                if (explicitActions.TryGetValue(win.ProcessName, out var mode))
                {
                    handledHandles.Add(win.Handle);
                    try
                    {
                        if (mode == WindowCloseMode.Close)
                        {
                            _host.CloseWindow(win.Handle);
                            closedCount++;
                        }
                        else
                        {
                            _host.MinimizeWindow(win.Handle);
                            minimizedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Failed to {mode} window for {win.ProcessName}", ex);
                    }
                }
            }

            // 4. If minimize_others is true, minimize remaining active windows that are not in launchTargetProcesses
            if (scene.Actions.MinimizeOthers)
            {
                foreach (var win in activeWindows)
                {
                    if (handledHandles.Contains(win.Handle)) continue;
                    if (launchTargetProcesses.Contains(win.ProcessName)) continue;

                    try
                    {
                        _host.MinimizeWindow(win.Handle);
                        minimizedCount++;
                    }
                    catch (Exception ex)
                    {
                        _log.Warn($"Failed to minimize other window {win.ProcessName}", ex);
                    }
                }
            }

            // Log time elapsed until launch commands begin (Specification non-functional requirement: <= 50ms)
            var preLaunchElapsed = sw.ElapsedMilliseconds;
            _log.Info($"Preparation for scene execution completed in {preLaunchElapsed}ms. Starting launches...");

            // 5. Launch or focus target items
            foreach (var item in scene.Actions.Launch)
            {
                try
                {
                    if (_host.LaunchOrFocus(item))
                    {
                        launchedCount++;
                    }
                    else
                    {
                        failures.Add($"'{item.Path}' の起動またはフォーカスに失敗しました。");
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"Error launching '{item.Path}'", ex);
                    failures.Add($"'{item.Path}' の起動中に例外が発生しました。");
                }
            }

            // 6. System settings warning log if present (deferred per v0.1.0)
            if (scene.Actions.System.FocusAssist || !string.IsNullOrWhiteSpace(scene.Actions.System.AudioOutput))
            {
                _log.Warn("FocusAssist and AudioOutput are deferred in v0.1.0 prototype.");
            }

            SetCurrentSceneId(scene.Id);
            sw.Stop();
            var totalElapsed = sw.ElapsedMilliseconds;

            _log.Info($"Scene '{scene.Name}' executed in {totalElapsed}ms (Launched: {launchedCount}, Minimized: {minimizedCount}, Closed: {closedCount}, Failures: {failures.Count})");

            return new SceneExecutionResult(
                scene.Id,
                failures.Count == 0,
                launchedCount,
                minimizedCount,
                closedCount,
                failures,
                totalElapsed);
        });
    }

    public async Task<int> RestoreSceneAsync()
    {
        return await Task.Run(() =>
        {
            _log.Info("Restoring previous window snapshot...");
            var count = _host.RestoreSnapshot();
            _log.Info($"Restore complete. Restored {count} windows.");
            return count;
        });
    }
}
