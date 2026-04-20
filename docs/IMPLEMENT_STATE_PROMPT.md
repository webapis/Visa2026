# State Implementation Prompt Templates

Use these templates when asking an AI assistant to implement or update a state on the State Dashboard.
Copy the relevant template, fill in the `[ ]` placeholders, and paste into the chat.

---

---

## TEMPLATE A — Implement a New BO State (Evaluator-based)

> Use when: the state is sourced from `BO` in `STATE_SPECIFICATIONS.md` and Status is `Planned`.
> **Pre-check (AR-01):** Confirm the state's criteria involve only ONE Business Object type.
> If criteria span more than one BO type → stop, reclassify Source to SQL in the spec, and use Template B instead.

```
Before starting, read the following files in full:
- Docs/STATE_SPECIFICATIONS.md         (state criteria, severity, participants)
- Docs/IMPLEMENT_STATE_PROMPT.md       (this file — patterns and conventions)

## Task
Implement the BO state **[STATE_ID]** (e.g. V-02 · Expiring Soon) from STATE_SPECIFICATIONS.md.

## State to implement
State ID:   [STATE_ID]
Section:    [SECTION — e.g. VISA STATES]
State code: [CODE — e.g. ExpiringSoon]
BO type:    [BO class name — e.g. Visa]

## Files to change

### 1. Evaluator
File: Visa2026.Module/Services/StateEvaluation/Evaluators/[BoType]StateEvaluator.cs
- Add or correct the evaluation branch for state code `[CODE]`
- Follow the existing `Make(...)` pattern in the same file
- Severity must match STATE_SPECIFICATIONS.md exactly
- Criteria must match STATE_SPECIFICATIONS.md exactly

### 2. Dashboard criteria method
File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Add a case for `[CODE]` in the `[BoType]Criteria(...)` switch method
- CriteriaOperator must mirror the evaluator logic exactly so list view filter matches evaluator count

### 3. Dashboard static state list
File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Ensure the state is present in the correct `SectionDef` inside `SectionDefs`
- Source must be `"BO"`, Severity must match spec

### 4. Spec status update
File: Docs/STATE_SPECIFICATIONS.md
- Change Status from `Planned` → `Implemented` for state [STATE_ID]
- Update the Implementation Summary table counts
- Add a row to the Change Log at the bottom

## Conventions to follow
- **AR-01 — Single BO only:** A BO evaluator state must read properties from exactly ONE Business Object type.
  If the criteria require data from a second BO type, stop — reclassify Source to SQL and use Template B.
- StateSeverity enum values: None, Info, Warning, Critical, Breach
- BO evaluator BoType strings (must match exactly for dashboard count lookup):
    Visa              → "Visa"
    Passport          → "Passport"
    WorkPermitItem    → "WorkPermit"
    EmployeeContract  → "Employee Contract"
    MedicalRecord     → "Medical Record"
    AddressOfResidence→ "Address"
- Dashboard count key format: "{BoType}|{StateCode}" e.g. "Visa|ExpiringSoon"
- SystemSettings expiry threshold: `StateEvaluationSettings.FromSystemSettings(SystemSettings.GetInstance(space))`
- CriteriaOperator uses DevExpress.Data.Filtering: BinaryOperator, CriteriaOperator.And(...)

## Done when
- [ ] Evaluator returns correct state code and severity for the criteria in the spec
- [ ] Dashboard criteria method produces a filter that returns the same records as the evaluator
- [ ] State appears in the correct section with Source=BO and correct Severity badge
- [ ] STATE_SPECIFICATIONS.md status updated to Implemented
- [ ] No other existing states are broken (evaluator priority order preserved)
```

---

---

## TEMPLATE B — Implement a New SQL State (View-based)

> Use when: the state is sourced from `SQL` in `STATE_SPECIFICATIONS.md` and Status is `Planned`.
> **Also use this template** whenever a state's criteria involve more than one Business Object type,
> even if it was originally designed as a BO state (AR-01 — reclassify Source to SQL first).

```
Before starting, read the following files in full:
- Docs/STATE_SPECIFICATIONS.md         (state criteria, severity, participants)
- Docs/IMPLEMENT_STATE_PROMPT.md       (this file — patterns and conventions)

## Task
Implement the SQL state **[STATE_ID]** (e.g. V-09 · Submitted to Ministry) from STATE_SPECIFICATIONS.md.

## State to implement
State ID:   [STATE_ID]
Section:    [SECTION]
State code: [CODE]
SQL view:   [view name — e.g. vw_VisaProcessStates]

## What SQL states mean
SQL states are NOT evaluated from BO properties. Their counts come from a SQL Server view
that reflects an external process or workflow stage. The dashboard reads this view directly.

## Files to change

### 1. SQL View
File: Visa2026.Module/BusinessObjects/Migrations/[new migration] or a .sql file in Docs/SqlViews/
- Create or update the SQL view `[view name]`
- The view must return columns: PersonId (Guid), StateCode (nvarchar), Count (int)
  OR one row per person with StateCode set to `[CODE]` for matching records
- Criteria must match STATE_SPECIFICATIONS.md exactly

### 2. Service / query
File: [to be determined — e.g. a new ISqlStateDashboardService or extension of existing]
- Add a method that queries `[view name]` and returns Dictionary<string, int> keyed as
  "{SectionBoType}|{StateCode}"
- Inject via IObjectSpaceFactory or raw DbContext as appropriate

### 3. Dashboard wiring
File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Merge SQL state counts into `_counts` dictionary during LoadData()
- Key format: "[SectionBoType]|[CODE]" — must match the SectionDef EvalBoType for that section

### 4. Dashboard static state list
File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Ensure state `[CODE]` is present in the correct SectionDef with Source = "SQL"
- SQL states are NOT clickable (no navigation on click — count is read-only)

### 5. Spec status update
File: Docs/STATE_SPECIFICATIONS.md
- Change Status from `Planned` → `Implemented` for state [STATE_ID]
- Remove the `Depends on` note or update it to the actual view name
- Update Implementation Summary table counts
- Add a row to the Change Log

## Conventions to follow
- **AR-01 — Cross-BO goes to SQL:** If a state's criteria span more than one BO type, it belongs
  here in a SQL view — not in a BO evaluator. Update Source in STATE_SPECIFICATIONS.md to SQL before proceeding.
- SQL view naming: vw_[Section]States — e.g. vw_VisaProcessStates, vw_RegistrationStates
- Dashboard SectionDef EvalBoType for SQL-only sections (e.g. Invitations) may be null —
  use a dedicated SQL key prefix instead (e.g. "Invitation|Draft")
- SQL states never appear as clickable links — the dashboard renders them as plain text
- A SQL state count of 0 should still be shown as "0" (not "—") once the view is implemented;
  "—" is only for unimplemented states

## Done when
- [ ] SQL view exists and returns correct counts for the criteria in the spec
- [ ] Dashboard _counts dictionary is populated with the SQL state counts
- [ ] State row shows a real count (not "—") in the dashboard
- [ ] STATE_SPECIFICATIONS.md status updated to Implemented
- [ ] Existing BO state counts are unaffected
```

