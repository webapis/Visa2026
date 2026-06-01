# Business Object State Colors

> **Purpose:** Single source of truth for **visual color** applied to business-object states across list views, detail views, dashboards, and notifications.  
> All new `[Appearance]` rules, inbox CSS, and dashboard badges must conform to this document.
>
> **Core rules:**
> 1. Every canonical **state code** has exactly **one** registered tone (specific `BackColor`, hex, and `FontColor`).
> 2. States with **similar meaning** share the same **hue family**; they differ by **tone** (lighter / darker / slightly warmer variant within that family).
> 3. States with **different meaning** use a **different hue family** so officers can scan by color group.
>
> **Related docs:**
> - [`APPLICATION_PROGRESS_DOMAIN_NOTES.md`](APPLICATION_PROGRESS_DOMAIN_NOTES.md) — officer UI examples, unnamed states (**ideation**)
> - [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md) — SLA / transition design (**not implemented**)
> - [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) — canonical state definitions per BO
> - [`BO_STATE_TEMPORAL_TYPES.md`](BO_STATE_TEMPORAL_TYPES.md) — **`DaysRemaining`** vs **`DaysElapsed`** classification per BO
> - [`DEPRECATED.md`](DEPRECATED.md) — `ApplicationStatus` enum is deprecated in favour of `ApplicationProgress`
> - [`.cursor/skills/visa2026-bo-state-colors/SKILL.md`](../.cursor/skills/visa2026-bo-state-colors/SKILL.md) — agent workflow for states, registry updates, and ListView row appearance

---

## How BO states are determined

A **state** is not always a single persisted property. Before assigning a color, identify **how** the state is derived. Multiple sources can apply to one BO at the same time ([BR-049](BUSINESS_LOGIC_BASELINE.md) — multi-dimensional states).

### Determination sources (summary)

| Source | What drives the state | Typical examples | Color wiring |
|---|---|---|---|
| **A. Persisted flags** | Boolean (or enum) columns set by users or sync rules | `IsCancelled`, `IsChanged`, `IsExtended`, `IsUsed`, `ExtensionRequired`, `Person.IsArchived` | Column accent; row rule on flag **or** mapped into `PrimaryStateCode` |
| **B. Date / time** | `ExpirationDate`, `StartDate`, `DaysRemaining` vs `Today` + thresholds | `IExpirationLogic`, `ExpirationState`, evaluators `Expired` / `ExpiringSoon` | Evaluator → registry alias (`ExpiringSoon` → `Expiring`) |
| **C. C# evaluator** | Static function combining A+B+D+E | `VisaStateEvaluator`, `PassportStateEvaluator`, … | `BoStateResult.StateCode` → alias map → color |
| **D. Process history** | Latest `ApplicationProgress` row (`Date`, then `ID`) | `ApplicationState.Code`, `ApplicationLocation.Code` | Direct registry lookup by catalog code |
| **E. Cross-BO linkage** | Related records and application type | `ApplicationItem.CurrentVisa`, `IssuingApplicationItem`, `Registration`, `ApplicationType.Name`, `RejectionItem` | Evaluator or SQL view; often defines `OnExtension`, `IsRejected` |
| **F. Current vs historical** | Collection ordering — “is this the active row for the person?” | `PersonCurrentItems.GetCurrentVisa`, `GetCurrentWorkPermitItem`, … | `Archived` when instance ≠ current |
| **G. SQL view (read model)** | Pre-joined cross-BO row for dashboard/list | `View_VisaExtensionStatus`, `View_WorkPermitExtensionStatus` | View entity `CurrentState` / computed column → registry |
| **H. Configuration** | `SystemSettings` thresholds, type-driven UI flags | `ExpirationWarningThreshold`, `DefaultExpiringSoonDays`, `ApplicationType.Show*` | Affects **branching** inside evaluators, not a state code itself |

