using FinanceTracker.Core;
using FinanceTracker.Data;

namespace FinanceTracker.Tests;

public sealed class SettingsAndBackupTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"finance-settings-{Guid.NewGuid():N}");
    private string DatabasePath => Path.Combine(_directory, "finance.db");
    private SqliteFinanceRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _repository = new SqliteFinanceRepository(DatabasePath);
        await _repository.InitializeAsync();
    }

    public Task DisposeAsync()
    {
        SqliteConnectionCleanup();
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Setting_RoundTripsAndReturnsDefaultWhenMissing()
    {
        Assert.Equal("грн", await _repository.GetSettingAsync("Currency", "грн"));

        await _repository.SetSettingAsync("Currency", "₴");

        Assert.Equal("₴", await _repository.GetSettingAsync("Currency", "грн"));
    }

    [Fact]
    public async Task BackupAndRestore_PreservesTransactions()
    {
        var backupPath = Path.Combine(_directory, "backup.db");
        await _repository.AddAsync(Transaction.Create(0, TransactionType.Income, 123_45, "Зарплата", "Тест", new DateOnly(2026, 10, 27)));
        await _repository.CreateBackupAsync(backupPath);
        await _repository.DeleteAsync((await _repository.GetAllAsync()).Single().Id);

        await _repository.RestoreBackupAsync(backupPath);

        var restored = Assert.Single(await _repository.GetAllAsync());
        Assert.Equal(123_45, restored.AmountMinor);
        Assert.Equal("Тест", restored.Description);
    }

    private static void SqliteConnectionCleanup() => Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
}
