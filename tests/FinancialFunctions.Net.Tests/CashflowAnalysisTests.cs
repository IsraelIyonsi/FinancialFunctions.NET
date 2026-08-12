using FinancialFunctions.Tests.TestSupport;

namespace FinancialFunctions.Tests;

public class CashflowAnalysisTests
{
    private const decimal MoneyTolerance = 0.001m;
    private const double RateTolerance = 1e-6;

    public static IEnumerable<object[]> NetPresentValueCases()
    {
        yield return new object[] { 0.1m, new[] { -500m, 200m, 300m, 400m, 500m }, 519.804285598972m };
        yield return new object[] { 0m, new[] { -500m, 200m, 300m, 400m, 500m }, 900m };
    }

    [Theory]
    [MemberData(nameof(NetPresentValueCases))]
    public void NetPresentValue_matches_reference_formula(decimal rate, decimal[] cashflows, decimal expected)
    {
        var actual = Financial.NetPresentValue(rate, cashflows);
        Approximately.Equal(expected, actual, MoneyTolerance);
    }

    public static IEnumerable<object[]> InternalRateOfReturnCases()
    {
        yield return new object[] { new[] { -1000m, 300m, 420m, 680m }, 0.1, 0.163405600688989 };
        yield return new object[] { new[] { -1000m, 1100m }, 0.1, 0.1 };
        yield return new object[] { new[] { 5000m, -1200m, -1200m, -1200m, -1200m, -1200m }, 0.1, 0.064022407643101 };
    }

    [Theory]
    [MemberData(nameof(InternalRateOfReturnCases))]
    public void InternalRateOfReturn_matches_reference_formula(decimal[] cashflows, double guess, double expected)
    {
        var actual = Financial.InternalRateOfReturn(cashflows, guess);
        Approximately.Equal(expected, actual, RateTolerance);
    }

    public static IEnumerable<object[]> ModifiedInternalRateOfReturnCases()
    {
        yield return new object[] { new[] { -1000m, 300m, 420m, 680m }, 0.1m, 0.12m, 0.151471336646763 };
        yield return new object[] { new[] { -7500m, 3000m, 5000m, 1500m, 1200m }, 0.1m, 0.1m, 0.145046648210607 };
    }

    [Theory]
    [MemberData(nameof(ModifiedInternalRateOfReturnCases))]
    public void ModifiedInternalRateOfReturn_matches_reference_formula(decimal[] cashflows, decimal financeRate, decimal reinvestRate, double expected)
    {
        var actual = Financial.ModifiedInternalRateOfReturn(cashflows, financeRate, reinvestRate);
        Approximately.Equal(expected, actual, RateTolerance);
    }

    [Fact]
    public void NetPresentValue_rejects_rate_at_negative_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.NetPresentValue(-1m, new[] { 100m }));
    }

    [Fact]
    public void NetPresentValue_rejects_empty_cashflows()
    {
        Assert.Throws<ArgumentException>(() => Financial.NetPresentValue(0.1m, Array.Empty<decimal>()));
    }

    [Fact]
    public void InternalRateOfReturn_rejects_fewer_than_two_cashflows()
    {
        Assert.Throws<ArgumentException>(() => Financial.InternalRateOfReturn(new[] { -1000m }));
    }

    [Fact]
    public void InternalRateOfReturn_rejects_series_without_a_sign_change()
    {
        Assert.Throws<ArgumentException>(() => Financial.InternalRateOfReturn(new[] { 100m, 200m, 300m }));
    }

    [Fact]
    public void InternalRateOfReturn_rejects_all_negative_series()
    {
        Assert.Throws<ArgumentException>(() => Financial.InternalRateOfReturn(new[] { -100m, -200m, -300m }));
    }

    [Fact]
    public void ModifiedInternalRateOfReturn_rejects_series_without_a_sign_change()
    {
        Assert.Throws<ArgumentException>(() => Financial.ModifiedInternalRateOfReturn(new[] { 100m, 200m }, 0.1m, 0.1m));
    }

    [Fact]
    public void ModifiedInternalRateOfReturn_rejects_finance_rate_at_negative_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.ModifiedInternalRateOfReturn(new[] { -100m, 200m }, -1m, 0.1m));
    }

    [Fact]
    public void ModifiedInternalRateOfReturn_rejects_reinvest_rate_at_negative_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.ModifiedInternalRateOfReturn(new[] { -100m, 200m }, 0.1m, -1m));
    }
}
