using System.Diagnostics;

namespace StrataScene.Core.Logging;

public sealed class FileLog : ILog
{
    private const long MaxFileSize = 1024 * 1024; // 1 MB
    private const int MaxGenerations = 3;

    private readonly string _logDirectory;
    private readonly Lock _syncLock = new();

    public FileLog(string? directory = null)
    {
        _logDirectory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "StrataScene",
            "logs");

        Directory.CreateDirectory(_logDirectory);
    }

    public string LogDirectory => _logDirectory;

    public void Info(string message) => Write("INFO", message, null);
    public void Debug(string message) => Write("DEBUG", message, null);
    public void Warn(string message, Exception? exception = null) => Write("WARN", message, exception);
    public void Error(string message, Exception? exception = null) => Write("ERROR", message, exception);

    private void Write(string level, string message, Exception? ex)
    {
        var now = DateTime.Now;
        var dateStr = now.ToString("yyyyMMdd");
        var baseFilePath = Path.Combine(_logDirectory, $"strata-{dateStr}.log");

        var text = $"[{now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        if (ex != null)
        {
            text += Environment.NewLine + ex;
        }
        text += Environment.NewLine;

        lock (_syncLock)
        {
            try
            {
                RotateIfNeeded(baseFilePath);
                File.AppendAllText(baseFilePath, text);
            }
            catch (Exception writeEx)
            {
                Trace.TraceError($"Failed to write log: {writeEx}");
            }
        }
    }

    private static void RotateIfNeeded(string baseFilePath)
    {
        if (!File.Exists(baseFilePath)) return;

        var fileInfo = new FileInfo(baseFilePath);
        if (fileInfo.Length < MaxFileSize) return;

        for (var i = MaxGenerations - 1; i >= 1; i--)
        {
            var older = $"{baseFilePath}.{i}";
            var newer = $"{baseFilePath}.{i + 1}";
            if (File.Exists(older))
            {
                File.Move(older, newer, overwrite: true);
            }
        }

        File.Move(baseFilePath, $"{baseFilePath}.1", overwrite: true);
    }
}
