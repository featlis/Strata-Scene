namespace StrataScene.Core.Scenes;

public sealed record SceneExecutionResult(
    string SceneId,
    bool Success,
    int LaunchedCount,
    int MinimizedCount,
    int ClosedCount,
    IReadOnlyList<string> Failures,
    long ElapsedMilliseconds);
