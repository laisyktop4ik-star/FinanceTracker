using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Core;
using FinanceTracker.Data;

namespace FinanceTracker.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SqliteFinanceRepository _repository;
    private List<Transaction> _allTransactions = [];
    private long _editId;
    private bool _initialized;

    public MainViewModel(string? databasePath = null)
    {
        var localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localData))
            localData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        var overrideDirectory = Environment.GetEnvironmentVariable("FINANCETRACKER_DATA_DIR");
        DatabasePath = databasePath ?? (string.IsNullOrWhiteSpace(overrideDirectory)
            ? Path.Combine(localData, "FinanceTracker", "finance.db")
            : Path.Combine(overrideDirectory, "finance.db"));
        _repository = new SqliteFinanceRepository(DatabasePath);

        TypeOptions = ["Расход", "Доход"];
        FilterTypeOptions = ["Все", "Расходы", "Доходы"];
        CategoryTypeOptions = ["Расход", "Доход"];
        CurrencyOptions = ["грн", "₴", "€", "$", "₽"];
        MonthOptions = new ObservableCollection<MonthOption>(Enumerable.Range(0, 24)
            .Select(offset => DateTime.Today.AddMonths(-offset))
            .Select(date => new MonthOption(date.Year, date.Month)));
        SelectedMonth = MonthOptions[0];
        OperationDate = DateTimeOffset.Now;
    }

    public ObservableCollection<TransactionRow> Transactions { get; } = [];
    public ObservableCollection<TransactionRow> RecentTransactions { get; } = [];
    public ObservableCollection<CategoryStat> CategoryStats { get; } = [];
    public ObservableCollection<string> Categories { get; } = [];
    public ObservableCollection<string> ExpenseCategories { get; } = [];
    public ObservableCollection<string> IncomeCategories { get; } = [];
    public ObservableCollection<string> TrendLabels { get; } = [];
    public ObservableCollection<MonthlyTrendRow> MonthlyTrendRows { get; } = [];
    public Points IncomeTrendPoints { get; } = [];
    public Points ExpenseTrendPoints { get; } = [];
    public ObservableCollection<MonthOption> MonthOptions { get; }
    public IReadOnlyList<string> TypeOptions { get; }
    public IReadOnlyList<string> FilterTypeOptions { get; }
    public IReadOnlyList<string> CategoryTypeOptions { get; }
    public IReadOnlyList<string> CurrencyOptions { get; }

    public string DatabasePath { get; }
    public string AppVersion => "1.1.0";

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _statusMessage = "Готово";
    [ObservableProperty] private string _formError = string.Empty;
    [ObservableProperty] private string _formTitle = "Добавить операцию";
    [ObservableProperty] private string _saveButtonText = "Добавить";
    [ObservableProperty] private string _selectedType = "Расход";
    [ObservableProperty] private string? _selectedCategory;
    [ObservableProperty] private string _amountInput = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private DateTimeOffset? _operationDate;
    [ObservableProperty] private MonthOption? _selectedMonth;
    [ObservableProperty] private string _filterType = "Все";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _budgetInput = "10000";
    [ObservableProperty] private double _budgetProgress;
    [ObservableProperty] private string _budgetCaption = "0 / 10 000 грн";
    [ObservableProperty] private string _incomeTotal = "0 грн";
    [ObservableProperty] private string _expenseTotal = "0 грн";
    [ObservableProperty] private string _balanceTotal = "0 грн";
    [ObservableProperty] private string _operationCount = "0 операций";
    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _newCategoryType = "Расход";
    [ObservableProperty] private string _currencySymbol = "грн";
    [ObservableProperty] private bool _hasNoRecentTransactions = true;
    [ObservableProperty] private bool _hasNoCategoryStats = true;
    [ObservableProperty] private bool _hasTrendData;
    [ObservableProperty] private bool _hasNoTrendData = true;
    [ObservableProperty] private bool _hasIncomeTrend;
    [ObservableProperty] private bool _hasExpenseTrend;
    [ObservableProperty] private string _trendAxisMaximum = string.Empty;
    [ObservableProperty] private string _trendAxisThreeQuarters = string.Empty;
    [ObservableProperty] private string _trendAxisHalf = string.Empty;
    [ObservableProperty] private string _trendAxisQuarter = string.Empty;
    [ObservableProperty] private string _trendAxisZero = string.Empty;
    [ObservableProperty] private Bitmap? _backgroundImage;
    [ObservableProperty] private double _backgroundOpacity = 0.22;
    [ObservableProperty] private string _backgroundFileName = "Фон не выбран";

    partial void OnSelectedTypeChanged(string value)
    {
        if (_initialized) _ = LoadFormCategoriesAsync();
    }
    partial void OnSelectedMonthChanged(MonthOption? value)
    {
        if (_initialized) _ = RefreshViewAsync();
    }
    partial void OnFilterTypeChanged(string value) => ApplyFilters();
    partial void OnSearchTextChanged(string value) => ApplyFilters();

    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            await _repository.InitializeAsync();
            _initialized = true;
            CurrencySymbol = await _repository.GetSettingAsync("Currency", "грн");
            await LoadAppearanceAsync();
            await LoadCategoryListsAsync();
            await LoadFormCategoriesAsync();
            _allTransactions = (await _repository.GetAllAsync()).ToList();
            await RefreshViewAsync();
            StatusMessage = "Данные загружены";
        }
        catch (Exception ex)
        {
            FormError = $"Ошибка запуска: {ex.Message}";
            StatusMessage = "Не удалось открыть базу данных";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SaveTransactionAsync()
    {
        FormError = string.Empty;
        if (!TryParseAmount(AmountInput, out var amount) || amount <= 0)
        {
            FormError = "Введите сумму больше нуля.";
            return;
        }
        if (string.IsNullOrWhiteSpace(SelectedCategory))
        {
            FormError = "Выберите категорию.";
            return;
        }
        if (OperationDate is null)
        {
            FormError = "Выберите дату.";
            return;
        }

        try
        {
            var transaction = Transaction.Create(
                _editId,
                SelectedType == "Доход" ? TransactionType.Income : TransactionType.Expense,
                Money.ToMinorUnits(amount),
                SelectedCategory,
                Description,
                DateOnly.FromDateTime(OperationDate.Value.Date));

            if (_editId > 0)
            {
                await _repository.UpdateAsync(transaction);
                StatusMessage = "Операция изменена";
            }
            else
            {
                await _repository.AddAsync(transaction);
                StatusMessage = "Операция добавлена";
            }

            ResetForm();
            _allTransactions = (await _repository.GetAllAsync()).ToList();
            await RefreshViewAsync();
        }
        catch (Exception ex) { FormError = ex.Message; }
    }

    [RelayCommand]
    private void EditTransaction(TransactionRow? row)
    {
        if (row is null) return;
        _editId = row.Id;
        SelectedType = row.Type == TransactionType.Income ? "Доход" : "Расход";
        AmountInput = Money.FromMinorUnits(row.AmountMinor).ToString("0.00", CultureInfo.CurrentCulture);
        SelectedCategory = row.Category;
        Description = row.Description;
        OperationDate = new DateTimeOffset(row.Date.ToDateTime(TimeOnly.MinValue));
        FormTitle = "Изменить операцию";
        SaveButtonText = "Сохранить";
        StatusMessage = "Редактирование операции";
    }

    [RelayCommand]
    private async Task DeleteTransactionAsync(TransactionRow? row)
    {
        if (row is null) return;
        await _repository.DeleteAsync(row.Id);
        _allTransactions = (await _repository.GetAllAsync()).ToList();
        await RefreshViewAsync();
        StatusMessage = $"Удалено: {row.DescriptionOrCategory}";
    }

    [RelayCommand]
    private void CancelEdit() => ResetForm();

    [RelayCommand]
    private async Task SaveBudgetAsync()
    {
        if (SelectedMonth is null || !TryParseAmount(BudgetInput, out var amount) || amount < 0)
        {
            StatusMessage = "Введите корректный бюджет";
            return;
        }
        await _repository.SetMonthlyBudgetAsync(SelectedMonth.Year, SelectedMonth.Month, Money.ToMinorUnits(amount));
        await RefreshViewAsync();
        StatusMessage = "Бюджет сохранён";
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            StatusMessage = "Введите название категории";
            return;
        }
        var type = NewCategoryType == "Доход" ? TransactionType.Income : TransactionType.Expense;
        await _repository.AddCategoryAsync(type, NewCategoryName);
        NewCategoryName = string.Empty;
        await LoadCategoryListsAsync();
        await LoadFormCategoriesAsync();
        StatusMessage = "Категория добавлена";
    }

    [RelayCommand]
    private void ClearFilters()
    {
        FilterType = "Все";
        SearchText = string.Empty;
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(downloads);
        var path = Path.Combine(downloads, $"FinanceTracker-{DateTime.Now:yyyy-MM-dd-HHmm}.csv");
        var csv = new StringBuilder("Дата;Тип;Категория;Описание;Сумма\n");
        foreach (var item in _allTransactions.OrderByDescending(x => x.Date))
        {
            csv.Append(item.Date.ToString("dd.MM.yyyy")).Append(';')
                .Append(item.Type == TransactionType.Income ? "Доход" : "Расход").Append(';')
                .Append(Csv(item.Category)).Append(';').Append(Csv(item.Description)).Append(';')
                .Append(Money.FromMinorUnits(item.AmountMinor).ToString("0.00", CultureInfo.InvariantCulture)).AppendLine();
        }
        await File.WriteAllTextAsync(path, csv.ToString(), new UTF8Encoding(true));
        StatusMessage = $"Экспортировано: {path}";
    }

    [RelayCommand]
    private void OpenDataFolder()
    {
        var folder = Path.GetDirectoryName(DatabasePath)!;
        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo("xdg-open", folder) { UseShellExecute = false });
        StatusMessage = "Открыта папка данных";
    }

    [RelayCommand]
    private async Task SaveCurrencyAsync()
    {
        if (string.IsNullOrWhiteSpace(CurrencySymbol)) return;
        await _repository.SetSettingAsync("Currency", CurrencySymbol);
        await RefreshViewAsync();
        StatusMessage = "Валюта сохранена";
    }

    [RelayCommand]
    private async Task CreateBackupAsync()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "FinanceTracker Backups");
        var path = Path.Combine(directory, $"FinanceTracker-{DateTime.Now:yyyy-MM-dd-HHmmss}.db");
        await _repository.CreateBackupAsync(path);
        StatusMessage = $"Резервная копия создана: {path}";
    }

    public async Task RestoreBackupFromAsync(string path)
    {
        await _repository.RestoreBackupAsync(path);
        CurrencySymbol = await _repository.GetSettingAsync("Currency", "грн");
        await LoadAppearanceAsync();
        await LoadCategoryListsAsync();
        await LoadFormCategoriesAsync();
        _allTransactions = (await _repository.GetAllAsync()).ToList();
        await RefreshViewAsync();
        StatusMessage = "Резервная копия восстановлена";
    }

    public async Task SetBackgroundImageAsync(string sourcePath)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("Изображение не найдено.", sourcePath);
        var info = new FileInfo(sourcePath);
        if (info.Length > 25 * 1024 * 1024) throw new InvalidOperationException("Размер изображения не должен превышать 25 МБ.");

        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp"))
            throw new InvalidOperationException("Поддерживаются PNG, JPG, WEBP и BMP.");

        using (var testStream = File.OpenRead(sourcePath))
        using (var testBitmap = new Bitmap(testStream)) { }

        var backgroundDirectory = Path.Combine(Path.GetDirectoryName(DatabasePath)!, "Appearance");
        Directory.CreateDirectory(backgroundDirectory);
        foreach (var oldFile in Directory.EnumerateFiles(backgroundDirectory, "background.*"))
            if (!Path.GetFullPath(oldFile).Equals(Path.GetFullPath(sourcePath), StringComparison.Ordinal)) File.Delete(oldFile);

        var destination = Path.Combine(backgroundDirectory, $"background{extension}");
        if (!Path.GetFullPath(sourcePath).Equals(Path.GetFullPath(destination), StringComparison.Ordinal))
            File.Copy(sourcePath, destination, true);

        await _repository.SetSettingAsync("BackgroundImagePath", destination);
        ReplaceBackgroundImage(LoadBitmap(destination));
        BackgroundFileName = Path.GetFileName(sourcePath);
        StatusMessage = "Фоновое изображение установлено";
    }

    [RelayCommand]
    private async Task SaveAppearanceAsync()
    {
        BackgroundOpacity = Math.Clamp(BackgroundOpacity, 0.05, 0.55);
        await _repository.SetSettingAsync("BackgroundOpacity", BackgroundOpacity.ToString(CultureInfo.InvariantCulture));
        StatusMessage = "Настройки оформления сохранены";
    }

    [RelayCommand]
    private async Task RemoveBackgroundAsync()
    {
        var backgroundDirectory = Path.Combine(Path.GetDirectoryName(DatabasePath)!, "Appearance");
        if (Directory.Exists(backgroundDirectory))
            foreach (var file in Directory.EnumerateFiles(backgroundDirectory, "background.*")) File.Delete(file);
        await _repository.SetSettingAsync("BackgroundImagePath", string.Empty);
        ReplaceBackgroundImage(null);
        BackgroundFileName = "Фон не выбран";
        StatusMessage = "Фоновое изображение удалено";
    }

    private async Task LoadAppearanceAsync()
    {
        var opacityText = await _repository.GetSettingAsync("BackgroundOpacity", "0.22");
        if (double.TryParse(opacityText, NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity))
            BackgroundOpacity = Math.Clamp(opacity, 0.05, 0.55);

        var path = await _repository.GetSettingAsync("BackgroundImagePath", string.Empty);
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            try
            {
                ReplaceBackgroundImage(LoadBitmap(path));
                BackgroundFileName = Path.GetFileName(path);
                return;
            }
            catch
            {
                await _repository.SetSettingAsync("BackgroundImagePath", string.Empty);
            }
        }
        ReplaceBackgroundImage(null);
        BackgroundFileName = "Фон не выбран";
    }

    private static Bitmap LoadBitmap(string path)
    {
        using var stream = File.OpenRead(path);
        return new Bitmap(stream);
    }

    private void ReplaceBackgroundImage(Bitmap? bitmap)
    {
        var previous = BackgroundImage;
        BackgroundImage = bitmap;
        previous?.Dispose();
    }

    private async Task RefreshViewAsync()
    {
        if (SelectedMonth is null) return;
        var monthly = _allTransactions
            .Where(x => x.Date.Year == SelectedMonth.Year && x.Date.Month == SelectedMonth.Month)
            .ToList();
        var summary = FinanceCalculator.Calculate(monthly);
        IncomeTotal = FormatMoney(summary.IncomeMinor);
        ExpenseTotal = FormatMoney(summary.ExpenseMinor);
        BalanceTotal = FormatMoney(summary.BalanceMinor);

        var budget = await _repository.GetMonthlyBudgetAsync(SelectedMonth.Year, SelectedMonth.Month);
        if (budget == 0)
        {
            budget = 1_000_000;
            await _repository.SetMonthlyBudgetAsync(SelectedMonth.Year, SelectedMonth.Month, budget);
        }
        BudgetInput = Money.FromMinorUnits(budget).ToString("0.##", CultureInfo.CurrentCulture);
        BudgetProgress = budget == 0 ? 0 : Math.Min(100, summary.ExpenseMinor * 100d / budget);
        BudgetCaption = $"{FormatMoney(summary.ExpenseMinor)} / {FormatMoney(budget)}";

        CategoryStats.Clear();
        var expense = Math.Max(1, summary.ExpenseMinor);
        foreach (var group in monthly.Where(x => x.Type == TransactionType.Expense)
                     .GroupBy(x => x.Category).Select(g => new { Name = g.Key, Total = g.Sum(x => x.AmountMinor) })
                     .OrderByDescending(x => x.Total))
            CategoryStats.Add(new CategoryStat(group.Name, FormatMoney(group.Total), group.Total * 100d / expense));
        HasNoCategoryStats = CategoryStats.Count == 0;
        BuildTrendChart();

        ApplyFilters();
        RecentTransactions.Clear();
        foreach (var row in monthly.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Take(8).Select(x => TransactionRow.From(x, CurrencySymbol)))
            RecentTransactions.Add(row);
        HasNoRecentTransactions = RecentTransactions.Count == 0;
    }

    private void BuildTrendChart()
    {
        if (SelectedMonth is null) return;
        var chart = TrendChartCalculator.Build(
            new DateOnly(SelectedMonth.Year, SelectedMonth.Month, 1),
            _allTransactions);
        HasTrendData = chart.HasData;
        HasNoTrendData = !HasTrendData;
        HasIncomeTrend = chart.HasIncome;
        HasExpenseTrend = chart.HasExpenses;

        IncomeTrendPoints.Clear();
        ExpenseTrendPoints.Clear();
        TrendLabels.Clear();
        MonthlyTrendRows.Clear();
        const double top = 10;
        const double chartWidth = 800;
        const double chartHeight = 180;
        for (var index = 0; index < chart.Months.Count; index++)
        {
            var x = chartWidth * (index + 0.5) / chart.Months.Count;
            IncomeTrendPoints.Add(new Point(x, top + chartHeight * (1d - chart.IncomeMinor[index] / (double)chart.ScaleMaximumMinor)));
            ExpenseTrendPoints.Add(new Point(x, top + chartHeight * (1d - chart.ExpenseMinor[index] / (double)chart.ScaleMaximumMinor)));
            TrendLabels.Add(chart.Months[index].ToString("MMM", CultureInfo.GetCultureInfo("ru-RU")).TrimEnd('.').ToUpperInvariant());
        }

        foreach (var total in chart.MonthlyTotals)
            MonthlyTrendRows.Add(MonthlyTrendRow.From(total, CurrencySymbol));

        TrendAxisMaximum = FormatCompactMoney(chart.ScaleMaximumMinor);
        TrendAxisThreeQuarters = FormatCompactMoney(chart.ScaleMaximumMinor * 3 / 4);
        TrendAxisHalf = FormatCompactMoney(chart.ScaleMaximumMinor / 2);
        TrendAxisQuarter = FormatCompactMoney(chart.ScaleMaximumMinor / 4);
        TrendAxisZero = FormatCompactMoney(0);
    }

    private string FormatCompactMoney(long minor) =>
        $"{Money.FromMinorUnits(minor).ToString("N0", CultureInfo.GetCultureInfo("ru-RU"))} {CurrencySymbol}";

    private void ApplyFilters()
    {
        if (SelectedMonth is null) return;
        IEnumerable<Transaction> query = _allTransactions.Where(x => x.Date.Year == SelectedMonth.Year && x.Date.Month == SelectedMonth.Month);
        query = FilterType switch
        {
            "Доходы" => query.Where(x => x.Type == TransactionType.Income),
            "Расходы" => query.Where(x => x.Type == TransactionType.Expense),
            _ => query
        };
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var needle = SearchText.Trim();
            query = query.Where(x => x.Category.Contains(needle, StringComparison.CurrentCultureIgnoreCase) ||
                                     x.Description.Contains(needle, StringComparison.CurrentCultureIgnoreCase));
        }
        Transactions.Clear();
        foreach (var row in query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).Select(x => TransactionRow.From(x, CurrencySymbol)))
            Transactions.Add(row);
        OperationCount = $"{Transactions.Count} операций";
    }

    private async Task LoadFormCategoriesAsync()
    {
        var type = SelectedType == "Доход" ? TransactionType.Income : TransactionType.Expense;
        var items = await _repository.GetCategoriesAsync(type);
        var previous = SelectedCategory;
        Categories.Clear();
        foreach (var item in items) Categories.Add(item);
        SelectedCategory = previous is not null && Categories.Contains(previous) ? previous : Categories.FirstOrDefault();
    }

    private async Task LoadCategoryListsAsync()
    {
        ExpenseCategories.Clear();
        IncomeCategories.Clear();
        foreach (var item in await _repository.GetCategoriesAsync(TransactionType.Expense)) ExpenseCategories.Add(item);
        foreach (var item in await _repository.GetCategoriesAsync(TransactionType.Income)) IncomeCategories.Add(item);
    }

    private void ResetForm()
    {
        _editId = 0;
        AmountInput = string.Empty;
        Description = string.Empty;
        OperationDate = DateTimeOffset.Now;
        FormTitle = "Добавить операцию";
        SaveButtonText = "Добавить";
        FormError = string.Empty;
    }

    private static bool TryParseAmount(string value, out decimal amount) =>
        decimal.TryParse(value?.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out amount);

    private string FormatMoney(long minor) =>
        $"{Money.FromMinorUnits(minor).ToString("N2", CultureInfo.GetCultureInfo("ru-RU"))} {CurrencySymbol}";

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}

