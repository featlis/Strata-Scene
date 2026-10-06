using System.Windows.Input;
using System.Windows.Media;

namespace StrataScene.App.Hud.Widgets;

public partial class CurrentModeWidget : NoActivateWindow
{
    public Action? OnClicked { get; set; }

    public CurrentModeWidget()
    {
        InitializeComponent();
    }

    public void UpdateMode(string? name, string? hexColor)
    {
        Dispatcher.Invoke(() =>
        {
            ModeNameText.Text = string.IsNullOrWhiteSpace(name) ? "—" : name;

            if (!string.IsNullOrWhiteSpace(hexColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hexColor);
                    ModeIndicator.Fill = new SolidColorBrush(color);
                    return;
                }
                catch
                {
                    // Fallback below
                }
            }

            ModeIndicator.Fill = new SolidColorBrush(Color.FromRgb(128, 128, 128));
        });
    }

    public void SetWidgetOpacity(double opacity)
    {
        Dispatcher.Invoke(() =>
        {
            RootBorder.Opacity = Math.Clamp(opacity, 0.1, 1.0);
        });
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        OnClicked?.Invoke();
    }
}
