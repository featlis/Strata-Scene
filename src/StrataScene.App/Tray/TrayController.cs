using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using StrataScene.Core.Logging;

namespace StrataScene.App.Tray;

public sealed class TrayController : IDisposable
{
    private readonly ILog _log;
    private readonly TaskbarIcon _taskbarIcon;
    private readonly Action _onExit;

    public TrayController(ILog log, Action onExit)
    {
        _log = log;
        _onExit = onExit;

        var menu = new ContextMenu();
        var exitItem = new MenuItem { Header = "終了" };
        exitItem.Click += (_, _) => _onExit();
        menu.Items.Add(exitItem);

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Strata Scene 0.1.0",
            ContextMenu = menu
        };

        try
        {
            var iconUri = new Uri("pack://application:,,,/assets/StrataScene.ico", UriKind.RelativeOrAbsolute);
            _taskbarIcon.IconSource = new BitmapImage(iconUri);
        }
        catch (Exception ex)
        {
            _log.Warn("Failed to load pack URI icon, using fallback", ex);
        }

        _taskbarIcon.ForceCreate();
    }

    public void Notify(string title, string message)
    {
        try
        {
            _taskbarIcon.ShowNotification(title, message);
        }
        catch (Exception ex)
        {
            _log.Error($"Failed to show tray notification: {title}", ex);
        }
    }

    public void Dispose()
    {
        _taskbarIcon.Dispose();
    }
}
