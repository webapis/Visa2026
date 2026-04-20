# State Implementation Prompt Templates

Use these templates when asking an AI assistant to implement or update a state on the State Dashboard.
Copy the relevant template, fill in the `[ ]` placeholders, and paste into the chat.

> **Core principle:** The only difference between states is their criteria.
> Adding a state = adding one branch to an existing evaluator (BO) or SQL view (SQL).
> No new files are created per state — only per section (the first time that section is set up).
>
> **State tracking is a living system.** States are created, updated, and deleted throughout the project lifetime.
> Every operation — add, update, remove — must follow the same patterns documented here.
> Never introduce one-off designs for a specific state. Consistency is what keeps future changes safe.

---

---

## TEMPLATE A — Implement a New BO State (Evaluator-based)

> Use when: the state is sourced from `BO` in `STATE_SPECIFICATIONS.md` and Status is `Planned`.
> **Pre-check (AR-01):** Confirm criteria involve only ONE Business Object type.
> If criteria span more than one BO type → stop, reclassify Source to SQL, use Template B.
>
> **No new files are created.** A BO state = one new `if/else` branch in the existing evaluator
> and one new `case` in the existing criteria switch. That is the entire change.

```
Before starting, read the following files in full:
- Docs/STATE_SPECIFICATIONS.md                   (state criteria, severity, participants)
- Docs/IMPLEMENT_STATE_PROMPT.md                 (this file — patterns and conventions)
- Visa2026.DataImporter/SCENARIO_GUIDE.md        (scenario numbering, anchor patterns, sheet structure)

## Task
Implement BO state **[STATE_ID]** by adding its criteria branch to the existing section evaluator.
The only difference between states is their criteria — no new files are needed.

## State to implement
State ID:   [STATE_ID]
Section:    [SECTION — e.g. VISA STATES]
State code: [CODE — e.g. ExpiringSoon]
BO type:    [BO class name — e.g. Visa]

## Changes — all in existing files

### 1. Add criteria branch to the evaluator
File: Visa2026.Module/Services/StateEvaluation/Evaluators/[BoType]StateEvaluator.cs
- Add one `if/else` branch for state code `[CODE]`
- Use the existing `Make(...)` helper pattern
- Severity must match STATE_SPECIFICATIONS.md exactly
- Criteria must match STATE_SPECIFICATIONS.md exactly
- Insert at the correct position in the priority chain (AR-02)

### 2. Add criteria case to the dashboard filter method
File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Add one `case "[CODE]"` to the existing `[BoType]Criteria(...)` switch
- CriteriaOperator logic must mirror the evaluator branch exactly (AR-03)

### 3. Add state row to the dashboard section definition
File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Add one `StateDef` row for `[CODE]` in the correct `SectionDef` inside `SectionDefs`
- Source = `"BO"`, Severity matches spec

### 4. Update spec
File: Docs/STATE_SPECIFICATIONS.md
- Change Status from `Planned` → `Implemented` for [STATE_ID]
- Update Implementation Summary counts
- Add a Change Log row

### 5. Create test scenario
File: Visa2026.DataImporter/data.yaml
- Read SCENARIO_GUIDE.md to determine the next available scenario order number and naming conventions
- Add a new scenario that seeds exactly the minimum data required to put at least one record into state `[CODE]`
- Choose an anchor that uniquely identifies this scenario's seed data (idempotent re-run safe)
- Scenario name convention: `[STATE_ID] [Human readable description]`  (e.g. `V-02 ExpiringSoon`)
- After seeding, the dashboard count for `[CODE]` must show ≥ 1

File: Docs/STATE_SPECIFICATIONS.md
- Add `Test Scenario` row to the [STATE_ID] state table: value = scenario name from data.yaml

## Conventions
- **AR-01:** Single BO type only — if criteria need a second BO type, use Template B instead
- **AR-02:** Insert new branch at the correct priority position in the evaluator chain
- **AR-03:** CriteriaOperator must return exactly the same records as the evaluator branch
- StateSeverity enum: None, Info, Warning, Critical, Breach
- BoType strings for dashboard count key (must match exactly):
    Visa → "Visa" | Passport → "Passport" | WorkPermitItem → "WorkPermit"
    EmployeeContract → "Employee Contract" | MedicalRecord → "Medical Record" | AddressOfResidence → "Address"
- Count key format: "{BoType}|{StateCode}"
- Settings: `StateEvaluationSettings.FromSystemSettings(SystemSettings.GetInstance(space))`

## Done when
- [ ] Evaluator branch returns correct code and severity per spec criteria
- [ ] Dashboard criteria case returns same records as evaluator branch
- [ ] State row visible in dashboard with correct Source=BO and Severity badge
- [ ] STATE_SPECIFICATIONS.md updated to Implemented
- [ ] No other states broken (priority order preserved)
- [ ] Test scenario added to data.yaml — dashboard count shows ≥ 1 after seeding
- [ ] STATE_SPECIFICATIONS.md Test Scenario field updated with scenario name
```

---

---

## TEMPLATE B — Implement a New SQL State (View-based)