**Precedence:** evaluators and business rules define which state **wins for row background** when dimensions conflict (see [Row background resolution](#row-background-resolution)). Secondary dimensions use **column** colors.

### Source details

**A. Persisted state flags** — optional booleans on document BOs (`Visa`, `WorkPermitItem`, `InvitationItem`, …). Some are **mutually exclusive** (e.g. `InvitationItem`: `IsCancelled` / `IsChanged` / `IsUsed` — at most one; see `InvitationStatusFlagsHelper`). Application items may mirror flags (`ApplicationItem.VisaIsCancelled`, `WorkPermitItemIsChanged`) for form workflow; target BO flags are authoritative for document state.

**B. Date-derived validity** — BOs implementing `IExpirationLogic` use the **`DaysRemaining`** temporal type (countdown to `ExpirationDate`). See [`BO_STATE_TEMPORAL_TYPES.md`](BO_STATE_TEMPORAL_TYPES.md). They expose computed `DaysRemaining`. `ExpirationLogicHelper` and evaluators use:
- **Per-BO days window:** `ExpirationAlertRule.ExpiringSoonDays` (officer-configurable)
- **Fallback:** `SystemSettings.DefaultExpiringSoonDays`

**B2. Date-derived follow-up** — **`DaysElapsed`** states anchor on a past date (e.g. `ApplicationProgress.Date`, `TravelHistory.TravelDate`). State **codes** come from progress/catalog; elapsed days drive follow-up alerts (see [`BO_STATE_TEMPORAL_TYPES.md`](BO_STATE_TEMPORAL_TYPES.md) §4).

**C. Evaluators** — `Visa2026.Module/Services/StateEvaluation/Evaluators/*.cs`. Pure C#: load BO + `StateEvaluationSettings`, return `BoStateResult` (`StateCode`, `Severity`). **Preferred** single place for ListView `PrimaryStateCode` on date-bound BOs. Evaluator `StateCode` may differ from registry code — document alias (e.g. `Cancelled` → `IsCancelled`, `ExpiringSoon` → `Expiring`).

**D. Application progress** — `ApplicationProgress` stores `State` (`ApplicationState` lookup) and `Location` (`ApplicationLocation` lookup). Current process state = **latest** progress entry (`ApplicationProgressHelper.GetLatest`). Catalog codes (`PROCESS_ISSUED`, `AT_OFFICE`, …) are registry state codes for workflow dimension.

**E. Cross-BO linkage** — state from graph, not one column:
- Extension in progress: `ApplicationItem.CurrentVisa` + extension `ApplicationType` + no `IssuingApplicationItem` completion ([BR-044](BUSINESS_LOGIC_BASELINE.md))
- Cancellation evidence: linked cancel application types ([BR-043](BUSINESS_LOGIC_BASELINE.md)) — flag alone may be insufficient for full rule
- Person rejection: `RejectionItem` required for person-level rejected ([BR-064](BUSINESS_LOGIC_BASELINE.md)); application `PROCESS_REJECTED` optional ([BR-065](BUSINESS_LOGIC_BASELINE.md))
- Registration compliance: `TravelHistory` + `Registration` + application type ([`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) §9)

**F. Current-item resolution** — `PersonCurrentItems` picks the effective child record (passport, visa, work permit, …) by date/ID rules. A BO instance is **`Archived`** when it is not the person's current item even if dates are still valid. Visas also use `VisaIsEffectiveOn` for as-of selection.

**G. SQL views** — `SqlViewsUpdater` creates views mapped to read-only entities (e.g. `VisaExtensionStatus`). Used where joins are heavy; dashboard counts must match evaluator/list filters ([`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md)).

**H. Settings & type config** — not states themselves but change outcomes: `ExtensionRequired` branches `ExpiringSoonNotRequired`; `SystemSettings` drives warning windows; `ApplicationType` drives which application types participate in linkage rules.

### Multi-dimensional display

One BO may simultaneously have:
- **Validity dimension** — `Expiring`, `Expired` (B+C)
- **Document flag dimension** — `IsExtended`, `IsChanged` (A)
- **Process dimension** — `OnExtension`, `PROCESS_STARTED`, `AT_THE_MINISTERY_1` (D+E)

List row: **one** color from the highest-priority active code. Detail/list columns: show **each** dimension with its own family tone (see [Column accent](#column-accent-preferred-for-multi-state-rows) below).

### Choosing `PrimaryStateCode` for row color

| BO kind | Primary derivation |
|---|---|
| Date-bound document (`Visa`, `Passport`, `WorkPermitItem`, …) | Evaluator `StateCode` + alias map |
| `Application` | Latest `ApplicationProgress.State.Code` — **planned** ListView row color per [`APPLICATION_LISTVIEW_STATE_COLORS.md`](APPLICATION_LISTVIEW_STATE_COLORS.md) |
| `ApplicationProgress` (history list) | That row's `State.Code`; `Location.Code` as second column |
| Flag-only item (`InvitationItem`) | Set flag (`IsUsed`, …) if single; else evaluator when added |
| SQL view entity (`VisaExtensionStatus`) | View's resolved state / progress code column |

Canonical state **meanings**: [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md). Implementation plan: [`STATE_TRACKING_IMPLEMENTATION_PLAN.md`](STATE_TRACKING_IMPLEMENTATION_PLAN.md).

---

## Color families

| Family | Hue | Meaning | Tone scale |
|---|---|---|---|
| **Red** | `#dc3545` base | Cancelled, rejected, expired — terminal or breach | T1 lightest → T6 deepest (within red / rose) |
| **Amber** | `#fd7e14` base | Expiring — deadline approaching | T1 (may expand if more warning states are added) |
| **Blue** | `#0d6efd` base | In process — submitted, under review, at authority | T1 lightest → T6 deepest |
| **Teal** | `#0dcaf0` base | Extension in progress | T1 → T2 |
| **Gold** | `#ffc107` base | At office / being prepared locally | T1 → T3 |
| **Violet** | `#6f42c1` base | Document amended after issuance | T1 → T2 |
| **Green** | `#198754` base | Completed, issued, registered, consumed | T1 lightest → T6 deepest |
| **Gray** | `#6c757d` base | Archived / inactive | T1 |

Severity (below) controls **row priority** when several states apply; **family + tone** control **appearance**.

---

## Severity (priority only)

| Tier | Meaning | Used for priority |
|---|---|---|
| **Critical** | Terminal failure or compliance breach | Highest |
| **Warning** | Deadline approaching | High |
| **Info** | In progress or amended | Medium |
| **Healthy** | Normal or successfully completed | Low |
| **Archived** | Superseded / historical | Lowest |

---

## Master color registry

Look up color **by state code**. The **Family** and **Tone** columns explain why related states look alike.

### Document and validity flags

| State code | Family | Tone | Severity | XAF row `BackColor` | CSS bg / text | Column `FontColor` |
|---|---|---|---|---|---|---|
| `IsCancelled` | Red | T1 | Critical | `LightCoral` | `#f8d7da` / `#842029` | `Red` |
| `IsRejected` | Red | T2 | Critical | `MistyRose` | `#ffe4e6` / `#9f1239` | `Crimson` |
| `Expired` | Red | T3 | Critical | `LightPink` | `#fecdd3` / `#881337` | `DarkRed` |
| `Expiring` | Amber | T1 | Warning | `LightSalmon` | `#ffd8a8` / `#7c2d12` | `DarkOrange` |
| `OnExtension` | Teal | T1 | Info | `PaleTurquoise` | `#cffafe` / `#155e75` | `DarkCyan` |
| `OnProcess` | Blue | T1 | Info | `LightSkyBlue` | `#e0f2fe` / `#075985` | `SteelBlue` |
| `IsChanged` | Violet | T1 | Info | `Lavender` | `#e9d5ff` / `#581c87` | `BlueViolet` |
| `IsExtended` | Violet | T2 | Info | `Thistle` | `#ddd6fe` / `#5b21b6` | `MediumPurple` |
| `AtOffice` | Gold | T1 | Info | `LightGoldenrodYellow` | `#fef08a` / `#713f12` | `Goldenrod` |
| `ProcessComplete` | Green | T1 | Healthy | `Honeydew` | `#dcfce7` / `#14532d` | `Green` |
| `IsRegistered` | Green | T2 | Healthy | `LightGreen` | `#bbf7d0` / `#166534` | `ForestGreen` |
| `IsUsed` | Green | T3 | Healthy | `PaleGreen` | `#d9f99d` / `#365314` | `OliveDrab` |
| `IsArchived` | Gray | T1 | Archived | `Gainsboro` | `#e9ecef` / `#6c757d` | `Gray` |

### `ApplicationState` codes

Lookup: `DatabaseUpdate/LookupCatalogs/application-state.json` · BO: `ApplicationProgress.State`

| State code | Display (TM seed) | Family | Tone | Severity | XAF row `BackColor` | CSS bg / text | Column `FontColor` |
|---|---|---|---|---|---|---|---|
| `IS_BEING_PREPARED` | TAÝÝARLYKDA | Gold | T2 | Info | `LemonChiffon` | `#fef9c3` / `#854d0e` | `DarkGoldenrod` |
| `1_REVIEW_STARTED` | 1-NJI IŞ YLALAŞYKDA | Blue | T2 | Info | `LightSteelBlue` | `#dbeafe` / `#1e40af` | `DodgerBlue` |
| `2_REVIEW_STARTED` | 2-NJI IŞ YLALAŞYKDA | Blue | T3 | Info | `SkyBlue` | `#bae6fd` / `#0369a1` | `DeepSkyBlue` |
| `1_REVIEW_APPROVED` | 1-NJI IŞ YLALAŞYK ALYNDY | Green | T4 | Info | `Aquamarine` | `#a7f3d0` / `#047857` | `SeaGreen` |
| `2_REVIEW_APPROVED` | 2-NJI IŞ YLALAŞYK ALYNDY | Green | T5 | Info | `MintCream` | `#ecfccb` / `#3f6212` | `DarkGreen` |
| `1_REVIEW_REJECTED` | 1-NJI IŞ YLALAŞYK BERILMEDI | Red | T4 | Critical | `PeachPuff` | `#ffedd5` / `#c2410c` | `OrangeRed` |
| `2_REVIEW_REJECTED` | 2-NJI IŞ YLALAŞYK BERILMEDI | Red | T5 | Critical | `NavajoWhite` | `#fed7aa` / `#9a3412` | `Chocolate` |
| `PROCESS_STARTED` | İŞLENMEKDE | Blue | T4 | Info | `CornflowerBlue` | `#93c5fd` / `#1d4ed8` | `RoyalBlue` |
| `PROCESS_CANCELLED` | ÝÜZTUTMA ÝATYRYLDY | Red | T6 | Critical | `RosyBrown` | `#fecaca` / `#991b1b` | `Firebrick` |
| `PROCESS_REJECTED` | GARŞYLYK BERILDI | Red | T6′ | Critical | `Salmon` | `#fca5a5` / `#b91c1c` | `IndianRed` |
| `PROCESS_ISSUED` | RESMILEŞDİRİLDİ | Green | T6 | Healthy | `SpringGreen` | `#86efac` / `#15803d` | `DarkGreen` |

Review rejections (T4–T5) sit in the **Red** family but use warmer rose/peach tones to distinguish **internal review refusal** from document-level `IsCancelled` (T1) or final `PROCESS_REJECTED` (T6′).

### `ApplicationLocation` codes

Lookup: `DatabaseUpdate/LookupCatalogs/application-location.json` · BO: `ApplicationProgress.Location`

| State code | Display (TM seed) | Family | Tone | Severity | XAF row `BackColor` | CSS bg / text | Column `FontColor` |
|---|---|---|---|---|---|---|---|
| `AT_OFFICE` | OFISDE | Gold | T3 | Info | `Cornsilk` | `#fef3c7` / `#92400e` | `Peru` |
| `AT_THE_MINISTERY_1` | 1-NJI MINISTRLIKDE | Blue | T5 | Info | `#c7d2fe` * | `#c7d2fe` / `#4338ca` | `MediumBlue` |
| `AT_THE_MINISTERY_2` | 2-NJI MINISTRLIKDE | Blue | T6 | Info | `#a5b4fc` * | `#a5b4fc` / `#3730a3` | `SlateBlue` |
| `AT_MIGRATION_SERVICE` | MIGRASIÝA GULLUGYNDA | Blue | T6′ | Info | `LightCyan` | `#67e8f9` / `#0e7490` | `Teal` |

\* No exact System.Drawing name — use hex in CSS; nearest XAF fallback `CornflowerBlue` / `LightSteelBlue` respectively.

**Gold family** (`AtOffice`, `IS_BEING_PREPARED`, `AT_OFFICE`) — same “held locally / not yet dispatched” meaning, three tones.  
**Blue family** (`OnProcess`, `*_REVIEW_STARTED`, `PROCESS_STARTED`, ministry & migration locations) — same “in the pipeline” meaning, progressively deeper blues.  
**Red family** — all cancellation, rejection, and expiry states; document flags use cooler rose tones, workflow rejections use warmer peach-to-red tones.

---

## Semantic aliases (no separate tone)

| Alias | Canonical state code | Family |
|---|---|---|
| `ExpiringSoon` | `Expiring` | Amber |
| `Office` (`ApplicationStatus`) | `AT_OFFICE` | Gold |
| `ToMinistry` (`ApplicationStatus`) | `AT_THE_MINISTERY_1` | Blue |
| `Processed` (`ApplicationStatus`) | `PROCESS_ISSUED` | Green |

`ApplicationStatus.cs` is deprecated — use `ApplicationProgress` for new work.

---

## Adding a new state

1. Identify the **family** from meaning (see [Color families](#color-families)).
2. Pick the next unused **tone** in that family (T1, T2, …) — do not reuse an existing tone slot.
3. Keep the new hex between the lighter and darker neighbours in the same family.
4. If no family fits, propose a **new family** (new hue) in this doc before implementing.

---

## How to apply color

### Column accent (preferred for multi-state rows)

Tint each flag or progress field with its registered tone. This is the only way to show multiple family colors on one row (e.g. `Expiring` amber + `OnExtension` teal).

### Row background resolution

One row → one `BackColor`. When several states apply:

1. Collect active state codes.
2. Sort by **severity** (Critical → … → Archived).
3. Within the same severity, prefer the most **specific** code (`ApplicationState` / `ApplicationLocation` over generic `OnProcess`).
4. Apply the winning code’s registered tone.

Examples:

- `Expiring` + `OnExtension` → Warning beats Info → row uses **Amber** `Expiring`.
- `PROCESS_REJECTED` at `AT_MIGRATION_SERVICE` → Critical state wins → row uses **Red** T6′, not Blue T6′.
- `IS_BEING_PREPARED` at `AT_OFFICE` → same **Gold** family; prefer the more specific progress `State.Code` tone.

### Deleted records

`IsDeleted` uses `SoftDeleteAppearanceRegistration` (`Gainsboro` / `Gray`, priority 500). Reserved for soft delete — not shared with `IsArchived` when both could apply.

---

## Application progress display

> **Officer UI & examples:** [`APPLICATION_PROGRESS_DOMAIN_NOTES.md`](APPLICATION_PROGRESS_DOMAIN_NOTES.md).  
> **Application ListView row color (requirement, not built):** [`APPLICATION_LISTVIEW_STATE_COLORS.md`](APPLICATION_LISTVIEW_STATE_COLORS.md) — each `Application` row tinted from **latest** `ApplicationProgress` (`CurrentState` = last progress by `Date`).  
> Registry tones below are **draft**; `[Appearance]` / `BoStateAppearanceColors` are **not implemented** on `Application` or `ApplicationProgress` lists.

Latest progress: `ApplicationProgressHelper.GetLatest`.

| Condition on latest progress | Primary state code for row color |
|---|---|
| Terminal reject / cancel codes | That `State.Code` (Red family) |
| `PROCESS_ISSUED` | `PROCESS_ISSUED` (Green) |
| `IS_BEING_PREPARED` | `IS_BEING_PREPARED` (Gold) |
| `Location.Code` = `AT_OFFICE` (non-terminal) | `AT_OFFICE` (Gold) |
| Otherwise | Latest `State.Code`, or `Location.Code` if state is generic (Blue family) |

Progress **history** list: each row uses its own `State.Code` tone; `Location` as a separate column in the Blue or Gold family.

---

## `StateSeverity` evaluators (date-bound BOs)

Evaluators on `Visa`, `WorkPermitItem`, `Passport`, etc. still map to three bucket row colours in code. Target state: per-code tone from this registry.

| Evaluator result | State code | Family |
|---|---|---|
| `Cancelled` | `IsCancelled` | Red T1 |
| `Expired` | `Expired` | Red T3 |
| `ExpiringSoon` | `Expiring` | Amber T1 |
| `Changed` | `IsChanged` | Violet T1 |
| `Extended` | `IsExtended` | Violet T2 |
| `Archived` | `IsArchived` | Gray T1 |
| `Active` | *(none)* | No row tint |

---

## Implementation checklist

1. Resolve **family** from meaning, then **tone** from the registry — never pick an unrelated hue for a related state.
2. Grep this file before adding a tone; each state code owns one tone slot.
3. Multi-state UI: column accents so related families can appear together (e.g. Amber + Teal + Blue).
4. New catalog seed row → assign family + next tone in the same PR.
5. Optional: `BoStateAppearanceColors` static class + registrar keyed by state code.

---

## Quick reference by family

### Red — cancelled / rejected / expired
| State code | Tone | XAF `BackColor` |
|---|---|---|
| `IsCancelled` | T1 | LightCoral |
| `IsRejected` | T2 | MistyRose |
| `Expired` | T3 | LightPink |
| `1_REVIEW_REJECTED` | T4 | PeachPuff |
| `2_REVIEW_REJECTED` | T5 | NavajoWhite |
| `PROCESS_CANCELLED` | T6 | RosyBrown |
| `PROCESS_REJECTED` | T6′ | Salmon |

### Amber — expiring
| `Expiring` | T1 | LightSalmon |

### Blue — in process / at authority
| State code | Tone | XAF `BackColor` |
|---|---|---|
| `OnProcess` | T1 | LightSkyBlue |
| `1_REVIEW_STARTED` | T2 | LightSteelBlue |
| `2_REVIEW_STARTED` | T3 | SkyBlue |
| `PROCESS_STARTED` | T4 | CornflowerBlue |
| `AT_THE_MINISTERY_1` | T5 | `#c7d2fe` |
| `AT_THE_MINISTERY_2` | T6 | `#a5b4fc` |
| `AT_MIGRATION_SERVICE` | T6′ | LightCyan |

### Teal — extension
| `OnExtension` | T1 | PaleTurquoise |

### Gold — at office / prepared
| State code | Tone | XAF `BackColor` |
|---|---|---|
| `AtOffice` | T1 | LightGoldenrodYellow |
| `IS_BEING_PREPARED` | T2 | LemonChiffon |
| `AT_OFFICE` | T3 | Cornsilk |

### Violet — amended document
| `IsChanged` | T1 | Lavender |
| `IsExtended` | T2 | Thistle |

### Green — complete / compliant
| State code | Tone | XAF `BackColor` |
|---|---|---|
| `ProcessComplete` | T1 | Honeydew |
| `IsRegistered` | T2 | LightGreen |
| `IsUsed` | T3 | PaleGreen |
| `1_REVIEW_APPROVED` | T4 | Aquamarine |
| `2_REVIEW_APPROVED` | T5 | MintCream |
| `PROCESS_ISSUED` | T6 | SpringGreen |

### Gray — archived
| `IsArchived` | T1 | Gainsboro |

**28 state codes · 8 hue families · 1 unique tone per code**
