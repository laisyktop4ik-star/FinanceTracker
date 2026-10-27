using FinanceTracker.Core;

namespace FinanceTracker.Tests;

public sealed class FinanceCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsIncomeExpenseAndBalanceInMinorUnits()
    {
        var transactions = new[]
        {
            Transaction.Create(1, TransactionType.Income, 15_000_00, "Зарплата", "Работа", new DateOnly(2026, 10, 1)),
            Transaction.Create(2, TransactionType.Expense, 4_200_00, "Продукты", "Покупки", new DateOnly(2026, 10, 2)),
            Transaction.Create(3, TransactionType.Income, 2_000_50, "Подработка", "Фриланс", new DateOnly(2026, 10, 3))
        };

        var result = FinanceCalculator.Calculate(transactions);

        Assert.Equal(17_000_50, result.IncomeMinor);
        Assert.Equal(4_200_00, result.ExpenseMinor);
        Assert.Equal(12_800_50, result.BalanceMinor);
    }

    [Theory]
    [InlineData(150.25, 15025)]
    [InlineData(0.01, 1)]
    [InlineData(10.999, 1100)]
    public void Money_ToMinorUnits_RoundsToKopiykas(decimal amount, long expected)
    {
        Assert.Equal(expected, Money.ToMinorUnits(amount));
    }

    [Fact]
    public void Transaction_Create_RejectsNonPositiveAmount()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            Transaction.Create(0, TransactionType.Expense, 0, "Продукты", "", DateOnly.FromDateTime(DateTime.Today)));

        Assert.Equal("amountMinor", error.ParamName);
    }

    [Fact]
    public void Transaction_Create_RejectsEmptyCategory()
    {
        Assert.Throws<ArgumentException>(() =>
            Transaction.Create(0, TransactionType.Expense, 100, "  ", "", DateOnly.FromDateTime(DateTime.Today)));
    }

    [Fact]
    public void Transaction_Create_RejectsDescriptionLongerThan500Characters()
    {
        Assert.Throws<ArgumentException>(() =>
            Transaction.Create(0, TransactionType.Expense, 100, "Другое", new string('x', 501), DateOnly.FromDateTime(DateTime.Today)));
    }
}
