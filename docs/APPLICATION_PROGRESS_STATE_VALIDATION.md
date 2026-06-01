# Application progress — state validation and time scopes

> **Purpose:** Specify how `ApplicationProgress` records workflow position for an `Application`, how **`ApplicationState`** and **`ApplicationLocation`** combine, how **`DaysElapsed`** time scopes apply, and what validation/alerts officers need when advancing the process **manually**.
>
> **Related:**
> - [`BO_STATE_TEMPORAL_TYPES.md`](BO_STATE_TEMPORAL_TYPES.md) — `ApplicationProgress` is **`DaysElapsed`**
> - [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) §8 — Application workflow overview
> - [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md) — BR-037, BR-038 (progress history, latest wins)
> - [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md) — follow-up inbox (`FollowUpState`)
> - Module: [`ApplicationProgress.cs`](../Visa2026.Module/BusinessObjects/ApplicationProgress.cs), [`ApplicationProgressHelper.cs`](../Visa2026.Module/BusinessObjects/ApplicationProgressHelper.cs)
> - Catalogs: [`application-state.json`](../Visa2026.Module/DatabaseUpdate/LookupCatalogs/application-state.json), [`application-location.json`](../Visa2026.Module/DatabaseUpdate/LookupCatalogs/application-location.json)

---

## 1. What `ApplicationProgress` represents

Each row is an **immutable milestone** (append-only history):

| Field | Role |
|-------|------|
| `Application` | Parent process record |
| `State` | Workflow **outcome phase** (`ApplicationState` lookup, stable `Code`) |
| `Location` | **Where** the file is held (`ApplicationLocation` lookup, stable `Code`) |
| `Date` | When this step took effect — **anchor for `DaysElapsed`** |
| `Description` | Optional officer comment |

**Current process position** = latest row by `Date`, then `ID` ([`ApplicationProgressHelper.GetLatest`](../Visa2026.Module/BusinessObjects/ApplicationProgressHelper.cs)). This is **BR-038**.

Officers do **not** edit the current state in place. They **add a new progress row** (or correct history under a controlled policy) to move forward.

---

## 2. Two lookup dimensions (not one)

| Dimension | Lookup | Example codes | Officer question |
|-----------|--------|---------------|------------------|
| **State** | `ApplicationState` | `IS_BEING_PREPARED`, `1_REVIEW_STARTED`, `PROCESS_ISSUED` | *What happened in the workflow?* (preparing, review, issued, rejected, …) |
| **Location** | `ApplicationLocation` | `AT_OFFICE`, `AT_THE_MINISTERY_1`, `AT_MIGRATION_SERVICE` | *Who holds the file physically?* |

Both are required on every `ApplicationProgress` row today. Validation and SLA rules should treat **`(State.Code, Location.Code)`** as the **process step key**, not `State` alone.

**Deprecated:** [`ApplicationStatus`](../Visa2026.Module/BusinessObjects/ApplicationStatus.cs) enum (`Office`, `ToMinistry`, `Processed`) — replaced by progress + `ApplicationLocation`. See [`DEPRECATED.md`](DEPRECATED.md).

---

## 3. Temporal model: `DaysElapsed`

For the **current** (latest) progress row:

```text
DaysInCurrentStep = (Today − LatestProgress.Date.Date).Days
```

| Signal | Meaning | Officer action style |
|--------|---------|----------------------|
| Within SLA | Step is still inside allowed processing time | Monitor |
| SLA exceeded | Step has been open longer than configured **MaxDaysInStep** | **Follow-up** — chase authority or advance process |
| Terminal step | `PROCESS_ISSUED`, `PROCESS_REJECTED`, `PROCESS_CANCELLED` | No SLA — history frozen unless correction |

This is **`DaysElapsed`** ([`BO_STATE_TEMPORAL_TYPES.md`](BO_STATE_TEMPORAL_TYPES.md) §4). Elapsed days do **not** change `State` by themselves.

