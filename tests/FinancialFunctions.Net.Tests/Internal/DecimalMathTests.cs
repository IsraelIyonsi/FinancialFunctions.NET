using FinancialFunctions.Internal;

namespace FinancialFunctions.Tests.Internal;

public class DecimalMathTests
{
    [Theory]
    [InlineData(2, 10, 1024)]
    [InlineData(5, 0, 1)]
    [InlineData(1.5, 4, 5.0625)]
    [InlineData(0, 3, 0)]
    [InlineData(-2, 3, -8)]
    [InlineData(-2, 2, 4)]
    public void Pow_matches_expected_value_for_positive_and_zero_exponents(decimal value, int exponent, decimal expected)
    {
        Assert.Equal(expected, DecimalMath.Pow(value, exponent));
    }

    [Theory]
    [InlineData(2, -2, 0.25)]
    [InlineData(4, -1, 0.25)]
    [InlineData(10, -3, 0.001)]
    public void Pow_matches_expected_value_for_negative_exponents(decimal value, int exponent, decimal expected)
    {
        Assert.Equal(expected, DecimalMath.Pow(value, exponent));
    }

    [Fact]
    public void Pow_of_zero_with_negative_exponent_throws()
    {
        Assert.Throws<DivideByZeroException>(() => DecimalMath.Pow(0m, -1));
    }
}
