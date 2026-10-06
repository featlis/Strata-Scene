using System.Windows.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Accessibility;
using StrataScene.Core.Logging;

namespace StrataScene.Platform;

public sealed class ForegroundTracker : IDisposable
{
    private readonly ILog _log;
    private readonly WINEVENTPROC _proc;
    private readonly HWINEVENTHOOK _hook;
    private readonly DispatcherTimer _timer;
    private IntPtr _currentForeground;
    private bool _isDisposed;

    public event Action<IntPtr>? ForegroundWindowChanged;

    public IntPtr CurrentForegroundWindow => _currentForeground;

    public ForegroundTracker(ILog log)
    {
        _log = log;

        _proc = OnWinEvent;
        _hook = PInvoke.SetWinEventHook(
            Win32Constants.EVENT_SYSTEM_FOREGROUND,
            Win32Constants.EVENT_SYSTEM_FOREGROUND,
            HMODULE.Null,
            _proc,
            0,
            0,
            Win32Constants.WINEVENT_OUTOFCONTEXT | Win32Constants.WINEVENT_SKIPOWNPROCESS);

        if (_hook == IntPtr.Zero)
        {
            _log.Warn("Failed to install SetWinEventHook for EVENT_SYSTEM_FOREGROUND. Relying on timer fallback.");
        }
        else
        {
            _log.Info("Installed SetWinEventHook for EVENT_SYSTEM_FOREGROUND.");
        }

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += (_, _) => CheckForegroundWindow();
        _timer.Start();

        CheckForegroundWindow();
    }

    private void OnWinEvent(
        HWINEVENTHOOK hWinEventHook,
        uint @event,
        HWND hwnd,
        int idObject,
        int idChild,
        uint idEventThread,
        uint dwmsEventTime)
    {
        UpdateForeground((IntPtr)hwnd);
    }

    private void CheckForegroundWindow()
    {
        var fg = (IntPtr)PInvoke.GetForegroundWindow();
        UpdateForeground(fg);
    }

    private void UpdateForeground(IntPtr newForeground)
    {
        if (newForeground == _currentForeground) return;

        _currentForeground = newForeground;
        ForegroundWindowChanged?.Invoke(newForeground);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _timer.Stop();
        if (_hook != IntPtr.Zero)
        {
            PInvoke.UnhookWinEvent(_hook);
        }
    }
}
