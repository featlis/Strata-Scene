using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;
using StrataScene.Core.Config;
using StrataScene.Core.Logging;
using StrataScene.Platform;

namespace StrataScene.App.Settings;

public partial class SettingsWindow : Window
{
    private readonly ILog _log;
    private readonly Action<AppConfig> _onSave;
    private readonly string _configDirectory;
    private readonly AppConfig _workingConfig;
    private SceneConfig? _currentSelectedScene;

    public SettingsWindow(
        ILog log,
        AppConfig initialConfig,
        string configDirectory,
        Action<AppConfig> onSave)
    {
        _log = log;
        _configDirectory = configDirectory;
        _onSave = onSave;
        _workingConfig = initialConfig.Clone();

        InitializeComponent();

        SourceInitialized += OnSourceInitialized;
        LoadFromConfig();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        DwmHelper.ApplyModernWindowDecorations(hwnd, darkMode: true, roundCorners: true);
    }

    private void LoadFromConfig()
    {
        // 1. General Tab
        RunAtStartupCheckBox.IsChecked = _workingConfig.GlobalSettings.RunAtStartup;

        foreach (ComboBoxItem item in LauncherMonitorComboBox.Items)
        {
            if (string.Equals(item.Tag?.ToString(), _workingConfig.GlobalSettings.LauncherMonitor, StringComparison.OrdinalIgnoreCase))
            {
                LauncherMonitorComboBox.SelectedItem = item;
                break;
            }
        }
        if (LauncherMonitorComboBox.SelectedItem == null && LauncherMonitorComboBox.Items.Count > 0)
        {
            LauncherMonitorComboBox.SelectedIndex = 0;
        }

        LauncherHotkeyBox.Text = _workingConfig.GlobalSettings.Hotkeys.LauncherToggle;
        RestoreSceneHotkeyBox.Text = _workingConfig.GlobalSettings.Hotkeys.RestoreScene;
        EditWidgetsHotkeyBox.Text = _workingConfig.GlobalSettings.Hotkeys.EditWidgets;

        MinimizeExcludeTextBox.Text = string.Join(Environment.NewLine, _workingConfig.GlobalSettings.MinimizeExclude);

        // 2. Scenes Tab
        RefreshScenesList();
        if (_workingConfig.Scenes.Count > 0)
        {
            ScenesListBox.SelectedIndex = 0;
        }

        // 3. Widgets Tab
        var modeWidget = _workingConfig.Widgets.FirstOrDefault(w => w.Type == "CurrentMode");
        if (modeWidget != null)
        {
            CurrentModeEnabledCheckBox.IsChecked = modeWidget.Enabled;
            SelectComboByTag(CurrentModeDockComboBox, modeWidget.DockAlignment);
        }

        var pomodoroWidget = _workingConfig.Widgets.FirstOrDefault(w => w.Type == "FocusTimer");
        if (pomodoroWidget != null)
        {
            FocusTimerEnabledCheckBox.IsChecked = pomodoroWidget.Enabled;
            FocusTimerWorkMinutesBox.Text = (pomodoroWidget.WorkMinutes ?? 25).ToString();
            FocusTimerBreakMinutesBox.Text = (pomodoroWidget.BreakMinutes ?? 5).ToString();
        }

        var scratchpadWidget = _workingConfig.Widgets.FirstOrDefault(w => w.Type == "Scratchpad");
        if (scratchpadWidget != null)
        {
            ScratchpadEnabledCheckBox.IsChecked = scratchpadWidget.Enabled;
            ScratchpadWidthBox.Text = ((int)(scratchpadWidget.Width ?? 240)).ToString();
        }
    }

