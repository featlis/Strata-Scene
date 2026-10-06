using System.Text.RegularExpressions;
using StrataScene.Core.Hotkeys;

namespace StrataScene.Core.Config;

public static partial class ConfigValidator
{
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColorRegex();

    public static List<string> Validate(AppConfig config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.Version))
        {
            errors.Add("設定のバージョンが指定されていません。");
        }

        // Validate hotkeys
        ValidateHotkey(config.GlobalSettings.Hotkeys.LauncherToggle, "ランチャー表示ホットキー", errors);
        ValidateHotkey(config.GlobalSettings.Hotkeys.RestoreScene, "ウィンドウ復元ホットキー", errors);
        ValidateHotkey(config.GlobalSettings.Hotkeys.EditWidgets, "ウィジェット配置編集ホットキー", errors);

        // Validate Scenes
        var sceneIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var scene in config.Scenes)
        {
            if (string.IsNullOrWhiteSpace(scene.Id))
            {
                errors.Add("Scene ID が空です。");
            }
            else if (!sceneIds.Add(scene.Id))
            {
                errors.Add($"Scene ID '{scene.Id}' が重複しています。");
            }

            if (string.IsNullOrWhiteSpace(scene.Name))
            {
                errors.Add($"Scene '{scene.Id}' の名前が空です。");
            }

            if (!string.IsNullOrWhiteSpace(scene.Shortcut))
            {
                ValidateHotkey(scene.Shortcut, $"Scene '{scene.Name}' のショートカット", errors);
            }

            if (!string.IsNullOrWhiteSpace(scene.Color) && !HexColorRegex().IsMatch(scene.Color))
            {
                errors.Add($"Scene '{scene.Name}' のカラーコード '{scene.Color}' が無効です (#RRGGBB 形式で指定してください)。");
            }
        }

        // Validate Widgets
        var widgetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var widget in config.Widgets)
        {
            if (string.IsNullOrWhiteSpace(widget.Id))
            {
                errors.Add("ウィジェット ID が空です。");
            }
            else if (!widgetIds.Add(widget.Id))
            {
                errors.Add($"ウィジェット ID '{widget.Id}' が重複しています。");
            }

            if (widget.Opacity is < 0.1 or > 1.0)
            {
                errors.Add($"ウィジェット '{widget.Id}' の不透明度は 0.1 から 1.0 の間で指定してください。");
            }
        }

        return errors;
    }

    private static void ValidateHotkey(string? hotkeyStr, string label, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(hotkeyStr)) return;

        if (!HotkeyGesture.TryParse(hotkeyStr, out _))
        {
            errors.Add($"{label} '{hotkeyStr}' の形式が無効です。");
        }
    }
}
