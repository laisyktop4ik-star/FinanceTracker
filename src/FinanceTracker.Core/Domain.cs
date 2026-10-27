namespace FinanceTracker.Core;

public enum TransactionType
{
    Income,
    Expense
}

public sealed record Transaction
{
    private Transaction(long id, TransactionType type, long amountMinor, string category, string description, DateOnly date)
    {
        Id = id;
        Type = type;
        AmountMinor = amountMinor;
        Category = category;
        Description = description;
        Date = date;
    }

    public long Id { get; init; }
    public TransactionType Type { get; init; }
    public long AmountMinor { get; init; }
    public string Category { get; init; }
    public string Description { get; init; }
    public DateOnly Date { get; init; }

    public static Transaction Create(long id, TransactionType type, long amountMinor, string category, string? description, DateOnly date)
    {
        if (amountMinor <= 0)
            throw new ArgumentOutOfRangeException(nameof(amountMinor), "Сумма должна быть больше нуля.");
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Категория обязательна.", nameof(category));

        var cleanDescription = (description ?? string.Empty).Trim();
        if (cleanDescription.Length > 500)
            throw new ArgumentException("Описание не может превышать 500 символов.", nameof(description));

        return new Transaction(id, type, amountMinor, category.Trim(), cleanDescription, date);
    }
}

public readonly record struct FinanceSummary(long IncomeMinor, long ExpenseMinor)
{
    public long BalanceMinor => IncomeMinor - ExpenseMinor;
}

public static class FinanceCalculator
{
    public static FinanceSummary Calculate(IEnumerable<Transaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        long income = 0;
        long expense = 0;

        foreach (var transaction in transactions)
        {
            if (transaction.Type == TransactionType.Income)
                income = checked(income + transaction.AmountMinor);
            else
                expense = checked(expense + transaction.AmountMinor);
        }

        return new FinanceSummary(income, expense);
    }
}

public static class Money
{
    public static long ToMinorUnits(decimal amount) =>
        checked((long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    public static decimal FromMinorUnits(long amountMinor) => amountMinor / 100m;
}
