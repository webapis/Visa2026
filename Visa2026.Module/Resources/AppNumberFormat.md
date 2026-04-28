# Application Number Format

## Overview

Each `Company` can define its own application numbering format via the `AppNumberFormat` property. The format is a template string containing **tokens** that are substituted at the time a new `Application` is saved.

If `AppNumberFormat` is left blank, the system falls back to the legacy behavior: `{AppNumberPrefix}{ApplicationNumber}` (e.g. `TRM-001`).

---

## Tokens

| Token | Description | Example |
|---|---|---|
| `{PREFIX}` | The value of `Company.AppNumberPrefix` | `TRM-` |
| `{YEAR}` | 4-digit year from `ApplicationDate` | `2026` |
| `{YEAR2}` | 2-digit year from `ApplicationDate` | `26` |
| `{MONTH}` | Month number, no padding | `3` |
| `{MONTH2}` | Month number, zero-padded to 2 digits | `03` |
| `{NUMBER}` | Sequential number, zero-padded to `ApplicationNumberPadding` digits | `001` |

---

## Counter Scope

The sequential counter (`{NUMBER}`) resets based on which tokens are present in the format:

| Tokens present | Counter resets per |
|---|---|
| Neither `{YEAR}` nor `{MONTH}` | Prefix only (never resets) |
| `{YEAR}` or `{YEAR2}` | Prefix + Year |
| `{MONTH}` or `{MONTH2}` (with or without `{YEAR}`) | Prefix + Year + Month |

> **Note:** Month-scoped formats **always** implicitly scope by year as well, even if `{YEAR}` is absent from the format string. This ensures the counter resets to `1` at the start of each new year. For example, March 2026 and March 2027 are treated as independent sequences.

The scope is derived automatically — no extra configuration needed.

---

## Examples

| `AppNumberPrefix` | `AppNumberFormat` | `ApplicationNumberPadding` | Result |
|---|---|---|---|
| `TRM-` | *(empty)* | `3` | `TRM-001` |
| `TRM-` | `{PREFIX}{YEAR}-{NUMBER}` | `3` | `TRM-2026-001` |
| *(empty)* | `№{MONTH}/-{NUMBER}` | `3` | `№3/-377` |
| `TRM-` | `{PREFIX}{YEAR2}/{MONTH2}-{NUMBER}` | `3` | `TRM-26/03-001` |
| `ABC-` | `{PREFIX}{YEAR}-{NUMBER}` | `4` | `ABC-2026-0001` |

---

## Mid-Year Deployment — Continuing an Existing Sequence

If the system is deployed while the visa department is already using an external numbering sequence (e.g. a spreadsheet has reached `4/-1150`), set `Company.ApplicationNumberSeed` to the **last used number** (`1150`).

The counter formula is:

```
next = max(dbMax, localMax, seed) + 1
```

| Situation | `dbMax` | `seed` | First generated number |
|---|---|---|---|
| Fresh install, no seed | `0` | `0` | `1` → `0001` |
| Fresh install, seed = 1150 | `0` | `1150` | `1151` |
| DB already has 1155, seed = 1150 | `1155` | `1150` | `1156` (seed ignored) |

Once the live DB surpasses the seed value the seed has **no further effect** — it is a one-time floor, not a reset point.

> **To configure:** open the Company record → set **App Number Seed** to the last number the department used before going live with this system.

---

## Manual Entry — Entering Historical Records

When the visa department needs to back-enter applications that existed **before** the system was deployed, enable the **Manual Entry** flag on the Application record.

### What changes when Manual Entry is on

| Field | Normal mode | Manual Entry mode |
|---|---|---|
| `ApplicationNumber` | Read-only, auto-generated | **Editable** |
| `AppNumberPrefix` | Read-only, copied from Company | **Editable** |
| `ApplicationDate` | Editable (defaults to today) | Editable — set to the original historical date |
| `FullApplicationNumber` | Built from format tokens | Built as `{AppNumberPrefix}{ApplicationNumber}` |
| Auto-generation | Runs if number is empty | **Skipped entirely** |

