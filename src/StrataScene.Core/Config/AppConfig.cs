using System.Text.Json.Serialization;

namespace StrataScene.Core.Config;

public sealed class AppConfig
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; } = "./schema.json";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "0.1";

    [JsonPropertyName("global_settings")]
    public GlobalSettings GlobalSettings { get; set; } = new();

    [JsonPropertyName("scenes")]
    public List<SceneConfig> Scenes { get; set; } = [];

    [JsonPropertyName("widgets")]
    public List<WidgetConfig> Widgets { get; set; } = [];

    public AppConfig Clone()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(this, ConfigJsonContext.Default.AppConfig);
        return System.Text.Json.JsonSerializer.Deserialize(json, ConfigJsonContext.Default.AppConfig)!;
    }
}

public sealed class GlobalSettings
{
    [JsonPropertyName("run_at_startup")]
    public bool RunAtStartup { get; set; } = false;

    [JsonPropertyName("theme")]
    public string Theme { get; set; } = "Dark";

    [JsonPropertyName("launcher_monitor")]
    public string LauncherMonitor { get; set; } = "Cursor";

    [JsonPropertyName("minimize_exclude")]
    public List<string> MinimizeExclude { get; set; } = [];

    [JsonPropertyName("hotkeys")]
    public GlobalHotkeys Hotkeys { get; set; } = new();
}

public sealed class GlobalHotkeys
{
    [JsonPropertyName("launcher_toggle")]
    public string LauncherToggle { get; set; } = "Alt+Space";

    [JsonPropertyName("restore_scene")]
    public string RestoreScene { get; set; } = "Ctrl+Alt+Back";

    [JsonPropertyName("edit_widgets")]
    public string EditWidgets { get; set; } = "Ctrl+Shift+E";
}

public sealed class SceneConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("shortcut")]
    public string? Shortcut { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = "#0078D4";

    [JsonPropertyName("actions")]
    public SceneActions Actions { get; set; } = new();

    [JsonPropertyName("active_widgets")]
    public List<string> ActiveWidgets { get; set; } = [];
}

public sealed class SceneActions
{
    [JsonPropertyName("minimize_others")]
    public bool MinimizeOthers { get; set; } = true;

    [JsonPropertyName("launch")]
    public List<LaunchItem> Launch { get; set; } = [];

    [JsonPropertyName("close_or_minimize")]
    public List<CloseOrMinimizeItem> CloseOrMinimize { get; set; } = [];

    [JsonPropertyName("system")]
    public SystemSettings System { get; set; } = new();
}

public sealed class LaunchItem
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public string Args { get; set; } = string.Empty;

    [JsonPropertyName("process_name")]
    public string? ProcessName { get; set; }
}

public sealed class SystemSettings
{
    [JsonPropertyName("focus_assist")]
    public bool FocusAssist { get; set; } = false;

    [JsonPropertyName("audio_output")]
    public string? AudioOutput { get; set; }
}

public sealed class WidgetConfig
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("monitor_index")]
    public int MonitorIndex { get; set; } = 0;

    [JsonPropertyName("dock_alignment")]
    public string DockAlignment { get; set; } = "TaskbarRight";

    [JsonPropertyName("offset_x")]
    public double OffsetX { get; set; } = 0;

    [JsonPropertyName("offset_y")]
    public double OffsetY { get; set; } = 6;

    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 0.9;

    [JsonPropertyName("width")]
    public double? Width { get; set; }

    [JsonPropertyName("work_minutes")]
    public int? WorkMinutes { get; set; }

    [JsonPropertyName("break_minutes")]
    public int? BreakMinutes { get; set; }
}