    private void SelectComboByTag(ComboBox combo, string? tagValue)
    {
        foreach (ComboBoxItem item in combo.Items)
        {
            if (string.Equals(item.Tag?.ToString(), tagValue, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private void RefreshScenesList()
    {
        ScenesListBox.ItemsSource = null;
        ScenesListBox.ItemsSource = _workingConfig.Scenes;
    }

    private void SaveCurrentSceneFields()
    {
        if (_currentSelectedScene == null) return;

        _currentSelectedScene.Name = SceneNameTextBox.Text.Trim();
        _currentSelectedScene.Shortcut = string.IsNullOrWhiteSpace(SceneHotkeyBox.Text) ? null : SceneHotkeyBox.Text.Trim();
        _currentSelectedScene.Color = string.IsNullOrWhiteSpace(SceneColorTextBox.Text) ? "#0078D4" : SceneColorTextBox.Text.Trim();
        _currentSelectedScene.Actions.MinimizeOthers = SceneMinimizeOthersCheckBox.IsChecked ?? true;

        var activeWidgets = new List<string>();
        if (WidgetModeActiveCheckBox.IsChecked == true) activeWidgets.Add("widget_mode");
        if (WidgetPomodoroActiveCheckBox.IsChecked == true) activeWidgets.Add("widget_pomodoro");
        if (WidgetScratchpadActiveCheckBox.IsChecked == true) activeWidgets.Add("widget_scratchpad");
        _currentSelectedScene.ActiveWidgets = activeWidgets;
    }

    private void OnSceneSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SaveCurrentSceneFields();

        if (ScenesListBox.SelectedItem is SceneConfig scene)
        {
            _currentSelectedScene = scene;
            SceneDetailPanel.Visibility = Visibility.Visible;

            SceneNameTextBox.Text = scene.Name;
            SceneHotkeyBox.Text = scene.Shortcut ?? string.Empty;
            SceneColorTextBox.Text = scene.Color;
            SceneMinimizeOthersCheckBox.IsChecked = scene.Actions.MinimizeOthers;

            LaunchAppsListBox.ItemsSource = null;
            LaunchAppsListBox.ItemsSource = scene.Actions.Launch.Select(l => $"{l.Path} {l.Args}").ToList();

            WidgetModeActiveCheckBox.IsChecked = scene.ActiveWidgets.Contains("widget_mode", StringComparer.OrdinalIgnoreCase);
            WidgetPomodoroActiveCheckBox.IsChecked = scene.ActiveWidgets.Contains("widget_pomodoro", StringComparer.OrdinalIgnoreCase);
            WidgetScratchpadActiveCheckBox.IsChecked = scene.ActiveWidgets.Contains("widget_scratchpad", StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            _currentSelectedScene = null;
            SceneDetailPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void OnAddSceneClick(object sender, RoutedEventArgs e)
    {
        SaveCurrentSceneFields();

        var newId = "scene_" + Guid.NewGuid().ToString("N")[..8];
        var newScene = new SceneConfig
        {
            Id = newId,
            Name = "新規シーン",
            Color = "#0078D4",
            Actions = new SceneActions { MinimizeOthers = true },
            ActiveWidgets = ["widget_mode"]
        };

        _workingConfig.Scenes.Add(newScene);
        RefreshScenesList();
        ScenesListBox.SelectedItem = newScene;
    }

    private void OnDuplicateSceneClick(object sender, RoutedEventArgs e)
    {
        if (_currentSelectedScene == null) return;
        SaveCurrentSceneFields();

        var clone = new SceneConfig
        {
            Id = "scene_" + Guid.NewGuid().ToString("N")[..8],
            Name = _currentSelectedScene.Name + " (コピー)",
            Shortcut = null,
            Color = _currentSelectedScene.Color,
            Actions = new SceneActions
            {
                MinimizeOthers = _currentSelectedScene.Actions.MinimizeOthers,
                Launch = _currentSelectedScene.Actions.Launch.Select(l => new LaunchItem { Path = l.Path, Args = l.Args }).ToList(),
                CloseOrMinimize = _currentSelectedScene.Actions.CloseOrMinimize.Select(c => new CloseOrMinimizeItem(c.Process, c.Mode)).ToList()
            },
            ActiveWidgets = [.. _currentSelectedScene.ActiveWidgets]
        };

        _workingConfig.Scenes.Add(clone);
        RefreshScenesList();
        ScenesListBox.SelectedItem = clone;
    }

    private void OnDeleteSceneClick(object sender, RoutedEventArgs e)
    {
        if (_currentSelectedScene == null) return;

        if (_workingConfig.Scenes.Count <= 1)
        {
            MessageBox.Show(this, "少なくとも1つのシーンが必要です。", "削除不可", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var idx = _workingConfig.Scenes.IndexOf(_currentSelectedScene);
        _workingConfig.Scenes.Remove(_currentSelectedScene);
        _currentSelectedScene = null;

        RefreshScenesList();
        if (_workingConfig.Scenes.Count > 0)
        {
            ScenesListBox.SelectedIndex = Math.Clamp(idx, 0, _workingConfig.Scenes.Count - 1);
        }
    }

    private void OnAddLaunchAppClick(object sender, RoutedEventArgs e)
    {
        if (_currentSelectedScene == null) return;

        var ofd = new OpenFileDialog
        {
            Title = "起動する実行ファイルを選択",
            Filter = "実行可能ファイル (*.exe;*.bat;*.cmd)|*.exe;*.bat;*.cmd|すべてのファイル (*.*)|*.*"
        };

        if (ofd.ShowDialog(this) == true)
        {
            _currentSelectedScene.Actions.Launch.Add(new LaunchItem
            {
                Path = ofd.FileName,
                Args = string.Empty
            });

            LaunchAppsListBox.ItemsSource = null;
            LaunchAppsListBox.ItemsSource = _currentSelectedScene.Actions.Launch.Select(l => $"{l.Path} {l.Args}").ToList();
        }
    }

    private void OnRemoveLaunchAppClick(object sender, RoutedEventArgs e)
    {
        if (_currentSelectedScene == null) return;

        var idx = LaunchAppsListBox.SelectedIndex;
        if (idx >= 0 && idx < _currentSelectedScene.Actions.Launch.Count)
        {
            _currentSelectedScene.Actions.Launch.RemoveAt(idx);
            LaunchAppsListBox.ItemsSource = null;
            LaunchAppsListBox.ItemsSource = _currentSelectedScene.Actions.Launch.Select(l => $"{l.Path} {l.Args}").ToList();
        }
    }

    private void OnOpenConfigFolderClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Directory.Exists(_configDirectory))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", _configDirectory) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            _log.Error("Failed to open config directory", ex);
        }
    }

    private void OnOpenLogFolderClick(object sender, RoutedEventArgs e)
    {
        try
        {
            var logDir = Path.Combine(_configDirectory, "logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            Process.Start(new ProcessStartInfo("explorer.exe", logDir) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error("Failed to open log directory", ex);
        }
    }

    private void OnSaveAndApplyClick(object sender, RoutedEventArgs e)
    {
        SaveCurrentSceneFields();

        // 1. General Settings
        _workingConfig.GlobalSettings.RunAtStartup = RunAtStartupCheckBox.IsChecked ?? false;
        if (LauncherMonitorComboBox.SelectedItem is ComboBoxItem item)
        {
            _workingConfig.GlobalSettings.LauncherMonitor = item.Tag?.ToString() ?? "Cursor";
        }
        _workingConfig.GlobalSettings.Hotkeys.LauncherToggle = LauncherHotkeyBox.Text.Trim();
        _workingConfig.GlobalSettings.Hotkeys.RestoreScene = RestoreSceneHotkeyBox.Text.Trim();
        _workingConfig.GlobalSettings.Hotkeys.EditWidgets = EditWidgetsHotkeyBox.Text.Trim();

        _workingConfig.GlobalSettings.MinimizeExclude = MinimizeExcludeTextBox.Text
            .Split([Environment.NewLine, "\n", "\r"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        // 2. Widget Settings
        var modeWidget = _workingConfig.Widgets.FirstOrDefault(w => w.Type == "CurrentMode");
        if (modeWidget != null)
        {
            modeWidget.Enabled = CurrentModeEnabledCheckBox.IsChecked ?? true;
            if (CurrentModeDockComboBox.SelectedItem is ComboBoxItem dockItem)
            {
                modeWidget.DockAlignment = dockItem.Tag?.ToString() ?? "TaskbarRight";
            }
        }

        var pomodoroWidget = _workingConfig.Widgets.FirstOrDefault(w => w.Type == "FocusTimer");
        if (pomodoroWidget != null)
        {
            pomodoroWidget.Enabled = FocusTimerEnabledCheckBox.IsChecked ?? true;
            if (int.TryParse(FocusTimerWorkMinutesBox.Text, out var workMin)) pomodoroWidget.WorkMinutes = workMin;
            if (int.TryParse(FocusTimerBreakMinutesBox.Text, out var breakMin)) pomodoroWidget.BreakMinutes = breakMin;
        }

        var scratchpadWidget = _workingConfig.Widgets.FirstOrDefault(w => w.Type == "Scratchpad");
        if (scratchpadWidget != null)
        {
            scratchpadWidget.Enabled = ScratchpadEnabledCheckBox.IsChecked ?? true;
            if (double.TryParse(ScratchpadWidthBox.Text, out var width)) scratchpadWidget.Width = width;
        }

        // 3. Validation
        var errors = ConfigValidator.Validate(_workingConfig);
        if (errors.Count > 0)
        {
            var msg = "設定内容にエラーがあります:\n\n" + string.Join("\n", errors);
            MessageBox.Show(this, msg, "入力エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _onSave(_workingConfig);
            Close();
        }
        catch (Exception ex)
        {
            _log.Error("Failed to apply configuration", ex);
            MessageBox.Show(this, $"設定の保存・適用に失敗しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
