# Application BO — application type selection refactor

Refactoring plan to simplify how users pick an **Application type** on the **Application** detail view. Replaces the two-step **Category → ApplicationTypeFilter → ApplicationType** flow with a single persisted **`ApplicationType`** plus a lightweight **3-digit quick code** and an optional **reference popup**.

**Scope:** `Visa2026.Module/BusinessObjects/Application.cs` and related UI/controllers. **Out of scope for this doc:** redesigning the `ApplicationTypeFilter` lookup catalog itself (it may remain for admin grouping until a later cleanup).

**Status:** Decisions locked (see §2.1). Ready for implementation.

---

## 1. Problem (current UX)

Today the Application detail view uses three coordinated fields:

| Order | Property | Type | Role |
|-------|----------|------|------|
| 1 | `Category` | `ApplicationTypeCategory` enum | Employee / Family member / Both |
| 2 | `ApplicationTypeFilter` | `ApplicationTypeFilter` lookup | Narrows types (invitation, visa, registration, …) |
| 3 | `ApplicationType` | `ApplicationType` lookup | Final type; drives visibility flags |

```84:151:Visa2026.Module/BusinessObjects/Application.cs
        private ApplicationTypeCategory category;
        [ImmediatePostData]

        public virtual ApplicationTypeCategory Category
        {
            get => category;
            set
            {
                if (category != value)
                {
                    category = value;
                    ApplicationTypeFilter = null;
                    ApplicationType = null;
                }
            }
        }
        // ...
        [DataSourceCriteria("Category = '@This.Category'")]
        public virtual ApplicationTypeFilter ApplicationTypeFilter { ... }

        [DataSourceCriteria("ApplicationTypeFilter = '@This.ApplicationTypeFilter'")]
        public virtual ApplicationType ApplicationType { ... }
```

**Pain:** users switch back and forth between **ApplicationTypeFilter** and **ApplicationType** to find the right row. **Category** duplicates information already on **`ApplicationType.Category`** (same enum on the lookup row).

---

## 2. Goals

1. **Remove** `Category` from **`Application`** — use `Application.ApplicationType.Category` when Employee / Family member / Both is needed.
2. **Remove** `ApplicationTypeFilter` from **`Application`** — stop requiring an intermediate filter BO on the form.
3. **Add** a **non-persistent string** on **`Application`** used only for quick selection by **3-digit code** (see §4 — data model prerequisite).
4. Keep **`ApplicationType`** as the **only persisted** type field; dropdown selection remains supported.
5. Add a **small button** beside the code field that opens a **reference list** (Code | Type name | Category group); user can **pick a row** to set the type (optional A4 print).

---

## 2.1 Decisions (locked)

| # | Topic | Decision |
|---|--------|----------|
| 1 | Code on lookup | Add **`ApplicationType.SelectionCode`** (`nvarchar(3)`). Keep existing **`Code`** / **`Name`** for reports, sync, Word defs. |
| 2 | Employee / FM / Both on Application | **Remove** `Application.Category` from the form. Category comes **only** from **`Application.ApplicationType.Category`**. Update `ApplicationItem` and person controllers accordingly. |
| 3 | Quick code timing | Resolve **only when exactly 3 digits** are entered. No dropdown filtering while typing 1–2 digits. |
| 4 | Invalid / unknown code | **Show validation error** and **clear** **`ApplicationType`** (dropdown empty until user enters a valid code, picks from dropdown, or chooses a row in the reference popup). |
| 5 | Reference popup | User **can click a row** (or Accept) to set **`ApplicationType`** and sync **`ApplicationTypeQuickCode`**. Optional A4 print view for the same data. |
| 6 | Master list | Current ministry list is the **starting seed**; **administrators maintain** types via **`ApplicationType`** lookup (add rows, set **`SelectionCode`**, active flag). Enforce unique **`SelectionCode`** among active types. |

---

## 3. Proposed Application detail layout

