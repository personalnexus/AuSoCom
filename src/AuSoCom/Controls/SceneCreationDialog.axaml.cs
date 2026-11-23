using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AuSoCom.Controls;

public partial class SceneCreationDialog : Window
{
    public SceneCreationDialog()
    {
        InitializeComponent();
    }

    private void ConfirmClicked(object? sender, RoutedEventArgs e)
    {
        Close(new SceneCreationOptions(
            OneSceneRadioButton.IsChecked == true,
            AppendToShowCheckBox.IsChecked == true,
            AutoStartScenesCheckBox.IsChecked == true));
    }

    private void CancelClicked(object? sender, RoutedEventArgs e) => Close(null);
}
