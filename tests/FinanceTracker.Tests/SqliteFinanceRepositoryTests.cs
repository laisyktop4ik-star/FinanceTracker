using FinanceTracker.Core;
using FinanceTracker.Data;

namespace FinanceTracker.Tests;

public sealed class SqliteFinanceRepositoryTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"finance-tracker-{Guid.NewGuid():N}.db");
    private SqliteFinanceRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _repository = new SqliteFinanceRepository(_databasePath);
        await _repository.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_databasePath)) File.Delete(_databasePath);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task AddAndGetAll_RoundTripsTransaction()
    {
        var source = Transaction.Create(0, TransactionType.Expense, 52_025, "Продукты", "АТБ", new DateOnly(2026, 10, 25));

        var id = await _repository.AddAsync(source);
        var result = Assert.Single(await _repository.GetAllAsync());

        Assert.True(id > 0);
        Assert.Equal(id, result.Id);
        Assert.Equal(source with { Id = id }, result);
    }

    [Fact]
    public async Task Update_ReplacesPersistedValues()
    {
        var id = await _repository.AddAsync(Transaction.Create(0, TransactionType.Expense, 100, "Другое", "До", new DateOnly(2026, 1, 1)));
        var changed = Transaction.Create(id, TransactionType.Income, 250_00, "Подработка", "После", new DateOnly(2026, 2, 2));

        Assert.True(await _repository.UpdateAsync(changed));
        Assert.Equal(changed, Assert.Single(await _repository.GetAllAsync()));
    }

    [Fact]
    public async Task Delete_RemovesTransaction()
    {
        var id = await _repository.AddAsync(Transaction.Create(0, TransactionType.Expense, 100, "Другое", "", new DateOnly(2026, 1, 1)));

        Assert.True(await _repository.DeleteAsync(id));
        Assert.Empty(await _repository.GetAllAsync());
    }

    [Fact]
    public async Task Initialize_SeedsDefaultCategories()
    {
        var expense = await _repository.GetCategoriesAsync(TransactionType.Expense);
        var income = await _repository.GetCategoriesAsync(TransactionType.Income);

        Assert.Contains("Продукты", expense);
        Assert.Contains("Зарплата", income);
    }

    [Fact]
    public async Task Budget_RoundTripsByMonth()
    {
        await _repository.SetMonthlyBudgetAsync(2026, 10, 1_000_000);

        Assert.Equal(1_000_000, await _repository.GetMonthlyBudgetAsync(2026, 10));
        Assert.Equal(0, await _repository.GetMonthlyBudgetAsync(2026, 11));
    }
}
