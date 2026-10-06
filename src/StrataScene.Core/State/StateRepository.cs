using System.Text.Json;
using StrataScene.Core.Config;
using StrataScene.Core.Logging;

namespace StrataScene.Core.State;

public sealed class StateRepository : IDisposable
{
    private readonly string _stateFilePath;
    private readonly ILog _log;
    private readonly Lock _syncLock = new();
    private readonly Timer _debounceTimer;
    private AppState _currentState;
    private bool _isDirty;

    public StateRepository(ILog log, string? baseDirectory = null)
    {
        _log = log;
        var directory = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "StrataScene");

        Directory.CreateDirectory(directory);
        _stateFilePath = Path.Combine(directory, "state.json");
        _currentState = LoadInternal();
        _debounceTimer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public AppState CurrentState
    {
        get
        {
            lock (_syncLock)
            {
                return _currentState;
            }
        }
    }

    public void Update(Action<AppState> updateAction)
    {
        lock (_syncLock)
        {
            updateAction(_currentState);
            _isDirty = true;
            _debounceTimer.Change(500, Timeout.Infinite);
        }
    }

    public void Flush()
    {
        lock (_syncLock)
        {
            if (!_isDirty) return;
            SaveInternal();
            _isDirty = false;
        }
    }

    private void OnTimerTick(object? state)
    {
        lock (_syncLock)
        {
            if (_isDirty)
            {
                SaveInternal();
                _isDirty = false;
            }
        }
    }

    private AppState LoadInternal()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = File.ReadAllText(_stateFilePath);
                var loaded = JsonSerializer.Deserialize(json, ConfigJsonContext.Default.AppState);
                if (loaded != null) return loaded;
            }
        }
        catch (Exception ex)
        {
            _log.Warn("Failed to load state.json, initializing empty state", ex);
        }

        return new AppState();
    }

    private void SaveInternal()
    {
        try
        {
            var temp = _stateFilePath + ".tmp";
            var json = JsonSerializer.Serialize(_currentState, ConfigJsonContext.Default.AppState);
            File.WriteAllText(temp, json);
            if (File.Exists(_stateFilePath))
            {
                File.Replace(temp, _stateFilePath, null);
            }
            else
            {
                File.Move(temp, _stateFilePath);
            }
        }
        catch (Exception ex)
        {
            _log.Error("Failed to save state.json", ex);
        }
    }

    public void Dispose()
    {
        _debounceTimer.Dispose();
        Flush();
    }
}
