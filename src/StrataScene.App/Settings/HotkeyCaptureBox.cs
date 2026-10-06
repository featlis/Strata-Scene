using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StrataScene.Core.Hotkeys;

namespace StrataScene.App.Settings;

public class HotkeyCaptureBox : TextBox
{
    private Brush? _originalBorder;
    private bool _isRecording;

    public event Action? RecordingStarted;
    public event Action? RecordingEnded;
    public event Action<string>? HotkeyChanged;

    public HotkeyCaptureBox()
    {
        IsReadOnly = true;
        Cursor = Cursors.Hand;
        TextAlignment = TextAlignment.Center;
        FontWeight = FontWeights.SemiBold;
        FontFamily = new FontFamily("Consolas, Segoe UI, monospace");
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        _isRecording = true;
        _originalBorder = BorderBrush;
        BorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 212));
        BorderThickness = new Thickness(1.5);

        RecordingStarted?.Invoke();
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        _isRecording = false;
        if (_originalBorder != null)
        {
            BorderBrush = _originalBorder;
        }
        BorderThickness = new Thickness(1);

        RecordingEnded?.Invoke();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (!_isRecording)
        {
            base.OnPreviewKeyDown(e);
            return;
        }

        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Clear on Delete or Backspace
        if (key is Key.Delete or Key.Back)
        {
            Text = string.Empty;
            HotkeyChanged?.Invoke(string.Empty);
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            return;
        }

        // Cancel on Escape
        if (key == Key.Escape)
        {
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            return;
        }

        // Ignore pure modifier presses
        if (key is Key.LeftCtrl or Key.RightCtrl or
            Key.LeftAlt or Key.RightAlt or
            Key.LeftShift or Key.RightShift or
            Key.LWin or Key.RWin)
        {
            return;
        }

        var mods = new List<string>();
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) mods.Add("Ctrl");
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) mods.Add("Alt");
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) mods.Add("Shift");
        if ((Keyboard.Modifiers & ModifierKeys.Windows) != 0) mods.Add("Win");

        var keyName = GetKeyName(key);
        if (string.IsNullOrEmpty(keyName)) return;

        // Hotkeys require at least one modifier unless it's a Function key
        if (mods.Count == 0 && !keyName.StartsWith('F'))
        {
            return;
        }

        mods.Add(keyName);
        var gestureStr = string.Join("+", mods);

        if (HotkeyGesture.TryParse(gestureStr, out _))
        {
            Text = gestureStr;
            HotkeyChanged?.Invoke(gestureStr);
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }
    }

    private static string? GetKeyName(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
        {
            return key.ToString();
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            return ((int)key - (int)Key.D0).ToString();
        }

        if (key >= Key.F1 && key <= Key.F24)
        {
            return key.ToString();
        }

        return key switch
        {
            Key.Space => "Space",
            Key.Back => "Back",
            Key.Return => "Return",
            Key.Tab => "Tab",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            _ => null
        };
    }
}
