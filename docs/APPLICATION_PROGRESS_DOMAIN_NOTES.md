# Application progress — domain notes (living, ideation only)

> **Status:** **Ideation / documentation only** — captures officer language, UI behaviour, and state–color intent. **No validation, SLA, or appearance rules are implemented** from this file until promoted into [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md) or [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md).
>
> **Purpose:** Build a shared picture of what `ApplicationProgress` means in practice before we implement transition rules, `DaysElapsed` alerts, or ListView colors.

---

## 1. What officers see today (Progress History UI)

On **Application** detail → **Progress** tab → nested list **Progress History**:

| Column | Meaning for officers |
|--------|----------------------|
| **State** | *At which stage of processing* is the application (review started, approved, in processing, issued, …) |
| **Location** | *Where* the file physically is (office, 1st ministry, 2nd ministry, migration service) |
| **Date** | *When* that stage/location became true |
| **Description** | Optional note (often empty) |

Reading the list **top to bottom in date order** answers:

- **When** did the application reach each stage?
- **Where** was it at each point?
- **How long** between steps? (calendar days between row `Date` values — **`DaysElapsed`** between milestones)

The **latest row by `Date`** is the current process position ([`ApplicationProgressHelper`](../Visa2026.Module/BusinessObjects/ApplicationProgressHelper.cs), BR-038). Older rows are audit history, not “current.”

---

## 2. Example: happy-path two-ministry application (from officer UI)

Observed sequence (English UI labels; dates from sample screenshot):

| # | State (UI) | Location (UI) | Date | Inferred `ApplicationState.Code` | Inferred `ApplicationLocation.Code` | Days in this step* |
|---|------------|---------------|------|----------------------------------|-------------------------------------|-------------------|
| 1 | In preparation | At office | 01.06.2026 | `IS_BEING_PREPARED` | `AT_OFFICE` | — (start) |
| 2 | 1st ministry review (in progress) | At 1st ministry | 02.06.2026 | `1_REVIEW_STARTED` | `AT_THE_MINISTERY_1` | 1 |
| 3 | 1st ministry review approved | At 1st ministry | 05.06.2026 | `1_REVIEW_APPROVED` | `AT_THE_MINISTERY_1` | 3 |
| 4 | 2nd ministry review (in progress) | At 2nd ministry | 08.06.2026 | `2_REVIEW_STARTED` | `AT_THE_MINISTERY_2` | 3 |
| 5 | 2nd ministry review approved | At 2nd ministry | 10.06.2026 | `2_REVIEW_APPROVED` | `AT_THE_MINISTERY_2` | 2 |
| 6 | In processing | At migration service | 11.06.2026 | `PROCESS_STARTED` | `AT_MIGRATION_SERVICE` | 1 |
| 7 | Issued | At office | 20.06.2026 | `PROCESS_ISSUED` | `AT_OFFICE` | 9 |

\* *Days in this step* = days from this row’s `Date` until the **next** row’s `Date` (not elapsed since today). This is how officers reason about “how long ministry kept it” vs SLA we will configure later.

**Notes from this example**

- **State and Location move together** on the happy path but are **independent fields** — e.g. final row is **Issued** (`PROCESS_ISSUED`) while **Location** returns to **At office** (`AT_OFFICE`), not migration service.
- Ministry review uses **pairs**: *in progress* (`*_REVIEW_STARTED`) then *approved* (`*_REVIEW_APPROVED`) at the **same** location before moving to the next authority.
- Total calendar time office → issued in this sample: **19 days** (01.06 → 20.06).

---

## 3. Named states in catalog (`application-state.json`)

These **stable codes** exist in seed today ([`application-state.json`](../Visa2026.Module/DatabaseUpdate/LookupCatalogs/application-state.json)):

