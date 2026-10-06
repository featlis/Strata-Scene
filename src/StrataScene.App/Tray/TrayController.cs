using System.Windows.Controls;
using System.Windows.Media.Imaging;
using H.NotifyIcon;
using StrataScene.Core.Config;
using StrataScene.Core.Logging;

namespace StrataScene.App.Tray;

public sealed class TrayController : IDisposable
{
    private readonly ILog _log;
    private readonly TaskbarIcon _taskbarIcon;
    private readonly Action _onExit;

    public Action<SceneConfig>? OnSelectScene { get; set; }
    public Action? OnRestore { get; set; }
    public Action? OnToggleEditWidgets { get; set; }
    public Action? OnOpenSettings { get; set; }
    public Action? OnOpenLauncher { get; set; }

    public TrayController(ILog log, Action onExit)
    {
        _log = log;
        _onExit = onExit;

        _taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "Strata Scene 0.1.0"
        };

        _taskbarIcon.TrayMouseDoubleClick += (_, _) =>
        {
            OnOpenLauncher?.Invoke();
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

    public void UpdateMenu(AppConfig config, string? currentSceneId, bool isEditMode = false)
    {
        var menu = new ContextMenu();

        // 1. Scene submenu
        var sceneMenu = new MenuItem { Header = "Scene" };
        foreach (var scene in config.Scenes)
        {
            var item = new MenuItem
            {
                Header = scene.Name,
                IsChecked = string.Equals(scene.Id, currentSceneId, StringComparison.OrdinalIgnoreCase)
            };
            var capturedScene = scene;
            item.Click += (_, _) => OnSelectScene?.Invoke(capturedScene);
            sceneMenu.Items.Add(item);
        }
        menu.Items.Add(sceneMenu);

        // 2. Restore windows
        var restoreItem = new MenuItem { Header = "ウィンドウを復元" };
        restoreItem.Click += (_, _) => OnRestore?.Invoke();
        menu.Items.Add(restoreItem);

        // 3. Edit widgets toggle
        var editWidgetsItem = new MenuItem
        {
            Header = "ウィジェット配置の編集",
            IsChecked = isEditMode
        };
        editWidgetsItem.Click += (_, _) => OnToggleEditWidgets?.Invoke();
        menu.Items.Add(editWidgetsItem);

        // 4. Settings
        var settingsItem = new MenuItem { Header = "設定..." };
        settingsItem.Click += (_, _) => OnOpenSettings?.Invoke();
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Separator());

        // 5. Exit
        var exitItem = new MenuItem { Header = "終了" };
        exitItem.Click += (_, _) => _onExit();
        menu.Items.Add(exitItem);

        _taskbarIcon.ContextMenu = menu;

        // Tooltip update
        var currentSceneName = config.Scenes.FirstOrDefault(s => s.Id == currentSceneId)?.Name ?? "—";
        _taskbarIcon.ToolTipText = $"Strata Scene 0.1.0 — {currentSceneName}";
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
