namespace FinancialFunctions;

/// <summary>
/// Identifies whether a periodic payment in an annuity falls due at the start
/// or the end of each period. Mirrors the "type" argument used by the PV, FV,
/// PMT, NPER and RATE functions in Excel and LibreOffice Calc, where 0 means
/// end of period and 1 means beginning of period.
/// </summary>
public enum PaymentTiming
{
    /// <summary>
    /// The payment is due at the end of each period (an ordinary annuity).
    /// This is the conventional default for loans and mortgages.
    /// </summary>
    EndOfPeriod = 0,

    /// <summary>
    /// The payment is due at the start of each period (an annuity due).
    /// Typical of leases and rent paid in advance.
    /// </summary>
    BeginningOfPeriod = 1
}
