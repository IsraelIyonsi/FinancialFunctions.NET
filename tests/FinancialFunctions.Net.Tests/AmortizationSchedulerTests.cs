namespace FinancialFunctions.Tests;

public class AmortizationSchedulerTests
{
    public static IEnumerable<object[]> PrincipalSummingCases()
    {
        yield return new object[] { 200000m, 0.06m / 12m, 360 };
        yield return new object[] { 1200m, 0m, 12 };
        yield return new object[] { 1000m, 0.007123m, 3 };
        yield return new object[] { 9999.99m, 0.0325m, 47 };
        yield return new object[] { 50m, 0.2m, 1 };
    }

    [Theory]
    [MemberData(nameof(PrincipalSummingCases))]
    public void PrincipalPaid_sums_exactly_to_principal(decimal principal, decimal periodicRate, int numberOfPayments)
    {
        var schedule = AmortizationScheduler.GenerateSchedule(principal, periodicRate, numberOfPayments);

        var totalPrincipalPaid = schedule.Sum(period => period.PrincipalPaid);

        Assert.Equal(principal, totalPrincipalPaid);
    }

    [Theory]
    [MemberData(nameof(PrincipalSummingCases))]
    public void RemainingBalance_reaches_exactly_zero_on_the_final_period(decimal principal, decimal periodicRate, int numberOfPayments)
    {
        var schedule = AmortizationScheduler.GenerateSchedule(principal, periodicRate, numberOfPayments);

        Assert.Equal(0m, schedule[^1].RemainingBalance);
    }

    [Theory]
    [MemberData(nameof(PrincipalSummingCases))]
    public void PeriodNumbers_are_sequential_starting_at_one(decimal principal, decimal periodicRate, int numberOfPayments)
    {
        var schedule = AmortizationScheduler.GenerateSchedule(principal, periodicRate, numberOfPayments);

        for (var i = 0; i < schedule.Count; i++)
        {
            Assert.Equal(i + 1, schedule[i].PeriodNumber);
        }
    }

    [Fact]
    public void First_period_payment_matches_the_standard_mortgage_formula()
    {
        var schedule = AmortizationScheduler.GenerateSchedule(200000m, 0.06m / 12m, 360);

        Assert.Equal(1199.10m, schedule[0].PaymentAmount);
        Assert.Equal(1000.00m, schedule[0].InterestPaid);
        Assert.Equal(199.10m, schedule[0].PrincipalPaid);
    }

    [Fact]
    public void Interest_free_loan_splits_principal_evenly_with_no_interest()
    {
        var schedule = AmortizationScheduler.GenerateSchedule(1200m, 0m, 12);

        Assert.All(schedule, period => Assert.Equal(0m, period.InterestPaid));
        Assert.All(schedule, period => Assert.Equal(100m, period.PrincipalPaid));
        Assert.Equal(100m, schedule[0].PaymentAmount);
        Assert.Equal(0m, schedule[^1].RemainingBalance);
    }

    [Fact]
    public void RemainingBalance_decreases_monotonically()
    {
        var schedule = AmortizationScheduler.GenerateSchedule(10000m, 0.01m, 24);

        for (var i = 1; i < schedule.Count; i++)
        {
            Assert.True(schedule[i].RemainingBalance <= schedule[i - 1].RemainingBalance);
        }
    }

    [Fact]
    public void InterestPaid_equals_rounded_balance_times_rate_for_every_period_except_the_last()
    {
        const decimal principal = 5000m;
        const decimal periodicRate = 0.015m;
        const int numberOfPayments = 10;

        var schedule = AmortizationScheduler.GenerateSchedule(principal, periodicRate, numberOfPayments);
        var balance = principal;

        for (var i = 0; i < schedule.Count - 1; i++)
        {
            var expectedInterest = Math.Round(balance * periodicRate, 2, MidpointRounding.AwayFromZero);
            Assert.Equal(expectedInterest, schedule[i].InterestPaid);
            balance -= schedule[i].PrincipalPaid;
        }
    }

    [Fact]
    public void GenerateSchedule_rejects_non_positive_principal()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AmortizationScheduler.GenerateSchedule(0m, 0.05m, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => AmortizationScheduler.GenerateSchedule(-100m, 0.05m, 12));
    }

    [Fact]
    public void GenerateSchedule_rejects_rate_at_negative_one()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AmortizationScheduler.GenerateSchedule(1000m, -1m, 12));
    }

    [Fact]
    public void GenerateSchedule_rejects_non_positive_number_of_payments()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AmortizationScheduler.GenerateSchedule(1000m, 0.05m, 0));
    }

    [Fact]
    public void GenerateSchedule_rejects_negative_rounding_decimals()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => AmortizationScheduler.GenerateSchedule(1000m, 0.05m, 12, -1));
    }
}
