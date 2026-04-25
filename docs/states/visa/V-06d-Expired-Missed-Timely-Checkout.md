# V-06d Expired (Missed Timely Checkout)

## 1) State identity (target)

- State ID: `V-06d`
- Caption: `Expired (Missed Timely Checkout)`
- Internal state code: `ExpiredMissedTimelyCheckout`
- Replaces part of legacy state: `V-06 Expired`
- Source type: `SQL` (cross-BO)
- Severity: `Critical`
- Click target view: `Registration_ListView`

## 2) Business meaning (target)

Visa is expired and the person has not been checked out within required timeline (more than 3 working days after expiry) and no active in-progress/completed check-out condition is taking precedence.

## 3) Eligibility scope (shared across V-06a..V-06d)

A visa is eligible for V-06 split states only if all are true:

- `Visa.IsActive = true`
- `Visa.IsCancelled = false`
- `Visa.IsDeleted = false`
- `Visa.ExpirationDate < Today` (strictly less)

## 4) Cross-BO linkage model (target)

Check-out app resolution uses:

- `Registration.Application.ApplicationType.Name = 'App_Reg_Check_Out'`
- same person as expired visa owner
- evaluate the **latest** check-out application (latest by app date; tie-breaker ID desc)

## 5) Working-day rule (target, exact)

Working days:

- Monday to Friday
- weekends excluded
- holidays ignored for now

Elapsed working days:

- count working days since `Visa.ExpirationDate` to `Today`

Threshold:

- timely window = first 3 working days after expiry
- missed timely checkout when elapsed working days is `> 3`

## 6) State condition (target, exact)

`ExpiredMissedTimelyCheckout` is true when:

1. Visa matches eligibility scope (section 3), and
2. Elapsed working days since visa expiry is `> 3`, and
3. Latest check-out application is **absent** OR latest check-out application state is not sufficient to qualify higher-priority branches.

Operationally with priority:

- If latest check-out state is `PROCESS_ISSUED` -> `ExpiredCheckedOut` wins.
- Else if latest check-out app exists and state != `PROCESS_ISSUED` -> `ExpiredOnCheckOutProcess` wins.
- Else (no check-out app) and day > 3 -> `ExpiredMissedTimelyCheckout`.

## 7) Priority and exclusivity (target)

Priority order:

1. `ExpiredCheckedOut`
2. `ExpiredOnCheckOutProcess`
3. `ExpiredMissedTimelyCheckout`
4. `ExpiredToBeCheckedOut`

`ExpiredMissedTimelyCheckout` applies only after higher-priority states fail.

## 8) Navigation behavior (target)

When user clicks this state row:

- open `Registration_ListView`
- apply filter representing:
  - eligible expired visa (section 3)
  - elapsed working days `> 3`
  - no higher-priority check-out completion/in-progress branch match

## 9) Implementation notes (target)

- Must be implemented as SQL-backed logic (cross-BO)
- Working-day calculation must be centralized/reused so tile count and list filter stay equivalent
- Latest check-out app resolution must be identical in count and click filter paths

## 10) Acceptance checks (target)

1. Expired visa + no check-out app + elapsed working days > 3 -> appears in V-06d.
2. Same data with elapsed working days < 3 -> appears in V-06a instead.
3. If check-out app appears and is in progress -> moves to V-06b.
4. If check-out app issued -> moves to V-06c.
5. Clicking V-06d opens `Registration_ListView` with matching records.

