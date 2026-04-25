# V-01 Active

## 1) State identity

- State ID: `V-01`
- State name: `Active`
- State code: `Active`
- Section: `Visa States`
- Source type: `BO` (queried from `Visa` records, not a SQL status view)
- Severity: `Healthy`
- Status in specification: `Implemented`

## 2) Business meaning

The employee has an active visa record that is not cancelled, not extended, not deleted, and not expired as of today.

## 3) Authoritative implementation logic (current code)

### 3.1 Dashboard count logic

Stored in `Visa2026.Blazor.Server/Components/StateDashboardComponent.razor` inside `LoadData()`:

- Key written: `_counts["Visa|Active"]`
- Query predicate:
  - `v.IsActive`
  - `!v.IsCancelled`
  - `!v.IsExtended`
  - `!v.IsDeleted`
  - `v.ExpirationDate.HasValue`
  - `v.ExpirationDate.Value >= DateTime.Today`

### 3.2 Dashboard row rendering

Stored in `Visa2026.Blazor.Server/Components/StateDashboardComponent.razor`:

- Row label: `Active`
- Source badge: `BO`
- Badge renderer: `@CountBadgeWithSeverity("Visa|Active", "Healthy")`

### 3.3 Click navigation behavior

Stored in `Visa2026.Blazor.Server/Components/StateDashboardComponent.razor`:

- On click, dashboard calls:
  - `NavigateWithVisaFilter("IsActive = true AND IsCancelled = false AND IsExtended = false AND IsDeleted = false AND ExpirationDate >= LocalDateTimeToday()", "Active Visas")`

`NavigateWithVisaFilter(...)` does:

1. Calls `VisaFilterService.SetPending(criteria, caption)`.
2. Navigates to route `/Visa_ListView`.

Filter handoff and application are implemented by:

- `Visa2026.Module/Services/VisaFilterService.cs`
- `Visa2026.Module/Controllers/VisaListViewController.cs`
  - Applies pending criteria to `View.CollectionSource.Criteria["NavFilter"]`
  - Sets view caption to provided caption

## 4) View displayed when state link is clicked

- Target ListView route: `/Visa_ListView`
- Target BO type: `Visa`
- Applied criteria:
  - `IsActive = true AND IsCancelled = false AND IsExtended = false AND IsDeleted = false AND ExpirationDate >= LocalDateTimeToday()`
- Caption after navigation: `Active Visas`

## 5) Data source and retrieval path

This state is retrieved live on dashboard load:

1. `StateDashboardComponent.LoadData()` creates `Visa2026EFCoreDbContext`.
2. Executes LINQ query on `db.Visas`.
3. Stores count in `_counts["Visa|Active"]`.
4. UI reads that key and displays the badge.

No SQL status view is used for this state.

## 6) Implementation contract for future changes

When changing V-01 logic, all of the following must remain aligned:

1. Dashboard count query (`_counts["Visa|Active"]`).
2. Click filter criteria passed to `NavigateWithVisaFilter(...)`.
3. Effective ListView criteria applied by `VisaListViewController`.

If these diverge, dashboard count and opened list will not match (bug).

## 7) Spec alignment note

`docs/STATE_SPECIFICATIONS.md` currently describes V-01 with additional constraints:

- `Visa.IsChanged = false`
- `Visa.ExpirationDate > Today + SystemSettings.DefaultExpiringSoonDays`

Current implemented dashboard logic does **not** include those two conditions.
If specification-driven behavior is required, update implementation and this file together.

## 8) Test data

No dedicated `Test Scenario` is currently recorded for V-01 in `STATE_SPECIFICATIONS.md`.
Any seeded visa satisfying section 3.1 conditions contributes to this count.

## 9) ListView projection (default)

- Target ListView id/route: `Visa_ListView` (`/Visa_ListView`)
- Backing BO: `Visa`

### 9.1 Where column layout is defined

For `Visa_ListView`, no explicit column layout override is currently versioned in:

- `Visa2026.Module/Model.DesignedDiffs.xafml`
- `Visa2026.Blazor.Server/Model.xafml`

So this ListView uses XAF default model generation (plus any user/runtime customizations).

### 9.2 Default properties expected to be shown (auto-generated baseline)

From `Visa` BO metadata (`Visa2026.Module/BusinessObjects/Visa.cs`), the default generated list projection is expected to be based on browsable scalar/reference members such as:

- `VisaNumber`
- `VisaType`
- `VisaCategory`
- `VisaIssuedPlace`
- `IssueDate`
- `StartDate`
- `ExpirationDate`
- `Passport`
- `IssuingApplicationItem`
- `RegistrationState`
- `IsCancelled`
- `IsChanged`
- `IsExtended`
- `ExtensionRequired`

### 9.3 Hidden but filter-relevant fields

These fields participate in V-01 filtering but may be hidden from the visible projection:

- `IsActive` (inherited, used by filter)
- `IsDeleted` (soft-delete guard in filter)
- `ExpirationDate` (visible in many layouts, but still critical to filtering semantics)

### 9.4 Customization caveat

End users/admins can personalize ListView columns at runtime. This document defines the **implementation baseline** (how state is counted and filtered), not a guaranteed per-user UI layout snapshot.

