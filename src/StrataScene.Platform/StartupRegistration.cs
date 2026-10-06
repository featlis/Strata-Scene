using Microsoft.Win32;

namespace StrataScene.Platform;

public static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "StrataScene";

    public static bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(ValueName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetRegistration(bool enable, string? executablePath = null)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return false;

            if (enable)
            {
                var path = executablePath ?? Environment.ProcessPath;
                if (!string.IsNullOrEmpty(path))
                {
                    key.SetValue(ValueName, $"\"{path}\"");
                    return true;
                }
                return false;
            }
            else
            {
                key.DeleteValue(ValueName, false);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
