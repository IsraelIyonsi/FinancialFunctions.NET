using FinancialFunctions.Tests.TestSupport;

namespace FinancialFunctions.Tests;

public class TimeValueOfMoneyTests
{
    private const decimal MoneyTolerance = 0.001m;
    private const double PeriodsTolerance = 1e-6;
    private const double RateTolerance = 1e-6;

    public static IEnumerable<object[]> PresentValueCases()
    {
        yield return new object[] { 0.05m, 10, -1000m, 0m, PaymentTiming.EndOfPeriod, 7721.73492918482m };
        yield return new object[] { 0m, 10, -1000m, 0m, PaymentTiming.EndOfPeriod, 10000m };
        yield return new object[] { 0.06m / 12m, 360, -1199.10105030551m, 0m, PaymentTiming.EndOfPeriod, 199999.999999999m };
        yield return new object[] { 0.05m, 10, -1000m, 0m, PaymentTiming.BeginningOfPeriod, 8107.82167564406m };
    }

    [Theory]
    [MemberData(nameof(PresentValueCases))]
    public void PresentValue_matches_reference_formula(decimal rate, int nper, decimal pmt, decimal fv, PaymentTiming timing, decimal expected)
    {
        var actual = Financial.PresentValue(rate, nper, pmt, fv, timing);
        Approximately.Equal(expected, actual, MoneyTolerance);
    }

    public static IEnumerable<object[]> FutureValueCases()
    {
        yield return new object[] { 0.06m, 10, -2000m, 0m, PaymentTiming.EndOfPeriod, 26361.5898847618m };
        yield return new object[] { 0.05m / 12m, 60, -200m, -1000m, PaymentTiming.BeginningOfPeriod, 14941.2469823728m };
        yield return new object[] { 0.06m, 10, -2000m, 0m, PaymentTiming.BeginningOfPeriod, 27943.2852778475m };
    }

    [Theory]
    [MemberData(nameof(FutureValueCases))]
    public void FutureValue_matches_reference_formula(decimal rate, int nper, decimal pmt, decimal pv, PaymentTiming timing, decimal expected)
    {
        var actual = Financial.FutureValue(rate, nper, pmt, pv, timing);
        Approximately.Equal(expected, actual, MoneyTolerance);
    }

    public static IEnumerable<object[]> PaymentCases()
    {
        yield return new object[] { 0.06m / 12m, 360, 200000m, 0m, PaymentTiming.EndOfPeriod, -1199.10105030551m };
        yield return new object[] { 0.08m, 5, -10000m, 0m, PaymentTiming.EndOfPeriod, 2504.56454566836m };
        yield return new object[] { 0.08m, 5, -10000m, 0m, PaymentTiming.BeginningOfPeriod, 2319.04124598923m };
        yield return new object[] { 0.07m, 15, -20000m, 5000m, PaymentTiming.EndOfPeriod, 1996.9193705151m };
    }

    [Theory]
    [MemberData(nameof(PaymentCases))]
    public void Payment_matches_reference_formula(decimal rate, int nper, decimal pv, decimal fv, PaymentTiming timing, decimal expected)
    {
        var actual = Financial.Payment(rate, nper, pv, fv, timing);
        Approximately.Equal(expected, actual, MoneyTolerance);
    }

    public static IEnumerable<object[]> NumberOfPeriodsCases()
    {
        yield return new object[] { 0.1m, -1000m, 5000m, 0m, PaymentTiming.EndOfPeriod, 7.27254089734171 };
        yield return new object[] { 0m, -500m, 5000m, 0m, PaymentTiming.EndOfPeriod, 10.0 };
        yield return new object[] { 0.1m, -1000m, 5000m, 0m, PaymentTiming.BeginningOfPeriod, 6.35961242350747 };
    }

    [Theory]
    [MemberData(nameof(NumberOfPeriodsCases))]
    public void NumberOfPeriods_matches_reference_formula(decimal rate, decimal pmt, decimal pv, decimal fv, PaymentTiming timing, double expected)
    {
        var actual = Financial.NumberOfPeriods(rate, pmt, pv, fv, timing);
        Approximately.Equal(expected, actual, PeriodsTolerance);
    }

    public static IEnumerable<object[]> RateCases()
    {
        yield return new object[] { 48, -100m, 4000m, 0m, PaymentTiming.EndOfPeriod, 0.1, 0.00770147248820241 };
        yield return new object[] { 24, -300m, 6000m, 0m, PaymentTiming.BeginningOfPeriod, 0.05, 0.0165501190666839 };
    }

    [Theory]
    [MemberData(nameof(RateCases))]
    public void Rate_matches_reference_formula(int nper, decimal pmt, decimal pv, decimal fv, PaymentTiming timing, double guess, double expected)
    {
        var actual = Financial.Rate(nper, pmt, pv, fv, timing, guess);
        Approximately.Equal(expected, actual, RateTolerance);
    }

    [Fact]
    public void PresentValue_and_Payment_are_mutually_consistent()
    {
        var pmt = Financial.Payment(0.005m, 360, 200000m);
        var pv = Financial.PresentValue(0.005m, 360, pmt);
        Approximately.Equal(200000m, pv, MoneyTolerance);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1.5)]
    public void PresentValue_rejects_rate_at_or_below_negative_one(double rate)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.PresentValue((decimal)rate, 10, -100m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void PresentValue_rejects_non_positive_number_of_periods(int nper)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.PresentValue(0.05m, nper, -100m));
    }

    [Fact]
    public void FutureValue_rejects_rate_at_negative_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.FutureValue(-1m, 10, -100m));
    }

    [Fact]
    public void Payment_rejects_non_positive_number_of_periods()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.Payment(0.05m, 0, 1000m));
    }

    [Fact]
    public void NumberOfPeriods_throws_when_rate_and_payment_are_both_zero()
    {
        Assert.Throws<ArgumentException>(() => Financial.NumberOfPeriods(0m, 0m, 1000m));
    }

    [Fact]
    public void NumberOfPeriods_rejects_rate_at_negative_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.NumberOfPeriods(-1m, -100m, 1000m));
    }

    [Fact]
    public void NumberOfPeriods_throws_instead_of_returning_nan_when_payment_does_not_cover_interest()
    {
        // At a 10% periodic rate, a present value of 5000 accrues 500 of interest per period;
        // a payment of only 100 can never amortize it, so no finite number of periods exists
        // (Excel's NPER returns #NUM! for this input).
        var exception = Assert.Throws<ArgumentException>(() => Financial.NumberOfPeriods(0.1m, -100m, 5000m));
        Assert.Equal("payment", exception.ParamName);
    }

    [Fact]
    public void Rate_rejects_non_positive_number_of_periods()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.Rate(0, -100m, 1000m));
    }
}
