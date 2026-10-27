using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FinanceTracker.App.Views;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog() => InitializeComponent();

    public ConfirmationDialog(string title, string message, string confirmText = "Подтвердить") : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmText;
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e) => Close(false);
    private void Confirm_Click(object? sender, RoutedEventArgs e) => Close(true);
}
