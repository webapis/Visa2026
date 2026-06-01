---
name: visa2026-bo-state-colors
description: >-
  Manages Visa2026 business-object states and ListView row colors: how states are
  determined (flags, dates, evaluators, ApplicationProgress, cross-BO linkage,
  PersonCurrentItems, SQL views), color families/tones (docs/BO_STATE_COLORS.md),
  [Appearance] rules, BoStateAppearanceColors registry, and Blazor row styling.
  Use when adding BO states, state determination logic, assigning state colors,
  row background on ListViews, StateSeverityLevel, evaluators for UI, or migrating
  from severity-bucket coloring.
disable-model-invocation: false
---

# Visa2026: BO state colors and ListView row appearance

## Scope

| Concern | Where it lives | This skill |
|---------|----------------|------------|
| **State meaning & criteria** | [`docs/BO_STATE_TRACKING.md`](../../../docs/BO_STATE_TRACKING.md), [`docs/STATE_SPECIFICATIONS.md`](../../../docs/STATE_SPECIFICATIONS.md) | Read before adding states; update docs when criteria change |
| **Temporal type (`DaysRemaining` / `DaysElapsed`)** | [`docs/BO_STATE_TEMPORAL_TYPES.md`](../../../docs/BO_STATE_TEMPORAL_TYPES.md) | Classify new date-driven states before implementation |
| **Color registry (families + tones)** | [`docs/BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) | **Authoritative for every hue/hex/XAF name** |
| **Evaluation logic** | `Visa2026.Module/Services/StateEvaluation/` | Evaluators return `BoStateResult` (`StateCode`, `Severity`) |
| **State determination** | Flags, dates, progress, linkage, `PersonCurrentItems`, SQL views | See § [How states are determined](#how-states-are-determined) and [`BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) § How BO states are determined |
| **ListView row tint** | `[Appearance]` on BOs, future registrar, optional Blazor controller | Target end state; partial today |
| **Application workflow states** | `ApplicationProgress`, catalogs `application-state.json`, `application-location.json` | Map `State.Code` / `Location.Code` to registry |

