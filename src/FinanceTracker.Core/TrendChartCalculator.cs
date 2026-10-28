namespace FinanceTracker.Core;

public sealed record TrendChartData(
    IReadOnlyList<DateOnly> Months,
    IReadOnlyList<long> IncomeMinor,
    IReadOnlyList<long> ExpenseMinor,
    IReadOnlyList<MonthlyTrendTotal> MonthlyTotals,
    long ScaleMaximumMinor)
{
    public bool HasIncome => IncomeMinor.Any(value => value > 0);
    public bool HasExpenses => ExpenseMinor.Any(value => value > 0);
    public bool HasData => HasIncome || HasExpenses;
}

public sealed record MonthlyTrendTotal(DateOnly Month, long IncomeMinor, long ExpenseMinor)
{
    public long BalanceMinor => IncomeMinor - ExpenseMinor;
}

public static class TrendChartCalculator
{
    private static readonly double[] NiceScaleSteps = [1, 1.2, 1.5, 2, 2.5, 3, 4, 5, 6, 8, 10];

    public static TrendChartData Build(DateOnly selectedMonth, IEnumerable<Transaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        var end = new DateOnly(selectedMonth.Year, selectedMonth.Month, 1);
        var months = Enumerable.Range(0, 6)
            .Select(index => end.AddMonths(index - 5))
            .ToArray();
        var monthIndexes = months
            .Select((month, index) => new { Key = (month.Year, month.Month), Index = index })
            .ToDictionary(item => item.Key, item => item.Index);
        var income = new long[months.Length];
        var expenses = new long[months.Length];

        foreach (var transaction in transactions)
        {
            if (!monthIndexes.TryGetValue((transaction.Date.Year, transaction.Date.Month), out var index))
                continue;

            if (transaction.Type == TransactionType.Income)
                income[index] = checked(income[index] + transaction.AmountMinor);
            else
                expenses[index] = checked(expenses[index] + transaction.AmountMinor);
        }

        var maximum = income.Concat(expenses).DefaultIfEmpty(0).Max();
        var monthlyTotals = months
            .Select((month, index) => new MonthlyTrendTotal(month, income[index], expenses[index]))
            .ToArray();
        return new TrendChartData(months, income, expenses, monthlyTotals, NiceScaleMaximum(maximum));
    }

    private static long NiceScaleMaximum(long maximumMinor)
    {
        if (maximumMinor <= 0) return 10_000;

        var padded = maximumMinor * 1.05d;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(padded)));
        var normalized = padded / magnitude;
        var step = NiceScaleSteps.First(value => value >= normalized);
        return checked((long)Math.Ceiling(step * magnitude));
    }
}
