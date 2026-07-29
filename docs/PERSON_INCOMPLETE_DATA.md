# Person incomplete data (officer soft flag)

Canonical doc for the **manual incomplete** workflow on `Person`: officers mark master data as incomplete for migration work, record **what is missing** (checkboxes) and **notes**, then clear when ready. Soft flag only — does **not** block applications.

Related:

| Doc / skill | Role |
|-------------|------|
| [`REPORT_DASHBOARD.md`](REPORT_DASHBOARD.md) | **Incomplete persons** category (chart + Preview + ListView) |
| [`.cursor/skills/visa2026-report-dashboard/`](../.cursor/skills/visa2026-report-dashboard/SKILL.md) | Dashboard wiring / `vw_rd_*` contract |
| [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md) | Planned **automatic** `DataCompleteness` alerts (evaluators) — **orthogonal** to this manual flag |
| [`OPTIONAL_DETAIL_FIELDS.md`](OPTIONAL_DETAIL_FIELDS.md) | Gear pattern; incomplete fields use `[ExcludeFromOptionalDetailFields]` and are not gear-driven |

---

## 1. Business rules (v1)

| Rule | Decision |
|------|----------|
| Soft vs hard gate | **Soft only** — Incomplete does not block create/edit of applications |
| Process / ApplicationType scope | **None** — flag is person-level, not tied to invitation/extension/etc. |
| Who can act | Visa officers (**Users** role already has Person FullAccess) |
| Where | **Person DetailView only** (typed Employee / FamilyMember / TemporaryVisitor layouts) |
| Mark incomplete | Popup: ≥1 missing-area checkbox **required** + **Notes** required |
| Mark complete | Clears Incomplete flag, all checkboxes, Notes, marked-on/by |
| Update while incomplete | Same popup (**Update incomplete**) reloads current checkboxes/notes |
| DetailView fields | Read-only on **Incomplete data** tab under `PersonRecordTabs` (hidden when not incomplete) |
| ListViews | Incomplete / Missing-* columns hidden (`VisibleInListView(false)`) |
| Notes on complete | **Cleared** (no history episode BO in v1) |
| Free text for “Other” | Notes only (no separate Other-detail field) |

### Missing-area checkboxes (v1)

English labels (dashboard chart axis / `MissingAreasLabel`):

1. Personal data  
2. Passport  
3. CV  
4. Photo  
5. Education  
6. Medical  
7. Address  
8. Family docs  
9. Other  

Stable constants: `PersonIncompleteDataLabels` in Module.

---

## 2. Officer UX

### Actions (`PersonIncompleteDataController`)

| Action | When | Behavior |
|--------|------|----------|
| **Mark incomplete** / **Update incomplete** | Always on DetailView when a Person is current | Popup `PersonIncompleteMarkOptions` → Apply → sets flag, checkboxes, notes, `IncompleteMarkedOn` / `IncompleteMarkedBy` (`SecuritySystem.CurrentUserName`) |
| **Mark complete** | Only when `IsDataIncomplete` | Confirm → clears all incomplete fields |

Caption switches to **Update incomplete** while the person is already incomplete.

### DetailView — Incomplete data tab

Typed DetailViews (`Person_DetailView_Employee` / `_FamilyMember` / `_TemporaryVisitor`) include an **Incomplete data** layout group as the last tab in `PersonRecordTabs` (`Model.xafml` Id=`IncompleteData`).

- Appearance `PersonIncompleteTab_HideWhenComplete` hides the tab when `IsDataIncomplete = False`.
- Fields are read-only (`AllowEdit=False`); officers set/clear values via **Mark incomplete** / **Update incomplete** / **Mark complete**.
- Report Dashboard **Incomplete persons** category lists who is flagged across the tenant.

---

## 3. Domain model (`Person`)

| Property | Type | Notes |
|----------|------|--------|
| `IsDataIncomplete` | `bool` | Soft incomplete flag |
| `IncompleteMissingPersonalData` … `IncompleteMissingOther` | `bool` × 9 | Missing-area flags |
| `IncompleteNotes` | `string` (unlimited) | Officer free text |
| `IncompleteMarkedOn` | `DateTime?` | Set on Mark/Update incomplete |
| `IncompleteMarkedBy` | `string` | User name |
| `IncompleteMissingAreasDisplay` | `[NotMapped]` | Computed “Missing areas” summary on the Incomplete data tab |

All persisted incomplete fields: `[ExcludeFromOptionalDetailFields]`, `AllowEdit = False`.