**Not this skill:** state notification inbox UI ([`docs/STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](../../../docs/STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md)), E2E list appearance ([`visa2026-unit-tests`](../visa2026-unit-tests/SKILL.md) for evaluator unit tests only).

**Experience log:** [learnings.md](./learnings.md) — append-only (read before, append after verified ListView / appearance work).

**Implementation detail:** [reference.md](./reference.md) — file map, interfaces, registrar pattern, Blazor fallback.

**Related skills:** [visa2026-unit-tests](../visa2026-unit-tests/SKILL.md) (evaluator tests), [commit-after-verify](../commit-after-verify/SKILL.md).

---

## Source-of-truth contract

| Artifact | Role |
|----------|------|
| **`docs/BO_STATE_COLORS.md`** | Every **state code** → one **tone** within a **hue family**; severity for row **priority** only |
| **`docs/BO_STATE_TRACKING.md`** | What each state **means** and when it applies |
| **`docs/STATE_SPECIFICATIONS.md`** | Dashboard tiles; evaluator/SQL must match counts |
| **`ApplicationProgress` + catalogs** | Workflow state codes (`IS_BEING_PREPARED`, `PROCESS_ISSUED`, `AT_OFFICE`, …) |
| **`StateSeverity` / evaluators** | Computed validity states (`Expired`, `ExpiringSoon`, `Cancelled`, …) |

**Color rules (do not violate):**

1. Similar meaning → **same hue family**, different **tone** (T1, T2, …).
2. Different meaning → **different hue family** (Red, Amber, Blue, Teal, Gold, Violet, Green, Gray).
3. Each state code owns **one** registered tone — no duplicate hex/`BackColor` across codes.
4. Aliases (`ExpiringSoon` → `Expiring`) inherit tone; do not add a second color.

---

## How states are determined

Read [`docs/BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) § **How BO states are determined** for the full table. When adding state or row color, classify **every** contributing input:

| ID | Source | Module touchpoints |
|----|--------|-------------------|
| **A** | Persisted flags | BO properties; `InvitationStatusFlagsHelper`; `ApplicationItem` mirror flags |
| **B** | Date / time | `IExpirationLogic`, `DaysRemaining`, `ExpirationLogicHelper`, `SystemSettings` |
| **C** | C# evaluator | `Services/StateEvaluation/Evaluators/*` → `BoStateResult` |
| **D** | Process history | `ApplicationProgress`, `ApplicationProgressHelper`, lookup catalogs |
| **E** | Cross-BO linkage | `ApplicationItem.CurrentVisa`, `IssuingApplicationItem`, `Registration`, `RejectionItem`, `ApplicationType.Name` |
| **F** | Current vs historical | `PersonCurrentItems.*` → `Archived` when not current |
| **G** | SQL view read model | `SqlViewsUpdater`, `VisaExtensionStatus`, `WorkPermitExtensionStatus` |
| **H** | Configuration | `ExtensionRequired`, `ExpirationWarningThreshold`, `ApplicationType.Show*` |

**Multi-dimensional (BR-049):** one BO may have validity + process + flag states concurrently. Row color = one winning code; column accents = per dimension. Detail: [reference.md](./reference.md) § State determination matrix.

**Workflow when adding a state:**

1. Name the **state code** and registry **family/tone**.
2. Document which **source(s) A–H** compute it (update `BO_STATE_TRACKING.md` / `STATE_SPECIFICATIONS.md` if needed).
3. Implement in the **right layer** (flag only → A; date rule → B+C; workflow → D; cross-BO → C or G).
4. Wire **color** from registry; do not embed hue logic in evaluators.

---

## When this skill applies

- Add or rename a **trackable BO state** (flag, evaluator result, or `ApplicationState` seed row)
- Assign or change **ListView row background** / column accent for a state
- Wire **`ApplicationProgress`** display colors on `Application` or progress history lists
- Replace **3-bucket** `StateSeverityLevel` appearance (`LightSkyBlue` / `LightSalmon` / `LightCoral`) with per-code coloring
- Introduce **`BoStateAppearanceColors`** or **`BoStateRowAppearanceRegistration`**
- Fix nested ListView row colors (same class of issue as soft-delete — see reference)

---

## Decision: which workflow?

```
New state code (business meaning)?
  → § How states are determined (classify source A–H)
  → § Add a state (docs + evaluator + color row)

New state from dates / expiration only?
  → B + C: IExpirationLogic + evaluator; map ExpiringSoon → Expiring color

New state from application workflow?
  → D (+ E if linked to Visa/WP via ApplicationItem)

New state from related BO / person context?
  → E (+ F for archived/current); consider G if dashboard SQL view

Implement / fix ListView row background?
  → § ListView row appearance
```

---

## Add a state (full workflow)

1. **Classify determination source(s)** — A–H above; multi-dimensional states list **all** dimensions ([`BUSINESS_LOGIC_BASELINE.md`](../../../docs/BUSINESS_LOGIC_BASELINE.md) BR-049).
2. **Confirm criteria** with domain rules — [`BO_STATE_TRACKING.md`](../../../docs/BO_STATE_TRACKING.md); dashboard states also [`STATE_SPECIFICATIONS.md`](../../../docs/STATE_SPECIFICATIONS.md) and [`IMPLEMENT_STATE_PROMPT.md`](../../../docs/IMPLEMENT_STATE_PROMPT.md) if Planned.
3. **Pick hue family** from [`BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) § Color families; assign next free **tone** in that family.
4. **Add registry row** in `BO_STATE_COLORS.md` (master table + quick reference by family).
5. **Implement evaluation** in the correct layer:
   - **A only** — persisted flag + validation; optional sync via `StateChangeRule` / `SyncRule`
   - **B + C** — `IExpirationLogic` + static evaluator under `Services/StateEvaluation/Evaluators/`
   - **D** — seed `application-state.json` / `application-location.json`; progress row on save
   - **E** — evaluator joins `ApplicationItem`, `Registration`, `RejectionItem`, `ApplicationType`
   - **F** — use `PersonCurrentItems` in evaluator for `Archived` / current scope
   - **G** — extend `SqlViewsUpdater` + read-only entity when cross-BO joins are heavy
6. **Expose for UI** (choose one pattern — prefer A as standard grows):

   | Pattern | Use when |
   |---------|----------|
   | **A.** `[NotMapped] string PrimaryStateCode` + `[NotMapped] int StateDisplayPriority` | BO needs row color; computed from evaluator / flags / latest progress |
   | **B.** Boolean flag only | Simple document flags (`IsCancelled`) — column accent + row rule on flag |
   | **C.** Latest `ApplicationProgress` on parent | `Application` list — resolve via `ApplicationProgressHelper.GetLatest` |
   | **D.** SQL view entity column | Dashboard/list backed by `VisaExtensionStatus`-style views |

7. **Appearance:** register row rule keyed on `PrimaryStateCode` or flag (see [reference.md](./reference.md)); column rules for secondary concurrent states.
8. **Tests:** [visa2026-unit-tests](../visa2026-unit-tests/SKILL.md) — golden `StateCode` from STATE_SPECIFICATIONS.
9. **Append** [learnings.md](./learnings.md) if Blazor ListView behaviour differed from model `[Appearance]`.

---

## Application progress state + color

**BOs:** `ApplicationProgress.cs` (`State`, `Location`), catalogs in `DatabaseUpdate/LookupCatalogs/`.

| Dimension | Lookup | Example codes |
|-----------|--------|----------------|
| Workflow stage | `ApplicationState` | `PROCESS_STARTED`, `PROCESS_ISSUED`, `1_REVIEW_REJECTED` |
| Physical location | `ApplicationLocation` | `AT_OFFICE`, `AT_THE_MINISTERY_1`, `AT_MIGRATION_SERVICE` |

**Deprecated:** `ApplicationStatus` enum — map to progress codes only ([`DEPRECATED.md`](../../../docs/DEPRECATED.md)).

**Row color on `Application` list:**

1. `GetLatest(ProgressHistory)`.
2. Primary code = terminal `State.Code`, else `IS_BEING_PREPARED` / `AT_OFFICE`, else `State.Code` or `Location.Code` (see `BO_STATE_COLORS.md` § Application progress display).
3. Look up tone in registry; one row `BackColor`.

**Progress history ListView:** color **each row** by its own `State.Code`; show `Location` as separate column (Blue/Gold family).

**New catalog row:** add JSON seed + **unique tone** in same family in `BO_STATE_COLORS.md` in one PR.

---

## ListView row appearance

**Today:** `Visa`, `WorkPermitItem`, `Passport` use coarse `StateSeverityLevel` → 3 colors. **Target:** per `StateCode` from registry.

**Standard approach (implement incrementally):**

1. **`BoStateAppearanceColors`** — static lookup: `StateCode` → `BackColor`, `FontColor`, CSS hex ([reference.md](./reference.md)).
2. **`BoStateRowAppearanceRegistration.Register(ITypesInfo)`** — called from `Module.CustomizeTypesInfo` (alongside `SoftDeleteAppearanceRegistration`).
3. **BO implements `IBoListRowState`** (optional) with `PrimaryStateCode` for criteria strings.
4. **Priority:** soft-delete (500) > state row rules (100–400 by severity) — deleted rows stay gray.
5. **Blazor nested grids:** if `[Appearance] BackColor` fails, add `BoStateGridRowAppearanceController` mirroring [`SoftDeleteGridRowAppearanceController`](../../../Visa2026.Blazor.Server/Controllers/SoftDeleteGridRowAppearanceController.cs) with CSS classes from hex.

**Multi-state rows:** one row background = winning code by severity ([`BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) § Row background resolution). Use **column** `[Appearance]` so concurrent states (e.g. `Expiring` + `OnExtension`) stay visually distinct.

