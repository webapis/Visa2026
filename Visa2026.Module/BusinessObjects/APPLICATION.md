# Business Object: Application

## 1. Purpose

`Application` is the **header** for a collective request to the migration service: one application number, one **application type**, shared lookups (visa period, project contract, urgency, etc.), and **aggregated child data** (people lines, invitations, work permits, progress).

Individual people and their documents are modeled on [`ApplicationItem`](ApplicationItem.md), not as duplicate top-level collections for registration or per-person visas.

---

## 2. Inheritance and interfaces

- Base: `BaseObject`
- `SoftDeleteBaseObject` — soft delete metadata
- `IBoListRowState` — list view row styling from latest progress (see [`docs/APPLICATION_LISTVIEW_STATE_COLORS.md`](../../docs/APPLICATION_LISTVIEW_STATE_COLORS.md))

---

## 3. Properties (header / detail)

| Property | Type | Description | UI / rules |
|----------|------|-------------|------------|
| `IsManualEntry` | `bool` | Allow manual application number / full number entry for legacy data. | When false, number fields read-only on detail. |
| `ApplicationNumber` | `string` | Sequential part of the number. | Auto-assigned on save unless manual entry. Max 50. |
| `AppNumberPrefix` | `string` | Prefix segment (from numbering profile). | Synced from `ApplicationNumberingProfile` when empty. |
| `FullApplicationNumber` | `string` | Display/reference number (e.g. `4/-001`). | Built from format tokens on save. Max 100. |
| `Year` | `int` | From `ApplicationDate.Year`. | Read-only. |
| `Month` | `int` | From `ApplicationDate.Month`. | Read-only. Used when format includes month scope. |
| `ApplicationDate` | `DateTime` | Application date. | Required. Default `DateTime.Now` on create. |
| `ApplicationTypeQuickCode` | `string` | `[NotMapped]` 3-digit ministry code UI. | Resolves to `ApplicationType` via `SelectionCode`. See **`docs/APPLICATION_BO_TYPE_SELECTION_REFACTOR.md`**. |
| `ApplicationType` | `ApplicationType` | Procedure type (invitation, extension, registration, …). | Required. Read-only on detail after selection. `ImmediatePostData`. Drives all `Show*` flags. |
| `CreationProgressRoute` | `ApplicationProgressRouteKind?` | Optional route when creating progress. | |
| `ProjectContract` | `ProjectContract` | Construction project / contract. | `ShowProjectContract` |
| `Urgency` | `Urgency` | Processing priority. | `ShowUrgency` |
| `VisaPeriod` | `VisaPeriod` | Requested visa duration. | `ShowVisaPeriod` |
| `VisaCategory` | `VisaCategory` | Visa category. | `ShowVisaCategory` |
| `VisaType` | `VisaType` | Visa type. | `ShowVisaType` |
| `MigrationService` | `MigrationService` | Target office. | `ShowMigrationService` |
| `BusinessTripStartDate` | `DateTime?` | Business-trip application dates. | `ShowBusinessTrips` |
| `BusinessTripEndDate` | `DateTime?` | | `ShowBusinessTrips` |
| `BusinessTripPurpose` | `BusinessTripPurpose` | | `ShowBusinessTrips` |
| `MovementPermitLocation` | `MovementPermitLocation` | Movement permit lookup. | `ShowMovementPermitLocation` |
| `BorderZoneLocation` | `BorderZoneLocation` | **Application-level** border zone FK (lookup catalog). | `ShowBorderZoneLocation`. Not the same as comma-separated `ApplicationItem.BorderZoneLocation`. |
| `FromCity` | `City` | | `ShowFromCity` |
| `ToCity` | `City` | | `ShowToCity` |

**Removed / not on `Application` today:** persisted `Company`, `CompanyHead`, `Representative`, `ApplicationReason`, `ExpirationDate`, `CurrentState` FK. Company letterhead uses **singletons** (`CompanyProfile`, signatory/representative) via `[NotMapped]` aliases (`Application_Company_*`, `Application_CompanyHead_*`). See [`docs/DEPRECATED.md`](../../docs/DEPRECATED.md).

**Report aliases:** many `[NotMapped]` Word/PDF fields (`Application_Company_Name`, `VisaPeriod_NameTm`, `FamilyMember_Relationship_NameTm`, …) — see `Application.cs` and `docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`.

---

## 4. Collections

| Collection | Item type | Aggregation | Visibility |
|------------|-----------|-------------|------------|
| `ApplicationItems` | `ApplicationItem` | Aggregated | `ShowApplicationItems` |
| `Invitations` | `Invitation` | Aggregated | `ShowInvitations` |
| `Rejections` | `Rejection` | Aggregated | `ShowRejections` |
| `WorkPermits` | `WorkPermit` | Aggregated | `ShowWorkPermits` |
| `ProgressHistory` | `ApplicationProgress` | Aggregated | Always (progress workflow) |

