using FinancialFunctions.Internal;

namespace FinancialFunctions;

/// <summary>
/// Builds loan amortization schedules: the per-period split of a level
/// payment between interest and principal, and the balance remaining after
/// each payment.
/// </summary>
public static class AmortizationScheduler
{
    /// <summary>
    /// The rounding mode applied to every currency figure in a generated
    /// schedule. Money rounds half away from zero, the conventional rule for
    /// currency amounts.
    /// </summary>
    public const MidpointRounding CurrencyRounding = MidpointRounding.AwayFromZero;

    /// <summary>
    /// Generates a level-payment amortization schedule for a loan of
    /// <paramref name="principal"/> repaid over <paramref name="numberOfPayments"/>
    /// equal periods at a constant periodic interest rate.
    /// </summary>
    /// <param name="principal">The original loan amount. Must be greater than zero.</param>
    /// <param name="periodicRate">The interest rate charged per period, greater than -1. Use zero for an interest-free loan.</param>
    /// <param name="numberOfPayments">The number of payments over which the loan is repaid. Must be greater than zero.</param>
    /// <param name="roundingDecimals">The number of decimal places each currency figure is rounded to. Defaults to <see cref="FinancialConstants.DefaultAmortizationRoundingDecimals"/>.</param>
    /// <returns>
    /// One <see cref="AmortizationPeriod"/> per payment, in order. The
    /// <see cref="AmortizationPeriod.PrincipalPaid"/> values sum to exactly
    /// <paramref name="principal"/> and the final row's
    /// <see cref="AmortizationPeriod.RemainingBalance"/> is exactly zero: any
    /// rounding drift accumulated across earlier periods is absorbed into the
    /// final payment rather than lost.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="principal"/> is not greater than zero,
    /// <paramref name="periodicRate"/> is not greater than -1,
    /// <paramref name="numberOfPayments"/> is not greater than zero, or
    /// <paramref name="roundingDecimals"/> is negative.
    /// </exception>
    public static IReadOnlyList<AmortizationPeriod> GenerateSchedule(
        decimal principal,
        decimal periodicRate,
        int numberOfPayments,
        int roundingDecimals = FinancialConstants.DefaultAmortizationRoundingDecimals)
    {
        if (principal <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(principal), principal, "Principal must be greater than zero.");
        }

        ArgumentGuard.EnsureRateAboveDomainFloor(periodicRate, nameof(periodicRate));
        ArgumentGuard.EnsurePeriodsPositive(numberOfPayments, nameof(numberOfPayments));

        if (roundingDecimals < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roundingDecimals), roundingDecimals, "Rounding decimals cannot be negative.");
        }

        var levelPayment = Math.Round(ComputeLevelPayment(principal, periodicRate, numberOfPayments), roundingDecimals, CurrencyRounding);
        var schedule = new List<AmortizationPeriod>(numberOfPayments);
        var balance = principal;

        for (var period = 1; period <= numberOfPayments; period++)
        {
            var interestPaid = Math.Round(balance * periodicRate, roundingDecimals, CurrencyRounding);
            var isFinalPeriod = period == numberOfPayments;
            var principalPaid = isFinalPeriod ? balance : levelPayment - interestPaid;
            var paymentAmount = isFinalPeriod ? interestPaid + principalPaid : levelPayment;

            balance -= principalPaid;
            schedule.Add(new AmortizationPeriod(period, paymentAmount, principalPaid, interestPaid, balance));
        }

        return schedule;
    }

    private static decimal ComputeLevelPayment(decimal principal, decimal periodicRate, int numberOfPayments)
    {
        if (periodicRate == 0m)
        {
            return principal / numberOfPayments;
        }

        var growthFactor = DecimalMath.Pow(1m + periodicRate, numberOfPayments);
        return principal * periodicRate * growthFactor / (growthFactor - 1m);
    }
}
