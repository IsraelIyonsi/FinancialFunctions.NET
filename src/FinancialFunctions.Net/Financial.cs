using FinancialFunctions.Internal;

namespace FinancialFunctions;

/// <summary>
/// Time-value-of-money and cashflow functions equivalent to the financial
/// functions shipped by Excel and LibreOffice Calc: PV, FV, PMT, NPER, RATE,
/// NPV, IRR, MIRR, and the date-aware XNPV and XIRR (exposed as overloads of
/// <see cref="NetPresentValue(decimal, IReadOnlyList{decimal})"/> and
/// <see cref="InternalRateOfReturn(IReadOnlyList{decimal}, double)"/> that
/// accept a matching list of dates).
/// </summary>
/// <remarks>
/// <para>
/// Sign convention: money paid out (an investment, a loan repayment) is
/// negative; money received (a loan disbursed to you, a dividend) is
/// positive. This is the same convention Excel uses, and it is what makes an
/// equation like PV + FV/(1+rate)^n + PMT-annuity = 0 balance.
/// </para>
/// <para>
/// <see cref="Rate"/>, <see cref="InternalRateOfReturn(IReadOnlyList{decimal}, double)"/>
/// and its date-aware overload solve for a rate iteratively using Newton's
/// method with a bisection fallback. See <see cref="FinancialSolverDefaults"/>
/// for the convergence tolerance and iteration cap, and
/// <see cref="FinancialConvergenceException"/> for the failure mode.
/// </para>
/// </remarks>
public static class Financial
{
    /// <summary>
    /// Computes the present value of a series of future, equal periodic
    /// payments plus an optional lump-sum future value, given a constant
    /// periodic interest rate. Equivalent to Excel's PV function.
    /// </summary>
    /// <param name="rate">The periodic interest rate, greater than -1. Use an annual rate divided by the number of periods per year for a monthly schedule.</param>
    /// <param name="numberOfPeriods">The total number of payment periods.</param>
    /// <param name="payment">The payment made in each period. Negative for an outflow.</param>
    /// <param name="futureValue">The cash balance desired after the last payment. Defaults to zero.</param>
    /// <param name="timing">Whether payments fall due at the start or end of each period. Defaults to end of period.</param>
    /// <returns>The present value that, invested today at <paramref name="rate"/>, funds the described payment stream.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rate"/> is not greater than -1, or <paramref name="numberOfPeriods"/> is not greater than zero.</exception>
    public static decimal PresentValue(decimal rate, int numberOfPeriods, decimal payment, decimal futureValue = 0m, PaymentTiming timing = PaymentTiming.EndOfPeriod)
    {
        ArgumentGuard.EnsureRateAboveDomainFloor(rate, nameof(rate));
        ArgumentGuard.EnsurePeriodsPositive(numberOfPeriods, nameof(numberOfPeriods));

        if (rate == 0m)
        {
            return -(futureValue + (payment * numberOfPeriods));
        }

        var growthFactor = DecimalMath.Pow(1m + rate, numberOfPeriods);
        var annuityFactor = AnnuityFactor(rate, numberOfPeriods, growthFactor);
        var typeAdjustedPayment = payment * (1m + (rate * (int)timing));

        return -(futureValue + (typeAdjustedPayment * annuityFactor)) / growthFactor;
    }

    /// <summary>
    /// Computes the future value of a series of equal periodic payments plus
    /// an optional present value invested today, given a constant periodic
    /// interest rate. Equivalent to Excel's FV function.
    /// </summary>
    /// <param name="rate">The periodic interest rate, greater than -1.</param>
    /// <param name="numberOfPeriods">The total number of payment periods.</param>
    /// <param name="payment">The payment made in each period. Negative for an outflow.</param>
    /// <param name="presentValue">The lump sum invested at the start. Defaults to zero.</param>
    /// <param name="timing">Whether payments fall due at the start or end of each period. Defaults to end of period.</param>
    /// <returns>The value of the investment after the last payment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rate"/> is not greater than -1, or <paramref name="numberOfPeriods"/> is not greater than zero.</exception>
    public static decimal FutureValue(decimal rate, int numberOfPeriods, decimal payment, decimal presentValue = 0m, PaymentTiming timing = PaymentTiming.EndOfPeriod)
    {
        ArgumentGuard.EnsureRateAboveDomainFloor(rate, nameof(rate));
        ArgumentGuard.EnsurePeriodsPositive(numberOfPeriods, nameof(numberOfPeriods));

        if (rate == 0m)
        {
            return -(presentValue + (payment * numberOfPeriods));
        }

        var growthFactor = DecimalMath.Pow(1m + rate, numberOfPeriods);
        var annuityFactor = AnnuityFactor(rate, numberOfPeriods, growthFactor);
        var typeAdjustedPayment = payment * (1m + (rate * (int)timing));

        return -((presentValue * growthFactor) + (typeAdjustedPayment * annuityFactor));
    }

