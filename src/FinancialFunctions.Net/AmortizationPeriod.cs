namespace FinancialFunctions;

/// <summary>
/// One row of a loan amortization schedule: the split of a single payment
/// between interest and principal, and the balance remaining after the
/// payment is applied.
/// </summary>
/// <param name="PeriodNumber">The one-based index of the payment within the schedule.</param>
/// <param name="PaymentAmount">The total payment made in this period (principal plus interest).</param>
/// <param name="PrincipalPaid">The portion of <paramref name="PaymentAmount"/> that reduces the outstanding principal.</param>
/// <param name="InterestPaid">The portion of <paramref name="PaymentAmount"/> that pays accrued interest.</param>
/// <param name="RemainingBalance">The outstanding principal balance immediately after this payment is applied.</param>
public readonly record struct AmortizationPeriod(
    int PeriodNumber,
    decimal PaymentAmount,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal RemainingBalance);
