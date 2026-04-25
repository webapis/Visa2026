# V-06b Expired (On Check Out Process)

## 1) State identity (target)

- State ID: `V-06b`
- Caption: `Expired (On Check Out Process)`
- Internal state code: `ExpiredOnCheckOutProcess`
- Replaces part of legacy state: `V-06 Expired`
- Source type: `SQL` (cross-BO)
- Severity: `Info`
- Click target view: `Registration_ListView`

## 2) Business meaning (target)

Visa has expired, and a check-out application exists but is not completed yet.

## 3) Eligibility scope (shared across V-06a..V-06d)

A visa is eligible for the V-06 split states only if all are true:

- `Visa.IsActive = true`
- `Visa.IsCancelled = false`
- `Visa.IsDeleted = false`
- `Visa.ExpirationDate < Today` (strictly less)

## 4) Cross-BO linkage model (target)

Check-out linkage is determined via:

- `Registration.Application.ApplicationType.Name = 'App_Reg_Check_Out'`
- same person as expired visa owner
- use the **latest** check-out application (latest by application date; tie-breaker by ID desc)

## 5) State condition (target, exact)

`ExpiredOnCheckOutProcess` is true when:

1. Visa matches eligibility scope (section 3), and
2. Latest check-out application exists, and
3. Latest check-out application current state code is **not** `PROCESS_ISSUED`

Timing note:

- This state is independent of the 3-working-day threshold and has higher priority than missed-timely-checkout.
- If an in-progress check-out exists after day 3, state remains `ExpiredOnCheckOutProcess` until issued.

## 6) Priority and exclusivity (target)

Priority order for V-06 split states:

1. `ExpiredCheckedOut`
2. `ExpiredOnCheckOutProcess`
3. `ExpiredMissedTimelyCheckout`
4. `ExpiredToBeCheckedOut`

`ExpiredOnCheckOutProcess` must win over V-06c/V-06a whenever condition in section 5 is true.

## 7) Working-day rule (shared for V-06a/V-06d)

Working days:

- Monday through Friday
- weekends excluded
- holidays ignored

## 8) Navigation behavior (target)

When user clicks this state row:

- open `Registration_ListView`
- apply filter representing:
  - eligible expired visa (section 3)
  - latest `App_Reg_Check_Out` exists
  - latest check-out state code `!= PROCESS_ISSUED`

## 9) Implementation notes (target)

- Must be SQL-backed (cross-BO joins)
- Latest-app selection logic must be shared between count and click filter
- Count and click filter must remain semantically equivalent

## 10) Acceptance checks (target)

1. Expired visa + latest check-out app `PROCESS_STARTED` -> appears in V-06b.
2. Change latest check-out state to `PROCESS_ISSUED` -> moves out of V-06b.
3. Expired visa with no check-out app does **not** appear in V-06b.
4. V-06b records are visible in `Registration_ListView` using the same state filter.

## 11) Dashboard SQL tooltip content

Human description (shown in tooltip):

- `Expired visa, registration linked to current visa, checkout app exists, latest checkout state is not PROCESS_ISSUED.`

SQL count query (shown in tooltip):

```sql
SELECT COUNT(*)
FROM BoStateSnapshots
WHERE OwnerType = 'Visa'
  AND IsActive = 1
  AND StateCode = 'Visa|ExpiredOnCheckOutProcess';
```

