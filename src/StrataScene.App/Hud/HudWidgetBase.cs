using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using StrataScene.Core.Config;
using StrataScene.Core.Layout;
using StrataScene.Platform;

namespace StrataScene.App.Hud;

public abstract class HudWidgetBase : NoActivateWindow
{
    private bool _isEditMode;
    private bool _isDragging;
    private int _dragStartCursorX;
    private int _dragStartCursorY;
    private int _dragStartPhysicalX;
    private int _dragStartPhysicalY;
    private int _currentPhysicalX;
    private int _currentPhysicalY;
    private int _currentPhysicalWidth;
    private int _currentPhysicalHeight;

    public abstract string WidgetId { get; }
    public WidgetConfig? Config { get; set; }
    public PhysicalRect CurrentTaskbarBounds { get; set; }

    public bool IsEditMode => _isEditMode;

    public event Action<string, double, double>? OffsetChanged;

    public virtual void SetEditMode(bool isEditMode)
    {
        _isEditMode = isEditMode;
        UpdateEditVisuals(isEditMode);
    }

    protected abstract void UpdateEditVisuals(bool isEditMode);

    public new void PositionPhysical(int x, int y, int width, int height)
    {
        _currentPhysicalX = x;
        _currentPhysicalY = y;
        _currentPhysicalWidth = width;
        _currentPhysicalHeight = height;
        base.PositionPhysical(x, y, width, height);
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (_isEditMode)
        {
            _isDragging = true;
            if (WindowStyles.GetCursorPosition(out var cx, out var cy))
            {
                _dragStartCursorX = cx;
                _dragStartCursorY = cy;
            }
            _dragStartPhysicalX = _currentPhysicalX;
            _dragStartPhysicalY = _currentPhysicalY;

            CaptureMouse();
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseLeftButtonDown(e);
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        if (_isDragging && _isEditMode)
        {
            if (WindowStyles.GetCursorPosition(out var cx, out var cy))
            {
                var deltaX = cx - _dragStartCursorX;
                var deltaY = cy - _dragStartCursorY;

                var newX = _dragStartPhysicalX + deltaX;
                var newY = _dragStartPhysicalY + deltaY;

                PositionPhysical(newX, newY, _currentPhysicalWidth, _currentPhysicalHeight);
            }
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseMove(e);
    }

    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_isDragging && _isEditMode)
        {
            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;

            var dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
            var dockAlignment = Config?.DockAlignment ?? "TaskbarRight";

            var snappedOffset = WidgetLayoutCalculator.CalculateOffsetFromPhysicalPosition(
                CurrentTaskbarBounds,
                _currentPhysicalX,
                _currentPhysicalY,
                dpi,
                dockAlignment,
                snapToGrid: true);

            OffsetChanged?.Invoke(WidgetId, snappedOffset.X, snappedOffset.Y);
            return;
        }

        base.OnPreviewMouseLeftButtonUp(e);
    }
}
