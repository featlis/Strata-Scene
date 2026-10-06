using System.Diagnostics;
using System.IO;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using StrataScene.Core.Config;
using StrataScene.Core.Logging;

namespace StrataScene.Platform;

public sealed class ProcessLauncher
{
    private const uint WM_CLOSE = 0x0010;
    private readonly ILog _log;

    public ProcessLauncher(ILog log)
    {
        _log = log;
    }

    public bool LaunchOrFocus(LaunchItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Path))
        {
            _log.Warn("Launch item has empty path.");
            return false;
        }

        var expandedPath = Environment.ExpandEnvironmentVariables(item.Path.Trim());

        // Check if URL
        if (Uri.TryCreate(expandedPath, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return LaunchUrl(expandedPath);
        }

        return LaunchOrFocusProcess(expandedPath, item.Args, item.ProcessName);
    }

    private bool LaunchUrl(string url)
    {
        try
        {
            _log.Info($"Opening URL: {url}");
            Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to open URL: {url}", ex);
            return false;
        }
    }

    private bool LaunchOrFocusProcess(string exePath, string args, string? explicitProcessName)
    {
        var targetProcName = !string.IsNullOrWhiteSpace(explicitProcessName)
            ? explicitProcessName
            : Path.GetFileNameWithoutExtension(exePath);

        if (targetProcName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            targetProcName = targetProcName[..^4];
        }

        try
        {
            var currentSessionId = Process.GetCurrentProcess().SessionId;
            var running = Process.GetProcessesByName(targetProcName)
                .Where(p => p.SessionId == currentSessionId)
                .ToList();

            foreach (var proc in running)
            {
                var hwnd = (HWND)proc.MainWindowHandle;
                if (hwnd != HWND.Null && PInvoke.IsWindow(hwnd))
                {
                    _log.Info($"Process '{targetProcName}' is already running (PID {proc.Id}). Activating existing window.");
                    PInvoke.ShowWindowAsync(hwnd, SHOW_WINDOW_CMD.SW_RESTORE);
                    PInvoke.SetForegroundWindow(hwnd);
                    return true;
                }
            }

            _log.Info($"Launching process: '{exePath}' {args}");
            var startInfo = new ProcessStartInfo(exePath, args)
            {
                UseShellExecute = true
            };

            var dir = Path.GetDirectoryName(exePath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                startInfo.WorkingDirectory = dir;
            }

            Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to launch process '{exePath}'", ex);
            return false;
        }
    }

    public void MinimizeWindow(IntPtr handle)
    {
        var hwnd = (HWND)handle;
        if (PInvoke.IsWindow(hwnd))
        {
            PInvoke.ShowWindowAsync(hwnd, SHOW_WINDOW_CMD.SW_MINIMIZE);
        }
    }

    public void CloseWindow(IntPtr handle)
    {
        var hwnd = (HWND)handle;
        if (PInvoke.IsWindow(hwnd))
        {
            PInvoke.PostMessage(hwnd, WM_CLOSE, default, default);
        }
    }
}