| Code | Turkmen seed name (short) | Role |
|------|---------------------------|------|
| `IS_BEING_PREPARED` | TAÝÝARLYKDA | Preparation at office |
| `1_REVIEW_STARTED` | 1-NJI IŞ YLALAŞYKDA | 1st ministry review ongoing |
| `1_REVIEW_APPROVED` | 1-NJI IŞ YLALAŞYK ALYNDY | 1st ministry approved |
| `1_REVIEW_REJECTED` | 1-NJI IŞ YLALAŞYK BERILMEDI | 1st ministry rejected |
| `2_REVIEW_STARTED` | 2-NJI IŞ YLALAŞYKDA | 2nd ministry review ongoing |
| `2_REVIEW_APPROVED` | 2-NJI IŞ YLALAŞYK ALYNDY | 2nd ministry approved |
| `2_REVIEW_REJECTED` | 2-NJI IŞ YLALAŞYK BERILMEDI | 2nd ministry rejected |
| `PROCESS_STARTED` | İŞLENMEKDE | Processing (often migration service) |
| `PROCESS_ISSUED` | RESMILEŞDİRİLDİ | Issued / completed |
| `PROCESS_REJECTED` | GARŞYLYK BERILDI | Rejected (final) |
| `PROCESS_CANCELLED` | ÝÜZTUTMA ÝATYRYLDY | Cancelled |

**Locations** ([`application-location.json`](../Visa2026.Module/DatabaseUpdate/LookupCatalogs/application-location.json)): `AT_OFFICE`, `AT_THE_MINISTERY_1`, `AT_THE_MINISTERY_2`, `AT_MIGRATION_SERVICE`.

The happy-path screenshot uses **7 of 11** state codes and **all 4** locations. It does **not** show reject or cancel branches.

---

## 4. States we know exist in the business but are not fully named yet

Use this table as a **parking lot** while domain language is refined. Do **not** add to `application-state.json` until code + display names are agreed.

| # | Description (officer / domain language) | Relationship to progress | Catalog today? | Color intent (draft) |
|---|----------------------------------------|---------------------------|----------------|----------------------|
| U1 | *(add rows as you describe them)* | | | |
| | | | | |

**Examples of gaps we expect (confirm with officers):**

- Sub-states inside “in preparation” (waiting for documents, waiting for payment, …) — still `IS_BEING_PREPARED` or new codes?
- “Sent to ministry” as a distinct milestone vs `1_REVIEW_STARTED` — same step or separate row?
- Registration-only routes (office → migration, no ministry) — same codes, shorter graph?
- **Visa / WP / Person** dimensions while application is in progress (`OnExtension`, `ExpiringSoon`, …) — **separate BO states**, not `ApplicationProgress` rows ([BR-049](BUSINESS_LOGIC_BASELINE.md)); colors may apply on **Visa** list while **Application** list uses progress codes.
- Stuck / overdue as a **computed** signal (`DaysElapsed` > SLA) — **not** a separate catalog code; alert + row highlight, not a new `ApplicationState` name unless officers insist.

---

## 5. State vs color management (idea, not implemented)

| Layer | What we have in docs | Implementation |
|-------|----------------------|----------------|
| **Meaning** | [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) §8, this file | Partially in catalogs |
| **Temporal type** | [`BO_STATE_TEMPORAL_TYPES.md`](BO_STATE_TEMPORAL_TYPES.md) — progress = `DaysElapsed` | Not computed in UI |
| **Application ListView row color** | [`APPLICATION_LISTVIEW_STATE_COLORS.md`](APPLICATION_LISTVIEW_STATE_COLORS.md) — tint from **latest** progress = `CurrentState` | **Not** implemented |
| **Color registry** | [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) § Application progress — draft tones per `ApplicationState.Code` | **Not** wired to Blazor ListView / `[Appearance]` |
| **Validation / SLA** | [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md) | **Not** implemented |
| **Notifications** | [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md) | UI prototype only |

**Intended direction (from discussions, not built):**

- **`Application` ListView:** row background from **`CurrentState`** — the last `ApplicationProgress` in `ProgressHistory` (latest `Date`). Color key = `State.Code` per [`APPLICATION_LISTVIEW_STATE_COLORS.md`](APPLICATION_LISTVIEW_STATE_COLORS.md).
- **Progress History** nested list: each **historical row** may show its own state color later; **Location** as second column accent when it differs from “usual” pairing.
- **Reject / cancel** → **Red**; **issued** → **Green**; **in review at ministry** → **Blue**; **at office / preparing** → **Gold** — [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) master table (draft).

