using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace StrataScene.App.Hud.Widgets;

public enum FocusTimerMode
{
    Work,
    Break
}

public enum FocusTimerState
{
    Stopped,
    Running,
    Paused
}

public partial class FocusTimerWidget : HudWidgetBase
{
    public override string WidgetId => "widget_pomodoro";

    private readonly DispatcherTimer _timer;
    private FocusTimerMode _mode = FocusTimerMode.Work;
    private FocusTimerState _state = FocusTimerState.Stopped;
    private TimeSpan _remainingTime;

    public event Action<string, string>? TimerCompleted;

    public int WorkMinutes { get; set; } = 25;
    public int BreakMinutes { get; set; } = 5;

    public FocusTimerWidget()
    {
        InitializeComponent();

        _remainingTime = TimeSpan.FromMinutes(WorkMinutes);

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;

        UpdateDisplay();
    }

    public void ApplyConfig(int? workMin, int? breakMin)
    {
        WorkMinutes = workMin ?? 25;
        BreakMinutes = breakMin ?? 5;

        if (_state == FocusTimerState.Stopped)
        {
            _remainingTime = TimeSpan.FromMinutes(_mode == FocusTimerMode.Work ? WorkMinutes : BreakMinutes);
            UpdateDisplay();
        }
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

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_remainingTime.TotalSeconds > 1)
        {
            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(1));
            UpdateDisplay();
        }
        else
        {
            _timer.Stop();
            _state = FocusTimerState.Stopped;

            try
            {
                System.Media.SystemSounds.Beep.Play();
            }
            catch
            {
                // Ignore audio failure
            }

            var finishedMode = _mode;
            _mode = _mode == FocusTimerMode.Work ? FocusTimerMode.Break : FocusTimerMode.Work;
            _remainingTime = TimeSpan.FromMinutes(_mode == FocusTimerMode.Work ? WorkMinutes : BreakMinutes);
            UpdateDisplay();

            var title = finishedMode == FocusTimerMode.Work ? "集中セッション完了！" : "休憩時間終了！";
            var message = finishedMode == FocusTimerMode.Work
                ? $"休憩（{BreakMinutes}分）に入りましょう。"
                : $"次の集中セッション（{WorkMinutes}分）を開始しましょう。";

            TimerCompleted?.Invoke(title, message);
        }
    }

    private void UpdateDisplay()
    {
        TimeText.Text = $"{_remainingTime.Minutes:D2}:{_remainingTime.Seconds:D2}";

        var color = _mode == FocusTimerMode.Work
            ? Color.FromRgb(232, 17, 35)   // Red
            : Color.FromRgb(16, 124, 16);  // Green

        if (_state == FocusTimerState.Paused)
        {
            color = Color.FromRgb(255, 185, 0); // Yellow when paused
        }

        TimerIndicator.Fill = new SolidColorBrush(color);
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsEditMode) return;

        switch (_state)
        {
            case FocusTimerState.Stopped:
                _state = FocusTimerState.Running;
                _timer.Start();
                break;
            case FocusTimerState.Running:
                _state = FocusTimerState.Paused;
                _timer.Stop();
                break;
            case FocusTimerState.Paused:
                _state = FocusTimerState.Running;
                _timer.Start();
                break;
        }

        UpdateDisplay();
    }

    private void OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsEditMode) return;

        _timer.Stop();
        _state = FocusTimerState.Stopped;

        // Toggle mode on right click
        _mode = _mode == FocusTimerMode.Work ? FocusTimerMode.Break : FocusTimerMode.Work;
        _remainingTime = TimeSpan.FromMinutes(_mode == FocusTimerMode.Work ? WorkMinutes : BreakMinutes);

        UpdateDisplay();
    }
}
