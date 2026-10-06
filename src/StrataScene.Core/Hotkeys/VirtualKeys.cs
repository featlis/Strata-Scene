namespace StrataScene.Core.Hotkeys;

public static class VirtualKeys
{
    // Modifier flags for RegisterHotKey
    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;
    public const uint MOD_NOREPEAT = 0x4000;

    // Standard Win32 Virtual Keys
    public const uint VK_BACK = 0x08;
    public const uint VK_TAB = 0x09;
    public const uint VK_RETURN = 0x0D;
    public const uint VK_ESCAPE = 0x1B;
    public const uint VK_SPACE = 0x20;
    public const uint VK_PRIOR = 0x21; // Page Up
    public const uint VK_NEXT = 0x22;  // Page Down
    public const uint VK_END = 0x23;
    public const uint VK_HOME = 0x24;
    public const uint VK_LEFT = 0x25;
    public const uint VK_UP = 0x26;
    public const uint VK_RIGHT = 0x27;
    public const uint VK_DOWN = 0x28;
    public const uint VK_INSERT = 0x2D;
    public const uint VK_DELETE = 0x2E;

    // Digits '0'-'9': 0x30 - 0x39
    // Letters 'A'-'Z': 0x41 - 0x5A
    // Function keys F1-F24: 0x70 - 0x87

    public const uint VK_OEM_PLUS = 0xBB;
    public const uint VK_OEM_COMMA = 0xBC;
    public const uint VK_OEM_MINUS = 0xBD;
    public const uint VK_OEM_PERIOD = 0xBE;
}
