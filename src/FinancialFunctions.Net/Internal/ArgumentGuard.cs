namespace FinancialFunctions.Internal;

/// <summary>
/// Shared argument validation used across the public API so every function
/// rejects invalid input the same way and with the same wording.
/// </summary>
internal static class ArgumentGuard
{
    private const int FirstValidPeriod = 1;

    public static void EnsureRateAboveDomainFloor(decimal rate, string paramName)
    {
        if (rate <= (decimal)FinancialSolverDefaults.MinimumRate)
        {
            throw new ArgumentOutOfRangeException(paramName, rate, "Rate must be greater than -1 (a total loss of principal is not a valid periodic rate).");
        }
    }

    public static void EnsurePeriodsPositive(int numberOfPeriods, string paramName)
    {
        if (numberOfPeriods <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, numberOfPeriods, "Number of periods must be greater than zero.");
        }
    }

    public static void EnsurePeriodInRange(int period, int numberOfPeriods, string paramName)
    {
        if (period < FirstValidPeriod || period > numberOfPeriods)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                period,
                $"Period must be between {FirstValidPeriod} and the number of periods ({numberOfPeriods}), inclusive (Excel returns #NUM! otherwise).");
        }
    }

    public static void EnsureNotEmpty<T>(IReadOnlyList<T> values, string paramName)
    {
        ArgumentNullException.ThrowIfNull(values, paramName);

        if (values.Count == 0)
        {
            throw new ArgumentException("Cashflow series must contain at least one value.", paramName);
        }
    }

    public static void EnsureAtLeastTwoCashflows(IReadOnlyList<decimal> cashflows, string paramName)
    {
        EnsureNotEmpty(cashflows, paramName);

        if (cashflows.Count < 2)
        {
            throw new ArgumentException("Cashflow series must contain at least two values.", paramName);
        }
    }

    public static void EnsureContainsSignChange(IReadOnlyList<decimal> cashflows, string paramName)
    {
        var hasPositive = false;
        var hasNegative = false;

        foreach (var cashflow in cashflows)
        {
            hasPositive |= cashflow > 0m;
            hasNegative |= cashflow < 0m;
        }

        if (!hasPositive || !hasNegative)
        {
            throw new ArgumentException("Cashflow series must contain at least one positive and one negative value.", paramName);
        }
    }

    public static void EnsureDatesMatchCashflows(IReadOnlyList<decimal> cashflows, IReadOnlyList<DateTime> dates, string cashflowsParamName, string datesParamName)
    {
        ArgumentNullException.ThrowIfNull(dates, datesParamName);

        if (dates.Count != cashflows.Count)
        {
            throw new ArgumentException($"'{datesParamName}' must contain the same number of entries as '{cashflowsParamName}'.", datesParamName);
        }
    }

    /// <summary>
    /// Ensures no date in <paramref name="dates"/> falls before <c>dates[0]</c>,
    /// the valuation date every other date is measured against. Matches
    /// Excel's XNPV/XIRR, which return <c>#NUM!</c> when a date precedes the
    /// starting date; dates equal to or after the starting date, in any order,
    /// are permitted.
    /// </summary>
    public static void EnsureNoDateBeforeValuationDate(IReadOnlyList<DateTime> dates, string paramName)
    {
        var valuationDate = dates[0];

        for (var i = 1; i < dates.Count; i++)
        {
            if (dates[i] < valuationDate)
            {
                throw new ArgumentException(
                    $"'{paramName}' must not contain a date earlier than dates[0] ({valuationDate:d}), the valuation date every other date is measured against.",
                    paramName);
            }
        }
    }
}
