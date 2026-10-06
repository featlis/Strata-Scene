using System.Windows;
using StrataScene.App.Hud;
using StrataScene.App.Launcher;
using StrataScene.App.Settings;
using StrataScene.App.Tray;
using StrataScene.Core.Config;
using StrataScene.Core.Hotkeys;
using StrataScene.Core.Logging;
using StrataScene.Core.Scenes;
using StrataScene.Core.State;
using StrataScene.Platform;

namespace StrataScene.App;

public sealed class AppController : IDisposable
{
    private const int HotkeyIdLauncher = 1;
    private const int HotkeyIdRestore = 2;
    private const int HotkeyIdEditWidgets = 3;
    private const int HotkeyIdSceneBase = 100;

    private readonly ILog _log;
    private readonly Action _onExit;
    private readonly ConfigRepository _configRepository;
    private readonly StateRepository _stateRepository;
    private readonly HotkeyService _hotkeyService;
    private readonly SceneHost _sceneHost;
    private readonly SceneManager _sceneManager;
    private readonly TaskbarTracker _taskbarTracker;
    private readonly ForegroundTracker _foregroundTracker;
    private readonly FullscreenDetector _fullscreenDetector;
    private readonly HudManager _hudManager;
    private readonly TrayController _trayController;
    private readonly LauncherWindow _launcherWindow;

    private SettingsWindow? _settingsWindow;
    private AppConfig _config;
    private bool _isEditMode;

    public AppConfig Config => _config;
    public SceneManager SceneManager => _sceneManager;
    public HotkeyService HotkeyService => _hotkeyService;
    public TrayController TrayController => _trayController;
    public HudManager HudManager => _hudManager;
    public FullscreenDetector FullscreenDetector => _fullscreenDetector;
    public LauncherWindow LauncherWindow => _launcherWindow;

    public AppController(ILog log, Action onExit)
    {
        _log = log;
        _onExit = onExit;

        _configRepository = new ConfigRepository(_log);
        _stateRepository = new StateRepository(_log);

        var (config, wasRecovered) = _configRepository.LoadOrCreate();
        _config = config;

        _hotkeyService = new HotkeyService(_log);
        _sceneHost = new SceneHost(_log);
        _sceneManager = new SceneManager(_log, _sceneHost);

        _taskbarTracker = new TaskbarTracker(_log);
        _foregroundTracker = new ForegroundTracker(_log);
        _fullscreenDetector = new FullscreenDetector(_log, _foregroundTracker);

        _trayController = new TrayController(_log, onExit);
        SetupTray();

        _hudManager = new HudManager(_log, _taskbarTracker, _foregroundTracker, _sceneManager, _config, OpenLauncher);
        _hudManager.SetInitialScratchpadText(_stateRepository.CurrentState.ScratchpadText);

        _hudManager.WidgetOffsetChanged = (widgetId, offsetX, offsetY) =>
        {
            var w = _config.Widgets.FirstOrDefault(x => x.Id == widgetId);
            if (w != null)
            {
                w.OffsetX = offsetX;
                w.OffsetY = offsetY;
                _configRepository.Save(_config);
            }
        };

        _hudManager.ScratchpadTextChanged = text =>
        {
            _stateRepository.Update(s => s.ScratchpadText = text);
        };

        _hudManager.TimerCompleted = (title, msg) =>
        {
            _trayController.Notify(title, msg);
        };

        _fullscreenDetector.FullscreenChanged += (isFs, mon) => _hudManager.SetFullscreenHidden(isFs, mon);

        _launcherWindow = new LauncherWindow(
            _log,
            () => _config,
            scene => _ = ExecuteScene(scene),
            () => _ = RestoreScene(),
            ToggleEditMode,
            OpenSettings,
            _onExit);

        _hotkeyService.HotkeyPressed += OnHotkeyPressed;
        _sceneManager.CurrentSceneChanged += OnCurrentSceneChanged;

        // Restore initial scene state from state.json if valid
        var savedSceneId = _stateRepository.CurrentState.CurrentSceneId;
        if (!string.IsNullOrEmpty(savedSceneId) && _config.Scenes.Any(s => s.Id == savedSceneId))
        {
            _sceneManager.SetCurrentSceneId(savedSceneId);
        }

        RegisterAllHotkeys();

        if (wasRecovered)
        {
            _trayController.Notify(
                "設定ファイルの復元",
                "破損した設定ファイルが見つかったため、既定の設定で復元しました。破損ファイルはバックアップされました。");
        }
    }

    private void SetupTray()
    {
        _trayController.OnSelectScene = scene =>
        {
            _ = ExecuteScene(scene);
        };

        _trayController.OnRestore = () =>
        {
            _ = RestoreScene();
        };

        _trayController.OnToggleEditWidgets = () =>
        {
            ToggleEditMode();
        };

        _trayController.OnOpenSettings = () =>
        {
            OpenSettings();
        };

        _trayController.OnOpenLauncher = () =>
        {
            OpenLauncher();
        };

        _trayController.UpdateMenu(_config, _sceneManager.CurrentSceneId, _isEditMode);
    }