**Not on `Application`:** `Registrations`, `BusinessTrips`, `Visas`. Registration travel/check-in data is on **`ApplicationItem`**. **`Visa`** records link via `ApplicationItem` / `IssuingApplicationItem` (see **`Visa.md`**).

---

## 5. Application numbering

On **first save** (`OnSaving`, new object):

1. `Year` / `Month` from `ApplicationDate`.
2. Prefix and format from **`ApplicationNumberingProfile`** (`OrganizationReportHelper.GetApplicationNumbering`) — tenant seed: `DatabaseUpdate/LookupCatalogs/tenant/application-numbering.json`.
3. Next `ApplicationNumber` scoped by prefix and, depending on format tokens, year and/or month (see [`Resources/AppNumberFormat.md`](../Resources/AppNumberFormat.md)).
4. `FullApplicationNumber` = `BuildFullNumber(format, prefix, year, month, number)` (e.g. `{MONTH}/-{NUMBER}` → `4/-001`).

**Manual entry:** when `IsManualEntry` is true, user can set `ApplicationNumber` or `FullApplicationNumber` directly.

Importer and child sheets should reference **`FullApplicationNumber`**, not legacy `TRM-2026-…` patterns.

---

## 6. Business rules and logic

### Application type selection

- Persisted type: **`ApplicationType`** only (category = `ApplicationType.Category`: Employee / FamilyMember / Both).
- UI quick code: **`ApplicationTypeQuickCode`** → `ApplicationTypeSelectionController` resolves `SelectionCode`.
- Types with empty `SelectionCode` are excluded from the dropdown.
- Changing type refreshes conditional UI on the application and nested items.

### Initial progress

[`ApplicationProgressInitializer`](ApplicationProgressInitializer.cs) adds the first `ApplicationProgress` row (`IS_BEING_PREPARED` @ `AT_OFFICE`, date = `ApplicationDate`) when the application is created, unless history already exists.

### Progress / “current state”

- **No** persisted `CurrentState` on `Application` (legacy column removed).
- List row color and “current state” semantics come from **latest `ApplicationProgress`** in `ProgressHistory` — [`ApplicationProgressHelper`](ApplicationProgressHelper.cs), [`docs/APPLICATION_LISTVIEW_STATE_COLORS.md`](../../docs/APPLICATION_LISTVIEW_STATE_COLORS.md).

### Visa issuance

Visas are **not** a child collection on `Application`. Trace issuance through **`ApplicationItem`** and **`Visa.IssuingApplicationItem`**.

### Type-specific defaults

`Application` applies default visa period/category/type for certain types (e.g. `App_Inv`, `App_Inv_And_WP`) when the type is set — see constants at top of `Application.cs`.

### Conditional UI

Header fields and collections use `[Appearance]` with `ApplicationType.Show*`. Nested **`ApplicationItem`** fields use **separate** `Show*` flags on `ApplicationType` (document links, registration columns, work permitted locations, etc.) — see [`ApplicationItem.md`](ApplicationItem.md).

### PDF / dynamic forms

`PdfMappingHelper` gates mappings by `ApplicationType.Show*` and by paths on `ApplicationItem` (including legacy token names `CurrentRegistration.*` mapped to flattened item fields).

---

## 7. UI and behavior

- **Navigation:** `NavigationItem(false)` — opened from application lists / workflows, not a top-level menu node.
- **Default property:** `ApplicationNumber`.
- **Detail:** Application type code + read-only type name; nested tabs/list for items, invitations, work permits, progress.
- **`OnCreated`:** sets `ApplicationDate`, default lookups (urgency, visa type/category/period, default project contract), initial progress.

---

## 8. Related docs

| Topic | Location |
|-------|----------|
| Line items / registration on item | [`ApplicationItem.md`](ApplicationItem.md) |
| Application type catalog | `DatabaseUpdate/LookupCatalogs/ApplicationTypeConfigurationCatalog.json` |
| Number format tokens | [`Resources/AppNumberFormat.md`](../Resources/AppNumberFormat.md) |
| Type quick code UI | [`docs/APPLICATION_BO_TYPE_SELECTION_REFACTOR.md`](../../docs/APPLICATION_BO_TYPE_SELECTION_REFACTOR.md) |
| Legacy removals | [`docs/DEPRECATED.md`](../../docs/DEPRECATED.md) |
| Environments / DB update | [`docs/ENVIRONMENTS.md`](../../docs/ENVIRONMENTS.md) |
