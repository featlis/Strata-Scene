using System.Windows.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.WindowsAndMessaging;
using StrataScene.Core.Logging;

namespace StrataScene.Platform;

public readonly record struct ScreenRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;

    internal static ScreenRect FromWin32(RECT r) => new(r.left, r.top, r.right, r.bottom);
}

public sealed record TaskbarInfo(
    IntPtr Handle,
    int MonitorIndex,
    bool IsPrimary,
    ScreenRect Bounds,
    ScreenRect MonitorBounds,
    bool IsHorizontal,
    bool IsAutoHide);

public sealed class TaskbarTracker : IDisposable
{
    private const int WM_DISPLAYCHANGE = 0x007E;
    private const int WM_SETTINGCHANGE = 0x001A;
    private const int WM_DPICHANGED = 0x02E0;

    private readonly ILog _log;
    private readonly HwndSource _listenerWindow;
    private readonly uint _msgTaskbarCreated;
    private readonly Lock _syncLock = new();

    private List<TaskbarInfo> _taskbars = [];
    private bool _isDisposed;

    public event Action? TaskbarsChanged;

    public TaskbarTracker(ILog log)
    {
        _log = log;

        _msgTaskbarCreated = PInvoke.RegisterWindowMessage("TaskbarCreated");

        var parameters = new HwndSourceParameters("StrataScene_TaskbarTrackerListener")
        {
            WindowStyle = unchecked((int)0x80000000) // WS_POPUP, hidden
        };
        _listenerWindow = new HwndSource(parameters);
        _listenerWindow.AddHook(WndProc);

        RefreshTaskbars();
    }

    public IReadOnlyList<TaskbarInfo> GetTaskbars()
    {
        lock (_syncLock)
        {
            return _taskbars.ToList();
        }
    }

    public TaskbarInfo? GetTaskbarForMonitor(int monitorIndex)
    {
        lock (_syncLock)
        {
            return _taskbars.FirstOrDefault(t => t.MonitorIndex == monitorIndex)
                   ?? _taskbars.FirstOrDefault(t => t.IsPrimary)
                   ?? _taskbars.FirstOrDefault();
        }
    }

    public bool IsTaskbarWindow(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return false;
        lock (_syncLock)
        {
            return _taskbars.Any(t => t.Handle == hwnd);
        }
    }

    public unsafe void RefreshTaskbars()
    {
        lock (_syncLock)
        {
            var monitors = EnumerateMonitors();
            var list = new List<TaskbarInfo>();

            // 1. Primary taskbar
            var primaryHwnd = PInvoke.FindWindow("Shell_TrayWnd", null);
            if (primaryHwnd != HWND.Null && PInvoke.IsWindow(primaryHwnd))
            {
                var info = CreateTaskbarInfo(primaryHwnd, isPrimary: true, monitors);
                if (info != null) list.Add(info);
            }

            // 2. Secondary taskbars
            PInvoke.EnumWindows((hwnd, _) =>
            {
                if (!PInvoke.IsWindow(hwnd)) return true;

                var classNameBuffer = new char[256];
                fixed (char* p = classNameBuffer)
                {
                    PInvoke.GetClassName(hwnd, p, classNameBuffer.Length);
                }
                var className = new string(classNameBuffer).TrimEnd('\0');

                if (string.Equals(className, "Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase))
                {
                    var secInfo = CreateTaskbarInfo(hwnd, isPrimary: false, monitors);
                    if (secInfo != null) list.Add(secInfo);
                }

                return true;
            }, 0);

            _taskbars = list;
            _log.Info($"Refreshed taskbars. Found {_taskbars.Count} taskbars.");
        }

        TaskbarsChanged?.Invoke();
    }

    private unsafe TaskbarInfo? CreateTaskbarInfo(HWND hwnd, bool isPrimary, List<(HMONITOR Monitor, RECT Bounds, bool IsPrimary)> monitors)
    {
        if (!PInvoke.GetWindowRect(hwnd, out var rect))
        {
            return null;
        }

        var hmon = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        var monitorIndex = 0;
        var monitorBounds = default(RECT);

        for (var i = 0; i < monitors.Count; i++)
        {
            if (monitors[i].Monitor == hmon)
            {
                monitorIndex = i;
                monitorBounds = monitors[i].Bounds;
                break;
            }
        }

        var width = rect.right - rect.left;
        var height = rect.bottom - rect.top;
        var isHorizontal = width >= height;

        // Simple autohide detection: taskbar edge is thin (<= 5px) or outside visible area
        var isAutoHide = (isHorizontal && height <= 5) || (!isHorizontal && width <= 5);

        return new TaskbarInfo(
            (IntPtr)hwnd,
            monitorIndex,
            isPrimary,
            ScreenRect.FromWin32(rect),
            ScreenRect.FromWin32(monitorBounds),
            isHorizontal,
            isAutoHide);
    }

    private unsafe List<(HMONITOR Monitor, RECT Bounds, bool IsPrimary)> EnumerateMonitors()
    {
        var rawList = new List<(HMONITOR Monitor, RECT Bounds, bool IsPrimary)>();

        PInvoke.EnumDisplayMonitors(HDC.Null, (RECT?)null, (hmon, _, _, _) =>
        {
            var mi = new MONITORINFO
            {
                cbSize = (uint)sizeof(MONITORINFO)
            };
            if (PInvoke.GetMonitorInfo(hmon, &mi))
            {
                var isPrimary = (mi.dwFlags & 1) != 0; // MONITORINFOF_PRIMARY = 1
                rawList.Add((hmon, mi.rcMonitor, isPrimary));
            }
            return true;
        }, 0);

        // Put primary monitor first (index 0), followed by secondary monitors in order
        var sorted = new List<(HMONITOR Monitor, RECT Bounds, bool IsPrimary)>();
        var primary = rawList.FirstOrDefault(m => m.IsPrimary);
        if (primary.Monitor != IntPtr.Zero)
        {
            sorted.Add(primary);
        }

        foreach (var m in rawList.Where(m => !m.IsPrimary))
        {
            sorted.Add(m);
        }

        return sorted;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_DISPLAYCHANGE || msg == WM_SETTINGCHANGE || msg == WM_DPICHANGED || (uint)msg == _msgTaskbarCreated)
        {
            _log.Info($"Display/Taskbar changed notification received (msg 0x{msg:X4}). Refreshing taskbars...");
            RefreshTaskbars();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _listenerWindow.RemoveHook(WndProc);
        _listenerWindow.Dispose();
    }
}