> Use when: the state is sourced from `SQL` in `STATE_SPECIFICATIONS.md` and Status is `Planned`.
> Also use when criteria span more than one BO type (AR-01).
>
> **Two scenarios:**
> - **Section Status BO already exists** → add one `CASE` branch to the existing SQL view. No new files.
> - **Section has no Status BO yet** → one-time section setup first, then add the state branch.

```
Before starting, read the following files in full:
- Docs/STATE_SPECIFICATIONS.md                   (state criteria, severity, participants)
- Docs/IMPLEMENT_STATE_PROMPT.md                 (this file — patterns and conventions)
- Visa2026.DataImporter/SCENARIO_GUIDE.md        (scenario numbering, anchor patterns, sheet structure)

## Task
Implement SQL state **[STATE_ID]**. The only difference between states is their criteria —
a SQL state = one CASE branch added to the section's existing Status view.

## State to implement
State ID:    [STATE_ID]
Section:     [SECTION]
State code:  [CODE]
Status BO:   [ProcessName]Status  (e.g. VisaExtensionStatus)
Status view: View_[ProcessName]Status

---

## SCENARIO 1 — Section Status BO already exists (most common, no new files)

### 1. Add CASE branch to the SQL view
File: Docs/SqlViews/View_[ProcessName]Status.sql  +  apply via SQL migration
- Add one `CASE WHEN [criteria] THEN [ApplicationState ID for CODE]` branch
- Criteria must match STATE_SPECIFICATIONS.md exactly
- Insert at the correct priority position within the CASE expression

### 2. Add state row to the dashboard section definition
File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Add one `StateDef` row for `[CODE]` in the correct `SectionDef`
- Source = `"SQL"`, Severity matches spec

### 3. Update spec
File: Docs/STATE_SPECIFICATIONS.md
- Change Status → `Implemented`, update Summary counts, add Change Log row

---

## SCENARIO 2 — First-time section setup (create components once, then follow Scenario 1)

### A. SQL view scripts
Files: Docs/SqlViews/View_[ProcessName]Status.sql
       Docs/SqlViews/View_[ProcessName]Tracking.sql
- Status view: one row per person/application; `CurrentStateID` set by CASE expression
  Required columns: ID (Guid PK), PersonID, [document FK], CurrentStateID (FK → ApplicationState),
  ApplicationNumber, ApplicationDate, StatusDate, StatusDescription, DaysRemaining
- Tracking view: one row per state transition (history log)
- Apply both via EF Core migration using `migrationBuilder.Sql(...)`

### B. Status and Tracking BO classes
Files: Visa2026.Module/BusinessObjects/[ProcessName]Status.cs
       Visa2026.Module/BusinessObjects/[ProcessName]Tracking.cs
- Copy VisaExtensionStatus.cs / VisaExtensionTracking.cs exactly
- Adjust FK navigation properties for this section's document BO

### C. DbContext registration
File: Visa2026.Module/BusinessObjects/Visa2026DbContext.cs
- Add `public DbSet<[ProcessName]Status> [ProcessName]Status { get; set; }`
- Add `public DbSet<[ProcessName]Tracking> [ProcessName]Tracking { get; set; }`
- OnModelCreating: `b.HasKey(t => t.ID); b.ToView("View_[ProcessName]Status");` for both

### D. Dashboard wiring
File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- LoadData(): query `[ProcessName]Status` grouped by `CurrentState.Name`, merge into `_counts`
- OpenFilteredList(): add case for this section — navigate to `[ProcessName]Status_ListView`
  filtered by `CurrentState`

### E. Then follow Scenario 1 steps 1–3 for the specific state

---

## Step 4 (both scenarios) — Create test scenario
File: Visa2026.DataImporter/data.yaml
- Read SCENARIO_GUIDE.md to determine the next available scenario order number and naming conventions
- Add a new scenario that seeds exactly the minimum data required to put at least one record into state `[CODE]`
  (i.e. seed a document/application whose dates/fields cause the SQL view CASE branch to select this state)
- Choose an anchor that uniquely identifies this scenario's seed data (idempotent re-run safe)
- Scenario name convention: `[STATE_ID] [Human readable description]`  (e.g. `V-09 ExtensionApproved`)
- After seeding, the dashboard count for `[CODE]` must show ≥ 1

File: Docs/STATE_SPECIFICATIONS.md
- Add `Test Scenario` row to the [STATE_ID] state table: value = scenario name from data.yaml

---

## Conventions
- **AR-01:** Cross-BO criteria → SQL view, never BO evaluator
- View naming: `View_[ProcessName]Status`, `View_[ProcessName]Tracking`
- State codes must match `ApplicationState` name values in the lookup table exactly
- Once implemented, SQL state rows are clickable (same as BO states)
- Count of 0 shows as "0" — "—" is only for Planned (unimplemented) states

## Done when
- [ ] CASE branch in SQL view returns correct records per spec criteria
- [ ] Dashboard count shows a number (not "—")
- [ ] Dashboard state row navigates to filtered Status ListView on click
- [ ] STATE_SPECIFICATIONS.md updated to Implemented
- [ ] No existing state counts affected
- [ ] Test scenario added to data.yaml — dashboard count shows ≥ 1 after seeding
- [ ] STATE_SPECIFICATIONS.md Test Scenario field updated with scenario name
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

## Step 3 — Verify or update test scenario
File: Visa2026.DataImporter/data.yaml
- Check whether the existing test scenario for [STATE_ID] still seeds data that lands in this state
- If the criteria change makes the old seed data no longer match → update the scenario or add a new one
- Re-seed and verify the dashboard count for `[CODE]` shows ≥ 1

File: Docs/STATE_SPECIFICATIONS.md
- Update the Test Scenario field for [STATE_ID] if the scenario name changed

## Done when
- [ ] STATE_SPECIFICATIONS.md reflects the new criteria (update spec before touching code)
- [ ] Evaluator / SQL view produces correct results for the new criteria
- [ ] Dashboard criteria method (BO) or count key (SQL) matches the updated logic
- [ ] Severity badge color on dashboard is correct if severity changed
- [ ] Change Log in STATE_SPECIFICATIONS.md updated
- [ ] No other states are broken by the change
- [ ] Test scenario still produces ≥ 1 count for [CODE] after re-seeding
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

---

## TEMPLATE E — Remove an Existing State

> Use when: a state is no longer needed and must be fully removed from the dashboard and code.
> **Remove exactly the one branch for this state. Do not touch any other state or surrounding code.**

```
Before starting, read the following files in full:
- Docs/STATE_SPECIFICATIONS.md                   (confirm the state to remove and its source)
- Docs/IMPLEMENT_STATE_PROMPT.md                 (this file — patterns and conventions)