public sealed record MonthOption(int Year, int Month)
{
    public string Title => new DateTime(Year, Month, 1).ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"));
    public override string ToString() => char.ToUpper(Title[0], CultureInfo.CurrentCulture) + Title[1..];
}

public sealed record CategoryStat(string Name, string Amount, double Percent)
{
    public string PercentText => $"{Percent:0}%";
}

public sealed record MonthlyTrendRow(DateOnly Month, long IncomeMinor, long ExpenseMinor, string CurrencySymbol)
{
    public static MonthlyTrendRow From(MonthlyTrendTotal total, string currencySymbol) =>
        new(total.Month, total.IncomeMinor, total.ExpenseMinor, currencySymbol);

    public long BalanceMinor => IncomeMinor - ExpenseMinor;
    public string MonthText => char.ToUpper(Month.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"))[0], CultureInfo.CurrentCulture) +
                               Month.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU"))[1..];
    public string IncomeText => FormatSigned(IncomeMinor, "+");
    public string ExpenseText => FormatSigned(ExpenseMinor, "−");
    public string BalanceText => FormatSigned(BalanceMinor, BalanceMinor > 0 ? "+" : BalanceMinor < 0 ? "−" : string.Empty, absolute: true);
    public string BalanceColor => BalanceMinor > 0 ? "#45D38A" : BalanceMinor < 0 ? "#FF666B" : "#9FB0BF";

    private string FormatSigned(long value, string sign, bool absolute = false)
    {
        if (value == 0) return $"0,00 {CurrencySymbol}";
        var displayed = absolute ? Math.Abs(value) : value;
        return $"{sign}{Money.FromMinorUnits(displayed).ToString("N2", CultureInfo.GetCultureInfo("ru-RU"))} {CurrencySymbol}";
    }
}

public sealed record TransactionRow(long Id, TransactionType Type, long AmountMinor, string Category, string Description, DateOnly Date, string CurrencySymbol)
{
    public static TransactionRow From(Transaction value, string currencySymbol) => new(value.Id, value.Type, value.AmountMinor, value.Category, value.Description, value.Date, currencySymbol);
    public string DateText => Date.ToString("dd.MM.yyyy");
    public string TypeText => Type == TransactionType.Income ? "Доход" : "Расход";
    public string AmountText => $"{(Type == TransactionType.Income ? "+" : "−")}{Money.FromMinorUnits(AmountMinor):N2} {CurrencySymbol}";
    public string AmountColor => Type == TransactionType.Income ? "#45D38A" : "#FF666B";
    public string DescriptionOrCategory => string.IsNullOrWhiteSpace(Description) ? Category : Description;
}
