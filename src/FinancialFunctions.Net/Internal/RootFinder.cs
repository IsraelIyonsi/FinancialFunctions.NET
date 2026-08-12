namespace FinancialFunctions.Internal;

/// <summary>
/// A general purpose root finder shared by RATE, IRR and XIRR. All three solve
/// for a periodic or annualized rate greater than
/// <see cref="FinancialSolverDefaults.MinimumRate"/> that makes a cashflow
/// equation evaluate to zero.
/// </summary>
/// <remarks>
/// The solver tries Newton's method first, estimating the derivative by
/// central finite difference so a single implementation serves every caller
/// without maintaining a separate hand-derived derivative per formula. If
/// Newton diverges, stalls, or steps outside the valid domain, the solver
/// falls back to bisection: it scans outward from the domain floor for a pair
/// of points where the function changes sign and then bisects that bracket.
/// If no such bracket exists, the cashflow series has no real root in range
/// and <see cref="FinancialConvergenceException"/> is thrown.
/// </remarks>
internal static class RootFinder
{
    /// <summary>
    /// Finds a rate greater than <see cref="FinancialSolverDefaults.MinimumRate"/>
    /// at which <paramref name="equation"/> evaluates to zero.
    /// </summary>
    /// <param name="equation">The cashflow equation to solve, expressed as f(rate) = 0.</param>
    /// <param name="initialGuess">The starting point for Newton's method.</param>
    /// <param name="failureContext">A short description of the calling function, used in the exception message on failure.</param>
    /// <returns>The converged rate.</returns>
    /// <exception cref="FinancialConvergenceException">
    /// Thrown when neither Newton's method nor the bisection fallback converge.
    /// </exception>
    public static double FindRate(Func<double, double> equation, double initialGuess, string failureContext)
    {
        var newtonRoot = TryNewton(equation, initialGuess);
        if (newtonRoot is { } converged)
        {
            return converged;
        }

        var bracket = FindSignChangingBracket(equation);
        if (bracket is { } found)
        {
            return Bisect(equation, found.Lower, found.Upper);
        }

        throw new FinancialConvergenceException(
            $"{failureContext} did not converge: Newton's method failed and no sign-changing bracket " +
            $"was found for rate in ({FinancialSolverDefaults.MinimumRate}, +infinity). The cashflow " +
            "series likely has no real solution.");
    }

    private static double? TryNewton(Func<double, double> equation, double initialGuess)
    {
        var rate = ClampAboveDomainFloor(initialGuess);

        for (var iteration = 0; iteration < FinancialSolverDefaults.MaxIterations; iteration++)
        {
            var value = equation(rate);
            if (!double.IsFinite(value))
            {
                return null;
            }

            if (Math.Abs(value) < FinancialSolverDefaults.ConvergenceTolerance)
            {
                return rate;
            }

            var derivative = EstimateDerivative(equation, rate);
            if (!double.IsFinite(derivative) || Math.Abs(derivative) < FinancialSolverDefaults.DomainEpsilon)
            {
                return null;
            }

            var nextRate = rate - (value / derivative);
            if (!double.IsFinite(nextRate) || nextRate <= FinancialSolverDefaults.MinimumRate + FinancialSolverDefaults.DomainEpsilon)
            {
                return null;
            }

            rate = nextRate;
        }

        return null;
    }

    private static double EstimateDerivative(Func<double, double> equation, double rate)
    {
        var step = Math.Max(Math.Abs(rate) * FinancialSolverDefaults.DerivativeStepFraction, FinancialSolverDefaults.MinimumDerivativeStep);
        var upper = rate + step;
        var lower = ClampAboveDomainFloor(rate - step);
        var effectiveStep = upper - lower;

        if (effectiveStep <= 0)
        {
            return double.NaN;
        }

        return (equation(upper) - equation(lower)) / effectiveStep;
    }

    private static (double Lower, double Upper)? FindSignChangingBracket(Func<double, double> equation)
    {
        var probePoints = BuildProbePoints();
        var previousPoint = probePoints[0];
        var previousValue = equation(previousPoint);

        if (double.IsFinite(previousValue) && Math.Abs(previousValue) < FinancialSolverDefaults.ConvergenceTolerance)
        {
            return (previousPoint, previousPoint);
        }

        for (var i = 1; i < probePoints.Length; i++)
        {
            var currentPoint = probePoints[i];
            var currentValue = equation(currentPoint);

            if (double.IsFinite(currentValue) && Math.Abs(currentValue) < FinancialSolverDefaults.ConvergenceTolerance)
            {
                return (currentPoint, currentPoint);
            }

            if (double.IsFinite(previousValue) && double.IsFinite(currentValue) && Math.Sign(previousValue) != Math.Sign(currentValue))
            {
                return (previousPoint, currentPoint);
            }

            previousPoint = currentPoint;
            previousValue = currentValue;
        }

        return null;
    }

    private static double[] BuildProbePoints()
    {
        const double positiveGrowthFactor = 1.7;

        var halfCount = FinancialSolverDefaults.MaxBracketProbes / 2;
        var floor = FinancialSolverDefaults.MinimumRate + FinancialSolverDefaults.DomainEpsilon;
        var negativeStep = -floor / halfCount;

        var points = new double[FinancialSolverDefaults.MaxBracketProbes];

        for (var i = 0; i < halfCount; i++)
        {
            points[i] = floor + (negativeStep * (i + 1));
        }

        for (var i = 0; i < halfCount; i++)
        {
            points[halfCount + i] = Math.Pow(positiveGrowthFactor, i + 1) - 1.0;
        }

        return points;
    }

    private static double Bisect(Func<double, double> equation, double lower, double upper)
    {
        if (lower == upper)
        {
            return lower;
        }

        var lowerValue = equation(lower);
        var upperValue = equation(upper);

        for (var iteration = 0; iteration < FinancialSolverDefaults.MaxIterations; iteration++)
        {
            var midpoint = (lower + upper) / 2.0;
            var midValue = equation(midpoint);

            if (Math.Abs(midValue) < FinancialSolverDefaults.ConvergenceTolerance || (upper - lower) / 2.0 < FinancialSolverDefaults.ConvergenceTolerance)
            {
                return midpoint;
            }

            if (Math.Sign(midValue) == Math.Sign(lowerValue))
            {
                lower = midpoint;
                lowerValue = midValue;
            }
            else
            {
                upper = midpoint;
                upperValue = midValue;
            }
        }

        return (lower + upper) / 2.0;
    }

    private static double ClampAboveDomainFloor(double rate)
    {
        var floor = FinancialSolverDefaults.MinimumRate + FinancialSolverDefaults.DomainEpsilon;
        return rate <= floor ? floor : rate;
    }
}
