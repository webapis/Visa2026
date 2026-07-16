# Report Dashboard — Implementation Plan

Living tracker for promoting all dashboard categories from mock data to real SQL views.
Update the **Status** column and append to `learnings.md` as each view ships.

---

## Status tracker

| View | Category | Sub-reports served | Phase | Status |
|------|----------|--------------------|-------|--------|
| `vw_rd_application` | Application | **by-progress**, **by-type** (live) | 1 | EF Wired |
| `vw_rd_passport` | Passport | by-type, by-citizenship, **by-validity** (live) | 1 | EF Wired (by-validity only) |
| `vw_rd_registration` | Registration | default | 1 | Planned |
| `vw_rd_work_permit` | WorkPermit | **by-validity** (live), by-status | 1 | EF Wired (by-validity) |
| `vw_rd_border_zone` | BorderZone | default | 1 | Planned |
| `vw_rd_travel` | Travel | default | 2 | Planned |
| `vw_rd_invitation_issued` | Invitation | issued-inv | 2 | Planned |
| `vw_rd_visa_state` | Visa | visa-state | 1 | EF Wired (Extension Started) |`n| `vw_rd_visa_by_category` | Visa | by-category | 1 | EF Wired |`n| `vw_rd_visa_by_type` | Visa | by-type | 1 | EF Wired |
| `vw_rd_app_progress` | Visa + Invitation | app-progress (both) | 3 | Planned |
| `vw_rd_education` | Education | by-level, by-country, by-specialty | 1 | EF Wired |
| `vw_rd_position_history` | PositionHistory | by-status, by-position | 1 | EF Wired |
| `vw_rd_snapshot_counts` | All (snapshot) | LoadSnapshot counts | 4 | Cancelled (sidebar totals removed) |

**Status values:** `Planned` → `In Progress` → `View Created` → `EF Wired` → `Done`

---

## Implementation phases

- **Phase 1 — Simple** (single header table, no item join): `vw_rd_passport`, `vw_rd_registration`, `vw_rd_work_permit`, `vw_rd_border_zone`
- **Phase 2 — Moderate** (header + line-item join): `vw_rd_travel`, `vw_rd_invitation_issued`
- **Phase 3 — Complex** (reuse existing view, or latest-progress join): `vw_rd_visa_state`, `vw_rd_app_progress`
- **Phase 4 — Snapshot**: `vw_rd_snapshot_counts` — replace inline EF counts in `LoadSnapshot()`

---

## Common conventions

### Soft-delete filter (every table)
```sql
ISNULL(t.GCRecord, 0) = 0
```

### Person and project join pattern (every view)
```sql
-- Base join (employees and temporary visitors)
JOIN  People           p   ON p.ID = <document_table>.PersonID
                           AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc ON pc.ID = p.ProjectContractID

-- Family members inherit contract from sponsoring employee
-- If p.PersonRole = 'FamilyMember', sponsor's contract is used:
LEFT JOIN People        sp  ON sp.ID = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID

-- Use COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '') AS ProjectName
```

### Standard output columns (every view must include all of these)
```sql
p.ID                                                       AS PersonOid,
LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))         AS PersonName,
COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')    AS ProjectName,
p.PersonRole                                               AS PersonRoleCode,
<doc_id_expr>                                              AS ColumnA,
FORMAT(<date_expr>, 'MMM dd, yyyy')                        AS ColumnB,
<subreport_case>                                           AS SubReportKey,
<status_label_case>                                        AS StatusLabel,
<status_css_case>                                          AS StatusCssClass,
<cutoff_date_expr>                                         AS RecordDate
```

