using System.Diagnostics;
using System.Windows.Interop;
using StrataScene.Core.Logging;

namespace StrataScene.Platform;

public sealed class MessageWindow : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private readonly ILog _log;
    private readonly HwndSource _hwndSource;
    private readonly Action<int, long> _onHotKey;

    public IntPtr Handle => _hwndSource.Handle;

    public MessageWindow(ILog log, Action<int, long> onHotKey)
    {
        _log = log;
        _onHotKey = onHotKey;

        var parameters = new HwndSourceParameters("StrataScene_MessageWindow")
        {
            ParentWindow = HWND_MESSAGE,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            var timestamp = Stopwatch.GetTimestamp();
            handled = true;
            _onHotKey(id, timestamp);
            return IntPtr.Zero;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
    }
}