    /// <summary>
    /// Computes the periodic payment required to amortize a present value (or
    /// reach a target future value) over a fixed number of periods at a
    /// constant periodic interest rate. Equivalent to Excel's PMT function.
    /// </summary>
    /// <param name="rate">The periodic interest rate, greater than -1.</param>
    /// <param name="numberOfPeriods">The total number of payment periods.</param>
    /// <param name="presentValue">The amount borrowed or invested today.</param>
    /// <param name="futureValue">The cash balance desired after the last payment. Defaults to zero.</param>
    /// <param name="timing">Whether payments fall due at the start or end of each period. Defaults to end of period.</param>
    /// <returns>The payment due each period. Negative when <paramref name="presentValue"/> represents money received (a loan), matching the outflow sign convention.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rate"/> is not greater than -1, or <paramref name="numberOfPeriods"/> is not greater than zero.</exception>
    public static decimal Payment(decimal rate, int numberOfPeriods, decimal presentValue, decimal futureValue = 0m, PaymentTiming timing = PaymentTiming.EndOfPeriod)
    {
        ArgumentGuard.EnsureRateAboveDomainFloor(rate, nameof(rate));
        ArgumentGuard.EnsurePeriodsPositive(numberOfPeriods, nameof(numberOfPeriods));

        if (rate == 0m)
        {
            return -(presentValue + futureValue) / numberOfPeriods;
        }

        var growthFactor = DecimalMath.Pow(1m + rate, numberOfPeriods);
        var annuityFactor = AnnuityFactor(rate, numberOfPeriods, growthFactor);
        var typeAdjustment = 1m + (rate * (int)timing);

        return -((presentValue * growthFactor) + futureValue) / (typeAdjustment * annuityFactor);
    }

    /// <summary>
    /// Computes the number of periods required to amortize a present value
    /// (or reach a target future value) with equal periodic payments at a
    /// constant periodic interest rate. Equivalent to Excel's NPER function.
    /// </summary>
    /// <param name="rate">The periodic interest rate, greater than -1.</param>
    /// <param name="payment">The payment made in each period. Negative for an outflow.</param>
    /// <param name="presentValue">The amount borrowed or invested today.</param>
    /// <param name="futureValue">The cash balance desired after the last payment. Defaults to zero.</param>
    /// <param name="timing">Whether payments fall due at the start or end of each period. Defaults to end of period.</param>
    /// <returns>The number of periods, which may be fractional.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rate"/> is not greater than -1.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="rate"/> is zero and <paramref name="payment"/> is zero, since the
    /// number of periods would be undefined or infinite; or when <paramref name="payment"/> does not
    /// cover the interest that accrues on <paramref name="presentValue"/> at <paramref name="rate"/>,
    /// so no finite number of periods reaches <paramref name="futureValue"/> (equivalent to Excel's
    /// <c>#NUM!</c> error for NPER).
    /// </exception>
    public static double NumberOfPeriods(decimal rate, decimal payment, decimal presentValue, decimal futureValue = 0m, PaymentTiming timing = PaymentTiming.EndOfPeriod)
    {
        ArgumentGuard.EnsureRateAboveDomainFloor(rate, nameof(rate));

        if (rate == 0m)
        {
            if (payment == 0m)
            {
                throw new ArgumentException("Number of periods is undefined when both rate and payment are zero.", nameof(payment));
            }

            return (double)(-(presentValue + futureValue) / payment);
        }

        var typeAdjustedPayment = payment * (1m + (rate * (int)timing));
        var numerator = (double)(typeAdjustedPayment - (futureValue * rate));
        var denominator = (double)(typeAdjustedPayment + (presentValue * rate));
        var ratio = numerator / denominator;

        if (!(ratio > 0.0))
        {
            throw new ArgumentException(
                "No finite number of periods solves this combination of rate, payment, present value and " +
                "future value; the payment does not cover the interest accruing on the present value at this " +
                "rate (equivalent to Excel's #NUM! error for NPER).",
                nameof(payment));
        }

        var numberOfPeriods = Math.Log(ratio) / Math.Log((double)(1m + rate));

        if (!double.IsFinite(numberOfPeriods))
        {
            throw new ArgumentException(
                "No finite number of periods solves this combination of rate, payment, present value and " +
                "future value (equivalent to Excel's #NUM! error for NPER).",
                nameof(payment));
        }

        return numberOfPeriods;
    }

