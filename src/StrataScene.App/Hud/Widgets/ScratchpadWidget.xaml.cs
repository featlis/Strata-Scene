using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using StrataScene.Platform;

namespace StrataScene.App.Hud.Widgets;

public partial class ScratchpadWidget : HudWidgetBase
{
    public override string WidgetId => "widget_scratchpad";

    private string _currentText = string.Empty;
    private bool _isInlineEditing;
    private IntPtr _previousForegroundHwnd;

    public event Action<string>? TextCommitted;

    public ScratchpadWidget()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    public void SetMemoText(string? text)
    {
        _currentText = text ?? string.Empty;
        UpdateDisplay();
    }

    public void SetWidgetOpacity(double opacity)
    {
        Dispatcher.Invoke(() =>
        {
            RootBorder.Opacity = Math.Clamp(opacity, 0.1, 1.0);
        });
    }

    protected override void UpdateEditVisuals(bool isEditMode)
    {
        Dispatcher.Invoke(() =>
        {
            if (isEditMode)
            {
                if (_isInlineEditing)
                {
                    CancelInlineEdit();
                }

                RootBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 185, 0));
                RootBorder.BorderThickness = new Thickness(1.5);
                RootBorder.Cursor = Cursors.SizeAll;
            }
            else
            {
                RootBorder.BorderBrush = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));
                RootBorder.BorderThickness = new Thickness(1);
                RootBorder.Cursor = Cursors.Hand;
            }
        });
    }

    private void UpdateDisplay()
    {
        Dispatcher.Invoke(() =>
        {
            if (string.IsNullOrWhiteSpace(_currentText))
            {
                DisplayText.Text = "メモを入力...";
                DisplayText.Foreground = new SolidColorBrush(Color.FromArgb(160, 200, 200, 200));
            }
            else
            {
                DisplayText.Text = _currentText;
                DisplayText.Foreground = new SolidColorBrush(Colors.White);
            }
        });
    }

    private void BeginInlineEdit()
    {
        if (_isInlineEditing || IsEditMode) return;

        _previousForegroundHwnd = WindowStyles.GetForegroundWindow();
        _isInlineEditing = true;

        WindowStyles.RemoveNoActivateStyles(Handle);

        DisplayText.Visibility = Visibility.Collapsed;
        EditTextBox.Visibility = Visibility.Visible;
        EditTextBox.Text = _currentText;

        Activate();
        EditTextBox.Focus();
        EditTextBox.SelectAll();
    }

    private void CommitInlineEdit()
    {
        if (!_isInlineEditing) return;

        _currentText = EditTextBox.Text.Trim();
        EndInlineEdit();
        UpdateDisplay();

        TextCommitted?.Invoke(_currentText);
    }

    private void CancelInlineEdit()
    {
        if (!_isInlineEditing) return;

        EndInlineEdit();
        UpdateDisplay();
    }

    private void EndInlineEdit()
    {
        _isInlineEditing = false;
        EditTextBox.Visibility = Visibility.Collapsed;
        DisplayText.Visibility = Visibility.Visible;

        WindowStyles.ApplyNoActivateStyles(Handle);

        if (_previousForegroundHwnd != IntPtr.Zero)
        {
            WindowStyles.RestoreForeground(_previousForegroundHwnd);
            _previousForegroundHwnd = IntPtr.Zero;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsEditMode) return;

        if (!_isInlineEditing)
        {
            BeginInlineEdit();
        }
    }

    private void OnEditTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CommitInlineEdit();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CancelInlineEdit();
        }
    }

    private void OnEditTextBoxLostFocus(object sender, RoutedEventArgs e)
    {
        if (_isInlineEditing)
        {
            CommitInlineEdit();
        }
    }
}
