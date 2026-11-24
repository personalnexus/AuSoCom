using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AuSoCom.Controls;

public sealed class RotaryGainControl : Control
{
    private const double MinimumGainDb = -50;
    private const double MaximumGainDb = 50;
    private const double GainPerPixel = 0.25;
    private static readonly IBrush KnobFill = new SolidColorBrush(Color.Parse("#E0E0E0"));
    private static readonly Pen KnobOutline = new(new SolidColorBrush(Color.Parse("#616161")), 2);
    private static readonly Pen KnobMarker = new(new SolidColorBrush(Color.Parse("#C62828")), 3);
    private double _value;
    private bool _isDragging;
    private Point _lastPointerPosition;

    public RotaryGainControl()
    {
        Focusable = true;
        IsTabStop = true;
    }

    public event Action<double>? ValueChanged;

    public double Value
    {
        get => _value;
        set
        {
            var nextValue = Math.Clamp(value, MinimumGainDb, MaximumGainDb);
            if (_value == nextValue)
                return;

            _value = nextValue;
            InvalidateVisual();
            ValueChanged?.Invoke(_value);
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var size = Math.Min(Bounds.Width, Bounds.Height);
        var radius = size / 2 - 3;
        if (radius <= 0)
            return;

        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        context.DrawEllipse(
            KnobFill,
            KnobOutline,
            center,
            radius,
            radius);

        var ratio = (_value - MinimumGainDb) / (MaximumGainDb - MinimumGainDb);
        var angle = (135 + ratio * 270) * Math.PI / 180;
        var markerStart = new Point(
            center.X + Math.Cos(angle) * radius * 0.35,
            center.Y + Math.Sin(angle) * radius * 0.35);
        var markerEnd = new Point(
            center.X + Math.Cos(angle) * radius * 0.8,
            center.Y + Math.Sin(angle) * radius * 0.8);
        context.DrawLine(KnobMarker, markerStart, markerEnd);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _isDragging = true;
        _lastPointerPosition = e.GetPosition(this);
        e.Pointer.Capture(this);
        Focus();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (!_isDragging)
            return;

        var position = e.GetPosition(this);
        Value += (_lastPointerPosition.Y - position.Y) * GainPerPixel;
        _lastPointerPosition = position;
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!_isDragging)
            return;

        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _isDragging = false;
        base.OnPointerCaptureLost(e);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        Value += Math.Sign(e.Delta.Y) * 0.5;
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
            case Key.Right:
                Value += 0.5;
                e.Handled = true;
                break;
            case Key.Down:
            case Key.Left:
                Value -= 0.5;
                e.Handled = true;
                break;
            case Key.Home:
                Value = MinimumGainDb;
                e.Handled = true;
                break;
            case Key.End:
                Value = MaximumGainDb;
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }
}
