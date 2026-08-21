# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-08-21

### Added

- `Financial.InterestPayment` (Excel `IPMT`) and `Financial.PrincipalPayment` (Excel `PPMT`): the interest and principal portions of the payment for a single period of a level-payment schedule, computed entirely in `decimal` with support for beginning-of-period or end-of-period payment timing (`PaymentTiming`). For any period the two sum back to `Financial.Payment`, and the `PrincipalPayment` values summed across every period repay exactly `-(presentValue + futureValue)`. A `period` outside 1 through the number of periods throws `ArgumentOutOfRangeException` (Excel returns `#NUM!`).

## [0.1.0] - 2026-08-12

### Added

- `Financial.PresentValue`, `Financial.FutureValue` and `Financial.Payment`: closed-form PV, FV and PMT, computed entirely in `decimal` with support for beginning-of-period or end-of-period payment timing (`PaymentTiming`).
- `Financial.NumberOfPeriods`: closed-form NPER via a logarithmic solve.
- `Financial.Rate`: iterative RATE solve using Newton's method with a bisection fallback.
- `Financial.NetPresentValue(rate, cashflows)`: NPV over a periodic cashflow series, computed entirely in `decimal`.
- `Financial.NetPresentValue(rate, cashflows, dates)`: XNPV over a dated cashflow series, using an ACT/365 day-count basis.
- `Financial.InternalRateOfReturn(cashflows, guess)`: IRR, solved iteratively with the same Newton-then-bisection strategy as RATE.
- `Financial.InternalRateOfReturn(cashflows, dates, guess)`: XIRR, the date-aware counterpart, also on an ACT/365 basis.
- `Financial.ModifiedInternalRateOfReturn`: MIRR, a closed-form single-root alternative to IRR that separates the finance rate from the reinvestment rate.
- `AmortizationScheduler.GenerateSchedule`: a per-period loan amortization schedule (`AmortizationPeriod`: payment, principal, interest, remaining balance) whose principal column always sums to exactly the original principal, with rounding drift absorbed into the final period.
- `FinancialConvergenceException`, thrown by the iterative solvers when neither Newton's method nor the bisection fallback can find a root.
- `FinancialSolverDefaults` and `FinancialConstants`: documented, named constants for the solver's convergence tolerance, iteration cap, default guess and the XNPV/XIRR day-count basis.
- Zero runtime dependencies; build-only `Microsoft.SourceLink.GitHub` reference.
- Table-driven test suite cross-checked against reference values computed independently from the published Excel/LibreOffice formulas for every function, plus dedicated amortization-schedule and root-finder edge case coverage.