    /// <summary>
    /// Solves for the constant periodic interest rate implied by a fixed
    /// number of periods, a periodic payment, a present value and an optional
    /// future value. Equivalent to Excel's RATE function.
    /// </summary>
    /// <param name="numberOfPeriods">The total number of payment periods.</param>
    /// <param name="payment">The payment made in each period. Negative for an outflow.</param>
    /// <param name="presentValue">The amount borrowed or invested today.</param>
    /// <param name="futureValue">The cash balance desired after the last payment. Defaults to zero.</param>
    /// <param name="timing">Whether payments fall due at the start or end of each period. Defaults to end of period.</param>
    /// <param name="guess">The starting estimate for the iterative solver. Defaults to <see cref="FinancialSolverDefaults.DefaultRateGuess"/>.</param>
    /// <returns>The periodic interest rate that reconciles the inputs.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="numberOfPeriods"/> is not greater than zero.</exception>
    /// <exception cref="FinancialConvergenceException">Thrown when the solver cannot find a rate that satisfies the equation. See <see cref="FinancialSolverDefaults"/>.</exception>
    public static double Rate(int numberOfPeriods, decimal payment, decimal presentValue, decimal futureValue = 0m, PaymentTiming timing = PaymentTiming.EndOfPeriod, double guess = FinancialSolverDefaults.DefaultRateGuess)
    {
        ArgumentGuard.EnsurePeriodsPositive(numberOfPeriods, nameof(numberOfPeriods));

        var pv = (double)presentValue;
        var pmt = (double)payment;
        var fv = (double)futureValue;
        var type = (double)timing;

        double Equation(double rate)
        {
            var annuityFactor = rate == 0.0
                ? numberOfPeriods
                : (Math.Pow(1.0 + rate, numberOfPeriods) - 1.0) / rate;

            return (pv * Math.Pow(1.0 + rate, numberOfPeriods)) + (pmt * (1.0 + (rate * type)) * annuityFactor) + fv;
        }

        return RootFinder.FindRate(Equation, guess, nameof(Rate));
    }

    /// <summary>
    /// Computes the net present value of a series of periodic cashflows
    /// occurring at the end of equally spaced periods, discounted at a
    /// constant periodic rate. Equivalent to Excel's NPV function.
    /// </summary>
    /// <param name="rate">The periodic discount rate, greater than -1.</param>
    /// <param name="cashflows">The cashflows, one per period, starting one period from now. The first entry is discounted by one period, not zero.</param>
    /// <returns>The present value of the cashflow series.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rate"/> is not greater than -1.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cashflows"/> is empty.</exception>
    public static decimal NetPresentValue(decimal rate, IReadOnlyList<decimal> cashflows)
    {
        ArgumentGuard.EnsureRateAboveDomainFloor(rate, nameof(rate));
        ArgumentGuard.EnsureNotEmpty(cashflows, nameof(cashflows));

        var discountBase = 1m + rate;
        var total = 0m;

        for (var i = 0; i < cashflows.Count; i++)
        {
            total += cashflows[i] / DecimalMath.Pow(discountBase, i + 1);
        }

        return total;
    }

    /// <summary>
    /// Computes the net present value of a series of dated cashflows,
    /// discounted at a constant annual rate using an ACT/365 day-count basis.
    /// Equivalent to Excel's XNPV function.
    /// </summary>
    /// <param name="rate">The annual discount rate, greater than -1.</param>
    /// <param name="cashflows">The cashflows, one per entry in <paramref name="dates"/>.</param>
    /// <param name="dates">The date each cashflow in <paramref name="cashflows"/> occurs on. The first date is the valuation date that every other date is measured against.</param>
    /// <returns>The present value of the cashflow series as of <c>dates[0]</c>.</returns>
    /// <remarks>
    /// Because the discount exponent (elapsed days divided by
    /// <see cref="FinancialConstants.DaysPerYearActual365"/>) is fractional,
    /// this overload computes internally in <see cref="double"/> rather than
    /// staying purely in <see cref="decimal"/>, then converts the result back.
    /// This is the same tradeoff Excel and LibreOffice make for XNPV.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="rate"/> is not greater than -1.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cashflows"/> is empty, <paramref name="dates"/> does not contain the
    /// same number of entries as <paramref name="cashflows"/>, or <paramref name="dates"/> contains a date
    /// earlier than <c>dates[0]</c> (matching Excel's XNPV, which returns <c>#NUM!</c> in that case).
    /// </exception>
    /// <exception cref="OverflowException">Thrown when the computed present value is too large to represent as a <see cref="decimal"/>.</exception>
    public static decimal NetPresentValue(decimal rate, IReadOnlyList<decimal> cashflows, IReadOnlyList<DateTime> dates)
    {
        ArgumentGuard.EnsureRateAboveDomainFloor(rate, nameof(rate));
        ArgumentGuard.EnsureNotEmpty(cashflows, nameof(cashflows));
        ArgumentGuard.EnsureDatesMatchCashflows(cashflows, dates, nameof(cashflows), nameof(dates));
        ArgumentGuard.EnsureNoDateBeforeValuationDate(dates, nameof(dates));

        var result = DatedNetPresentValue((double)rate, cashflows, dates);
        return ToDecimalChecked(result);
    }