### Standard validity bucket helpers (copy-paste)
```sql
-- StatusLabel for expiry-based categories
CASE
  WHEN <ExpiryDate> IS NULL                              THEN 'Pending'
  WHEN <ExpiryDate>  < GETDATE()                         THEN 'Expired'
  WHEN <ExpiryDate> <= DATEADD(day,  15, GETDATE())      THEN 'Expiring (<15 days)'
  WHEN <ExpiryDate> <= DATEADD(day,  30, GETDATE())      THEN 'Expiring (<30 days)'
  WHEN <ExpiryDate> <= DATEADD(day,  60, GETDATE())      THEN 'Expiring (<60 days)'
  WHEN <ExpiryDate> <= DATEADD(day,  90, GETDATE())      THEN 'Expiring (<90 days)'
  ELSE 'Valid'
END AS StatusLabel,

-- StatusCssClass for expiry-based categories
CASE
  WHEN <ExpiryDate> IS NULL                              THEN 'st-pending'
  WHEN <ExpiryDate>  < GETDATE()                         THEN 'st-expiring'
  WHEN <ExpiryDate> <= DATEADD(day,  30, GETDATE())      THEN 'st-expiring'
  WHEN <ExpiryDate> <= DATEADD(day,  90, GETDATE())      THEN 'st-pending'
  ELSE 'st-approved'
END AS StatusCssClass
```

---

## Phase 1 — Simple views

---

### vw_rd_passport

**Category:** Passport — Sub-reports: `by-type`, `by-citizenship`, `by-validity`  

**Universe (2026-07-16):** one row per `ApplicationItem` with `CurrentPassport`; filter `Applications.ApplicationDate` via dashboard date range (not latest passport per person).

**Tables:**
- `Passports` (main, GCRecord filter)
- `People` (join on `Passports.PersonID`)
- `ProjectContracts` (via `People.ProjectContractID`)
- `PassportTypes` (join on `Passports.PassportTypeID` — lookup name)
- `Countries` (join on `Passports.IssuedCountryID` — citizenship)

**Multi-label design:** Because `StatusLabel` must be different for each sub-report (`by-type` → type name, `by-citizenship` → country name, `by-validity` → expiry bucket), the view exposes **three label columns**. The C# loader picks the correct one based on `subReport`.

```sql
CREATE OR ALTER VIEW vw_rd_passport AS
SELECT
    p.ID                                                            AS PersonOid,
    LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))              AS PersonName,
    COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')         AS ProjectName,
    p.PersonRole                                                    AS PersonRoleCode,
    pp.PassportNumber                                               AS ColumnA,
    FORMAT(pp.ExpirationDate, 'MMM dd, yyyy')                       AS ColumnB,
    -- Three status labels, one per sub-report
    COALESCE(pt.Name, 'Unknown')                                    AS TypeLabel,
    COALESCE(c.Name, 'Unknown')                                     AS CitizenshipLabel,
    CASE
      WHEN pp.ExpirationDate IS NULL                                THEN 'Pending'
      WHEN pp.ExpirationDate  < GETDATE()                           THEN 'Expired'
      WHEN pp.ExpirationDate <= DATEADD(day,  30, GETDATE())        THEN 'Expiring (<30 days)'
      WHEN pp.ExpirationDate <= DATEADD(day,  60, GETDATE())        THEN 'Valid (<60 days)'
      WHEN pp.ExpirationDate <= DATEADD(day,  90, GETDATE())        THEN 'Valid (<90 days)'
      ELSE                                                           'Valid (>90 days)'
    END                                                             AS ValidityLabel,
    -- CSS for by-validity (by-type and by-citizenship use st-cat-1..5 — assigned in C#)
    CASE
      WHEN pp.ExpirationDate IS NULL                                THEN 'st-pending'
      WHEN pp.ExpirationDate  < GETDATE()                           THEN 'st-expiring'
      WHEN pp.ExpirationDate <= DATEADD(day,  30, GETDATE())        THEN 'st-expiring'
      WHEN pp.ExpirationDate <= DATEADD(day,  90, GETDATE())        THEN 'st-pending'
      ELSE                                                           'st-approved'
    END                                                             AS ValidityCssClass,
    pp.ExpirationDate                                               AS RecordDate
FROM Passports pp
JOIN  People           p   ON p.ID  = pp.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc  ON pc.ID = p.ProjectContractID
LEFT JOIN People          sp  ON sp.ID  = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID
LEFT JOIN PassportTypes   pt  ON pt.ID  = pp.PassportTypeID
LEFT JOIN Countries        c  ON c.ID   = pp.IssuedCountryID
WHERE ISNULL(pp.GCRecord, 0) = 0
  AND ISNULL(pp.IsCancelled, 0) = 0;
```

