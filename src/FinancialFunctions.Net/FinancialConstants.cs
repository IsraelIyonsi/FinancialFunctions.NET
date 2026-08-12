namespace FinancialFunctions;

/// <summary>
/// Named constants that describe the conventions this library uses when a
/// value is not otherwise supplied by the caller.
/// </summary>
public static class FinancialConstants
{
    /// <summary>
    /// The number of days per year used by the ACT/365 day-count basis that
    /// the date-aware overloads of <see cref="Financial.NetPresentValue(decimal, System.Collections.Generic.IReadOnlyList{decimal}, System.Collections.Generic.IReadOnlyList{System.DateTime})"/>
    /// (XNPV) and <see cref="Financial.InternalRateOfReturn(System.Collections.Generic.IReadOnlyList{decimal}, System.Collections.Generic.IReadOnlyList{System.DateTime}, double)"/>
    /// (XIRR) use to convert calendar-day gaps between cashflows into a
    /// fractional number of years: elapsedDays / 365. This matches Excel and
    /// LibreOffice Calc, which both use actual elapsed days over a fixed
    /// 365-day year rather than a 30/360 or actual/actual convention.
    /// </summary>
    public const double DaysPerYearActual365 = 365.0;

    /// <summary>
    /// The default number of decimal places an amortization schedule rounds
    /// each currency figure to, when the caller does not specify one.
    /// </summary>
    public const int DefaultAmortizationRoundingDecimals = 2;
}
