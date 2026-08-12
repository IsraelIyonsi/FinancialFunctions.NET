namespace FinancialFunctions.Internal;

/// <summary>
/// Exact decimal arithmetic helpers used by the closed-form time-value-of-money
/// formulas so that money values never round-trip through <see cref="double"/>
/// and lose precision.
/// </summary>
internal static class DecimalMath
{
    /// <summary>
    /// Raises <paramref name="value"/> to the integer power <paramref name="exponent"/>
    /// using exponentiation by squaring, staying entirely in <see cref="decimal"/>
    /// arithmetic. Negative exponents return the reciprocal of the positive power.
    /// </summary>
    /// <param name="value">The base value.</param>
    /// <param name="exponent">The integer exponent, which may be negative or zero.</param>
    /// <returns><paramref name="value"/> raised to <paramref name="exponent"/>.</returns>
    /// <exception cref="DivideByZeroException">
    /// Thrown when <paramref name="value"/> is zero and <paramref name="exponent"/> is negative.
    /// </exception>
    public static decimal Pow(decimal value, int exponent)
    {
        if (exponent == 0)
        {
            return decimal.One;
        }

        if (exponent < 0)
        {
            return decimal.One / Pow(value, -exponent);
        }

        var result = decimal.One;
        var baseValue = value;
        var remainingExponent = exponent;

        while (remainingExponent > 0)
        {
            if ((remainingExponent & 1) == 1)
            {
                result *= baseValue;
            }

            remainingExponent >>= 1;

            if (remainingExponent > 0)
            {
                baseValue *= baseValue;
            }
        }

        return result;
    }
}
