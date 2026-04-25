# V-06 Expired Split - Index and Consistency Rules

## 1) Purpose

This index is the single cross-state reference for the new split of legacy `V-06 Expired` into:

- `V-06a Expired (To Be Checked Out)` -> `ExpiredToBeCheckedOut`
- `V-06b Expired (On Check Out Process)` -> `ExpiredOnCheckOutProcess`
- `V-06c Expired (Checked Out)` -> `ExpiredCheckedOut`
- `V-06d Expired (Missed Timely Checkout)` -> `ExpiredMissedTimelyCheckout`

Use this file to ensure all four states remain consistent in logic, priority, and implementation.

## 2) Linked state docs

- `docs/states/visa/V-06a-Expired-To-Be-Checked-Out.md`
- `docs/states/visa/V-06b-Expired-On-Check-Out-Process.md`
- `docs/states/visa/V-06c-Expired-Checked-Out.md`
- `docs/states/visa/V-06d-Expired-Missed-Timely-Checkout.md`

## 3) Shared eligibility gate (applies to all V-06x)

A visa must satisfy all:

- `Visa.IsActive = true`
- `Visa.IsCancelled = false`
- `Visa.IsDeleted = false`
- `Visa.ExpirationDate < Today` (strictly less)

If this gate fails, visa is not evaluated against `V-06a..V-06d`.

## 4) Shared cross-BO linkage rule

Check-out app is identified by:

- `Registration.Application.ApplicationType.Name = 'App_Reg_Check_Out'`
- same person as expired visa owner
- use the **latest** matching application (latest app date, tie-breaker ID desc)

## 5) Working-day policy

- Working days: Monday to Friday
- Weekends excluded
- Holidays ignored for now
- Timely window: first 3 working days after visa expiry
- Missed window: elapsed working days since expiry `> 3`

## 6) Priority order (authoritative)

Evaluate in this order:

1. `ExpiredCheckedOut` (`V-06c`)
2. `ExpiredOnCheckOutProcess` (`V-06b`)
3. `ExpiredMissedTimelyCheckout` (`V-06d`)
4. `ExpiredToBeCheckedOut` (`V-06a`)

This order is mandatory for exclusivity when conditions overlap.

## 7) Decision matrix

Given visa passed shared eligibility:

- Latest check-out exists AND latest state = `PROCESS_ISSUED` -> `V-06c`
- Latest check-out exists AND latest state != `PROCESS_ISSUED` -> `V-06b`
- No latest check-out AND elapsed working days `> 3` -> `V-06d`
- No latest check-out AND elapsed working days `<= 3` -> `V-06a`

## 8) Navigation consistency

All four states must:

- open `Registration_ListView`
- use filters semantically equivalent to their count logic

Count and click-list parity is a hard requirement.

## 9) Severity map

- `V-06a Expired (To Be Checked Out)` -> `Warning`
- `V-06b Expired (On Check Out Process)` -> `Info`
- `V-06c Expired (Checked Out)` -> `Archived`
- `V-06d Expired (Missed Timely Checkout)` -> `Critical`

## 10) Implementation checklist

When implementing in code:

1. Replace legacy `V-06 Expired` dashboard row with `V-06a..V-06d`.
2. Implement SQL-backed count logic with shared eligibility gate.
3. Implement latest-checkout resolution consistently in all four queries.
4. Implement working-day calculator (Mon-Fri only) and reuse it.
5. Wire click filters to `Registration_ListView` for each state.
6. Ensure count logic equals click-filter semantics for every state.
7. Update state specs summary and test scenarios.

## 11) Test checklist (minimum)

- Expired + no checkout + <=3 working days -> only `V-06a`
- Expired + latest checkout in progress -> only `V-06b`
- Expired + latest checkout issued -> only `V-06c`
- Expired + no checkout + >3 working days -> only `V-06d`
- Verify each tile count matches opened `Registration_ListView` records.