    /// <summary>
    /// Solves for the constant periodic rate at which the net present value
    /// of a series of periodic cashflows is zero. Equivalent to Excel's IRR
    /// function.
    /// </summary>
    /// <param name="cashflows">The cashflows, one per period, starting at time zero (the first entry is not discounted).</param>
    /// <param name="guess">The starting estimate for the iterative solver. Defaults to <see cref="FinancialSolverDefaults.DefaultRateGuess"/>.</param>
    /// <returns>The internal rate of return.</returns>
    /// <remarks>
    /// A cashflow series can have zero, one, or multiple mathematically valid
    /// internal rates of return depending on how many times its cumulative
    /// balance changes sign (Descartes' rule of signs). This solver returns
    /// the root nearest <paramref name="guess"/> that Newton's method finds,
    /// or, if Newton fails, the first sign-changing root bisection discovers
    /// while scanning outward from the domain floor. Series with multiple
    /// sign changes may have other valid roots; supply a <paramref name="guess"/>
    /// close to the root you expect if the series is not a simple
    /// invest-then-return pattern.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cashflows"/> has fewer than two entries, or does not contain at least one positive and one negative value.</exception>
    /// <exception cref="FinancialConvergenceException">Thrown when the solver cannot find a rate that satisfies the equation.</exception>
    public static double InternalRateOfReturn(IReadOnlyList<decimal> cashflows, double guess = FinancialSolverDefaults.DefaultRateGuess)
    {
        ArgumentGuard.EnsureAtLeastTwoCashflows(cashflows, nameof(cashflows));
        ArgumentGuard.EnsureContainsSignChange(cashflows, nameof(cashflows));

        var doubleCashflows = new double[cashflows.Count];
        for (var i = 0; i < cashflows.Count; i++)
        {
            doubleCashflows[i] = (double)cashflows[i];
        }

        double Equation(double rate)
        {
            var total = 0.0;
            var discountBase = 1.0 + rate;

            for (var i = 0; i < doubleCashflows.Length; i++)
            {
                total += doubleCashflows[i] / Math.Pow(discountBase, i);
            }

            return total;
        }

        return RootFinder.FindRate(Equation, guess, nameof(InternalRateOfReturn));
    }

    /// <summary>
    /// Solves for the annualized rate at which the net present value of a
    /// series of dated cashflows is zero, using an ACT/365 day-count basis.
    /// Equivalent to Excel's XIRR function.
    /// </summary>
    /// <param name="cashflows">The cashflows, one per entry in <paramref name="dates"/>.</param>
    /// <param name="dates">The date each cashflow in <paramref name="cashflows"/> occurs on. The first date is the valuation date that every other date is measured against.</param>
    /// <param name="guess">The starting estimate for the iterative solver. Defaults to <see cref="FinancialSolverDefaults.DefaultRateGuess"/>.</param>
    /// <returns>The annualized internal rate of return.</returns>
    /// <remarks>
    /// See the remarks on <see cref="InternalRateOfReturn(IReadOnlyList{decimal}, double)"/>
    /// regarding multiple-root behavior; the same caveat applies here.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cashflows"/> has fewer than two entries, does not contain at least one
    /// positive and one negative value, <paramref name="dates"/> does not match <paramref name="cashflows"/>
    /// in length, or <paramref name="dates"/> contains a date earlier than <c>dates[0]</c> (matching Excel's
    /// XIRR, which returns <c>#NUM!</c> in that case).
    /// </exception>
    /// <exception cref="FinancialConvergenceException">Thrown when the solver cannot find a rate that satisfies the equation.</exception>
    public static double InternalRateOfReturn(IReadOnlyList<decimal> cashflows, IReadOnlyList<DateTime> dates, double guess = FinancialSolverDefaults.DefaultRateGuess)
    {
        ArgumentGuard.EnsureAtLeastTwoCashflows(cashflows, nameof(cashflows));
        ArgumentGuard.EnsureContainsSignChange(cashflows, nameof(cashflows));
        ArgumentGuard.EnsureDatesMatchCashflows(cashflows, dates, nameof(cashflows), nameof(dates));
        ArgumentGuard.EnsureNoDateBeforeValuationDate(dates, nameof(dates));

        double Equation(double rate) => DatedNetPresentValue(rate, cashflows, dates);

        return RootFinder.FindRate(Equation, guess, nameof(InternalRateOfReturn));
    }

