namespace StrataScene.Core.Logging;

public interface ILog
{
    void Info(string message);
    void Warn(string message, Exception? exception = null);
    void Error(string message, Exception? exception = null);
    void Debug(string message);
}
