namespace FinancialFunctions;

/// <summary>
/// Named constants that govern the iterative solvers used by
/// <see cref="Financial.Rate"/> and both overloads of
/// <see cref="Financial.InternalRateOfReturn(System.Collections.Generic.IReadOnlyList{decimal}, double)"/>
/// (IRR and its date-aware sibling, XIRR). Every solver uses Newton's method
/// as its primary strategy and falls back to bisection when Newton fails to
/// converge, stops decreasing the residual, or leaves the valid domain (rate
/// greater than negative one).
/// </summary>
public static class FinancialSolverDefaults
{
    /// <summary>
    /// The default starting guess (10%) used by the iterative solvers when the
    /// caller does not supply one. Matches the default guess used by Excel's
    /// RATE, IRR and XIRR functions. See <see cref="Financial.Rate"/> and the
    /// overloads of <see cref="Financial.InternalRateOfReturn(System.Collections.Generic.IReadOnlyList{decimal}, double)"/>.
    /// </summary>
    public const double DefaultRateGuess = 0.1;

    /// <summary>
    /// The absolute residual below which a candidate root is accepted as
    /// converged. The solver stops as soon as the cashflow equation evaluates
    /// to a value whose magnitude is smaller than this tolerance.
    /// </summary>
    public const double ConvergenceTolerance = 1e-7;

    /// <summary>
    /// The maximum number of Newton iterations attempted before falling back
    /// to bisection, and independently the maximum number of bisection steps
    /// attempted before giving up and throwing
    /// <see cref="FinancialConvergenceException"/>.
    /// </summary>
    public const int MaxIterations = 100;

    /// <summary>
    /// The number of expanding probe points scanned outward from the domain
    /// floor when searching for a sign-changing bracket to seed bisection.
    /// </summary>
    public const int MaxBracketProbes = 64;

    /// <summary>
    /// The lower bound of the valid domain for a periodic rate. A rate of
    /// exactly negative one implies total loss of principal with an
    /// undefined discount factor, so the domain is the open interval
    /// (-1, infinity).
    /// </summary>
    public const double MinimumRate = -1.0;

    /// <summary>
    /// The distance kept from <see cref="MinimumRate"/> when probing or
    /// stepping near the domain floor, so that (1 + rate) never reaches zero.
    /// </summary>
    public const double DomainEpsilon = 1e-9;

    /// <summary>
    /// The step size, as a fraction of the evaluation point, used to estimate
    /// derivatives by central finite difference during Newton iteration.
    /// </summary>
    public const double DerivativeStepFraction = 1e-6;

    /// <summary>
    /// The minimum absolute step size used to estimate derivatives by central
    /// finite difference, applied when the evaluation point is close to zero.
    /// </summary>
    public const double MinimumDerivativeStep = 1e-6;
}