    /// <summary>
    /// Computes the modified internal rate of return of a series of periodic
    /// cashflows, which reinvests positive cashflows at one rate and finances
    /// negative cashflows at another, avoiding the multiple-root ambiguity of
    /// plain IRR. Equivalent to Excel's MIRR function.
    /// </summary>
    /// <param name="cashflows">The cashflows, one per period, starting at time zero.</param>
    /// <param name="financeRate">The interest rate paid on the cash the negative cashflows consume.</param>
    /// <param name="reinvestRate">The interest rate earned when the positive cashflows are reinvested.</param>
    /// <returns>The modified internal rate of return.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cashflows"/> has fewer than two entries, or does not contain at least one positive and one negative value.</exception>
    public static double ModifiedInternalRateOfReturn(IReadOnlyList<decimal> cashflows, decimal financeRate, decimal reinvestRate)
    {
        ArgumentGuard.EnsureAtLeastTwoCashflows(cashflows, nameof(cashflows));
        ArgumentGuard.EnsureContainsSignChange(cashflows, nameof(cashflows));
        ArgumentGuard.EnsureRateAboveDomainFloor(financeRate, nameof(financeRate));
        ArgumentGuard.EnsureRateAboveDomainFloor(reinvestRate, nameof(reinvestRate));

        var periodCount = cashflows.Count;
        var lastIndex = periodCount - 1;
        var reinvestGrowth = 1m + reinvestRate;
        var financeGrowth = 1m + financeRate;

        var positiveFutureValue = 0m;
        var negativePresentValueAbs = 0m;

        for (var i = 0; i < periodCount; i++)
        {
            var cashflow = cashflows[i];
            if (cashflow > 0m)
            {
                positiveFutureValue += cashflow * DecimalMath.Pow(reinvestGrowth, lastIndex - i);
            }
            else if (cashflow < 0m)
            {
                negativePresentValueAbs += -cashflow * DecimalMath.Pow(financeGrowth, -i);
            }
        }

        var ratio = (double)(positiveFutureValue / negativePresentValueAbs);
        return Math.Pow(ratio, 1.0 / lastIndex) - 1.0;
    }

    private static decimal AnnuityFactor(decimal rate, int numberOfPeriods, decimal growthFactor)
    {
        return rate == 0m ? numberOfPeriods : (growthFactor - 1m) / rate;
    }

    /// <summary>
    /// Converts a <see cref="double"/> result to <see cref="decimal"/>, raising a clear,
    /// documented <see cref="OverflowException"/> instead of the CLR's default one when the
    /// value falls outside the range <see cref="decimal"/> can represent (for example, a
    /// discount rate very close to -1 applied over a long time horizon).
    /// </summary>
    private static decimal ToDecimalChecked(double value)
    {
        const double DecimalMinAsDouble = (double)decimal.MinValue;
        const double DecimalMaxAsDouble = (double)decimal.MaxValue;

        if (!double.IsFinite(value) || value < DecimalMinAsDouble || value > DecimalMaxAsDouble)
        {
            throw new OverflowException(
                $"The computed value ({value}) is too large in magnitude to represent as a decimal.");
        }

        return (decimal)value;
    }

    private static double DatedNetPresentValue(double rate, IReadOnlyList<decimal> cashflows, IReadOnlyList<DateTime> dates)
    {
        var baseDate = dates[0];
        var discountBase = 1.0 + rate;
        var total = 0.0;

        for (var i = 0; i < cashflows.Count; i++)
        {
            var elapsedDays = (dates[i] - baseDate).TotalDays;
            var exponent = elapsedDays / FinancialConstants.DaysPerYearActual365;
            total += (double)cashflows[i] / Math.Pow(discountBase, exponent);
        }

        return total;
    }
}