---

## 4. Design principle: manual advance, automated **remind** — not auto-switch

Visa officers **must** record each transition. That preserves audit trail and matches ministry reality (external delays, partial batches, exceptions).

| Mechanism | Recommended? | Why |
|-----------|--------------|-----|
| Officer saves new `ApplicationProgress` row | **Yes** (primary) | Authoritative, auditable |
| **Alert** when `DaysInCurrentStep > MaxDaysInStep` | **Yes** | “Normal procedure” = remind / escalate |
| **Suggest next** `(State, Location)` in UI | **Yes** | Reduces wrong transitions |
| **Validate** allowed transitions on save | **Yes** (hard block for illegal jumps) | Data integrity |
| **Auto-insert** next progress row when SLA expires | **No** (default) | Implies a decision that may not have happened |
| Auto-switch without officer | **No** | Breaks BR-037 audit intent |

**“Should move to next state after N days”** in business language = **SLA exceeded → notification + suggested next step**, not silent auto-advance.

Optional later: admin-only **“Apply suggested transition”** action that still creates a row attributed to the user (one click, still audited).

---

## 5. Validation layers (recommended)

Implement in order. All run when a new or edited `ApplicationProgress` is saved.

### Layer 1 — Row integrity (always)

| Rule | Status today | Recommendation |
|------|--------------|----------------|
| `Application`, `State`, `Location`, `Date` required | Enforced (`RuleRequiredField`) | Keep |
| `Date >= Application.ApplicationDate` | Commented out in code | **Enable** — progress cannot precede application |
| `Date <= Today` (or allow same-day future with role?) | Commented out | **Enable** for officers; admin override for backfill |
| New row `Date >=` previous latest row `Date` | Not enforced | **Enable** unless admin **correction mode** |
| Terminal state cannot transition except correction policy | Not enforced | **Enable** — see §6 |

### Layer 2 — Allowed transition graph

Define **per `ApplicationType`** (or per routing profile) which `(FromState, FromLocation) → (ToState, ToLocation)` edges are legal.

Examples:

- `IS_BEING_PREPARED` @ `AT_OFFICE` → `1_REVIEW_STARTED` @ `AT_THE_MINISTERY_1`
- `1_REVIEW_APPROVED` @ `AT_THE_MINISTERY_1` → `2_REVIEW_STARTED` @ `AT_THE_MINISTERY_2` (types with two ministries)
- `1_REVIEW_APPROVED` @ `AT_THE_MINISTERY_1` → `PROCESS_STARTED` @ `AT_MIGRATION_SERVICE` (single ministry types)
- Any review → `1_REVIEW_REJECTED` / `2_REVIEW_REJECTED` / `PROCESS_REJECTED`
- Any non-terminal → `PROCESS_CANCELLED`

**Reject** illegal jumps on save with a clear message (“Visa extension applications cannot skip first ministry review”).

Routing variants (registration check-in skips ministry) come from [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) §8d — encode as **different graphs**, not one global list.

### Layer 3 — SLA / time scope (soft + alerts)

For each **non-terminal step** in the graph:

| Config field | Meaning |
|--------------|---------|
| `MaxDaysInStep` | Normal maximum calendar days in this `(State, Location)` before follow-up |
| `WarningDaysBeforeMax` | Optional warning band (e.g. alert at 80% of SLA) |
| `SuggestedNextStateCode` | Default next state when officer advances |
| `SuggestedNextLocationCode` | Default next location |

When `DaysInCurrentStep > MaxDaysInStep`:

- Raise **state notification** (`FollowUpState` category) — e.g. “At 1st ministry 15 days (limit 10)”
- Show **badge** on Application list/detail
- Do **not** change `ApplicationProgress` automatically

SLA can differ by `ApplicationType` (invitation vs registration vs extension).

### Layer 4 — Preconditions (optional, high value)

Block or warn when business rules fail, e.g.:

