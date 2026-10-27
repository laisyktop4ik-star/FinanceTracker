using System.Globalization;
using FinanceTracker.Core;
using Microsoft.Data.Sqlite;

namespace FinanceTracker.Data;

public sealed class SqliteFinanceRepository
{
    private readonly string _databasePath;
    private readonly string _connectionString;

    public SqliteFinanceRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var fullPath = Path.GetFullPath(databasePath);
        _databasePath = fullPath;
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA journal_mode = WAL;
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Transactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Type TEXT NOT NULL CHECK (Type IN ('Income', 'Expense')),
                AmountMinor INTEGER NOT NULL CHECK (AmountMinor > 0),
                Category TEXT NOT NULL CHECK (length(trim(Category)) > 0),
                Description TEXT NOT NULL DEFAULT '' CHECK (length(Description) <= 500),
                Date TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Transactions_Date ON Transactions(Date DESC);
            CREATE INDEX IF NOT EXISTS IX_Transactions_Type ON Transactions(Type);

            CREATE TABLE IF NOT EXISTS Categories (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Type TEXT NOT NULL CHECK (Type IN ('Income', 'Expense')),
                Name TEXT NOT NULL CHECK (length(trim(Name)) > 0),
                UNIQUE(Type, Name)
            );

            CREATE TABLE IF NOT EXISTS Budgets (
                Year INTEGER NOT NULL,
                Month INTEGER NOT NULL CHECK (Month BETWEEN 1 AND 12),
                AmountMinor INTEGER NOT NULL CHECK (AmountMinor >= 0),
                PRIMARY KEY (Year, Month)
            );

            CREATE TABLE IF NOT EXISTS Settings (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync();

        foreach (var category in DefaultExpenseCategories)
            await AddCategoryAsync(TransactionType.Expense, category);
        foreach (var category in DefaultIncomeCategories)
            await AddCategoryAsync(TransactionType.Income, category);
    }

    public async Task<long> AddAsync(Transaction transaction)
    {
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Transactions (Type, AmountMinor, Category, Description, Date)
            VALUES ($type, $amount, $category, $description, $date);
            SELECT last_insert_rowid();
            """;
        AddTransactionParameters(command, transaction);
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    public async Task<bool> UpdateAsync(Transaction transaction)
    {
        if (transaction.Id <= 0) return false;
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Transactions
            SET Type=$type, AmountMinor=$amount, Category=$category, Description=$description, Date=$date
            WHERE Id=$id;
            """;
        AddTransactionParameters(command, transaction);
        command.Parameters.AddWithValue("$id", transaction.Id);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Transactions WHERE Id=$id;";
        command.Parameters.AddWithValue("$id", id);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync()
    {
        var result = new List<Transaction>();
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Type, AmountMinor, Category, Description, Date FROM Transactions ORDER BY Date DESC, Id DESC;";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(Transaction.Create(
                reader.GetInt64(0),
                Enum.Parse<TransactionType>(reader.GetString(1)),
                reader.GetInt64(2),
                reader.GetString(3),
                reader.GetString(4),
                DateOnly.ParseExact(reader.GetString(5), "yyyy-MM-dd", CultureInfo.InvariantCulture)));
        }
        return result;
    }

    public async Task AddCategoryAsync(TransactionType type, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Название категории обязательно.", nameof(name));
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "INSERT OR IGNORE INTO Categories (Type, Name) VALUES ($type, $name);";
        command.Parameters.AddWithValue("$type", type.ToString());
        command.Parameters.AddWithValue("$name", name.Trim());
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(TransactionType type)
    {
        var result = new List<string>();
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Name FROM Categories WHERE Type=$type ORDER BY Name COLLATE NOCASE;";
        command.Parameters.AddWithValue("$type", type.ToString());
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync()) result.Add(reader.GetString(0));
        return result;
    }

    public async Task SetMonthlyBudgetAsync(int year, int month, long amountMinor)
    {
        if (month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (amountMinor < 0) throw new ArgumentOutOfRangeException(nameof(amountMinor));
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Budgets (Year, Month, AmountMinor) VALUES ($year, $month, $amount)
            ON CONFLICT(Year, Month) DO UPDATE SET AmountMinor=excluded.AmountMinor;
            """;
        command.Parameters.AddWithValue("$year", year);
        command.Parameters.AddWithValue("$month", month);
        command.Parameters.AddWithValue("$amount", amountMinor);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<long> GetMonthlyBudgetAsync(int year, int month)
    {
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT AmountMinor FROM Budgets WHERE Year=$year AND Month=$month;";
        command.Parameters.AddWithValue("$year", year);
        command.Parameters.AddWithValue("$month", month);
        return Convert.ToInt64(await command.ExecuteScalarAsync() ?? 0L, CultureInfo.InvariantCulture);
    }

    public async Task<string> GetSettingAsync(string key, string defaultValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Value FROM Settings WHERE Key=$key;";
        command.Parameters.AddWithValue("$key", key);
        return (string?)await command.ExecuteScalarAsync() ?? defaultValue;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        await using var connection = await OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Settings (Key, Value) VALUES ($key, $value)
            ON CONFLICT(Key) DO UPDATE SET Value=excluded.Value;
            """;
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$value", value);
        await command.ExecuteNonQueryAsync();
    }

    public async Task CreateBackupAsync(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        var fullDestination = Path.GetFullPath(destinationPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullDestination)!);
        if (File.Exists(fullDestination)) File.Delete(fullDestination);

        await using var source = await OpenAsync();
        await using var destination = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = fullDestination,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());
        await destination.OpenAsync();
        source.BackupDatabase(destination);
    }

    public async Task RestoreBackupAsync(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        var fullSource = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSource)) throw new FileNotFoundException("Файл резервной копии не найден.", fullSource);

        await using (var validation = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = fullSource,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString()))
        {
            await validation.OpenAsync();
            var command = validation.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Transactions';";
            if (Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture) != 1)
                throw new InvalidDataException("Выбранный файл не является резервной копией FinanceTracker.");
        }

        SqliteConnection.ClearAllPools();
        foreach (var suffix in new[] { "-wal", "-shm" })
        {
            var sidecar = _databasePath + suffix;
            if (File.Exists(sidecar)) File.Delete(sidecar);
        }
        File.Copy(fullSource, _databasePath, true);
        await InitializeAsync();
    }

    private async Task<SqliteConnection> OpenAsync()
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    private static void AddTransactionParameters(SqliteCommand command, Transaction transaction)
    {
        command.Parameters.AddWithValue("$type", transaction.Type.ToString());
        command.Parameters.AddWithValue("$amount", transaction.AmountMinor);
        command.Parameters.AddWithValue("$category", transaction.Category);
        command.Parameters.AddWithValue("$description", transaction.Description);
        command.Parameters.AddWithValue("$date", transaction.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    private static readonly string[] DefaultExpenseCategories =
    ["Продукты", "Транспорт", "Развлечения", "Покупки", "Здоровье", "Коммуналка", "Рестораны", "Образование", "Другое"];

    private static readonly string[] DefaultIncomeCategories =
    ["Зарплата", "Подработка", "Подарок", "Возврат", "Другое"];
}
