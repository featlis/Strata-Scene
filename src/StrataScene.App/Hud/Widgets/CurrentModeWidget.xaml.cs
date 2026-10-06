using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace StrataScene.App.Hud.Widgets;

public partial class CurrentModeWidget : HudWidgetBase
{
    public override string WidgetId => "widget_mode";

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

    protected override void UpdateEditVisuals(bool isEditMode)
    {
        Dispatcher.Invoke(() =>
        {
            if (isEditMode)
            {
                RootBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(255, 185, 0)); // Gold accent
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

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsEditMode) return;
        OnClicked?.Invoke();
    }
}
