namespace FinancialFunctions.Tests.TestSupport;

internal static class Approximately
{
    public static void Equal(decimal expected, decimal actual, decimal tolerance)
    {
        var difference = Math.Abs(expected - actual);
        Assert.True(
            difference <= tolerance,
            $"Expected {expected} to be within {tolerance} of {actual}, but the difference was {difference}.");
    }

    public static void Equal(double expected, double actual, double tolerance)
    {
        var difference = Math.Abs(expected - actual);
        Assert.True(
            difference <= tolerance,
            $"Expected {expected} to be within {tolerance} of {actual}, but the difference was {difference}.");
    }
}