    private void RegisterAllHotkeys()
    {
        var targets = new List<(int Id, HotkeyGesture Gesture)>();

        // 1. Launcher toggle
        if (HotkeyGesture.TryParse(_config.GlobalSettings.Hotkeys.LauncherToggle, out var launcherGesture))
        {
            targets.Add((HotkeyIdLauncher, launcherGesture));
        }

        // 2. Restore scene
        if (HotkeyGesture.TryParse(_config.GlobalSettings.Hotkeys.RestoreScene, out var restoreGesture))
        {
            targets.Add((HotkeyIdRestore, restoreGesture));
        }

        // 3. Edit widgets
        if (HotkeyGesture.TryParse(_config.GlobalSettings.Hotkeys.EditWidgets, out var editGesture))
        {
            targets.Add((HotkeyIdEditWidgets, editGesture));
        }

        // 4. Scene direct shortcuts
        for (var i = 0; i < _config.Scenes.Count; i++)
        {
            var scene = _config.Scenes[i];
            if (!string.IsNullOrWhiteSpace(scene.Shortcut) &&
                HotkeyGesture.TryParse(scene.Shortcut, out var sceneGesture))
            {
                targets.Add((HotkeyIdSceneBase + i, sceneGesture));
            }
        }

        var conflicts = _hotkeyService.ApplyAll(targets);
        foreach (var conflict in conflicts)
        {
            _trayController.Notify("ショートカット競合", conflict.Reason);
        }
    }

    private void OnHotkeyPressed(int id, long timestamp)
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (id == HotkeyIdLauncher)
            {
                OpenLauncher();
            }
            else if (id == HotkeyIdRestore)
            {
                _ = RestoreScene();
            }
            else if (id == HotkeyIdEditWidgets)
            {
                ToggleEditMode();
            }
            else if (id >= HotkeyIdSceneBase)
            {
                var sceneIndex = id - HotkeyIdSceneBase;
                if (sceneIndex >= 0 && sceneIndex < _config.Scenes.Count)
                {
                    _ = ExecuteScene(_config.Scenes[sceneIndex]);
                }
            }
        });
    }

    public async Task ExecuteScene(SceneConfig scene)
    {
        var result = await _sceneManager.ExecuteSceneAsync(scene, _config.GlobalSettings.MinimizeExclude);
        if (!result.Success && result.Failures.Count > 0)
        {
            _trayController.Notify(
                $"Scene '{scene.Name}' 実行エラー",
                string.Join(Environment.NewLine, result.Failures.Take(3)));
        }
    }

    public async Task RestoreScene()
    {
        var restored = await _sceneManager.RestoreSceneAsync();
        if (restored == 0)
        {
            _trayController.Notify("ウィンドウ復元", "復元できるウィンドウ状態がありません。");
        }
    }

    public void ToggleEditMode()
    {
        _isEditMode = !_isEditMode;
        _log.Info($"Edit mode toggled: {_isEditMode}");
        _hudManager.SetEditMode(_isEditMode);
        _trayController.UpdateMenu(_config, _sceneManager.CurrentSceneId, _isEditMode);
    }

    public void OpenLauncher()
    {
        _log.Info("Launcher requested.");
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _launcherWindow.Summon();
        });
    }

    public void OpenSettings()
    {
        _log.Info("Settings requested.");
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_settingsWindow != null && _settingsWindow.IsLoaded)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(
                _log,
                _config,
                _configRepository.ConfigDirectory,
                OnSaveSettings);

            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.Show();
        });
    }

    private void OnSaveSettings(AppConfig newConfig)
    {
        _config = newConfig;
        _configRepository.Save(_config);

        // Sync startup registration
        StartupRegistration.SetRegistration(_config.GlobalSettings.RunAtStartup);

        // Re-register hotkeys
        RegisterAllHotkeys();

        // Update tray menu
        _trayController.UpdateMenu(_config, _sceneManager.CurrentSceneId, _isEditMode);

        // Relayout HUD widgets
        _hudManager.UpdateConfig(_config);

        _log.Info("Settings updated and reloaded live.");
    }

    private void OnCurrentSceneChanged(string? sceneId)
    {
        _stateRepository.Update(s => s.CurrentSceneId = sceneId);
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _trayController.UpdateMenu(_config, sceneId, _isEditMode);
        });
    }

    public void Dispose()
    {
        _hotkeyService.Dispose();
        _fullscreenDetector.Dispose();
        _settingsWindow?.Close();
        _launcherWindow.ForceClose();
        _hudManager.Dispose();
        _foregroundTracker.Dispose();
        _taskbarTracker.Dispose();
        _stateRepository.Dispose();
        _trayController.Dispose();
    }
}