## Task
Remove state **[STATE_ID]** from the dashboard and all implementation files.
Remove only the one branch for this state — no structural changes, no refactoring.

## State to remove
State ID:   [STATE_ID]
State code: [CODE]
Source:     [BO / SQL]

## Changes

### If Source = BO:
File: Visa2026.Module/Services/StateEvaluation/Evaluators/[BoType]StateEvaluator.cs
- Remove the `if/else` branch for `[CODE]`
- Verify the remaining priority chain is still correct after removal

File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Remove the `case "[CODE]"` from the `[BoType]Criteria(...)` switch
- Remove the `StateDef` row for `[CODE]` from `SectionDefs`

### If Source = SQL:
File: Docs/SqlViews/View_[ProcessName]Status.sql  +  apply via SQL migration
- Remove the `CASE WHEN ... THEN [ApplicationState ID for CODE]` branch
- Verify remaining CASE branches and their priority are still correct

File: Visa2026.Blazor.Server/Components/StateDashboardComponent.razor
- Remove the `StateDef` row for `[CODE]` from `SectionDefs`

### Spec update
File: Docs/STATE_SPECIFICATIONS.md
- Remove the [STATE_ID] state entry entirely
- Update Implementation Summary counts
- Update Implementation Summary counts for the section
- Add a Change Log row: "YYYY-MM-DD — [STATE_ID] removed. Reason: [reason]"

### Test scenario (optional)
File: Visa2026.DataImporter/data.yaml
- The test scenario for this state can remain (it seeds valid data for other purposes)
- Or remove it if it has no value without this state — document the decision in the Change Log

## Done when
- [ ] Evaluator branch (BO) or CASE branch (SQL) for [CODE] is removed
- [ ] Dashboard row and criteria case for [CODE] are removed
- [ ] STATE_SPECIFICATIONS.md entry removed, counts updated, Change Log updated
- [ ] No other states are broken — counts for all remaining states are unchanged
```

---

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
| `Visa2026.DataImporter/SCENARIO_GUIDE.md` | Scenario numbering, anchor patterns, sheet columns — read before writing data.yaml scenarios |
| `Visa2026.DataImporter/data.yaml` | Seed scenarios — one scenario per state test, append only |

**SQL State pattern — reference implementations:**

| File | Purpose |
|---|---|
| `Visa2026.Module/BusinessObjects/VisaExtensionStatus.cs` | Canonical SQL status BO — copy this pattern |
| `Visa2026.Module/BusinessObjects/VisaExtensionTracking.cs` | Canonical SQL tracking BO — copy this pattern |
| `Visa2026.Module/BusinessObjects/Visa2026DbContext.cs` | Add `DbSet<>` + `b.ToView(...)` here for new status BOs |
| `Visa2026.Module/BusinessObjects/LookupBusinessObjects.cs` | `ApplicationState` lookup — state names must match codes in spec |

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
Planned → In Progress → Implemented          (Templates D → A or B)
Planned → Pending                            (if blocked — note dependency)
Implemented → [criteria/severity change]     (Template C; status stays Implemented)
Implemented → [removed]                      (Template E; entry deleted from spec)
```

## Quick Reference — Architecture Rules

| Rule | Summary |
|---|---|
| **AR-01** | If criteria involve > 1 BO type → Source must be SQL. Use Template B. Update spec Source before coding. |
| **AR-02** | Evaluator state branches must follow a fixed priority order. Insert new states at the correct position. |
| **AR-03** | Dashboard `*Criteria(...)` method must return exactly the same records as the evaluator for that state code. |