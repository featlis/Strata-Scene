using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using StrataScene.Core.Logging;

namespace StrataScene.Platform;

internal sealed class WindowSnapshotService
{
    private readonly ILog _log;
    private readonly WindowEnumerator _enumerator;
    private readonly Lock _syncLock = new();
    private List<WindowInfo>? _lastSnapshot;

    public WindowSnapshotService(ILog log, WindowEnumerator enumerator)
    {
        _log = log;
        _enumerator = enumerator;
    }

    public bool HasSnapshot
    {
        get
        {
            lock (_syncLock)
            {
                return _lastSnapshot != null && _lastSnapshot.Count > 0;
            }
        }
    }

    public int CaptureSnapshot(IReadOnlyCollection<string>? excludeProcesses = null)
    {
        lock (_syncLock)
        {
            var windows = _enumerator.EnumerateCandidateWindows(excludeProcesses);
            _lastSnapshot = windows;
            _log.Info($"Captured snapshot of {windows.Count} windows.");
            return windows.Count;
        }
    }

    public int RestoreSnapshot()
    {
        List<WindowInfo>? snapshot;
        lock (_syncLock)
        {
            snapshot = _lastSnapshot;
            _lastSnapshot = null;
        }

        if (snapshot == null || snapshot.Count == 0)
        {
            _log.Warn("No snapshot available to restore.");
            return 0;
        }

        _log.Info($"Restoring {snapshot.Count} windows from snapshot...");
        var restoredCount = 0;
        IntPtr? topWindowToFocus = null;

        // Restore from back to front
        for (var i = snapshot.Count - 1; i >= 0; i--)
        {
            var win = snapshot[i];
            var hwnd = (HWND)win.Handle;

            if (!PInvoke.IsWindow(hwnd))
            {
                _log.Debug($"Window for {win.ProcessName} ('{win.Title}') no longer exists. Skipping.");
                continue;
            }

            var placement = win.Placement;
            // Clear WPF/Win32 async flags if any
            placement.flags = 0;

            if (PInvoke.SetWindowPlacement(hwnd, in placement))
            {
                restoredCount++;
                if (i == 0)
                {
                    topWindowToFocus = win.Handle;
                }
            }
            else
            {
                _log.Warn($"Failed to restore placement for window {win.ProcessName} ('{win.Title}').");
            }
        }

        if (topWindowToFocus.HasValue)
        {
            var topHwnd = (HWND)topWindowToFocus.Value;
            if (PInvoke.IsWindow(topHwnd))
            {
                PInvoke.SetForegroundWindow(topHwnd);
            }
        }

        _log.Info($"Restored {restoredCount}/{snapshot.Count} windows successfully.");
        return restoredCount;
    }
}