**EF entity:** `VwRdPassport` with all columns above plus `TypeLabel`, `CitizenshipLabel`, `ValidityLabel`, `ValidityCssClass`.

**C# wiring:** In `LoadPassport()`, switch on `subReport`:
- `by-type` → `StatusLabel = r.TypeLabel`, `StatusCssClass` = assign `st-cat-1..5` by index
- `by-citizenship` → `StatusLabel = r.CitizenshipLabel`, css by index
- `by-validity` → `StatusLabel = r.ValidityLabel`, `StatusCssClass = r.ValidityCssClass`

---

### vw_rd_registration

**Category:** Registration — Sub-report: `default`

**Tables:**
- `AddressesOfResidence` (GCRecord filter; include only rows with `ExpirationDate IS NOT NULL` for meaningful expiry reporting)
- `People`
- `ProjectContracts`

```sql
CREATE OR ALTER VIEW vw_rd_registration AS
SELECT
    p.ID                                                            AS PersonOid,
    LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))              AS PersonName,
    COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')         AS ProjectName,
    p.PersonRole                                                    AS PersonRoleCode,
    COALESCE(a.FullAddress, CAST(a.Type AS nvarchar(50)), '')        AS ColumnA,
    FORMAT(a.ExpirationDate, 'MMM dd, yyyy')                        AS ColumnB,
    'default'                                                       AS SubReportKey,
    CASE
      WHEN a.ExpirationDate IS NULL                                 THEN 'No Expiry'
      WHEN a.ExpirationDate  < GETDATE()                            THEN 'Expired'
      WHEN a.ExpirationDate <= DATEADD(day,  30, GETDATE())         THEN 'Expiring (<30 days)'
      WHEN a.ExpirationDate <= DATEADD(day,  90, GETDATE())         THEN 'Expiring Soon'
      ELSE                                                           'Active'
    END                                                             AS StatusLabel,
    CASE
      WHEN a.ExpirationDate IS NULL                                 THEN 'st-approved'
      WHEN a.ExpirationDate  < GETDATE()                            THEN 'st-expiring'
      WHEN a.ExpirationDate <= DATEADD(day,  30, GETDATE())         THEN 'st-expiring'
      WHEN a.ExpirationDate <= DATEADD(day,  90, GETDATE())         THEN 'st-pending'
      ELSE                                                           'st-approved'
    END                                                             AS StatusCssClass,
    COALESCE(a.ExpirationDate, GETDATE())                           AS RecordDate
FROM AddressesOfResidence a
JOIN  People           p   ON p.ID  = a.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc  ON pc.ID = p.ProjectContractID
LEFT JOIN People          sp  ON sp.ID  = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID
WHERE ISNULL(a.GCRecord, 0) = 0;
```

---

### vw_rd_work_permit

**Category:** WorkPermit — Sub-report: `default`

**Tables:**
- `WorkPermitItems` (line item with `WorkPermitNumber`, `ExpirationDate`, `IsCancelled`)
- `People`
- `ProjectContracts`

```sql
CREATE OR ALTER VIEW vw_rd_work_permit AS
SELECT
    p.ID                                                            AS PersonOid,
    LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))              AS PersonName,
    COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')         AS ProjectName,
    p.PersonRole                                                    AS PersonRoleCode,
    COALESCE(wpi.WorkPermitNumber, wpi.ASNumber, '')                AS ColumnA,
    FORMAT(wpi.ExpirationDate, 'MMM dd, yyyy')                      AS ColumnB,
    'default'                                                       AS SubReportKey,
    CASE
      WHEN wpi.ExpirationDate IS NULL                               THEN 'Pending'
      WHEN wpi.ExpirationDate  < GETDATE()                          THEN 'Expired'
      WHEN wpi.ExpirationDate <= DATEADD(day,  30, GETDATE())       THEN 'Expiring (<30 days)'
      WHEN wpi.ExpirationDate <= DATEADD(day,  90, GETDATE())       THEN 'Expiring Soon'
      ELSE                                                           'Active'
    END                                                             AS StatusLabel,
    CASE
      WHEN wpi.ExpirationDate IS NULL                               THEN 'st-pending'
      WHEN wpi.ExpirationDate  < GETDATE()                          THEN 'st-expiring'
      WHEN wpi.ExpirationDate <= DATEADD(day,  30, GETDATE())       THEN 'st-expiring'
      WHEN wpi.ExpirationDate <= DATEADD(day,  90, GETDATE())       THEN 'st-pending'
      ELSE                                                           'st-approved'
    END                                                             AS StatusCssClass,
    COALESCE(wpi.ExpirationDate, wpi.StartDate, GETDATE())          AS RecordDate
FROM WorkPermitItems wpi
JOIN  People           p   ON p.ID  = wpi.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc  ON pc.ID = p.ProjectContractID
LEFT JOIN People          sp  ON sp.ID  = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID
WHERE ISNULL(wpi.GCRecord, 0) = 0
  AND ISNULL(wpi.IsCancelled, 0) = 0;
```

