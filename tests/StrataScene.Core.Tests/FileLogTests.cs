using StrataScene.Core.Logging;
using Xunit;

namespace StrataScene.Core.Tests;

public class FileLogTests
{
    [Fact]
    public void FileLog_WritesLogEntries()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "StrataSceneTests_" + Guid.NewGuid());
        try
        {
            var log = new FileLog(tempDir);
            log.Info("Test info message");
            log.Warn("Test warn message");
            log.Error("Test error message", new InvalidOperationException("boom"));

            var files = Directory.GetFiles(tempDir, "*.log");
            Assert.Single(files);

            var content = File.ReadAllText(files[0]);
            Assert.Contains("[INFO] Test info message", content);
            Assert.Contains("[WARN] Test warn message", content);
            Assert.Contains("[ERROR] Test error message", content);
            Assert.Contains("InvalidOperationException: boom", content);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
