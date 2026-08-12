# FinancialFunctions.NET

Time-value-of-money and cashflow math for .NET: PV, FV, PMT, RATE, NPER, NPV, IRR, MIRR, date-aware XNPV/XIRR, and loan amortization schedules. Zero dependencies.

Excel and LibreOffice Calc ship a full set of financial functions. The .NET base class library ships none of them. The one .NET incumbent, `Microsoft.VisualBasic.Financial`, is Windows-only (it depends on COM interop types), VB-flavored, undocumented in its edge-case behavior, and has had no functional change in over a decade. FinancialFunctions.NET is a modern, cross-platform, fully documented replacement: `decimal`-precise where money is at stake, table-driven-tested against independently computed reference values, and dependency-free.

## Install

```
dotnet add package FinancialFunctions.Net
```

## Usage

### Mortgage payment and amortization schedule

```csharp
using FinancialFunctions;

decimal principal = 200_000m;
decimal monthlyRate = 0.06m / 12m;
int termInMonths = 360;

decimal monthlyPayment = Financial.Payment(monthlyRate, termInMonths, principal);
// -1199.10 (an outflow, by the PV/FV/PMT sign convention: negative means paid out.
// principal is passed positive here because it is money received by the borrower.)

var schedule = AmortizationScheduler.GenerateSchedule(principal, monthlyRate, termInMonths);
Console.WriteLine(schedule[0]);
// PeriodNumber: 1, PaymentAmount: 1199.10, PrincipalPaid: 199.10, InterestPaid: 1000.00, RemainingBalance: 199800.90

decimal totalPrincipalPaid = schedule.Sum(period => period.PrincipalPaid);
// exactly 200000.00 - the schedule always sums to the original principal, no minor units lost to rounding
```

### Investment return on irregular, dated cashflows (XIRR)

```csharp
using FinancialFunctions;

var cashflows = new[] { -10_000m, 2_750m, 4_250m, 3_250m, 2_750m };
var dates = new[]
{
    new DateTime(2024, 1, 1),
    new DateTime(2024, 3, 3),
    new DateTime(2024, 6, 7),
    new DateTime(2024, 9, 6),
    new DateTime(2024, 12, 6),
};

double annualReturn = Financial.InternalRateOfReturn(cashflows, dates);
// 0.6441... (64.41% annualized, using an ACT/365 day-count basis)
```

### Project appraisal with NPV and IRR

```csharp
using FinancialFunctions;

var projectCashflows = new[] { -50_000m, 15_000m, 18_000m, 21_000m, 17_000m };

decimal netPresentValue = Financial.NetPresentValue(0.1m, projectCashflows);
double internalRateOfReturn = Financial.InternalRateOfReturn(projectCashflows);

if (netPresentValue > 0)
{
    Console.WriteLine($"Accept: NPV is {netPresentValue:C}, IRR is {internalRateOfReturn:P2}");
}
```

## API

