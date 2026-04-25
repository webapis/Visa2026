# V-06c Expired (Checked Out)

## 1) State identity (target)

- State ID: `V-06c`
- Caption: `Expired (Checked Out)`
- Internal state code: `ExpiredCheckedOut`
- Replaces part of legacy state: `V-06 Expired`
- Source type: `SQL` (cross-BO)
- Severity: `Archived`
- Click target view: `Registration_ListView`

## 2) Business meaning (target)

Visa is expired and the employee has already completed check-out process.

This is the terminal/historical branch of the expired-state split.

## 3) Eligibility scope (shared across V-06a..V-06d)

A visa is eligible for the V-06 split states only if all are true:

- `Visa.IsActive = true`
- `Visa.IsCancelled = false`
- `Visa.IsDeleted = false`
- `Visa.ExpirationDate < Today` (strictly less)

## 4) Cross-BO linkage model (target)

Check-out linkage:

- via `Registration.Application.ApplicationType.Name = 'App_Reg_Check_Out'`
- same person as expired visa owner
- evaluate the **latest** check-out application (latest by app date, tie-breaker ID desc)

## 5) State condition (target, exact)

`ExpiredCheckedOut` is true when:

1. Visa matches eligibility scope (section 3), and
2. Latest check-out application exists, and
3. Latest check-out application current state code is `PROCESS_ISSUED`

## 6) Priority and exclusivity (target)

Priority order for V-06 split states:

1. `ExpiredCheckedOut`
2. `ExpiredOnCheckOutProcess`
3. `ExpiredMissedTimelyCheckout`
4. `ExpiredToBeCheckedOut`

`ExpiredCheckedOut` has highest priority and must win whenever condition in section 5 is true.

## 7) Working-day rule (shared)

Working-day threshold logic is relevant for V-06a/V-06d; V-06c itself is determined by completed check-out state and overrides timing branches via priority.

## 8) Navigation behavior (target)

When user clicks this state row:

- open `Registration_ListView`
- apply filter representing:
  - eligible expired visa (section 3)
  - latest `App_Reg_Check_Out` exists
  - latest check-out state code = `PROCESS_ISSUED`

## 9) Implementation notes (target)

- Must be SQL-backed (cross-BO)
- Latest-app selection and status evaluation must match exactly between tile count and clicked list filter
- Count/filter parity is mandatory

## 10) Acceptance checks (target)

1. Expired visa + latest check-out app `PROCESS_ISSUED` -> appears in V-06c.
2. If latest check-out app regresses to non-issued state -> leaves V-06c.
3. If no check-out app exists -> does not appear in V-06c.
4. Clicking V-06c opens `Registration_ListView` showing same population.

