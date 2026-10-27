using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FinanceTracker.App.ViewModels;

namespace FinanceTracker.App.Views;

public partial class MainWindow : Window
{
    private bool _initialized;

    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private async void OnOpened(object? sender, EventArgs e)
    {
        if (_initialized || DataContext is not MainViewModel viewModel) return;
        _initialized = true;
        await viewModel.InitializeAsync();
    }

    private void Navigation_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button selected || selected.Tag is not string page) return;

        DashboardPage.IsVisible = page == "Dashboard";
        OperationsPage.IsVisible = page == "Operations";
        StatisticsPage.IsVisible = page == "Statistics";
        BudgetPage.IsVisible = page == "Budget";
        CategoriesPage.IsVisible = page == "Categories";
        SettingsPage.IsVisible = page == "Settings";

        var buttons = new[] { DashboardNav, OperationsNav, StatisticsNav, BudgetNav, CategoriesNav, SettingsNav };
        foreach (var button in buttons)
        {
            button.Classes.Remove("active");
            if (ReferenceEquals(button, selected)) button.Classes.Add("active");
        }
    }

    private async void DeleteTransaction_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: TransactionRow row } || DataContext is not MainViewModel viewModel) return;
        var dialog = new ConfirmationDialog(
            "Удалить операцию?",
            $"{row.DateText} • {row.DescriptionOrCategory} • {row.AmountText}\n\nЭто действие нельзя отменить.",
            "Удалить");
        if (await dialog.ShowDialog<bool>(this))
            await viewModel.DeleteTransactionCommand.ExecuteAsync(row);
    }

    private async void RestoreBackup_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите резервную копию FinanceTracker",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("База SQLite") { Patterns = ["*.db", "*.sqlite", "*.sqlite3"] },
                FilePickerFileTypes.All
            ]
        });
        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;

        var dialog = new ConfirmationDialog(
            "Восстановить резервную копию?",
            "Текущая база данных будет заменена выбранной копией. Перед продолжением рекомендуется создать свежую резервную копию.",
            "Восстановить");
        if (!await dialog.ShowDialog<bool>(this)) return;

        try
        {
            await viewModel.RestoreBackupFromAsync(path);
        }
        catch (Exception ex)
        {
            viewModel.StatusMessage = $"Ошибка восстановления: {ex.Message}";
            var error = new ConfirmationDialog("Не удалось восстановить данные", ex.Message, "Закрыть");
            await error.ShowDialog<bool>(this);
        }
    }
}