---

### vw_rd_border_zone

**Category:** BorderZone — Sub-report: `default`

**Tables:**
- `BorderZoneItems` (line item — `IsCancelled`, FK to `BorderZoneID` and `PersonID`)
- `BorderZones` (header — `BorderZoneNumber`, `ExpirationDate`, `IsCancelled`)
- `People`
- `ProjectContracts`

```sql
CREATE OR ALTER VIEW vw_rd_border_zone AS
SELECT
    p.ID                                                            AS PersonOid,
    LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))              AS PersonName,
    COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')         AS ProjectName,
    p.PersonRole                                                    AS PersonRoleCode,
    COALESCE(bz.BorderZoneNumber, '')                               AS ColumnA,
    FORMAT(bz.ExpirationDate, 'MMM dd, yyyy')                       AS ColumnB,
    'default'                                                       AS SubReportKey,
    CASE
      WHEN bz.ExpirationDate IS NULL                                THEN 'Pending'
      WHEN bz.ExpirationDate  < GETDATE()                           THEN 'Expired'
      WHEN bz.ExpirationDate <= DATEADD(day,  15, GETDATE())        THEN 'Expiring (<15 days)'
      WHEN bz.ExpirationDate <= DATEADD(day,  30, GETDATE())        THEN 'Expiring (<30 days)'
      WHEN bz.ExpirationDate <= DATEADD(day,  90, GETDATE())        THEN 'Expiring Soon'
      ELSE                                                           'Active'
    END                                                             AS StatusLabel,
    CASE
      WHEN bz.ExpirationDate IS NULL                                THEN 'st-pending'
      WHEN bz.ExpirationDate  < GETDATE()                           THEN 'st-expiring'
      WHEN bz.ExpirationDate <= DATEADD(day,  15, GETDATE())        THEN 'st-expiring'
      WHEN bz.ExpirationDate <= DATEADD(day,  90, GETDATE())        THEN 'st-pending'
      ELSE                                                           'st-approved'
    END                                                             AS StatusCssClass,
    COALESCE(bz.ExpirationDate, bz.StartDate, GETDATE())            AS RecordDate
FROM BorderZoneItems bzi
JOIN  BorderZones      bz  ON bz.ID  = bzi.BorderZoneID AND ISNULL(bz.GCRecord, 0) = 0
JOIN  People           p   ON p.ID   = bzi.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc  ON pc.ID = p.ProjectContractID
LEFT JOIN People          sp  ON sp.ID  = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID
WHERE ISNULL(bzi.GCRecord, 0) = 0
  AND ISNULL(bzi.IsCancelled, 0) = 0
  AND ISNULL(bz.IsCancelled, 0) = 0;
```

---

## Phase 2 — Moderate views

---

### vw_rd_travel

**Category:** Travel — Sub-report: `default`

**Tables:**
- `ApplicationItems` (has `TravelDate`, `TravelType`, `PersonID`, `ApplicationID`)
- `Applications` (has `ApplicationType`, `ProjectContractID`, `ApplicationDate`, `ApplicationNumber`)
- `ApplicationTypes` (filter to travel-type applications)
- `People`
- `ProjectContracts`