---

## Agent workflow

When the user asks about state colors or ListView tinting:

1. **Read** [`docs/BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) and [learnings.md](./learnings.md).
2. **Identify state codes** involved (evaluator, flags, `ApplicationProgress`).
3. **Do not** invent colors — assign family + tone from registry or extend registry first.
4. **Implement in Module** (evaluator, `PrimaryStateCode`, appearance registrar) — not Blazor unless nested-grid controller needed.
5. **`dotnet build Visa2026.slnx -c Debug`**; manual check target ListView in Blazor.
6. **Append learnings** for appearance quirks (nested views, priority conflicts).

When adding **`ApplicationState`** seed without color row → **block** until `BO_STATE_COLORS.md` is updated.

---

## Definition of done

- [ ] State code documented in `BO_STATE_TRACKING.md` (and `STATE_SPECIFICATIONS.md` if dashboard-facing)
- [ ] Unique tone registered in `BO_STATE_COLORS.md`
- [ ] Evaluator or progress logic returns stable `StateCode`
- [ ] ListView row or column appearance wired (or tracked issue for BOs not yet migrated)
- [ ] Unit test for evaluator golden case (if new branch)
- [ ] No duplicate tone hex in registry

---

## Additional resources

- [reference.md](./reference.md) — code patterns, file map, migration checklist
- [learnings.md](./learnings.md) — append-only appearance / ListView experience
- [`docs/BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) — master color registry
- [`docs/STATE_TRACKING_IMPLEMENTATION_PLAN.md`](../../../docs/STATE_TRACKING_IMPLEMENTATION_PLAN.md) — evaluation architecture