Non-persistent dialog: `PersonIncompleteMarkOptions` (`[DomainComponent]`, registered in `Module.AdditionalExportedTypes`).

---

## 4. Schema / startup (PostgreSQL & SQL Server)

When ModuleInfo is already current, EF may **not** add new People columns. Host-start heal must run **before** creating the dashboard view:

| Piece | Role |
|-------|------|
| `PersonIncompleteDataSchemaSql.ApplyIfMissing` | Idempotent `ALTER TABLE People` for incomplete columns |
| `PersonIncompleteDataSchemaUpdater` | ModuleUpdater (before/after schema) |
| `Startup.Configure` | Calls schema heal **then** `ReportDashboardPostgresViewsHealSql.ApplyIfMissing` |

View heal for incomplete persons is **separate** from via-ministry standalone re-heal: only creates `vw_rd_incomplete_persons_by_missing_area` when People columns exist and the view is missing.

---

## 5. Report Dashboard — Incomplete persons

| Item | Value |
|------|--------|
| Category enum | `ReportDashboardCategory.IncompletePersons` |
| Sub-report | Key `by-missing-area`, Label **By Missing Area** (only sub-report; no “All incomplete” tab) |
| Chart | Buckets = missing-area labels; a person with Passport + CV increments **both** buckets |
| Preview Total | **Person count** (one row per incomplete person), not sum of buckets |
| Preview columns | Person, Person type, Missing areas, Notes, Marked |
| SQL view | `vw_rd_incomplete_persons_by_missing_area` (+ `.postgres.sql`) |
| EF BO / ListView | `VwRdIncompletePersonsByMissingArea` / `VwRdIncompletePersonsByMissingArea_ListView` |
| Hybrid | Real for `(IncompletePersons, by-missing-area)` |
| ProjectContracts | Use **`NameTm` only** (no `Name` column on that table) |
| Toggles | **None** (no Valid visa only / Include archived / Last-N) — all incomplete persons |

Open ListView criteria: person type / project on the VwRd row; chart segment filters the matching `Missing*` flag.

---

## 6. File map

```
Visa2026.Module/
  BusinessObjects/Person.cs                          # fields + Appearance
  BusinessObjects/PersonIncompleteDataLabels.cs      # English labels / FormatMissingAreas
  BusinessObjects/PersonIncompleteMarkOptions.cs     # popup dialog
  BusinessObjects/VwRdIncompletePersonsByMissingArea.cs
  Controllers/PersonIncompleteDataController.cs
  DatabaseUpdate/PersonIncompleteDataSchemaSql.cs
  DatabaseUpdate/PersonIncompleteDataSchemaUpdater.cs
  DatabaseUpdate/ReportDashboardPostgresViewsHealSql.cs  # gated incomplete view heal
  Services/ReportDashboard/ReportDashboardModels.cs      # IncompletePersons enum
  Services/ReportDashboard/ReportDashboardCatalog.cs
  Services/ReportDashboard/ReportDashboardQueryService.cs # LoadIncompletePersons
  Services/ReportDashboard/ReportDashboardMockQueryService.cs
  Services/ReportDashboard/ReportDashboardHybridQueryService.cs
  SqlViews/vw_rd_incomplete_persons_by_missing_area.sql
  SqlViews/vw_rd_incomplete_persons_by_missing_area.postgres.sql
Visa2026.Blazor.Server/
  Startup.cs                                         # schema heal before view heal
  Model.xafml                                        # Incomplete data tab on typed Person DetailViews + VwRd ListView
tools/GenerateModelLocalization/UiStrings.messages.json  # ReportDashboard.Category.IncompletePersons, …
```

---

## 7. Out of scope / future

- Automatic missing-field evaluators / State notifications `DataCompleteness` (separate track)
- Hard gates on Application create by incomplete flag
- Per-process (invitation vs extension) incomplete scope
- Incomplete episode history audit table
- ListView row color for incomplete persons (optional later; see `visa2026-bo-state-colors`)
- Incomplete episode history beyond MarkedOn/By (v1: tab + dashboard only)

---

## 8. Verify locally

1. Rebuild + restart Blazor host (schema heal + view heal on startup).  
2. Open a Person DetailView → **Mark incomplete** → check ≥1 area + Notes → Apply.  
3. Confirm **Mark complete** clears the flag (fields are not on the DetailView form).  
4. Report Dashboard → **Incomplete persons** → chart buckets match flags; Preview one row per person; Open ListView Total matches Preview.