**Note:** `TravelType` on `ApplicationItem` is a string/enum indicating entry/exit/transit. Use `ApplicationDate` from `Application` as `RecordDate`.

```sql
CREATE OR ALTER VIEW vw_rd_travel AS
SELECT
    p.ID                                                            AS PersonOid,
    LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))              AS PersonName,
    COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')         AS ProjectName,
    p.PersonRole                                                    AS PersonRoleCode,
    COALESCE(app.ApplicationNumber, app.FullApplicationNumber, '')   AS ColumnA,
    FORMAT(ai.TravelDate, 'MMM dd, yyyy')                           AS ColumnB,
    'default'                                                       AS SubReportKey,
    COALESCE(CAST(ai.TravelType AS nvarchar(50)), 'Unknown')         AS StatusLabel,
    'st-cat-1'                                                      AS StatusCssClass,
    COALESCE(ai.TravelDate, app.ApplicationDate, GETDATE())         AS RecordDate
FROM ApplicationItems ai
JOIN  Applications     app ON app.ID = ai.ApplicationID AND ISNULL(app.GCRecord, 0) = 0
JOIN  ApplicationTypes at  ON at.ID  = app.ApplicationTypeID
JOIN  People           p   ON p.ID   = ai.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc  ON pc.ID = COALESCE(app.ProjectContractID, p.ProjectContractID)
LEFT JOIN People          sp  ON sp.ID  = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID
WHERE ISNULL(ai.GCRecord, 0) = 0
  AND at.Name LIKE '%Travel%'       -- adjust filter to actual ApplicationType name for travel
  AND ai.TravelDate IS NOT NULL;
```

**Note for implementer:** Verify the exact `ApplicationType.Name` value used for travel entries (check `ApplicationTypes` table; likely `App_Travel` or similar).

---

### vw_rd_invitation_issued

**Category:** Invitation — Sub-report: `issued-inv`

**Tables:**
- `InvitationItems` (line item — `IsUsed`, `IsCancelled`, `IsChanged`, FK to `InvitationID`, `PersonID`)
- `Invitations` (header — `InvitationNumber`, `StartDate`, `ExpirationDate`, `IsCancelled`)
- `People`
- `ProjectContracts`

```sql
CREATE OR ALTER VIEW vw_rd_invitation_issued AS
SELECT
    p.ID                                                            AS PersonOid,
    LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))              AS PersonName,
    COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')         AS ProjectName,
    p.PersonRole                                                    AS PersonRoleCode,
    COALESCE(inv.InvitationNumber, '')                              AS ColumnA,
    FORMAT(inv.ExpirationDate, 'MMM dd, yyyy')                      AS ColumnB,
    'issued-inv'                                                    AS SubReportKey,
    CASE
      WHEN ii.IsUsed = 1                                            THEN 'Used'
      WHEN inv.ExpirationDate IS NULL                               THEN 'Pending'
      WHEN inv.ExpirationDate  < GETDATE()                          THEN 'Expired'
      WHEN inv.ExpirationDate <= DATEADD(day,  15, GETDATE())       THEN 'Valid (<15 days)'
      WHEN inv.ExpirationDate <= DATEADD(day,  30, GETDATE())       THEN 'Valid (<30 days)'
      WHEN inv.ExpirationDate <= DATEADD(day,  60, GETDATE())       THEN 'Valid (<60 days)'
      WHEN inv.ExpirationDate <= DATEADD(day,  90, GETDATE())       THEN 'Valid (<90 days)'
      ELSE                                                           'Valid'
    END                                                             AS StatusLabel,
    CASE
      WHEN ii.IsUsed = 1                                            THEN 'st-approved'
      WHEN inv.ExpirationDate IS NULL                               THEN 'st-pending'
      WHEN inv.ExpirationDate  < GETDATE()                          THEN 'st-expiring'
      WHEN inv.ExpirationDate <= DATEADD(day,  30, GETDATE())       THEN 'st-expiring'
      WHEN inv.ExpirationDate <= DATEADD(day,  90, GETDATE())       THEN 'st-pending'
      ELSE                                                           'st-approved'
    END                                                             AS StatusCssClass,
    COALESCE(inv.ExpirationDate, inv.StartDate, GETDATE())          AS RecordDate
FROM InvitationItems ii
JOIN  Invitations      inv ON inv.ID = ii.InvitationID AND ISNULL(inv.GCRecord, 0) = 0
JOIN  People           p   ON p.ID   = ii.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc  ON pc.ID = p.ProjectContractID
LEFT JOIN People          sp  ON sp.ID  = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID
WHERE ISNULL(ii.GCRecord, 0) = 0
  AND ISNULL(ii.IsCancelled, 0) = 0
  AND ISNULL(inv.IsCancelled, 0) = 0;
```

