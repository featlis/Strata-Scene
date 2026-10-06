using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using StrataScene.Core.Hotkeys;
using StrataScene.Core.Logging;

namespace StrataScene.Platform;

public sealed record HotkeyConflict(int Id, HotkeyGesture Gesture, string Reason);

public sealed class HotkeyService : IDisposable
{
    private const int ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

    private readonly ILog _log;
    private readonly MessageWindow _messageWindow;
    private readonly HWND _hwnd;
    private readonly Dictionary<int, HotkeyGesture> _registered = new();
    private readonly Lock _syncLock = new();
    private bool _isSuspended;

    public event Action<int, long>? HotkeyPressed;

    public HotkeyService(ILog log)
    {
        _log = log;
        _messageWindow = new MessageWindow(log, (id, timestamp) =>
        {
            HotkeyPressed?.Invoke(id, timestamp);
        });
        _hwnd = (HWND)_messageWindow.Handle;
    }

    public IReadOnlyList<HotkeyConflict> ApplyAll(IReadOnlyList<(int Id, HotkeyGesture Gesture)> targets)
    {
        lock (_syncLock)
        {
            UnregisterAllInternal();

            var conflicts = new List<HotkeyConflict>();

            if (_isSuspended)
            {
                // Remember targets so ResumeAll will register them
                foreach (var (id, gesture) in targets)
                {
                    _registered[id] = gesture;
                }
                return conflicts;
            }

            foreach (var (id, gesture) in targets)
            {
                if (RegisterInternal(id, gesture, out var reason))
                {
                    _registered[id] = gesture;
                }
                else
                {
                    conflicts.Add(new HotkeyConflict(id, gesture, reason));
                }
            }

            return conflicts;
        }
    }

    public void SuspendAll()
    {
        lock (_syncLock)
        {
            if (_isSuspended) return;
            _isSuspended = true;
            foreach (var id in _registered.Keys)
            {
                PInvoke.UnregisterHotKey(_hwnd, id);
            }
            _log.Debug("All hotkeys suspended.");
        }
    }

    public void ResumeAll()
    {
        lock (_syncLock)
        {
            if (!_isSuspended) return;
            _isSuspended = false;

            var copy = _registered.ToList();
            _registered.Clear();

            foreach (var (id, gesture) in copy)
            {
                if (RegisterInternal(id, gesture, out _))
                {
                    _registered[id] = gesture;
                }
            }
            _log.Debug("All hotkeys resumed.");
        }
    }

    private bool RegisterInternal(int id, HotkeyGesture gesture, out string reason)
    {
        reason = string.Empty;
        var mods = (HOT_KEY_MODIFIERS)gesture.Modifiers | HOT_KEY_MODIFIERS.MOD_NOREPEAT;

        var success = PInvoke.RegisterHotKey(_hwnd, id, mods, gesture.VirtualKey);
        if (success)
        {
            _log.Info($"Registered hotkey id={id}: {gesture}");
            return true;
        }

        var errorCode = Marshal.GetLastWin32Error();
        if (errorCode == ERROR_HOTKEY_ALREADY_REGISTERED)
        {
            reason = $"ショートカット '{gesture}' は他のアプリケーションで使用されています。";
        }
        else
        {
            reason = $"ショートカット '{gesture}' の登録に失敗しました (エラーコード: {errorCode})。";
        }

        _log.Warn($"Failed to register hotkey id={id}: {gesture}. {reason}");
        return false;
    }

    private void UnregisterAllInternal()
    {
        foreach (var id in _registered.Keys)
        {
            PInvoke.UnregisterHotKey(_hwnd, id);
        }
        _registered.Clear();
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            UnregisterAllInternal();
            _messageWindow.Dispose();
        }
    }
}
