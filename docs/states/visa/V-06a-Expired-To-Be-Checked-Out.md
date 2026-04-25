# V-06a Expired (To Be Checked Out)

## 1) State identity (target)

- State ID: `V-06a`
- Caption: `Expired (To Be Checked Out)`
- Internal state code: `ExpiredToBeCheckedOut`
- Replaces part of legacy state: `V-06 Expired`
- Source type: `SQL` (cross-BO)
- Severity: `Warning`
- Click target view: `Visa_ListView`

## 2) Business meaning (target)

Visa has already expired, and no check-out application has been submitted yet.

This is the first action state after expiry: user must submit check-out application.

## 3) Eligibility scope (shared across V-06a..V-06d)

A visa is eligible for the V-06 split states only if all are true:

- `Visa.IsActive = true`
- `Visa.IsCancelled = false`
- `Visa.IsDeleted = false`
- `Visa.ExpirationDate < Today` (strictly less; no grace for entering expired group)

## 4) Cross-BO linkage model (target)

Participants:

- `Visa`
- `Registration`
- `Application`
- `ApplicationType` (`Name = 'App_Reg_Check_Out'`)

Check-out app for an expired visa/person is identified as:

- registrations for same person as the expired visa owner
- where `Registration.Application.ApplicationType.Name = 'App_Reg_Check_Out'`
- use the **latest** such application (latest by application date; tie-breaker by ID desc)

## 5) State condition (target, exact)

`ExpiredToBeCheckedOut` is true when:

1. Visa matches eligibility scope (section 3), and
2. Elapsed working days since `Visa.ExpirationDate` is `<= 3`, and
3. **No** check-out application exists for that person (`latest checkout app is null`)

## 6) Priority and exclusivity (target)

Priority order for V-06 split states:

1. `ExpiredCheckedOut`
2. `ExpiredOnCheckOutProcess`
3. `ExpiredMissedTimelyCheckout`
4. `ExpiredToBeCheckedOut`

V-06a must be evaluated last among split states, and only when higher-priority states do not match.

## 7) Working-day rule (shared for V-06c/V-06d)

Working days:

- Monday through Friday
- Weekends excluded
- Holidays ignored for now

Threshold:

- "within three working days after expiry" means elapsed working days since visa expiry date is `<= 3`
- missed timely checkout means elapsed working days is `> 3`

## 8) Navigation behavior (target)

When user clicks this state row:

- Open `Visa_ListView`
- Apply filter that represents:
  - person has an eligible expired visa (section 3)
  - no latest `App_Reg_Check_Out` application exists

## 9) Implementation notes (target)

- Must be implemented as SQL-backed state (cross-BO joins)
- Count query and click filter must be semantically equivalent
- Latest check-out app selection rule must be identical in count and list filter logic

## 10) Acceptance checks (target)

1. Seed person with eligible expired visa and no check-out app -> appears in V-06a.
2. Move date so elapsed working days since expiry becomes `> 3` -> person leaves V-06a.
3. Ensure same person is present in `Visa_ListView` when V-06a row is clicked.

## 11) Dashboard SQL tooltip content

Human description (shown in tooltip):

- `Expired visa, registration linked to current visa, no checkout app yet, and within 3 working days.`

SQL count query (shown in tooltip):

```sql
SELECT COUNT(*)
FROM BoStateSnapshots
WHERE OwnerType = 'Visa'
  AND IsActive = 1
  AND StateCode = 'Visa|ExpiredToBeCheckedOut';
```

