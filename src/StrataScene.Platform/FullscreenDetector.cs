using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Shell;
using StrataScene.Core.Layout;
using StrataScene.Core.Logging;

namespace StrataScene.Platform;

public sealed class FullscreenDetector : IDisposable
{
    private readonly ILog _log;
    private readonly ForegroundTracker _foregroundTracker;
    private readonly DispatcherTimer _pollTimer;

    private bool _lastIsFullscreen;
    private IntPtr _lastMonitorHandle;
    private bool _isDisposed;

    public event Action<bool, IntPtr>? FullscreenChanged;

    public bool IsFullscreen => _lastIsFullscreen;
    public IntPtr CurrentMonitorHandle => _lastMonitorHandle;

    public FullscreenDetector(ILog log, ForegroundTracker foregroundTracker)
    {
        _log = log;
        _foregroundTracker = foregroundTracker;

        _foregroundTracker.ForegroundWindowChanged += OnForegroundWindowChanged;

        _pollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _pollTimer.Tick += (_, _) => CheckCurrentState();
        _pollTimer.Start();

        CheckCurrentState();
    }

    private void OnForegroundWindowChanged(IntPtr hwnd)
    {
        EvaluateFullscreen(hwnd);
    }

    private void CheckCurrentState()
    {
        var fg = _foregroundTracker.CurrentForegroundWindow;
        EvaluateFullscreen(fg);
    }

    private unsafe void EvaluateFullscreen(IntPtr hwndHandle)
    {
        if (hwndHandle == IntPtr.Zero)
        {
            UpdateState(false, IntPtr.Zero);
            return;
        }

        var hwnd = (HWND)hwndHandle;
        if (!PInvoke.IsWindow(hwnd))
        {
            UpdateState(false, IntPtr.Zero);
            return;
        }

        // Get class name
        var classNameBuf = new char[256];
        fixed (char* p = classNameBuf)
        {
            PInvoke.GetClassName(hwnd, p, classNameBuf.Length);
        }
        var className = new string(classNameBuf).TrimEnd('\0');

        // Fast path for shell windows
        if (FullscreenRules.IsDesktopOrShellWindow(className))
        {
            UpdateState(false, IntPtr.Zero);
            return;
        }

        // Get window rect
        if (!PInvoke.GetWindowRect(hwnd, out var winRect))
        {
            UpdateState(false, IntPtr.Zero);
            return;
        }

        // Get monitor info
        var hmon = PInvoke.MonitorFromWindow(hwnd, MONITOR_FROM_FLAGS.MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO
        {
            cbSize = (uint)sizeof(MONITORINFO)
        };

        if (!PInvoke.GetMonitorInfo(hmon, &mi))
        {
            UpdateState(false, IntPtr.Zero);
            return;
        }

        // Query notification state for D3D fullscreen games
        var hasD3dFs = false;
        try
        {
            PInvoke.SHQueryUserNotificationState(out var qState);
            hasD3dFs = qState is QUERY_USER_NOTIFICATION_STATE.QUNS_RUNNING_D3D_FULL_SCREEN
                               or QUERY_USER_NOTIFICATION_STATE.QUNS_PRESENTATION_MODE;
        }
        catch (Exception ex)
        {
            _log.Debug($"SHQueryUserNotificationState check ignored: {ex.Message}");
        }

        var winIntRect = new IntRect(winRect.left, winRect.top, winRect.right, winRect.bottom);
        var monIntRect = new IntRect(mi.rcMonitor.left, mi.rcMonitor.top, mi.rcMonitor.right, mi.rcMonitor.bottom);

        var isFs = FullscreenRules.Evaluate(className, winIntRect, monIntRect, hasD3dFs);
        UpdateState(isFs, (IntPtr)hmon.Value);
    }

    private void UpdateState(bool isFs, IntPtr monitorHandle)
    {
        if (isFs == _lastIsFullscreen && monitorHandle == _lastMonitorHandle)
        {
            return;
        }

        _lastIsFullscreen = isFs;
        _lastMonitorHandle = monitorHandle;

        _log.Info($"Fullscreen state changed: isFullscreen={isFs}, monitor=0x{monitorHandle:X}");
        FullscreenChanged?.Invoke(isFs, monitorHandle);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _pollTimer.Stop();
        _foregroundTracker.ForegroundWindowChanged -= OnForegroundWindowChanged;
    }
}