- Cannot set `PROCESS_ISSUED` unless required `ApplicationItem` / document checks pass
- Cannot send to ministry if mandatory attachments missing (links to data-completeness notifications)

Keep these in **evaluators** / dedicated validators, not scattered in UI.

---

## 6. Terminal and exceptional states

From [`application-state.json`](../Visa2026.Module/DatabaseUpdate/LookupCatalogs/application-state.json):

| Code | Terminal? | Notes |
|------|-----------|-------|
| `PROCESS_ISSUED` | Yes | Success — issue documents |
| `PROCESS_REJECTED` | Yes | Refused — may start new application later |
| `PROCESS_CANCELLED` | Yes | Withdrawn |
| `1_REVIEW_REJECTED`, `2_REVIEW_REJECTED` | Process branch | Often terminal for *this* submission path; officer may reopen via new application |
| All others | No | Subject to SLA |

**History correction:** If an officer entered the wrong row, prefer **admin-only** edit/delete with audit trail (or compensating row), not silent delete. [`ApplicationProgress.md`](../Visa2026.Module/BusinessObjects/ApplicationProgress.md) describes latest-wins on delete — policy should be explicit before enabling delete for Users role.

---

## 7. Canonical state and location codes (seed)

### 7.1 `ApplicationState` (`application-state.json`)

| Code | Typical meaning | Default |
|------|-----------------|---------|
| `IS_BEING_PREPARED` | Being prepared at office | **Default** lookup row |
| `1_REVIEW_STARTED` | First ministry review in progress | |
| `2_REVIEW_STARTED` | Second ministry review in progress | |
| `1_REVIEW_APPROVED` | First ministry approved | |
| `2_REVIEW_APPROVED` | Second ministry approved | |
| `1_REVIEW_REJECTED` | Rejected at first ministry | |
| `2_REVIEW_REJECTED` | Rejected at second ministry | |
| `PROCESS_STARTED` | Processing at migration service | |
| `PROCESS_ISSUED` | Completed / issued | Terminal |
| `PROCESS_REJECTED` | Rejected (final) | Terminal |
| `PROCESS_CANCELLED` | Cancelled | Terminal |

Display names are localized via `LocalizationKey` (Layer B).

### 7.2 `ApplicationLocation` (`application-location.json`)

| Code | Meaning | Default |
|------|---------|---------|
| `AT_OFFICE` | At company office | **Default** |
| `AT_THE_MINISTERY_1` | First ministry | |
| `AT_THE_MINISTERY_2` | Second ministry | |
| `AT_MIGRATION_SERVICE` | Migration service | |

### 7.3 Example standard routing (full visa / WP / invitation)

High-level sequence (see §8d in `BO_STATE_TRACKING.md` for per-type shortcuts):

```text
(IS_BEING_PREPARED, AT_OFFICE)
  → (1_REVIEW_STARTED, AT_THE_MINISTERY_1)
  → (1_REVIEW_APPROVED, AT_THE_MINISTERY_1)   [or REJECTED]
  → (2_REVIEW_STARTED, AT_THE_MINISTERY_2)    [optional second ministry]
  → (2_REVIEW_APPROVED, AT_THE_MINISTERY_2)
  → (PROCESS_STARTED, AT_MIGRATION_SERVICE)
  → (PROCESS_ISSUED, AT_MIGRATION_SERVICE)
```

Registration types that skip ministry omit ministry rows entirely.

---

## 8. Suggested configuration model (implementation)

Mirror [`ExpirationAlertRule`](../Visa2026.Module/BusinessObjects/ExpirationAlertRule.cs): officer-configurable, tenant JSON seed, **`InsertOnly`** for new steps.

**Option A — `ApplicationProgressStepRule` (recommended name)**

One row per **process step** (not per transition edge):

