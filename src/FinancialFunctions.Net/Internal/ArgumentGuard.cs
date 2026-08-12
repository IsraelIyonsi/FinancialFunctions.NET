namespace FinancialFunctions.Internal;

/// <summary>
/// Shared argument validation used across the public API so every function
/// rejects invalid input the same way and with the same wording.
/// </summary>
internal static class ArgumentGuard
{
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
}