All functions live on one static class, `FinancialFunctions.Financial`, mirroring the Excel names as closely as .NET naming conventions allow. `NetPresentValue` and `InternalRateOfReturn` each have a date-aware overload (Excel's XNPV and XIRR) that takes a matching list of `DateTime`.

| Method | Excel equivalent | Returns |
|---|---|---|
| `Financial.PresentValue(rate, nper, pmt, fv, timing)` | `PV` | `decimal` |
| `Financial.FutureValue(rate, nper, pmt, pv, timing)` | `FV` | `decimal` |
| `Financial.Payment(rate, nper, pv, fv, timing)` | `PMT` | `decimal` |
| `Financial.NumberOfPeriods(rate, pmt, pv, fv, timing)` | `NPER` | `double` |
| `Financial.Rate(nper, pmt, pv, fv, timing, guess)` | `RATE` | `double` |
| `Financial.NetPresentValue(rate, cashflows)` | `NPV` | `decimal` |
| `Financial.NetPresentValue(rate, cashflows, dates)` | `XNPV` | `decimal` |
| `Financial.InternalRateOfReturn(cashflows, guess)` | `IRR` | `double` |
| `Financial.InternalRateOfReturn(cashflows, dates, guess)` | `XIRR` | `double` |
| `Financial.ModifiedInternalRateOfReturn(cashflows, financeRate, reinvestRate)` | `MIRR` | `double` |
| `AmortizationScheduler.GenerateSchedule(principal, periodicRate, numberOfPayments, roundingDecimals)` | (no direct equivalent) | `IReadOnlyList<AmortizationPeriod>` |

**Sign convention:** money paid out is negative, money received is positive, exactly as in Excel. A `Payment` result is negative because it represents an outflow from the borrower; the amounts inside an `AmortizationPeriod` are unsigned magnitudes (how much the borrower pays), since a schedule is conventionally displayed as positive columns.

## Design notes

- **Decimal where it counts.** `PresentValue`, `FutureValue`, `Payment`, `NetPresentValue` (NPV) and the amortization schedule all compute entirely in `decimal`, including the `(1 + rate)^n` growth factor (via an internal exponentiation-by-squaring helper), so money values never round-trip through floating point. `NumberOfPeriods`, `Rate`, `InternalRateOfReturn` (IRR/XIRR) and `ModifiedInternalRateOfReturn` return a rate rather than a money amount and use `double` for the parts of the computation that require a logarithm, an iterative solve, or a fractional exponent; this mirrors what Excel itself does internally for these functions.
- **XNPV/XIRR day-count basis.** The date-aware overloads use ACT/365: the discount exponent for a cashflow is `(date - date[0]).TotalDays / 365`. This is the same convention Excel and LibreOffice Calc use, and it means the exponent is generally fractional, which is why those two overloads compute internally in `double`.
- **XNPV/XIRR date ordering.** Matching Excel, `dates[0]` is the valuation date every other date is measured against, and no other date may fall before it; a date earlier than `dates[0]` throws `ArgumentException` (Excel returns `#NUM!` in this case). Dates after `dates[0]` may appear in any order.
- **NPER infeasible inputs.** `NumberOfPeriods` throws `ArgumentException` when the payment does not cover the interest accruing on the present value at the given rate, so no finite number of periods reaches the target future value (Excel returns `#NUM!` in this case), rather than silently returning `NaN`.
- **Solver behavior.** `Rate`, `InternalRateOfReturn` and its date-aware overload solve for a root using Newton's method with a numerically estimated derivative (central finite difference), shared by a single internal root finder rather than three separately hand-derived formulas. If Newton's method diverges, stalls, or steps outside the valid domain (rate > -1), the solver falls back to bisection: it scans outward from the domain floor for a sign change and bisects that bracket. If no such bracket exists, `FinancialConvergenceException` is thrown. The convergence tolerance (1e-7) and iteration cap (100) are documented, named constants on `FinancialSolverDefaults`.
- **Multiple roots.** A cashflow series can have zero, one, or several mathematically valid internal rates of return, depending on how many times its cumulative balance changes sign (Descartes' rule of signs). `InternalRateOfReturn` returns whichever root Newton's method converges to from the supplied guess, or the first sign-changing root bisection finds while scanning outward. If your cashflows are not a simple invest-then-return pattern, pass a `guess` close to the root you expect, or use `ModifiedInternalRateOfReturn` (MIRR), which has a single closed-form answer by construction.
- **Amortization rounding.** Each period's interest is `Math.Round(balance * rate, roundingDecimals, MidpointRounding.AwayFromZero)`. The final period's principal is forced to exactly the remaining balance rather than the level payment amount, which guarantees the `PrincipalPaid` column sums to exactly the original principal and the final `RemainingBalance` is exactly zero, regardless of how many cents of rounding drift accumulated along the way.

## Testing methodology

Every closed-form function (PV, FV, PMT, NPER, NPV, XNPV) and every iterative solver (RATE, IRR, XIRR, MIRR) is checked in a table-driven `xUnit` `Theory` against reference values computed independently, in a separate script and a separate language runtime (PowerShell), from the same published closed-form formulas that Excel and LibreOffice Calc document for these functions. This project does not have access to a licensed copy of Excel or LibreOffice in its build environment, so this independent-implementation cross-check is used in place of pasting numbers out of a spreadsheet; the formulas themselves (PV/FV/PMT/NPER/RATE/NPV/IRR/MIRR/XNPV/XIRR) are the ones Microsoft and LibreOffice publish for these exact functions. If you spot a published Excel or LibreOffice worked example that disagrees with a fixture here, please open an issue.

The amortization schedule is checked against hand-computed values for a standard 30-year, 6% mortgage on $200,000 (first-period payment $1,199.10, of which $1,000.00 is interest and $199.10 is principal), and, separately, an invariant test that the `PrincipalPaid` column sums to exactly the original principal for every schedule generated, across a range of principals, rates and terms.

## Dependencies and AOT

Zero runtime NuGet dependencies. The only package reference is `Microsoft.SourceLink.GitHub`, a build-time-only reference used to embed source links in the package; it does not ship in your application. The library contains no reflection, no dynamic code generation, and no unmanaged interop, so it is fully compatible with Native AOT publishing and trimming.

## License

MIT. See [LICENSE](LICENSE).
