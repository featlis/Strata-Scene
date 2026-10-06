using System.Windows;
using System.Windows.Media;
using StrataScene.App.Hud.Widgets;
using StrataScene.Core.Config;
using StrataScene.Core.Logging;
using StrataScene.Core.Scenes;
using StrataScene.Platform;

namespace StrataScene.App.Hud;

public sealed class HudManager : IDisposable
{
    private readonly ILog _log;
    private readonly TaskbarTracker _taskbarTracker;
    private readonly ForegroundTracker _foregroundTracker;
    private readonly SceneManager _sceneManager;
    private readonly Action _onOpenLauncher;

    private AppConfig _config;
    private CurrentModeWidget? _currentModeWidget;
    private bool _isDisposed;

    public CurrentModeWidget? CurrentModeWidget => _currentModeWidget;

    public HudManager(
        ILog log,
        TaskbarTracker taskbarTracker,
        ForegroundTracker foregroundTracker,
        SceneManager sceneManager,
        AppConfig config,
        Action onOpenLauncher)
    {
        _log = log;
        _taskbarTracker = taskbarTracker;
        _foregroundTracker = foregroundTracker;
        _sceneManager = sceneManager;
        _config = config;
        _onOpenLauncher = onOpenLauncher;

        InitializeWidgets();

        _taskbarTracker.TaskbarsChanged += OnTaskbarsChanged;
        _foregroundTracker.ForegroundWindowChanged += OnForegroundWindowChanged;
        _sceneManager.CurrentSceneChanged += OnCurrentSceneChanged;

        RelayoutAll();
    }

    private void InitializeWidgets()
    {
        _currentModeWidget = new CurrentModeWidget
        {
            OnClicked = _onOpenLauncher
        };
        _currentModeWidget.ShowNoActivate();

        UpdateCurrentModeDisplay(_sceneManager.CurrentSceneId);
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        RelayoutAll();
    }

    private void OnTaskbarsChanged()
    {
        Application.Current.Dispatcher.InvokeAsync(RelayoutAll);
    }

    private void OnForegroundWindowChanged(IntPtr hwnd)
    {
        // When taskbar takes focus, bring widgets back to the very front
        if (_taskbarTracker.IsTaskbarWindow(hwnd))
        {
            Application.Current.Dispatcher.InvokeAsync(EnsureWidgetsTopmost);
        }
    }

    private void OnCurrentSceneChanged(string? sceneId)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            UpdateCurrentModeDisplay(sceneId);
            RelayoutAll();
        });
    }

    public void EnsureWidgetsTopmost()
    {
        _currentModeWidget?.EnsureTopmost();
    }

    public void SetFullscreenHidden(bool isFullscreen, IntPtr monitorHandle)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_currentModeWidget == null) return;

            if (isFullscreen)
            {
                _currentModeWidget.HideWindow();
            }
            else
            {
                _currentModeWidget.ShowNoActivate();
                EnsureWidgetsTopmost();
            }
        });
    }

    private void UpdateCurrentModeDisplay(string? sceneId)
    {
        var scene = _config.Scenes.FirstOrDefault(s => string.Equals(s.Id, sceneId, StringComparison.OrdinalIgnoreCase));
        if (scene != null)
        {
            _currentModeWidget?.UpdateMode(scene.Name, scene.Color);
        }
        else
        {
            _currentModeWidget?.UpdateMode("—", "#808080");
        }
    }

    public void RelayoutAll()
    {
        if (_isDisposed || _currentModeWidget == null) return;

        var widgetConfig = _config.Widgets.FirstOrDefault(w => w.Type == "CurrentMode");
        if (widgetConfig == null || !widgetConfig.Enabled)
        {
            _currentModeWidget.HideWindow();
            return;
        }

        // Check if active in current scene
        var currentScene = _config.Scenes.FirstOrDefault(s => s.Id == _sceneManager.CurrentSceneId);
        if (currentScene != null && currentScene.ActiveWidgets.Count > 0 &&
            !currentScene.ActiveWidgets.Contains(widgetConfig.Id, StringComparer.OrdinalIgnoreCase))
        {
            _currentModeWidget.HideWindow();
            return;
        }

        var taskbar = _taskbarTracker.GetTaskbarForMonitor(widgetConfig.MonitorIndex);
        if (taskbar == null || taskbar.IsAutoHide || !taskbar.IsHorizontal)
        {
            _currentModeWidget.HideWindow();
            return;
        }

        // Calculate layout
        var dpi = VisualTreeHelper.GetDpi(_currentModeWidget).DpiScaleX;
        var widgetWidthDip = 110.0;
        var widgetHeightDip = 28.0;

        var widgetWidthPx = (int)(widgetWidthDip * dpi);
        var widgetHeightPx = (int)(widgetHeightDip * dpi);

        var taskbarLeft = taskbar.Bounds.Left;
        var taskbarRight = taskbar.Bounds.Right;
        var taskbarTop = taskbar.Bounds.Top;
        var taskbarWidth = taskbarRight - taskbarLeft;

        var posX = widgetConfig.DockAlignment switch
        {
            "TaskbarLeft" => taskbarLeft + (int)(widgetConfig.OffsetX * dpi),
            "TaskbarCenter" => taskbarLeft + (taskbarWidth / 2) + (int)(widgetConfig.OffsetX * dpi),
            _ => taskbarRight + (int)(widgetConfig.OffsetX * dpi) // TaskbarRight default
        };

        var posY = taskbarTop + (int)(widgetConfig.OffsetY * dpi);

        _currentModeWidget.SetWidgetOpacity(widgetConfig.Opacity);
        _currentModeWidget.PositionPhysical(posX, posY, widgetWidthPx, widgetHeightPx);
        _currentModeWidget.ShowNoActivate();
        _currentModeWidget.EnsureTopmost();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _taskbarTracker.TaskbarsChanged -= OnTaskbarsChanged;
        _foregroundTracker.ForegroundWindowChanged -= OnForegroundWindowChanged;
        _sceneManager.CurrentSceneChanged -= OnCurrentSceneChanged;

        _currentModeWidget?.Close();
    }
}
