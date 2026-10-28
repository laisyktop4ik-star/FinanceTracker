using FinanceTracker.Core;

namespace FinanceTracker.Tests;

public sealed class TrendChartCalculatorTests
{
    [Fact]
    public void Build_ReturnsSixChronologicalMonthsAndSeparateTotals()
    {
        var selectedMonth = new DateOnly(2026, 10, 1);
        var transactions = new[]
        {
            Transaction.Create(1, TransactionType.Income, 600_000, "Зарплата", "", new DateOnly(2026, 8, 10)),
            Transaction.Create(2, TransactionType.Expense, 300_000, "Продукты", "", new DateOnly(2026, 9, 5)),
            Transaction.Create(3, TransactionType.Expense, 2_000_000, "Покупки", "", new DateOnly(2026, 10, 28)),
            Transaction.Create(4, TransactionType.Income, 999_999, "Старое", "", new DateOnly(2026, 4, 30)),
            Transaction.Create(5, TransactionType.Expense, 100_000, "Транспорт", "", new DateOnly(2026, 8, 15))
        };

        var chart = TrendChartCalculator.Build(selectedMonth, transactions);

        Assert.Equal(
            new[] { "2026-05", "2026-06", "2026-07", "2026-08", "2026-09", "2026-10" },
            chart.Months.Select(month => $"{month:yyyy-MM}"));
        Assert.Equal(new long[] { 0, 0, 0, 600_000, 0, 0 }, chart.IncomeMinor);
        Assert.Equal(new long[] { 0, 0, 0, 100_000, 300_000, 2_000_000 }, chart.ExpenseMinor);
        Assert.True(chart.HasIncome);
        Assert.True(chart.HasExpenses);
        Assert.Equal(2_500_000, chart.ScaleMaximumMinor);

        Assert.Collection(chart.MonthlyTotals,
            may => Assert.Equal((new DateOnly(2026, 5, 1), 0L, 0L, 0L), (may.Month, may.IncomeMinor, may.ExpenseMinor, may.BalanceMinor)),
            june => Assert.Equal((new DateOnly(2026, 6, 1), 0L, 0L, 0L), (june.Month, june.IncomeMinor, june.ExpenseMinor, june.BalanceMinor)),
            july => Assert.Equal((new DateOnly(2026, 7, 1), 0L, 0L, 0L), (july.Month, july.IncomeMinor, july.ExpenseMinor, july.BalanceMinor)),
            august => Assert.Equal((new DateOnly(2026, 8, 1), 600_000L, 100_000L, 500_000L), (august.Month, august.IncomeMinor, august.ExpenseMinor, august.BalanceMinor)),
            september => Assert.Equal((new DateOnly(2026, 9, 1), 0L, 300_000L, -300_000L), (september.Month, september.IncomeMinor, september.ExpenseMinor, september.BalanceMinor)),
            october => Assert.Equal((new DateOnly(2026, 10, 1), 0L, 2_000_000L, -2_000_000L), (october.Month, october.IncomeMinor, october.ExpenseMinor, october.BalanceMinor)));
    }

    [Fact]
    public void Build_WithNoTransactions_ReturnsStableEmptyChart()
    {
        var chart = TrendChartCalculator.Build(new DateOnly(2026, 10, 1), []);

        Assert.False(chart.HasData);
        Assert.False(chart.HasIncome);
        Assert.False(chart.HasExpenses);
        Assert.Equal(10_000, chart.ScaleMaximumMinor);
        Assert.All(chart.IncomeMinor, value => Assert.Equal(0, value));
        Assert.All(chart.ExpenseMinor, value => Assert.Equal(0, value));
    }
}