### `OnSaving()` branching logic

```
IsNewObject?
  ├── Set Year, Month from ApplicationDate          (always)
  ├── Default AppNumberPrefix from Company          (if not already set)
  ├── IsManualEntry = true  ──→  set FullApplicationNumber = prefix + number, RETURN
  └── IsManualEntry = false ──→  normal auto-generation path
```

### Step-by-step for back-entry

1. Create a new Application
2. Check **Manual Entry**
3. Set **Application Date** to the original date (e.g. `15.03.2026`)
4. Set **App Number Prefix** if it differs from the company default
5. Type the **Application Number** (e.g. `1150`)
6. Fill in all other required fields normally
7. Save — `FullApplicationNumber` is set to `{prefix}1150`, no sequence counter is touched

> **Uniqueness:** The DB unique index still applies. If a number already exists for the same prefix + year + month, the save will be rejected. Resolve any duplicates before saving.

---

## Where It Is Implemented

| File | What it does |
|---|---|
| `BusinessObjects/Company.cs` | Defines `AppNumberPrefix`, `ApplicationNumberPadding`, `AppNumberFormat`, `ApplicationNumberSeed` |
| `BusinessObjects/Application.cs` | `OnSaving()` generates or manually accepts `ApplicationNumber` and `FullApplicationNumber`; `BuildFullNumber()` substitutes tokens |
| `BusinessObjects/Visa2026DbContext.cs` | Unique index on `(AppNumberPrefix, ApplicationNumber, Year, Month)` enforces no duplicates |

### Key properties on `Application`

| Property | Role |
|---|---|
| `IsManualEntry` | Flag that unlocks `ApplicationNumber` / `AppNumberPrefix` for editing and bypasses auto-generation |
| `AppNumberPrefix` | Copied from `Company.AppNumberPrefix` at creation; editable in manual entry mode |
| `ApplicationNumber` | The raw sequential number (e.g. `001`); editable in manual entry mode |
| `Year` | Year from `ApplicationDate`, always set from the date — including historical dates |
| `Month` | Month from `ApplicationDate`, always set from the date |
| `FullApplicationNumber` | The fully assembled display number (e.g. `№3/-377`); persisted for OData and reports |

### Key properties on `Company`

| Property | Role |
|---|---|
| `AppNumberPrefix` | Static prefix used as sequence scope key (e.g. `TRM-`) |
| `AppNumberFormat` | Token-based format template; empty = legacy `{prefix}{number}` behavior |
| `ApplicationNumberPadding` | Zero-pad width for `{NUMBER}` (default `4`) |
| `ApplicationNumberSeed` | Floor value for the counter on fresh deployments; first number = `seed + 1` |

---

## DB Unique Index

```
IX_Applications_AppNumberPrefix_ApplicationNumber_Year_Month
  ON dbo.Applications (AppNumberPrefix, ApplicationNumber, Year, Month)
  WHERE IsManualEntry = 0
```

The `WHERE IsManualEntry = 0` filter means the constraint **only applies to auto-generated records**. Manual-entry records are completely exempt — they can hold any number in any format without conflicting with each other or with auto-generated records. For auto-generated records: year-scoped companies never repeat within a year, and month-scoped companies allow the same number in different months because `Month` differs.

> **Note:** The old 3-column index `IX_Applications_AppNumberPrefix_ApplicationNumber_Year` must be dropped manually if upgrading from the pre-Month schema:
> ```sql
> DROP INDEX IF EXISTS [IX_Applications_AppNumberPrefix_ApplicationNumber_Year] ON [dbo].[Applications];
> UPDATE [dbo].[Applications] SET [Month] = MONTH([ApplicationDate]) WHERE [Month] = 0;
> ```