---

---

## TEMPLATE C — Update an Existing State (Criteria or Severity Change)

> Use when: a state is already `Implemented` but criteria, severity, or action needs to change.

```
Before starting, read the following files in full:
- Docs/STATE_SPECIFICATIONS.md         (authoritative spec — update this FIRST)
- Docs/IMPLEMENT_STATE_PROMPT.md       (this file — patterns and conventions)

## Task
Update the existing implementation of state **[STATE_ID]** to match a revised specification.

## State to update
State ID:     [STATE_ID]
State code:   [CODE]
Source:       [BO / SQL]
Change reason:[describe what changed and why — e.g. "threshold is now date-based not percentage-based"]

## Step 1 — Update the spec first
File: Docs/STATE_SPECIFICATIONS.md
- Update the Criteria section for [STATE_ID] to reflect the new logic
- Update Severity if it changed
- Update Action Required if it changed
- Add a row to the Change Log describing what changed and why

## Step 2 — Update the implementation

### If Source = BO:
File: Visa2026.Module/Services/StateEvaluation/Evaluators/[BoType]StateEvaluator.cs
- Update the evaluation branch for `[CODE]` to match the new criteria in the spec
- Verify the priority order of state branches is still correct after the change

File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Update the `[BoType]Criteria(...)` switch case for `[CODE]` to mirror the updated evaluator logic
- Update the StateDef entry for `[CODE]` if Severity changed

### If Source = SQL:
File: [SQL view file]
- Update the WHERE / CASE logic for state `[CODE]` to match the new criteria

File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Update the StateDef entry for `[CODE]` if Severity changed

## Done when
- [ ] STATE_SPECIFICATIONS.md reflects the new criteria (update spec before touching code)
- [ ] Evaluator / SQL view produces correct results for the new criteria
- [ ] Dashboard criteria method (BO) or count key (SQL) matches the updated logic
- [ ] Severity badge color on dashboard is correct if severity changed
- [ ] Change Log in STATE_SPECIFICATIONS.md updated
- [ ] No other states are broken by the change
```

---

---

## TEMPLATE D — Mark a State as In Progress

> Use when: you are about to start implementing a state and want to track it.

```
Update Docs/STATE_SPECIFICATIONS.md:
- Change Status of state [STATE_ID] from `Planned` → `In Progress`
- Update the Implementation Summary table counts
- Add a Change Log row: "YYYY-MM-DD — [STATE_ID] moved to In Progress"
```

---

## Quick Reference — Key Files

| File | Purpose |
|---|---|
| `Docs/STATE_SPECIFICATIONS.md` | Canonical spec for all states — read before any implementation |
| `Visa2026.Module/Services/StateEvaluation/Evaluators/` | BO evaluator classes |
| `Visa2026.Module/Services/StateEvaluation/BoStateResult.cs` | Return type of all evaluators |
| `Visa2026.Module/Services/StateEvaluation/StateEvaluationSettings.cs` | Threshold settings |
| `Visa2026.Module/BusinessObjects/SystemSettings.cs` | Source of threshold configuration |
| `Visa2026.Blazor.Server/Components/StateDashboardComponent.razor` | Dashboard UI + criteria methods |
| `Docs/STATE_SPECIFICATIONS.md` Change Log | History of spec changes |

## Quick Reference — BoType Strings

| Class | BoType string used in evaluator and dashboard key |
|---|---|
| `Visa` | `"Visa"` |
| `Passport` | `"Passport"` |
| `WorkPermitItem` | `"WorkPermit"` |
| `EmployeeContract` | `"Employee Contract"` |
| `MedicalRecord` | `"Medical Record"` |
| `AddressOfResidence` | `"Address"` |

## Quick Reference — Status Transition Flow

```
Planned → In Progress → Implemented
Planned → Pending      (if blocked — note dependency)
Implemented → [update] (use Template C; status stays Implemented after update)
```

## Quick Reference — Architecture Rules

| Rule | Summary |
|---|---|
| **AR-01** | If criteria involve > 1 BO type → Source must be SQL. Use Template B. Update spec Source before coding. |
| **AR-02** | Evaluator state branches must follow a fixed priority order. Insert new states at the correct position. |
| **AR-03** | Dashboard `*Criteria(...)` method must return exactly the same records as the evaluator for that state code. |