---

## Phase 3 — Complex views

---

### vw_rd_visa_state

**Category:** Visa — Sub-reports: `visa-state`, `by-category`, `by-period`

**Strategy:** Wrap the existing `View_VisaExtensionStatus` (already maintained by the app) rather than duplicating its join logic.

**Key columns available on `View_VisaExtensionStatus`:**
- `ID` (= ApplicationItem.ID), `PersonID`, `ApplicationID`
- `ApplicationNumber`, `ApplicationDate`, `StatusDescription`, `StatusDate`
- `DaysRemainingOnVisa` (int, computed)
- Navigation: `CurrentState` (`ApplicationState` — the progress state name), `ExpiringVisa` (current visa FK)

**Multi-label design:** Three sub-reports need different `StatusLabel` groupings:

| Sub-report | StatusLabel source |
|------------|-------------------|
| `visa-state` | `StatusDescription` (from View_VisaExtensionStatus) |
| `by-category` | `VisaCategory.Name` (join `Visas` → `VisaCategories`) |
| `by-period` | CASE on `DaysRemainingOnVisa` |

```sql
CREATE OR ALTER VIEW vw_rd_visa_state AS
SELECT
    p.ID                                                            AS PersonOid,
    LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))              AS PersonName,
    COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')         AS ProjectName,
    p.PersonRole                                                    AS PersonRoleCode,
    COALESCE(ves.ApplicationNumber, '')                             AS ColumnA,
    FORMAT(ves.ApplicationDate, 'MMM dd, yyyy')                     AS ColumnB,
    -- Three label columns (C# picks one based on subReport param)
    COALESCE(ves.StatusDescription, 'Unknown')                      AS StateLabel,
    COALESCE(vc.Name, 'Unknown')                                    AS CategoryLabel,
    CASE
      WHEN ves.DaysRemainingOnVisa IS NULL OR ves.DaysRemainingOnVisa < 0 THEN 'Expired'
      WHEN ves.DaysRemainingOnVisa < 10                             THEN '<10 days'
      WHEN ves.DaysRemainingOnVisa < 30                             THEN '<1 month'
      WHEN ves.DaysRemainingOnVisa < 90                             THEN '<3 months'
      WHEN ves.DaysRemainingOnVisa < 120                            THEN '<4 months'
      WHEN ves.DaysRemainingOnVisa < 150                            THEN '<5 months'
      ELSE                                                           '<6 months'
    END                                                             AS PeriodLabel,
    -- CSS (state-based)
    CASE
      WHEN ves.StatusDescription LIKE '%Cancel%'
        OR ves.StatusDescription LIKE '%Reject%'                    THEN 'st-expiring'
      WHEN ves.StatusDescription LIKE '%Not Required%'              THEN 'st-approved'
      WHEN ves.StatusDescription LIKE '%Started%'                   THEN 'st-pending'
      ELSE                                                           'st-pending'
    END                                                             AS StateCssClass,
    COALESCE(ves.ApplicationDate, GETDATE())                        AS RecordDate
FROM View_VisaExtensionStatus ves
JOIN  People           p   ON p.ID  = ves.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc  ON pc.ID = p.ProjectContractID
LEFT JOIN People          sp  ON sp.ID  = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID
LEFT JOIN Visas            v   ON v.ID   = ves.ExpiringVisaID
LEFT JOIN VisaCategories   vc  ON vc.ID  = v.VisaCategoryID;
```

