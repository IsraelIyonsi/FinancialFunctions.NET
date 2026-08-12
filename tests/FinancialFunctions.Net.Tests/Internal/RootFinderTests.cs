using FinancialFunctions.Internal;

namespace FinancialFunctions.Tests.Internal;

public class RootFinderTests
{
    [Fact]
    public void FindRate_solves_a_simple_linear_equation_via_newton()
    {
        double Equation(double rate) => (3.0 * rate) - 0.9;

        var root = RootFinder.FindRate(Equation, initialGuess: 0.1, failureContext: "test");

        Assert.Equal(0.3, root, precision: 6);
    }

    [Fact]
    public void FindRate_falls_back_to_bisection_when_the_initial_guess_sits_on_a_stationary_point()
    {
        double Equation(double rate) => Math.Pow(rate - 0.5, 2) - 0.04;

        var root = RootFinder.FindRate(Equation, initialGuess: 0.5, failureContext: "test");

        Assert.True(Math.Abs(root - 0.3) < 1e-4 || Math.Abs(root - 0.7) < 1e-4);
    }

    [Fact]
    public void FindRate_throws_when_no_real_root_exists_in_the_search_domain()
    {
        double Equation(double rate) => (rate * rate) + 1.0;

        Assert.Throws<FinancialConvergenceException>(() => RootFinder.FindRate(Equation, initialGuess: 0.1, failureContext: "test"));
    }
}
