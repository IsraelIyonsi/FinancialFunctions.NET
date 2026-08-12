namespace FinancialFunctions;

/// <summary>
/// Thrown when an iterative solver (RATE, IRR or XIRR) fails to converge on a
/// root within the configured iteration cap, and no sign-changing bracket
/// could be found for the bisection fallback either. This typically means the
/// cashflow series has no real solution for the requested value, or the
/// solver's search range does not contain one.
/// </summary>
public sealed class FinancialConvergenceException : Exception
{
    /// <summary>
    /// Creates a new <see cref="FinancialConvergenceException"/> with a message
    /// describing why convergence failed.
    /// </summary>
    /// <param name="message">A human readable description of the failure.</param>
    public FinancialConvergenceException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates a new <see cref="FinancialConvergenceException"/> with a message
    /// and an inner exception describing why convergence failed.
    /// </summary>
    /// <param name="message">A human readable description of the failure.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public FinancialConvergenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
