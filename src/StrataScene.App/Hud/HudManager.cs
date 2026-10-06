using System.Windows;
using System.Windows.Media;
using StrataScene.App.Hud.Widgets;
using StrataScene.Core.Config;
using StrataScene.Core.Layout;
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
    private bool _isEditMode;
    private bool _isDisposed;

    private CurrentModeWidget? _currentModeWidget;
    private FocusTimerWidget? _focusTimerWidget;
    private ScratchpadWidget? _scratchpadWidget;

    public CurrentModeWidget? CurrentModeWidget => _currentModeWidget;
    public FocusTimerWidget? FocusTimerWidget => _focusTimerWidget;
    public ScratchpadWidget? ScratchpadWidget => _scratchpadWidget;

    public Action<string, double, double>? WidgetOffsetChanged { get; set; }
    public Action<string>? ScratchpadTextChanged { get; set; }
    public Action<string, string>? TimerCompleted { get; set; }

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
        // 1. CurrentMode
        _currentModeWidget = new CurrentModeWidget
        {
            OnClicked = _onOpenLauncher
        };
        _currentModeWidget.OffsetChanged += OnWidgetOffsetChanged;
        _currentModeWidget.ShowNoActivate();

        // 2. FocusTimer
        _focusTimerWidget = new FocusTimerWidget();
        _focusTimerWidget.OffsetChanged += OnWidgetOffsetChanged;
        _focusTimerWidget.TimerCompleted += (t, m) => TimerCompleted?.Invoke(t, m);
        _focusTimerWidget.ShowNoActivate();

        // 3. Scratchpad
        _scratchpadWidget = new ScratchpadWidget();
        _scratchpadWidget.OffsetChanged += OnWidgetOffsetChanged;
        _scratchpadWidget.TextCommitted += text => ScratchpadTextChanged?.Invoke(text);
        _scratchpadWidget.ShowNoActivate();

        UpdateCurrentModeDisplay(_sceneManager.CurrentSceneId);
    }

    public void SetInitialScratchpadText(string text)
    {
        _scratchpadWidget?.SetMemoText(text);
    }

    public void SetEditMode(bool isEditMode)
    {
        _isEditMode = isEditMode;
        _currentModeWidget?.SetEditMode(isEditMode);
        _focusTimerWidget?.SetEditMode(isEditMode);
        _scratchpadWidget?.SetEditMode(isEditMode);
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
        RelayoutAll();
    }

    private void OnWidgetOffsetChanged(string widgetId, double newOffsetX, double newOffsetY)
    {
        _log.Info($"Widget '{widgetId}' moved to offset ({newOffsetX}, {newOffsetY})");
        WidgetOffsetChanged?.Invoke(widgetId, newOffsetX, newOffsetY);
    }

    private void OnTaskbarsChanged()
    {
        Application.Current.Dispatcher.InvokeAsync(RelayoutAll);
    }

    private void OnForegroundWindowChanged(IntPtr hwnd)
    {
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
        _focusTimerWidget?.EnsureTopmost();
        _scratchpadWidget?.EnsureTopmost();
    }

    public void SetFullscreenHidden(bool isFullscreen, IntPtr monitorHandle)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (isFullscreen)
            {
                _currentModeWidget?.HideWindow();
                _focusTimerWidget?.HideWindow();
                _scratchpadWidget?.HideWindow();
            }
            else
            {
                RelayoutAll();
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
        if (_isDisposed) return;

        var currentScene = _config.Scenes.FirstOrDefault(s => s.Id == _sceneManager.CurrentSceneId);

        // 1. CurrentMode Widget
        LayoutSingleWidget(
            _currentModeWidget,
            "CurrentMode",
            defaultWidthDip: 110,
            defaultHeightDip: 28,
            currentScene);

        // 2. FocusTimer Widget
        var pomodoroConfig = _config.Widgets.FirstOrDefault(w => w.Type == "FocusTimer");
        if (_focusTimerWidget != null && pomodoroConfig != null)
        {
            _focusTimerWidget.ApplyConfig(pomodoroConfig.WorkMinutes, pomodoroConfig.BreakMinutes);
        }
        LayoutSingleWidget(
            _focusTimerWidget,
            "FocusTimer",
            defaultWidthDip: 110,
            defaultHeightDip: 28,
            currentScene);

        // 3. Scratchpad Widget
        var scratchpadConfig = _config.Widgets.FirstOrDefault(w => w.Type == "Scratchpad");
        var scratchpadWidth = scratchpadConfig?.Width ?? 240;
        LayoutSingleWidget(
            _scratchpadWidget,
            "Scratchpad",
            defaultWidthDip: scratchpadWidth,
            defaultHeightDip: 28,
            currentScene);
    }

    private void LayoutSingleWidget(
        HudWidgetBase? widget,
        string widgetType,
        double defaultWidthDip,
        double defaultHeightDip,
        SceneConfig? currentScene)
    {
        if (widget == null) return;

        var widgetConfig = _config.Widgets.FirstOrDefault(w => w.Type == widgetType);
        if (widgetConfig == null || !widgetConfig.Enabled)
        {
            widget.HideWindow();
            return;
        }

        if (currentScene != null && currentScene.ActiveWidgets.Count > 0 &&
            !currentScene.ActiveWidgets.Contains(widgetConfig.Id, StringComparer.OrdinalIgnoreCase))
        {
            widget.HideWindow();
            return;
        }

        var taskbar = _taskbarTracker.GetTaskbarForMonitor(widgetConfig.MonitorIndex);
        if (taskbar == null || taskbar.IsAutoHide || !taskbar.IsHorizontal)
        {
            widget.HideWindow();
            return;
        }

        widget.Config = widgetConfig;
        var tbRect = new PhysicalRect(taskbar.Bounds.Left, taskbar.Bounds.Top, taskbar.Bounds.Width, taskbar.Bounds.Height);
        widget.CurrentTaskbarBounds = tbRect;

        var dpi = VisualTreeHelper.GetDpi(widget).DpiScaleX;
        var bounds = WidgetLayoutCalculator.CalculatePhysicalBounds(
            tbRect,
            defaultWidthDip,
            defaultHeightDip,
            dpi,
            widgetConfig.DockAlignment,
            widgetConfig.OffsetX,
            widgetConfig.OffsetY);

        if (widget is CurrentModeWidget cmw) cmw.SetWidgetOpacity(widgetConfig.Opacity);
        else if (widget is FocusTimerWidget ftw) ftw.SetWidgetOpacity(widgetConfig.Opacity);
        else if (widget is ScratchpadWidget spw) spw.SetWidgetOpacity(widgetConfig.Opacity);

        widget.PositionPhysical(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        widget.ShowNoActivate();
        widget.EnsureTopmost();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _taskbarTracker.TaskbarsChanged -= OnTaskbarsChanged;
        _foregroundTracker.ForegroundWindowChanged -= OnForegroundWindowChanged;
        _sceneManager.CurrentSceneChanged -= OnCurrentSceneChanged;

        _currentModeWidget?.Close();
        _focusTimerWidget?.Close();
        _scratchpadWidget?.Close();
    }
}
