using System.Diagnostics.CodeAnalysis;

namespace StrataScene.Core.Hotkeys;

public sealed class HotkeyGesture : IEquatable<HotkeyGesture>
{
    public uint Modifiers { get; }
    public uint VirtualKey { get; }
    public string KeyName { get; }

    public bool HasControl => (Modifiers & VirtualKeys.MOD_CONTROL) != 0;
    public bool HasAlt => (Modifiers & VirtualKeys.MOD_ALT) != 0;
    public bool HasShift => (Modifiers & VirtualKeys.MOD_SHIFT) != 0;
    public bool HasWin => (Modifiers & VirtualKeys.MOD_WIN) != 0;

    public HotkeyGesture(uint modifiers, uint virtualKey, string keyName)
    {
        Modifiers = modifiers;
        VirtualKey = virtualKey;
        KeyName = keyName;
    }

    public static bool TryParse(string? text, [NotNullWhen(true)] out HotkeyGesture? gesture)
    {
        gesture = null;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        uint mods = 0;
        uint vk = 0;
        string? resolvedKeyName = null;

        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            var upper = part.ToUpperInvariant();

            if (i < parts.Length - 1 || IsModifier(upper))
            {
                if (upper is "CTRL" or "CONTROL")
                {
                    mods |= VirtualKeys.MOD_CONTROL;
                }
                else if (upper == "ALT")
                {
                    mods |= VirtualKeys.MOD_ALT;
                }
                else if (upper == "SHIFT")
                {
                    mods |= VirtualKeys.MOD_SHIFT;
                }
                else if (upper is "WIN" or "WINDOWS")
                {
                    mods |= VirtualKeys.MOD_WIN;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (!TryResolveVirtualKey(part, out vk, out resolvedKeyName))
                {
                    return false;
                }
            }
        }

        if (mods == 0 || vk == 0 || resolvedKeyName == null)
        {
            return false;
        }

        gesture = new HotkeyGesture(mods, vk, resolvedKeyName);
        return true;
    }

    private static bool IsModifier(string upper) =>
        upper is "CTRL" or "CONTROL" or "ALT" or "SHIFT" or "WIN" or "WINDOWS";

    private static bool TryResolveVirtualKey(string key, out uint vk, [NotNullWhen(true)] out string? canonicalName)
    {
        vk = 0;
        canonicalName = null;

        var upper = key.Trim().ToUpperInvariant();

        // Single letter A-Z
        if (upper.Length == 1 && upper[0] >= 'A' && upper[0] <= 'Z')
        {
            vk = upper[0];
            canonicalName = upper;
            return true;
        }

        // Single digit 0-9
        if (upper.Length == 1 && upper[0] >= '0' && upper[0] <= '9')
        {
            vk = upper[0];
            canonicalName = upper;
            return true;
        }

        // Function keys F1-F24
        if (upper.StartsWith('F') && int.TryParse(upper[1..], out var fNum) && fNum is >= 1 and <= 24)
        {
            vk = (uint)(0x70 + (fNum - 1));
            canonicalName = $"F{fNum}";
            return true;
        }

        switch (upper)
        {
            case "SPACE":
                vk = VirtualKeys.VK_SPACE;
                canonicalName = "Space";
                return true;
            case "BACK" or "BACKSPACE":
                vk = VirtualKeys.VK_BACK;
                canonicalName = "Back";
                return true;
            case "ENTER" or "RETURN":
                vk = VirtualKeys.VK_RETURN;
                canonicalName = "Enter";
                return true;
            case "ESC" or "ESCAPE":
                vk = VirtualKeys.VK_ESCAPE;
                canonicalName = "Esc";
                return true;
            case "TAB":
                vk = VirtualKeys.VK_TAB;
                canonicalName = "Tab";
                return true;
            case "DELETE" or "DEL":
                vk = VirtualKeys.VK_DELETE;
                canonicalName = "Delete";
                return true;
            case "INSERT" or "INS":
                vk = VirtualKeys.VK_INSERT;
                canonicalName = "Insert";
                return true;
            case "HOME":
                vk = VirtualKeys.VK_HOME;
                canonicalName = "Home";
                return true;
            case "END":
                vk = VirtualKeys.VK_END;
                canonicalName = "End";
                return true;
            case "PAGEUP" or "PGUP":
                vk = VirtualKeys.VK_PRIOR;
                canonicalName = "PageUp";
                return true;
            case "PAGEDOWN" or "PGDN":
                vk = VirtualKeys.VK_NEXT;
                canonicalName = "PageDown";
                return true;
            case "UP":
                vk = VirtualKeys.VK_UP;
                canonicalName = "Up";
                return true;
            case "DOWN":
                vk = VirtualKeys.VK_DOWN;
                canonicalName = "Down";
                return true;
            case "LEFT":
                vk = VirtualKeys.VK_LEFT;
                canonicalName = "Left";
                return true;
            case "RIGHT":
                vk = VirtualKeys.VK_RIGHT;
                canonicalName = "Right";
                return true;
            case "OEMPLUS" or "PLUS":
                vk = VirtualKeys.VK_OEM_PLUS;
                canonicalName = "OemPlus";
                return true;
            case "OEMMINUS" or "MINUS":
                vk = VirtualKeys.VK_OEM_MINUS;
                canonicalName = "OemMinus";
                return true;
            case "OEMCOMMA" or "COMMA":
                vk = VirtualKeys.VK_OEM_COMMA;
                canonicalName = "OemComma";
                return true;
            case "OEMPERIOD" or "PERIOD":
                vk = VirtualKeys.VK_OEM_PERIOD;
                canonicalName = "OemPeriod";
                return true;
            default:
                return false;
        }
    }

    public override string ToString()
    {
        var parts = new List<string>(5);
        if (HasControl) parts.Add("Ctrl");
        if (HasAlt) parts.Add("Alt");
        if (HasShift) parts.Add("Shift");
        if (HasWin) parts.Add("Win");
        parts.Add(KeyName);
        return string.Join("+", parts);
    }

    public bool Equals(HotkeyGesture? other)
    {
        if (other is null) return false;
        return Modifiers == other.Modifiers && VirtualKey == other.VirtualKey;
    }

    public override bool Equals(object? obj) => Equals(obj as HotkeyGesture);

    public override int GetHashCode() => HashCode.Combine(Modifiers, VirtualKey);

    public static bool operator ==(HotkeyGesture? left, HotkeyGesture? right) =>
        Equals(left, right);

    public static bool operator !=(HotkeyGesture? left, HotkeyGesture? right) =>
        !Equals(left, right);
}
