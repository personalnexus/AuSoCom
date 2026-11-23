using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AuSoCom.Controls;

public partial class SceneControlsView : UserControl
{
    public SceneControlsView()
    {
        InitializeComponent();
    }

    public StackPanel InputPanel => InputsPanel;
    public StackPanel OutputPanel => OutputsPanel;
    public DataGrid RoutingGrid => RoutingMatrixGrid;

    public event EventHandler<RoutedEventArgs>? AddInputRequested;
    public event EventHandler<RoutedEventArgs>? RemoveInputRequested;
    public event EventHandler<RoutedEventArgs>? AddOutputRequested;
    public event EventHandler<RoutedEventArgs>? RemoveOutputRequested;
    public event EventHandler<DataGridCellEditEndedEventArgs>? RoutingCellEditEnded;

    private void AddInputButtonClicked(object? sender, RoutedEventArgs e) =>
        AddInputRequested?.Invoke(this, e);

    private void RemoveInputButtonClicked(object? sender, RoutedEventArgs e) =>
        RemoveInputRequested?.Invoke(this, e);

    private void AddOutputButtonClicked(object? sender, RoutedEventArgs e) =>
        AddOutputRequested?.Invoke(this, e);

    private void RemoveOutputButtonClicked(object? sender, RoutedEventArgs e) =>
        RemoveOutputRequested?.Invoke(this, e);

    private void RoutingGridCellEditEnded(object? sender, DataGridCellEditEndedEventArgs e) =>
        RoutingCellEditEnded?.Invoke(this, e);
}