| Field | Example |
|-------|---------|
| `ApplicationTypeName` or `RoutingProfile` | `App_Visa_Ext` or `StandardMinistryRoute` |
| `StateCode` | `1_REVIEW_STARTED` |
| `LocationCode` | `AT_THE_MINISTERY_1` |
| `MaxDaysInStep` | 10 |
| `SuggestedNextStateCode` | `1_REVIEW_APPROVED` |
| `SuggestedNextLocationCode` | `AT_THE_MINISTERY_1` |
| `IsTerminal` | false |
| `SortOrder` | 20 |

Separate **`ApplicationProgressTransitionRule`** if you need many-to-many edges (reject from multiple steps); start with step rules + explicit reject edges in code or a small transition table.

**Option B — extend lookup JSON** with optional `MaxDaysInStep` on each state row — simpler but ignores location and per-type routing.

**Navigation:** System → **Application progress rules** (alongside Expiration alert rules).

---

## 9. UI / officer workflow (target)

1. Open **Application** detail → **Progress history** nested list.
2. **Add** new row — defaults:
   - `Date` = today
   - `State` / `Location` = suggested next from current step rule (editable)
3. On save:
   - Run Layer 1–2 validation
   - Append history; latest wins for `CurrentState` (when implemented on `Application`)
4. List views show **`DaysInCurrentStep`** and SLA status (OK / Warning / Overdue).
5. Inbox surfaces overdue steps across applications.

Officers never pick arbitrary state codes without validation — dropdown filtered to **allowed next steps** from the graph.

---

## 10. Implementation status (this repo)

| Capability | Status |
|------------|--------|
| `ApplicationProgress` BO + catalogs | **Done** |
| Latest progress resolution | **Helper only** (`ApplicationProgressHelper`) |
| `Application.CurrentState` sync on save/delete | **Documented** in `ApplicationProgress.md`; verify on `Application` BO |
| Date validation rules | **Commented out** on `ApplicationProgress` |
| Transition graph validation | **Not implemented** |
| SLA / `MaxDaysInStep` config | **Not implemented** |
| Follow-up notifications | **Planned** (`FollowUpState`) |
| SQL views joining progress | **Partial** (`SqlViewsUpdater`, extension tracking views) |

---

## 11. Phased rollout

| Phase | Deliverable |
|-------|-------------|
| **P0 — Document & align** | This doc; officer confirms SLA days per step per application family |
| **P1 — Integrity** | Enable date rules; monotonic `Date`; terminal state guards |
| **P2 — Transition matrix** | `ApplicationProgressStepRule` seed + validator on save; filtered UI |
| **P3 — SLA alerts** | `DaysInCurrentStep` computed field; notifications; list badges |
| **P4 — Preconditions** | Link to data completeness + issuance rules |

---

## 12. Open questions for officers

Fill before P2 seed JSON:

1. **SLA days** for each `(State, Location)` — office prep, each ministry stage, migration service?
2. **Per `ApplicationType`** differences — which types use two ministries vs one vs migration-only?
3. **Reject paths** — can an application return to `IS_BEING_PREPARED` @ `AT_OFFICE` on the same application number, or only via new application?
4. **Backdating** — who may set `Date` in the past for historical import?
5. **Concurrent dimensions** — when visa shows `OnExtension` + `AtMinistry1`, which SLA wins for notifications?

---

## 13. Code index

| Item | Path |
|------|------|
| Progress BO | `Visa2026.Module/BusinessObjects/ApplicationProgress.cs` |
| Latest row helper | `Visa2026.Module/BusinessObjects/ApplicationProgressHelper.cs` |
| State catalog BO | `LookupBusinessObjects.cs` → `ApplicationState` |
| Location catalog BO | `LookupBusinessObjects.cs` → `ApplicationLocation` |
| State seed | `DatabaseUpdate/LookupCatalogs/application-state.json` |
| Location seed | `DatabaseUpdate/LookupCatalogs/application-location.json` |
| Progress list criteria example | `Controllers/RegistrationListViewController.cs` (`PROCESS_ISSUED`) |