**C# wiring in `LoadVisaExtension()`:**
- `visa-state` → `StatusLabel = r.StateLabel`, `StatusCssClass = r.StateCssClass`
- `by-category` → `StatusLabel = r.CategoryLabel`, css = assign by index
- `by-period` → `StatusLabel = r.PeriodLabel`, css = period-based CASE in C#

**Note:** Confirm `VisaCategories` table name and FK column (`v.VisaCategoryID`) against actual schema.

---

### vw_rd_app_progress

**Category:** Visa + Invitation — Sub-report: `app-progress` (both categories share this view)

**Strategy:** One view filtered by `ApplicationType`. A `CategoryKey` column (`visa` / `invitation`) lets the C# service filter by category.

**Tables:**
- `ApplicationItems` → `Applications` → `ApplicationTypes`
- Latest `ApplicationProgress` row per application (ROW_NUMBER / OUTER APPLY)
- `ApplicationStates` (lookup for state name)
- `People`, `ProjectContracts`

```sql
CREATE OR ALTER VIEW vw_rd_app_progress AS
WITH LatestProgress AS (
    SELECT
        ap.ApplicationID,
        ap.ApplicationStateID,
        ap.Date AS ProgressDate,
        ROW_NUMBER() OVER (PARTITION BY ap.ApplicationID ORDER BY ap.Date DESC, ap.ID DESC) AS rn
    FROM ApplicationProgress ap
    WHERE ISNULL(ap.GCRecord, 0) = 0
)
SELECT
    p.ID                                                            AS PersonOid,
    LTRIM(RTRIM(CONCAT(p.FirstName, ' ', p.LastName)))              AS PersonName,
    COALESCE(pc.Name, pc.NameTm, spc.Name, spc.NameTm, '')         AS ProjectName,
    p.PersonRole                                                    AS PersonRoleCode,
    COALESCE(app.ApplicationNumber, app.FullApplicationNumber, '')   AS ColumnA,
    FORMAT(app.ApplicationDate, 'MMM dd, yyyy')                     AS ColumnB,
    -- CategoryKey drives which dashboard category this row belongs to
    CASE
      WHEN at.Name LIKE '%Inv%'    THEN 'invitation'
      ELSE                              'visa'
    END                                                             AS CategoryKey,
    'app-progress'                                                  AS SubReportKey,
    COALESCE(ast.Name, 'Being Prepared')                            AS StatusLabel,
    CASE
      WHEN ast.Name LIKE '%Approv%' OR ast.Name LIKE '%Issue%'      THEN 'st-approved'
      WHEN ast.Name LIKE '%Cancel%' OR ast.Name LIKE '%Reject%'     THEN 'st-expiring'
      ELSE                                                           'st-pending'
    END                                                             AS StatusCssClass,
    COALESCE(app.ApplicationDate, GETDATE())                        AS RecordDate
FROM ApplicationItems ai
JOIN  Applications     app ON app.ID  = ai.ApplicationID AND ISNULL(app.GCRecord, 0) = 0
JOIN  ApplicationTypes at  ON at.ID   = app.ApplicationTypeID
JOIN  People           p   ON p.ID    = ai.PersonID AND ISNULL(p.GCRecord, 0) = 0
LEFT JOIN ProjectContracts pc  ON pc.ID  = COALESCE(app.ProjectContractID, p.ProjectContractID)
LEFT JOIN People          sp  ON sp.ID   = p.SponsoringEmployeeID
LEFT JOIN ProjectContracts spc ON spc.ID = sp.ProjectContractID
LEFT JOIN LatestProgress   lp  ON lp.ApplicationID = ai.ApplicationID AND lp.rn = 1
LEFT JOIN ApplicationStates ast ON ast.ID = lp.ApplicationStateID
WHERE ISNULL(ai.GCRecord, 0) = 0
  AND (at.Name LIKE '%Visa%' OR at.Name LIKE '%Inv%');
      -- Adjust to actual ApplicationType name values in the database
```

**C# wiring:**
- `LoadVisaExtension(..., "app-progress")` → filter `.Where(r => r.CategoryKey == "visa")`
- `LoadInvitation(..., "app-progress")` → filter `.Where(r => r.CategoryKey == "invitation")`

**Note:** Confirm `ApplicationProgress` table name, `ApplicationStateID` FK column, and `ApplicationStates` lookup table name.

