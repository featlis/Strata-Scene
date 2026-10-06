using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using StrataScene.Core.Config;
using StrataScene.Core.Logging;
using StrataScene.Core.Search;
using StrataScene.Platform;

namespace StrataScene.App.Launcher;

public partial class LauncherWindow : Window
{
    private readonly ILog _log;
    private readonly Func<AppConfig> _getConfig;
    private readonly Action<SceneConfig> _onExecuteScene;
    private readonly Action _onRestoreScene;
    private readonly Action _onToggleEditMode;
    private readonly Action _onOpenSettings;
    private readonly Action _onExitApp;

    private readonly List<LauncherItem> _allItems = [];
    private readonly List<LauncherItem> _filteredItems = [];
    private IntPtr _previousForegroundHwnd;
    private bool _isClosingExplicitly;

    public LauncherWindow(
        ILog log,
        Func<AppConfig> getConfig,
        Action<SceneConfig> onExecuteScene,
        Action onRestoreScene,
        Action onToggleEditMode,
        Action onOpenSettings,
        Action onExitApp)
    {
        _log = log;
        _getConfig = getConfig;
        _onExecuteScene = onExecuteScene;
        _onRestoreScene = onRestoreScene;
        _onToggleEditMode = onToggleEditMode;
        _onOpenSettings = onOpenSettings;
        _onExitApp = onExitApp;

        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        Deactivated += OnDeactivated;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        DwmHelper.ApplyModernWindowDecorations(hwnd, darkMode: true, roundCorners: true);
    }

    public void Summon()
    {
        _previousForegroundHwnd = WindowStyles.GetForegroundWindow();
        RebuildItems();

        SearchBox.Text = string.Empty;
        UpdateFilter(string.Empty);

        PositionOnTargetMonitor();

        Visibility = Visibility.Visible;
        Activate();
        SearchBox.Focus();
    }

    public void Dismiss()
    {
        if (Visibility != Visibility.Visible) return;

        Visibility = Visibility.Collapsed;

        if (_previousForegroundHwnd != IntPtr.Zero)
        {
            WindowStyles.RestoreForeground(_previousForegroundHwnd);
            _previousForegroundHwnd = IntPtr.Zero;
        }
    }

    private void PositionOnTargetMonitor()
    {
        var config = _getConfig();
        var monitorPref = config.GlobalSettings.LauncherMonitor;
        var workArea = WindowStyles.GetMonitorWorkArea(monitorPref);

        var dpi = VisualTreeHelper.GetDpi(this);
        var dpiX = dpi.DpiScaleX;
        var dpiY = dpi.DpiScaleY;

        var targetWidthDip = Width;
        var targetHeightDip = Height;

        var targetWidthPx = targetWidthDip * dpiX;
        var targetHeightPx = targetHeightDip * dpiY;

        var leftPx = workArea.Left + ((workArea.Width - targetWidthPx) / 2.0);
        var topPx = workArea.Top + ((workArea.Height - targetHeightPx) / 2.5); // Slightly above dead center

        Left = leftPx / dpiX;
        Top = topPx / dpiY;
    }

    private void RebuildItems()
    {
        _allItems.Clear();
        var config = _getConfig();

        // 1. Scenes
        foreach (var scene in config.Scenes)
        {
            _allItems.Add(new LauncherItem(
                Id: scene.Id,
                Title: scene.Name,
                Subtitle: "シーン切り替え",
                Color: string.IsNullOrWhiteSpace(scene.Color) ? "#0078D4" : scene.Color,
                Shortcut: scene.Shortcut,
                Kind: LauncherItemKind.Scene));
        }

        // 2. System Commands
        _allItems.Add(new LauncherItem(
            Id: "restore",
            Title: "シーン復元 (ウィンドウ復元)",
            Subtitle: "直前のシーン切り替え前のウィンドウ配置を復元",
            Color: "#00BCF9",
            Shortcut: config.GlobalSettings.Hotkeys.RestoreScene,
            Kind: LauncherItemKind.Command,
            CommandAction: "restore"));

        _allItems.Add(new LauncherItem(
            Id: "edit_widgets",
            Title: "ウィジェット配置編集",
            Subtitle: "タスクバー上のウィジェットの位置調整モードを切り替え",
            Color: "#FFB900",
            Shortcut: config.GlobalSettings.Hotkeys.EditWidgets,
            Kind: LauncherItemKind.Command,
            CommandAction: "edit_widgets"));

        _allItems.Add(new LauncherItem(
            Id: "settings",
            Title: "設定を開く",
            Subtitle: "Strata Scene の設定ウィンドウを開く",
            Color: "#888899",
            Shortcut: null,
            Kind: LauncherItemKind.Command,
            CommandAction: "settings"));

        _allItems.Add(new LauncherItem(
            Id: "exit",
            Title: "Strata Scene を終了",
            Subtitle: "常駐を解除してアプリケーションを終了",
            Color: "#E81123",
            Shortcut: null,
            Kind: LauncherItemKind.Command,
            CommandAction: "exit"));
    }

    private void UpdateFilter(string query)
    {
        _filteredItems.Clear();
        _filteredItems.AddRange(FuzzyMatcher.Match(_allItems, query));

        ResultsList.ItemsSource = null;
        ResultsList.ItemsSource = _filteredItems;

        if (_filteredItems.Count > 0)
        {
            ResultsList.SelectedIndex = 0;
        }
    }

    private void ExecuteSelectedItem()
    {
        if (ResultsList.SelectedItem is LauncherItem selected)
        {
            Dismiss();
            try
            {
                if (selected.Kind == LauncherItemKind.Scene)
                {
                    var config = _getConfig();
                    var scene = config.Scenes.FirstOrDefault(s => s.Id == selected.Id);
                    if (scene != null)
                    {
                        _onExecuteScene(scene);
                    }
                }
                else if (selected.Kind == LauncherItemKind.Command)
                {
                    switch (selected.CommandAction)
                    {
                        case "restore":
                            _onRestoreScene();
                            break;
                        case "edit_widgets":
                            _onToggleEditMode();
                            break;
                        case "settings":
                            _onOpenSettings();
                            break;
                        case "exit":
                            _onExitApp();
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to execute launcher command {selected.Id}", ex);
            }
        }
    }

    private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateFilter(SearchBox.Text);
    }

    private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Down:
                e.Handled = true;
                if (ResultsList.SelectedIndex < _filteredItems.Count - 1)
                {
                    ResultsList.SelectedIndex++;
                    ResultsList.ScrollIntoView(ResultsList.SelectedItem);
                }
                break;

            case Key.Up:
                e.Handled = true;
                if (ResultsList.SelectedIndex > 0)
                {
                    ResultsList.SelectedIndex--;
                    ResultsList.ScrollIntoView(ResultsList.SelectedItem);
                }
                break;

            case Key.Enter:
                e.Handled = true;
                ExecuteSelectedItem();
                break;

            case Key.Escape:
                e.Handled = true;
                Dismiss();
                break;
        }
    }

    private void OnResultsListPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            ExecuteSelectedItem();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Dismiss();
        }
    }

    private void OnResultsListMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ExecuteSelectedItem();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (!_isClosingExplicitly && Visibility == Visibility.Visible)
        {
            Dismiss();
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_isClosingExplicitly)
        {
            e.Cancel = true;
            Dismiss();
        }
        else
        {
            base.OnClosing(e);
        }
    }

    public void ForceClose()
    {
        _isClosingExplicitly = true;
        Close();
    }
}
