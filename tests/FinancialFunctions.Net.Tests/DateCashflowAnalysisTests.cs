using FinancialFunctions.Tests.TestSupport;

namespace FinancialFunctions.Tests;

public class DateCashflowAnalysisTests
{
    private const decimal MoneyTolerance = 0.01m;
    private const double RateTolerance = 1e-6;

    private static readonly DateTime BaseDate = new(2024, 1, 1);

    private static DateTime[] DatesFromOffsets(params int[] dayOffsets) =>
        Array.ConvertAll(dayOffsets, offset => BaseDate.AddDays(offset));

    [Fact]
    public void XNpv_matches_reference_formula()
    {
        var cashflows = new[] { -10000m, 2750m, 4250m, 3250m, 2750m };
        var dates = DatesFromOffsets(0, 62, 158, 249, 340);

        var actual = Financial.NetPresentValue(0.09m, cashflows, dates);

        Approximately.Equal(2406.72797779464m, actual, MoneyTolerance);
    }

    [Fact]
    public void XIrr_matches_reference_formula()
    {
        var cashflows = new[] { -10000m, 2750m, 4250m, 3250m, 2750m };
        var dates = DatesFromOffsets(0, 62, 158, 249, 340);

        var actual = Financial.InternalRateOfReturn(cashflows, dates, 0.1);

        Approximately.Equal(0.644119465963316, actual, RateTolerance);
    }

    [Fact]
    public void XIrr_matches_reference_formula_for_irregular_intervals()
    {
        var cashflows = new[] { -5000m, 1500m, 2000m, 2500m };
        var dates = DatesFromOffsets(0, 45, 130, 365);

        var actual = Financial.InternalRateOfReturn(cashflows, dates, 0.1);

        Approximately.Equal(0.399400661954351, actual, RateTolerance);
    }

    [Fact]
    public void XNpv_and_XIrr_agree_at_the_solved_rate()
    {
        var cashflows = new[] { -5000m, 1500m, 2000m, 2500m };
        var dates = DatesFromOffsets(0, 45, 130, 365);

        var rate = Financial.InternalRateOfReturn(cashflows, dates, 0.1);
        var netPresentValueAtRoot = Financial.NetPresentValue((decimal)rate, cashflows, dates);

        Approximately.Equal(0m, netPresentValueAtRoot, MoneyTolerance);
    }

    [Fact]
    public void XNpv_uses_act_365_day_count_basis()
    {
        var dates = DatesFromOffsets(0, 365);
        var cashflows = new[] { -1000m, 1100m };

        var actual = Financial.NetPresentValue(0.1m, cashflows, dates);

        Approximately.Equal(0m, actual, MoneyTolerance);
    }

    [Fact]
    public void XNpv_rejects_rate_at_negative_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Financial.NetPresentValue(-1m, new[] { 100m }, DatesFromOffsets(0)));
    }

    [Fact]
    public void XNpv_rejects_mismatched_dates_length()
    {
        Assert.Throws<ArgumentException>(() =>
            Financial.NetPresentValue(0.1m, new[] { -100m, 200m }, DatesFromOffsets(0)));
    }

    [Fact]
    public void XIrr_rejects_series_without_a_sign_change()
    {
        Assert.Throws<ArgumentException>(() =>
            Financial.InternalRateOfReturn(new[] { 100m, 200m }, DatesFromOffsets(0, 30)));
    }

    [Fact]
    public void XIrr_rejects_mismatched_dates_length()
    {
        Assert.Throws<ArgumentException>(() =>
            Financial.InternalRateOfReturn(new[] { -100m, 200m, 50m }, DatesFromOffsets(0, 30)));
    }
}
