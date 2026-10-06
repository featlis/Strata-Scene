namespace StrataScene.Core.Config;

public static class ConfigDefaults
{
    public static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            Schema = "./schema.json",
            Version = "0.1",
            GlobalSettings = new GlobalSettings
            {
                RunAtStartup = false,
                Theme = "Dark",
                LauncherMonitor = "Cursor",
                MinimizeExclude = [],
                Hotkeys = new GlobalHotkeys
                {
                    LauncherToggle = "Alt+Space",
                    RestoreScene = "Ctrl+Alt+Back",
                    EditWidgets = "Ctrl+Shift+E"
                }
            },
            Scenes =
            [
                new SceneConfig
                {
                    Id = "scene_work",
                    Name = "Work",
                    Shortcut = "Ctrl+Alt+W",
                    Color = "#0078D4",
                    Actions = new SceneActions
                    {
                        MinimizeOthers = true,
                        Launch =
                        [
                            new LaunchItem { Path = "notepad.exe", Args = string.Empty },
                            new LaunchItem { Path = "https://calendar.google.com", Args = string.Empty }
                        ],
                        CloseOrMinimize = [],
                        System = new SystemSettings { FocusAssist = false, AudioOutput = null }
                    },
                    ActiveWidgets = ["widget_mode", "widget_pomodoro", "widget_scratchpad"]
                },
                new SceneConfig
                {
                    Id = "scene_game",
                    Name = "Game",
                    Shortcut = "Ctrl+Alt+G",
                    Color = "#107C10",
                    Actions = new SceneActions
                    {
                        MinimizeOthers = true,
                        Launch = [],
                        CloseOrMinimize = [],
                        System = new SystemSettings { FocusAssist = false, AudioOutput = null }
                    },
                    ActiveWidgets = ["widget_mode"]
                }
            ],
            Widgets =
            [
                new WidgetConfig
                {
                    Id = "widget_mode",
                    Type = "CurrentMode",
                    Enabled = true,
                    MonitorIndex = 0,
                    DockAlignment = "TaskbarRight",
                    OffsetX = -180,
                    OffsetY = 6,
                    Opacity = 0.9
                },
                new WidgetConfig
                {
                    Id = "widget_pomodoro",
                    Type = "FocusTimer",
                    Enabled = true,
                    MonitorIndex = 0,
                    DockAlignment = "TaskbarRight",
                    OffsetX = -320,
                    OffsetY = 6,
                    Opacity = 0.85,
                    WorkMinutes = 25,
                    BreakMinutes = 5
                },
                new WidgetConfig
                {
                    Id = "widget_scratchpad",
                    Type = "Scratchpad",
                    Enabled = true,
                    MonitorIndex = 0,
                    DockAlignment = "TaskbarCenter",
                    OffsetX = 260,
                    OffsetY = 6,
                    Opacity = 0.9,
                    Width = 240
                }
            ]
        };
    }
}