---

## Phase 4 — Snapshot counts

### vw_rd_snapshot_counts

Replace the per-category EF queries in `ReportDashboardQueryService.LoadSnapshot()` with a single view that returns one row per `(PersonRole, CategoryKey)` with a count.

```sql
CREATE OR ALTER VIEW vw_rd_snapshot_counts AS
SELECT PersonRoleCode, 'Passport'     AS CategoryKey, COUNT(*) AS TotalCount FROM vw_rd_passport    GROUP BY PersonRoleCode
UNION ALL
SELECT PersonRoleCode, 'Registration' AS CategoryKey, COUNT(*) AS TotalCount FROM vw_rd_registration GROUP BY PersonRoleCode
UNION ALL
SELECT PersonRoleCode, 'WorkPermit'   AS CategoryKey, COUNT(*) AS TotalCount FROM vw_rd_work_permit  GROUP BY PersonRoleCode
UNION ALL
SELECT PersonRoleCode, 'BorderZone'   AS CategoryKey, COUNT(*) AS TotalCount FROM vw_rd_border_zone  GROUP BY PersonRoleCode
UNION ALL
SELECT PersonRoleCode, 'Travel'       AS CategoryKey, COUNT(*) AS TotalCount FROM vw_rd_travel       GROUP BY PersonRoleCode
UNION ALL
SELECT PersonRoleCode, 'Invitation'   AS CategoryKey, COUNT(*) AS TotalCount FROM vw_rd_invitation_issued GROUP BY PersonRoleCode
UNION ALL
SELECT PersonRoleCode, 'Visa'         AS CategoryKey, COUNT(*) AS TotalCount FROM vw_rd_visa_state   GROUP BY PersonRoleCode;
```

**Note:** Create this view last, after all source views are deployed.

---

## EF entity checklist

For each view, create a keyless entity class and register it. Files go in `Visa2026.Module/BusinessObjects/ReportDashboard/` (or a new `SqlViews/` folder):

| Class name | View name | DbSet property |
|------------|-----------|---------------|
| `VwRdPassport` | `vw_rd_application` | Application | **by-progress**, **by-type** (live) | 1 | EF Wired |
| `vw_rd_passport` | `VwRdPassport` |
| `VwRdRegistration` | `vw_rd_registration` | `VwRdRegistration` |
| `VwRdWorkPermit` | `vw_rd_work_permit` | `VwRdWorkPermit` |
| `VwRdBorderZone` | `vw_rd_border_zone` | `VwRdBorderZone` |
| `VwRdTravel` | `vw_rd_travel` | `VwRdTravel` |
| `VwRdInvitationIssued` | `vw_rd_invitation_issued` | `VwRdInvitationIssued` |
| `VwRdVisaState` | `vw_rd_visa_state` | `VwRdVisaState` |
| `VwRdAppProgress` | `vw_rd_app_progress` | `VwRdAppProgress` |
| `VwRdSnapshotCounts` | `vw_rd_snapshot_counts` | `VwRdSnapshotCounts` |

In `Visa2026DbContext.OnModelCreating()`:
```csharp
modelBuilder.Entity<VwRdPassport>().HasNoKey().ToView("vw_rd_passport");
// ... repeat for each view
```

---

## Implementation notes (verify before creating each view)

- `ApplicationTypes.Name` values for visa extension: check against `ApplicationTypes` table (likely `App_Visa_Ext`, `App_Visa_New`, or similar)
- `ApplicationTypes.Name` values for invitation: likely `App_Invitation` or similar
- `ApplicationTypes.Name` values for travel: likely `App_Travel` or similar
- `ApplicationProgress` table: confirm column names (`ApplicationID`, `ApplicationStateID`, `Date`)
- `ApplicationStates` table: confirm table name and `Name` column
- `VisaCategories` table: confirm name and FK from `Visas`
- `BorderZones.BorderZoneNumber`: confirm column exists on `BorderZones` table (not on items)
- Family member project cascade: test with a known family member to confirm `SponsoringEmployeeID` → `ProjectContractID` path is correct

Append confirmations to `learnings.md` as each view is validated against real data.