using System.Diagnostics;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;
using Windows.Win32.UI.WindowsAndMessaging;
using StrataScene.Core.Logging;

namespace StrataScene.Platform;

internal sealed record WindowInfo(
    IntPtr Handle,
    int ProcessId,
    string ProcessName,
    string Title,
    WINDOWPLACEMENT Placement);

internal sealed class WindowEnumerator
{
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private readonly ILog _log;
    private readonly int _currentPid;

    public WindowEnumerator(ILog log)
    {
        _log = log;
        _currentPid = Environment.ProcessId;
    }

    public unsafe List<WindowInfo> EnumerateCandidateWindows(IReadOnlyCollection<string>? excludeProcesses = null)
    {
        var result = new List<WindowInfo>();
        var excludeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Progman",
            "WorkerW",
            "Shell_TrayWnd",
            "Shell_SecondaryTrayWnd"
        };

        if (excludeProcesses != null)
        {
            foreach (var p in excludeProcesses)
            {
                excludeSet.Add(p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? p[..^4] : p);
            }
        }

        PInvoke.EnumWindows((hwnd, _) =>
        {
            if (!PInvoke.IsWindow(hwnd) || !PInvoke.IsWindowVisible(hwnd))
            {
                return true;
            }

            // Exclude child or owned windows
            var owner = PInvoke.GetWindow(hwnd, GET_WINDOW_CMD.GW_OWNER);
            if (owner != HWND.Null && owner != IntPtr.Zero)
            {
                return true;
            }

            // Exclude tool windows
            var exStyle = PInvoke.GetWindowLong(hwnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE);
            if ((exStyle & WS_EX_TOOLWINDOW) != 0)
            {
                return true;
            }

            // Check cloaked state (e.g., hidden UWP apps, virtual desktop hidden windows)
            uint cloaked = 0;
            var hr = PInvoke.DwmGetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, &cloaked, sizeof(uint));
            if (hr.Value >= 0 && cloaked != 0)
            {
                return true;
            }

            // Must have a title
            var titleLength = PInvoke.GetWindowTextLength(hwnd);
            if (titleLength <= 0)
            {
                return true;
            }

            var titleBuffer = new char[titleLength + 1];
            fixed (char* pTitle = titleBuffer)
            {
                PInvoke.GetWindowText(hwnd, pTitle, titleBuffer.Length);
            }
            var title = new string(titleBuffer).TrimEnd('\0');
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            // Get Process ID
            uint pid = 0;
            PInvoke.GetWindowThreadProcessId(hwnd, &pid);
            if (pid == 0 || pid == _currentPid)
            {
                return true;
            }

            string procName;
            try
            {
                using var proc = Process.GetProcessById((int)pid);
                procName = proc.ProcessName;
            }
            catch
            {
                return true; // Process exited or inaccessible
            }

            if (excludeSet.Contains(procName))
            {
                return true;
            }

            // Get Window Placement
            var placement = new WINDOWPLACEMENT();
            placement.length = (uint)sizeof(WINDOWPLACEMENT);
            if (!PInvoke.GetWindowPlacement(hwnd, ref placement))
            {
                return true;
            }

            // Don't capture already minimized windows
            if (placement.showCmd == SHOW_WINDOW_CMD.SW_SHOWMINIMIZED)
            {
                return true;
            }

            result.Add(new WindowInfo((IntPtr)hwnd.Value, (int)pid, procName, title, placement));
            return true;
        }, 0);

        return result;
    }
}