```
┌─────────────────────────────────────────────────────────────────────────┐
│ ApplicationTypeQuickCode  [___] […]   ApplicationType  [dropdown ▼]   │
└─────────────────────────────────────────────────────────────────────────┘
```

| Control | Binding | Persisted | Behavior |
|---------|---------|-----------|----------|
| Quick code text | `ApplicationTypeQuickCode` | **No** (`[NotMapped]`) | On **exactly 3 digits**, resolve `ApplicationType` by **`SelectionCode`**. While editing (1–2 digits) or after **clear (X)**, clear **`ApplicationType`** so a wrong code can be corrected. Blazor: custom property editor posts on each keystroke. |
| Inline **…** picker (Blazor) | Custom property editor | — | Opens a link-style code table popup beside the quick-code field; click **Kod** or type name → fills code and resolves **ApplicationType**. Optional **Print type codes** / **Show in Report** for A4. |
| Application type | `ApplicationType` | **Yes** | Full dropdown; optional `[DataSourceCriteria("IsActive")]` only — **no** filter FK. Changing type updates dependent fields as today. |

**Suggested property name (C#):** `ApplicationTypeQuickCode`

| Alternative | Pros | Cons |
|-------------|------|------|
| `ApplicationTypeQuickCode` | Clear, matches “quick pick by code” | Long |
| `TypeQuickCode` | Short | Ambiguous on Application BO |
| `ApplicationTypeCodeInput` | Explicit “input not stored” | Sounds like persisted code |

**Suggested captions (localization):**

| Locale | Caption | Tooltip |
|--------|---------|---------|
| tk-TM | Arza görnüşi kody | 3 sanly kody ýazyň (mysal: 701) |
| tr-TR | Başvuru türü kodu | 3 haneli kodu girin |
| en | Application type code | Enter 3-digit code (e.g. 701) |

**UI model hints:** `[MaxLength(3)]`, `[ModelDefault("EditMask", "000")]`, `[ImmediatePostData]`, place in same layout row as `ApplicationType` via `Model.DesignedDiffs.xafml` (Module + Blazor copy).

---

## 4. Critical data model note — `ApplicationType.Code` today

`ApplicationType` inherits `LookupBase.Code` (`string`). In production data and integration code, **`Code` is a technical identifier**, not a 3-digit ministry code:

- Examples in sync/state rules: `change_invitation`, `visa_extension`, `cancel_visa_wp`
- Report wiring often uses **`ApplicationType.Name`**: `App_Inv`, `App_Visa_Ext_FM`, …

The reference image uses **numeric codes** (`101`, `305`, `701`, …) and a **category group** column (Çakylyk, Wiza, Hasaba Alyş, …) that aligns with **`ApplicationTypeFilter`**-style grouping, **not** with `ApplicationTypeCategory` (Employee / Family member / Both).

**Locked:** add **`ApplicationType.SelectionCode`** (`nvarchar(3)`, unique among active rows, indexed). Do **not** repurpose **`Code`**.

| Property | Role |
|----------|------|
| `SelectionCode` | 3-digit ministry quick-pick (`101`, `701`, …); shown in reference popup and quick-code field |
| `Code` | Legacy technical id (`change_invitation`, …) — unchanged for sync/state |
| `Name` | Report / Word def key (`App_Inv`, …) — unchanged |

**Admin maintenance:** `ApplicationType` remains a normal lookup under **Lookup / Application / Config**. Admins can **add** types and assign **`SelectionCode`**. Validation: required when active; unique; prefer `^\d{3}$`.

**Initial seed:** `ModuleUpdater` pass maps existing rows to the current ministry table (reference image). New types added later by administrators — no code deploy required.

---

## 5. Quick-code resolution rules (controller / property setter)

Implement in **`ApplicationTypeSelectionController`** (Module) — property setters alone are awkward for `[NotMapped]` + `ObjectSpace` lookup.

| Rule | Behavior |
|------|----------|
| Input length &lt; 3 | **Clear `ApplicationType`** if one was set (user is correcting the code); keep partial digits in the quick-code field |
| Quick code cleared (empty / X) | Clear **`ApplicationType`** and quick code |
| Input length = 3 | `FirstOrDefault(t => t.SelectionCode == input && t.IsActive)` → set `ApplicationType` |
| 0 matches | **Validation error**; set **`ApplicationType`** to **`null`** (clear dropdown) |
| &gt;1 match | Should not happen if unique index; log error |
| User picks from dropdown | Sync `ApplicationTypeQuickCode` from `ApplicationType.SelectionCode` |
| `ApplicationType` cleared | Clear quick code |
| Inactive type | Quick code must **not** resolve inactive rows |

**Validation:** `[RuleRequiredField]` stays on **`ApplicationType`** only — not on quick code (helper input).

**Employee / Family member / Both:** after type is set, `ApplicationItem` and person defaults use **`Application.ApplicationType.Category`** (see §6).

---

## 6. Removing `Category` from Application — downstream changes

Today **`Application.Category`** is referenced outside Application.cs:

| Location | Usage | Migration |
|----------|-------|-----------|
| `ApplicationItem.cs` | `Application.Category == Both/Employee` for person UI | `Application.ApplicationType?.Category` |
| `PersonDefaultsController.cs` | Defaults from `appItem.Application.Category` | Same |
| `PersonDetailViewController.cs` | `Application.Category == Both` | Same |

**Null-safe pattern:** until `ApplicationType` is chosen, `Application.ApplicationType?.Category` is null — treat like “no category” (hide or disable person-specific branches).

**Do not remove** enum `ApplicationTypeCategory` or `ApplicationType.Category` on the lookup BO.

---

## 7. ApplicationTypeFilter BO — what happens to it?

| Area | Plan |
|------|------|
| **Application form** | Remove property and `DataSourceCriteria` chain |
| **`ApplicationType.ApplicationTypeFilter` FK** | Can remain for **admin grouping** and reference popup “Category” column (`ApplicationTypeFilter.NameTm` or group name) |
| **Navigation / security** | Keep read-only for admins until deprecated |
| **DataImporter** | No change required for this refactor |
| **Reports doc** (`Reports/REPORT_GENERATION_GUIDE.md`) | Update references from filter groups to `SelectionCode` ranges or filter names via `ApplicationType` only |

Full removal of `ApplicationTypeFilter` entity is a **follow-up** project (DB column drop, seed cleanup).

---

## 8. Reference popup (select + optional A4 print)

**Purpose:** Help users find a **SelectionCode**; **choosing a row** applies **`ApplicationType`** and **`ApplicationTypeQuickCode`** on the parent Application detail view, then closes the popup.

**List columns (match reference image):**

| Column | Source |
|--------|--------|
| Code | `ApplicationType.SelectionCode` |
| Application type | `ApplicationType.NameTm` (fallback `Name`) |
| Category | `ApplicationType.ApplicationTypeFilter.NameTm` |

**Data source:** active `ApplicationType` rows only; sort by `SelectionCode` ascending.

**Locked UX:**

1. Button beside quick-code field opens **`PopupWindowShowAction`** with a **ListView** of `ApplicationType` (read-only grid, three columns above).
2. User **double-clicks a row** or selects one and clicks **OK** → controller sets `Application.ApplicationType` and `Application.ApplicationTypeQuickCode`, refreshes detail view.
3. Optional secondary action **Print** / **Preview** opens **`ApplicationTypeReferenceReport`** (XtraReport, A4 portrait) for ministry-style hard copy — same data, no selection required.

**Implementation:** primary = ListView popup (`ApplicationTypeSelectionController`); optional report in `Visa2026.Module/Reports` for print layout.

**Button:** on `Application` DetailView only; icon `Action_Search` or `BO_List`; caption tk: **“Kodlar sanawy”** / en: **“Type codes”**.

---

## 9. Model & localization checklist

- [x] `Application.cs` — remove `Category`, `ApplicationTypeFilter`; add `[NotMapped] ApplicationTypeQuickCode`
- [x] `ApplicationType` — add `SelectionCode` + validation; EF unique index
- [x] `Visa2026.Blazor.Server/Model.xafml` — layout: quick code + type (removed category/filter)
- [x] `Model.DesignedDiffs.Localization.*.xafml` — captions for quick code + `SelectionCode`
- [x] `ApplicationTypeSelectionController` — quick-code resolve, popup pick, validation, clear-on-edit / clear-on-empty
- [x] `ApplicationTypeQuickCodePropertyEditor` (Blazor) — `BindValueMode.OnInput`, writes `[NotMapped]` quick code on each keystroke
- [x] `ApplicationTypeSelectionCodeSeed` — all **35** ministry codes mapped to `ApplicationType.Name` (see `DatabaseUpdate/ApplicationTypeSelectionCodeSeed.cs`)
- [x] `ApplicationTypeSelectionCodeUpdater` — fills empty `SelectionCode` only; logs unmapped names
- [x] Downstream: `ApplicationItem`, person controllers → `ApplicationType.Category`
- [x] `APPLICATION.md` — update property table
- [x] Optional `ApplicationTypeReferenceReport` (A4 print)
- [ ] E2E: quick code (valid/invalid), dropdown, popup row pick

---

## 10. Phased implementation

| Phase | Deliverable | Risk |
|-------|-------------|------|
| **0 — Decisions** | ✅ Locked (§2.1) | — |
| **1 — Data** | `SelectionCode` column + seed from current list; unique index; admin CRUD | Medium |
| **2 — Application BO** | Remove duplicate fields; add quick code; widen `ApplicationType` data source | Medium |
| **3 — Controller** | Resolve code → `ApplicationType`; sync code when dropdown changes | Low |
| **4 — Reference UI** | ListView popup (row pick) + optional A4 print report | Low |
| **5 — Call sites** | `ApplicationItem`, person controllers | Medium |
| **6 — QA** | Manual matrix: each code, FM vs employee types, invalid code | — |

---

## 11. Minor items (optional, not blocking)

| Item | Default unless changed |
|------|-------------------------|
| Application **ListView** columns | Show `ApplicationType.NameTm`; optional `ApplicationType.SelectionCode` column for clerks |
| Historical applications | No migration on `Application` FK; backfill **`SelectionCode`** on lookup rows only |
| Invalid code clears type? | **Yes** (locked §2.1) |

---

## 12. Summary (locked)

| Topic | Recommendation |
|-------|----------------|
| Remove from `Application` | `Category`, `ApplicationTypeFilter` |
| Persisted type field | `ApplicationType` only |
| Quick entry property | **`ApplicationTypeQuickCode`** (`[NotMapped]`, 3-digit mask) |
| Stored digit on lookup | **`ApplicationType.SelectionCode`** (new); keep legacy `Code`/`Name` for reports & sync |
| Reference UI | ListView popup with **row pick**; optional A4 XtraReport for print |
| Category for person logic | `Application.ApplicationType.Category` only |
| Lookup maintenance | Admins edit **`ApplicationType`** + **`SelectionCode`**; initial updater seed |

---

## 13. Related files

| Path | Relevance |
|------|-----------|
| `Visa2026.Module/BusinessObjects/Application.cs` | Primary BO change |
| `Visa2026.Module/BusinessObjects/LookupBusinessObjects.cs` | `ApplicationType`, `ApplicationTypeFilter` |
| `Visa2026.Module/BusinessObjects/ApplicationItem.cs` | Category references |
| `Visa2026.Module/Controllers/PersonDefaultsController.cs` | Category references |
| `Visa2026.Module/BusinessObjects/APPLICATION.md` | BO documentation |
| `Visa2026.Module/Reports/REPORT_GENERATION_GUIDE.md` | Filter-group documentation (update after refactor) |

---

*Document status: **implemented (core)** — run DB update, populate `SelectionCode` on lookup rows, then manual QA.*