Until implementation, **Application** and **Progress History** lists use **default** grid styling (no per-state row background in screenshot).

---

## 6. How this connects to other BO states

`ApplicationProgress` is only the **application workflow timeline**. Officers also care about:

| BO / surface | Temporal type | Example |
|--------------|---------------|---------|
| `Visa`, `Passport`, `WorkPermitItem`, … | `DaysRemaining` | Expiring in 30 days |
| `ApplicationProgress` | `DaysElapsed` | 12 days at 1st ministry |
| `Person` registration compliance | `DaysElapsed` | Arrived 5 days ago, not registered |
| Flags on documents | Non-temporal | `IsCancelled`, `IsExtended` |

One **Application** can be `1_REVIEW_STARTED` @ `AT_THE_MINISTERY_1` while linked **Visa** is `ExtensionApplicationRequired` — **multi-dimensional**; color plan is one primary row tint + optional column accents ([`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) § Multi-dimensional display).

---

## 7. Changelog (domain capture)

| Date | Source | Note |
|------|--------|------|
| 2026-06-01 | Officer UI screenshot | Documented happy-path 7-step sequence; English labels mapped to catalog codes; `PROCESS_ISSUED` @ `AT_OFFICE` on final row |
| 2026-06-01 | Product rule | New `Application` auto-seeds first progress: `IS_BEING_PREPARED` @ `AT_OFFICE` (`ApplicationProgressInitializer`) |

---

## 8. Application processing route (do **not** use `ShowProjectContract`)

### Problem

Today, whether the next step after office preparation is **`1_REVIEW_STARTED`** (ministries) or **`PROCESS_STARTED`** (migration service) is often inferred from **`ApplicationType.ShowProjectContract`** (whether `ProjectContract` appears on the Application detail view). That flag is a **UI visibility** concern, not a workflow contract. Many types correlate by accident; others will diverge.

### Recommendation — explicit route on `ApplicationType`

Add a dedicated enum on **`ApplicationType`**, seeded in **`ApplicationTypeConfigurationCatalog.json`** (same pipeline as `Show*` flags), **not** derived from `ShowProjectContract`.

**Property name (suggested):** `ApplicationProgressRoute`  
**Enum (suggested):** `ApplicationProgressRouteKind`

| Value | Meaning | Typical next step after `IS_BEING_PREPARED` @ `AT_OFFICE` |
|-------|---------|-----------------------------------------------------------|
| `ViaMinistries` | File goes through ministry review and approval before migration processing | `1_REVIEW_STARTED` @ `AT_THE_MINISTERY_1` |
| `DirectToMigrationService` | No ministry leg; office sends straight to migration service | `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE` |

**Second axis (optional but useful):** `MinistryReviewDepth` enum — `None` | `FirstMinistryOnly` | `FirstAndSecondMinistry`.

- When `ApplicationProgressRoute = ViaMinistries` and depth is `FirstAndSecondMinistry`, allow `2_REVIEW_*` states and `AT_THE_MINISTERY_2`.
- When depth is `FirstMinistryOnly`, omit `2_REVIEW_*` from allowed states and from the transition graph.
- When `DirectToMigrationService`, depth should be `None` (validation ignores ministry codes).

Keep **`ShowProjectContract`** unchanged — it only controls whether officers capture contract data on the Application form.

**Implemented (Phase 1):** when `ShowProjectContract` and route is `ViaMinistries`, **`ProjectContract.MinistryReviewDepth`** overrides `ApplicationType.MinistryReviewDepth` per application. Contract required before leaving office preparation; warn (do not block) on contract change after progress. Canonical doc: [`APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md`](APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md).

### Which `ApplicationState` codes are available per route

Keep one global catalog ([`application-state.json`](../Visa2026.Module/DatabaseUpdate/LookupCatalogs/application-state.json)) for all environments. **Filter at runtime** when officers add `ApplicationProgress` on an `Application` (lookup datasource + save validation), using the parent type’s route + depth.

| `ApplicationState.Code` | `DirectToMigrationService` | `ViaMinistries` (1st only) | `ViaMinistries` (1st + 2nd) |
|-------------------------|----------------------------|----------------------------|-----------------------------|
| `IS_BEING_PREPARED` | Yes | Yes | Yes |
| `1_REVIEW_STARTED` | **No** | Yes | Yes |
| `1_REVIEW_APPROVED` | **No** | Yes | Yes |
| `1_REVIEW_REJECTED` | **No** | Yes | Yes |
| `2_REVIEW_STARTED` | **No** | **No** | Yes |
| `2_REVIEW_APPROVED` | **No** | **No** | Yes |
| `2_REVIEW_REJECTED` | **No** | **No** | Yes |
| `PROCESS_STARTED` | Yes | Yes | Yes |
| `PROCESS_ISSUED` | Yes | Yes | Yes |
| `PROCESS_REJECTED` | Yes | Yes | Yes |
| `PROCESS_CANCELLED` | Yes | Yes | Yes |

**Locations:** filter `ApplicationLocation` similarly — `AT_THE_MINISTERY_*` only when route is `ViaMinistries`; `AT_MIGRATION_SERVICE` when migration leg applies; `AT_OFFICE` always where office steps exist.

**Transitions** (legal next `(State, Location)` pairs) are defined **per route profile** in [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md) §5 Layer 2 — not by re-reading `ShowProjectContract`.

### Navigation (implemented)

Under the **Application** menu, two list entries (see `ApplicationProgressRouteNavigation`, `CustomNavigationUpdater`):

| Nav item | Route | Meaning |
|----------|-------|---------|
| **Application_ViaMinistries** | `ViaMinistries` | Types that go through ministry review before migration service |
| **Application_DirectMigration** | `DirectToMigrationService` | Types that bypass ministries and go straight to migration processing |

The default **Application** ListView nav item is removed (`[NavigationItem(false)]` on `Application`; group kept via `ApplicationItem` + custom items). Filtering uses `ApplicationType.ApplicationProgressRoute` (set per type in catalog seed).

### Implementation sketch (when coding)

1. Enum + column on `ApplicationType`; extend `ApplicationTypeConfigurationRow` / catalog JSON / applier.
2. Static helper e.g. `ApplicationProgressRouteHelper.GetAllowedStateCodes(ApplicationType)` and `GetAllowedLocationCodes`.
3. `ApplicationProgress` — `[DataSourceProperty]` or controller filtering `State` / `Location` from `Application.ApplicationType`.
4. Validator on save — same rules as datasource (hard block illegal codes).
5. **Suggest next step** after save uses route (office → ministry vs office → migration).
6. One-time seed: set route from business knowledge per type name; use `ShowProjectContract` only as a **hint** when filling the catalog, then **verify** each row with domain owners.

### Alternative considered (not recommended for v1)

- **Many-to-many** `ApplicationType` ↔ `ApplicationState` join table — flexible but heavy to maintain for ~40 types × 11 states.
- **Duplicate state catalogs** per route — breaks single `LocalizationKey` / deploy sync story.

---

## 9. What we still need from you

To complete the picture before any implementation:

1. **Reject / cancel** — typical progress rows officers enter (codes + locations).
2. **Per-type route matrix** — for each `ApplicationType` name: `ViaMinistries` vs `DirectToMigrationService`, and `FirstMinistryOnly` vs `FirstAndSecondMinistry` (replaces inferring from `ShowProjectContract`).
3. **Unnamed states** — list in §4 table (U1, U2, …) in your words.
4. **SLA expectations** — e.g. “1st ministry should not take more than N days” per step.
5. **Color priorities** — when workflow state and visa validity conflict on one screen, which color wins?

Append new rows to §4 and §7 as you provide more examples.
