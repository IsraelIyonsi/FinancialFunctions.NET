using FinancialFunctions.Tests.TestSupport;

namespace FinancialFunctions.Tests;

public class PeriodicPaymentSplitTests
{
    private const decimal MoneyTolerance = 0.001m;

    private const decimal AutoLoanRate = 0.10m / 12m;
    private const int AutoLoanPeriods = 36;
    private const decimal AutoLoanPrincipal = 8000m;

    public static IEnumerable<object[]> InterestPaymentCases()
    {
        // rate=0.10/12, nper=36, pv=8000, fv=0, type=0. Cross-checked against Excel/LibreOffice IPMT.
        yield return new object[] { AutoLoanRate, 1, AutoLoanPeriods, AutoLoanPrincipal, 0m, PaymentTiming.EndOfPeriod, -66.66666667m };
        yield return new object[] { AutoLoanRate, 36, AutoLoanPeriods, AutoLoanPrincipal, 0m, PaymentTiming.EndOfPeriod, -2.13336775m };

        // rate=0.05 annual, nper=10, pv=100000, fv=0, type=0.
        yield return new object[] { 0.05m, 1, 10, 100000m, 0m, PaymentTiming.EndOfPeriod, -5000m };
        yield return new object[] { 0.05m, 10, 10, 100000m, 0m, PaymentTiming.EndOfPeriod, -616.68845222m };

        // Annuity-due: the first period carries no interest because the payment is made up front.
        yield return new object[] { AutoLoanRate, 1, AutoLoanPeriods, AutoLoanPrincipal, 0m, PaymentTiming.BeginningOfPeriod, 0m };
    }

    [Theory]
    [MemberData(nameof(InterestPaymentCases))]
    public void InterestPayment_matches_reference_formula(decimal rate, int period, int nper, decimal pv, decimal fv, PaymentTiming timing, decimal expected)
    {
        var actual = Financial.InterestPayment(rate, period, nper, pv, fv, timing);
        Approximately.Equal(expected, actual, MoneyTolerance);
    }

    public static IEnumerable<object[]> PrincipalPaymentCases()
    {
        yield return new object[] { AutoLoanRate, 1, AutoLoanPeriods, AutoLoanPrincipal, 0m, PaymentTiming.EndOfPeriod, -191.47083088m };
        yield return new object[] { AutoLoanRate, 36, AutoLoanPeriods, AutoLoanPrincipal, 0m, PaymentTiming.EndOfPeriod, -256.00412980m };
        yield return new object[] { 0.05m, 1, 10, 100000m, 0m, PaymentTiming.EndOfPeriod, -7950.45749655m };
        yield return new object[] { 0.05m, 10, 10, 100000m, 0m, PaymentTiming.EndOfPeriod, -12333.76904433m };
    }

    [Theory]
    [MemberData(nameof(PrincipalPaymentCases))]
    public void PrincipalPayment_matches_reference_formula(decimal rate, int period, int nper, decimal pv, decimal fv, PaymentTiming timing, decimal expected)
    {
        var actual = Financial.PrincipalPayment(rate, period, nper, pv, fv, timing);
        Approximately.Equal(expected, actual, MoneyTolerance);
    }

    [Theory]
    [InlineData(3, 12, 5000)]
    [InlineData(1, 12, 5000)]
    public void InterestPayment_is_zero_and_PrincipalPayment_is_the_whole_payment_when_rate_is_zero(int period, int nper, double pv)
    {
        var presentValue = (decimal)pv;
        var payment = Financial.Payment(0m, nper, presentValue);

        var interest = Financial.InterestPayment(0m, period, nper, presentValue);
        var principal = Financial.PrincipalPayment(0m, period, nper, presentValue);

        Approximately.Equal(0m, interest, MoneyTolerance);
        Approximately.Equal(payment, principal, MoneyTolerance);
    }

    [Theory]
    [InlineData(PaymentTiming.EndOfPeriod)]
    [InlineData(PaymentTiming.BeginningOfPeriod)]
    public void InterestPayment_plus_PrincipalPayment_equals_Payment_for_every_period(PaymentTiming timing)
    {
        var payment = Financial.Payment(AutoLoanRate, AutoLoanPeriods, AutoLoanPrincipal, 0m, timing);

        for (var period = 1; period <= AutoLoanPeriods; period++)
        {
            var interest = Financial.InterestPayment(AutoLoanRate, period, AutoLoanPeriods, AutoLoanPrincipal, 0m, timing);
            var principal = Financial.PrincipalPayment(AutoLoanRate, period, AutoLoanPeriods, AutoLoanPrincipal, 0m, timing);

            Approximately.Equal(payment, interest + principal, MoneyTolerance);
        }
    }

    [Theory]
    [InlineData(0.10 / 12, 36, 8000, 0, PaymentTiming.EndOfPeriod)]
    [InlineData(0.05, 10, 100000, 0, PaymentTiming.EndOfPeriod)]
    [InlineData(0.07, 15, 20000, 5000, PaymentTiming.EndOfPeriod)]
    [InlineData(0.08, 5, 10000, 0, PaymentTiming.BeginningOfPeriod)]
    public void PrincipalPayment_summed_over_all_periods_fully_amortizes_the_balance(double rate, int nper, double pv, double fv, PaymentTiming timing)
    {
        var presentValue = (decimal)pv;
        var futureValue = (decimal)fv;
        var totalPrincipal = 0m;

        for (var period = 1; period <= nper; period++)
        {
            totalPrincipal += Financial.PrincipalPayment((decimal)rate, period, nper, presentValue, futureValue, timing);
        }

        Approximately.Equal(-(presentValue + futureValue), totalPrincipal, MoneyTolerance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    [InlineData(-1)]
    public void InterestPayment_rejects_period_outside_one_through_number_of_periods(int period)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.InterestPayment(AutoLoanRate, period, AutoLoanPeriods, AutoLoanPrincipal));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(37)]
    [InlineData(-1)]
    public void PrincipalPayment_rejects_period_outside_one_through_number_of_periods(int period)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.PrincipalPayment(AutoLoanRate, period, AutoLoanPeriods, AutoLoanPrincipal));
    }

    [Fact]
    public void InterestPayment_rejects_rate_at_negative_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.InterestPayment(-1m, 1, 12, 1000m));
    }

    [Fact]
    public void InterestPayment_rejects_non_positive_number_of_periods()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Financial.InterestPayment(0.05m, 1, 0, 1000m));
    }
}
