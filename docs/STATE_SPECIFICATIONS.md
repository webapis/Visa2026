# State Dashboard — State Specifications

> **Purpose:** Single source of truth for every state shown on the State Dashboard.  
> All dashboard UI, BO evaluators, and SQL views must conform to the definitions here.  
> AI assistants and developers should read this file before implementing or modifying any state logic.
>
> **This is a living document.** States are created, updated, and deleted throughout the life of the project.
> Every change — add, modify, remove — must use the same design, patterns, and file structure as every other state.
> Strict consistency is what makes future changes safe and predictable.
>
> **Criteria status:** Criteria for `Planned` states are **drafts** — not confirmed by the domain expert.
> Before implementing any Planned state, present the draft criteria to the user and get explicit confirmation.
> Never write evaluator or SQL code against unconfirmed criteria.

---

## Status Codes

| Code | Meaning |
|---|---|
| `Implemented` | Criteria are coded and active in production |
| `In Progress` | Currently being implemented |
| `Planned` | Spec defined; implementation not started |
| `Pending` | Blocked on a dependency (noted in state entry) |

---

## State Entry Fields

Each state entry contains a table with the following fields:

| Field | Description |
|---|---|
| Code | State code string — must match `ApplicationState` name value exactly |
| Severity | One of: `Critical`, `Warning`, `Info`, `Healthy`, `Archived` |
| Source | `BO` (evaluator) or `SQL` (view-backed) |
| Status | One of the Status Codes above |
| Dashboard link | What the row opens when clicked |
| Test Scenario | Scenario name in `data.yaml` that seeds data exercising this state. `—` = not yet created. |

The `Test Scenario` field is added when a scenario is created (Step 5 of Templates A/B).
After seeding the scenario, the dashboard count for this state must show ≥ 1.

---

## Implementation Summary

| Section | Total | Implemented | In Progress | Planned | Pending |
|---|---|---|---|---|---|
| Visa States | 23 | 23 | 0 | 0 | 0 |
| Registration States | 14 | 4 | 0 | 10 | 0 |
| Passport States | 5 | 5 | 0 | 0 | 0 |
| Medical Record States | 5 | 4 | 0 | 1 | 0 |
| Invitation States | 16 | 0 | 0 | 16 | 0 |
| Work Permit States | 16 | 7 | 0 | 9 | 0 |
| Employee Contract States | 4 | 4 | 0 | 0 | 0 |
| **TOTAL** | **83** | **47** | **0** | **36** | **0** |

---

## Source Types

| Badge | Meaning |
|---|---|
| `BO` | Evaluated in C# via a `*StateEvaluator` class — one file per BO type, all states inside it |
| `SQL` | Queried from a SQL Server view — one view per section, all states inside it |

---

## Component Model — Per Section, Not Per State

> **The only difference between states is their criteria.**  
> All other structure — evaluator files, view-backed BOs, dashboard wiring — is shared within a section.  
> Adding a new state means adding one branch (criteria) to an existing evaluator or SQL view — not creating new files.

### BO Section — one evaluator file covers all states for that BO type

| Component | File | All states share it via |
|---|---|---|
| Evaluator | `Evaluators/[BoType]StateEvaluator.cs` | One `if/else` branch per state |
| Dashboard filter | `StateDashboardComponent.razor` · `[BoType]Criteria(...)` | One `switch` case per state |
| Dashboard list entry | `StateDashboardComponent.razor` · `SectionDefs` | One `StateDef` row per state |

### SQL Section — one set of components covers all states for that process

| Component | File | All states share it via |
|---|---|---|
| Status SQL view script | `Docs/SqlViews/View_[ProcessName]Status.sql` | One `CASE WHEN` branch per state |
| Tracking SQL view script | `Docs/SqlViews/View_[ProcessName]Tracking.sql` | Joined to Status view |
| Status BO | `BusinessObjects/[ProcessName]Status.cs` | One class, `CurrentState` FK selects the state |
| Tracking BO | `BusinessObjects/[ProcessName]Tracking.cs` | One class, history rows |
| DbContext | `BusinessObjects/Visa2026DbContext.cs` | `DbSet<>` + `b.ToView(...)` — added once per section |
| Dashboard count | `StateDashboardComponent.razor` · `LoadData()` | Group by `CurrentState` — one query covers all states |
| Dashboard navigation | `StateDashboardComponent.razor` · `OpenFilteredList()` | Filter by `CurrentState` — one case per section |
| Dashboard list entry | `StateDashboardComponent.razor` · `SectionDefs` | One `StateDef` row per state |

### Section → Component Map

| Section | BO type / Process | Evaluator / Status BO | SQL View | Status |
|---|---|---|---|---|
| Visa (BO states) | `Visa` | `VisaStateEvaluator` | — | ✅ Implemented |
| Visa (SQL states) | `VisaExtension` | `VisaExtensionStatus` | `View_VisaExtensionStatus` | ✅ Implemented |
| Visa (Transfer states) | `VisaTransfer` | `VisaTransferStatus` | `View_VisaTransferStatus` | ✅ Implemented |
| Registration (BO) | `AddressOfResidence` | `AddressOfResidenceStateEvaluator` | — | ✅ Implemented |
| Registration (SQL) | `Registration` | `RegistrationStatus` | `View_RegistrationStatus` | ❌ Planned |
| Passport | `Passport` | `PassportStateEvaluator` | — | ✅ Implemented |
| Medical Record (BO) | `MedicalRecord` | `MedicalRecordStateEvaluator` | — | ✅ Implemented |
| Medical Record (SQL) | `MedicalRenewal` | `MedicalRenewalStatus` | `View_MedicalRenewalStatus` | ❌ Planned |
| Invitation | `Invitation` | `InvitationStatus` | `View_InvitationStatus` | ❌ Planned |
| Work Permit (BO) | `WorkPermitItem` | `WorkPermitItemStateEvaluator` | — | ✅ Implemented |
| Work Permit (SQL) | `WorkPermitExtension` | `WorkPermitExtensionStatus` | `View_WorkPermitExtensionStatus` | ✅ Exists (needs dashboard wiring) |
| Employee Contract | `EmployeeContract` | `EmployeeContractStateEvaluator` | — | ✅ Implemented |

**State code mapping:** `CurrentState` is an FK to the `ApplicationState` lookup table.
State codes in this document must match `ApplicationState` name values exactly.

---

## Architecture Rules

These rules are binding. Do not override them without a documented reason in the Change Log.

### AR-01 · Cross-BO states must always use SQL

> **If a state's criteria involve more than one Business Object type, its Source must be `SQL` — never `BO`.**

**Why:**
- BO evaluators operate on a single object instance. Reaching across to another BO type causes N+1 queries at scale.
- SQL joins across tables are the correct tool for cross-entity aggregation.
- Keeps evaluators single-responsibility and independently testable.

**Examples of cross-BO states → SQL:**
- A state that depends on both `Visa` and `AddressOfResidence` properties → SQL
- A state that compares two records of the same type (e.g. previous vs current `MedicalRecord`) → SQL
- A state that reads from an external process/workflow table alongside a BO → SQL

**Examples of single-BO states → BO evaluator:**
- A state that reads only `Visa.ExpirationDate`, `Visa.IsActive`, `Visa.IsCancelled` → BO
- A state that reads only `Passport.ExpirationDate`, `Passport.IsActive` → BO

### AR-02 · Evaluator priority order must be preserved

Each evaluator checks states in a fixed priority order (e.g. Cancelled before Expired before ExpiringSoon before Active).
When adding a new BO state, insert it at the correct position in the priority chain — do not append blindly at the end.
Document the intended priority order in the evaluator file header if it is non-obvious.

### AR-03 · CriteriaOperator must mirror evaluator logic exactly

The `*Criteria(...)` switch method in `StateDashboardComponent.razor` produces the ListView filter for a state.
It must return exactly the same record set as the evaluator for that state code.
Any divergence causes the dashboard count to differ from the filtered list count — this is a bug.

---

## Severity Levels

| Level | Color | Meaning |
|---|---|---|
| `Critical` | Red `#f8d7da / #842029` | Compliance breach or cancellation — act immediately |
| `Warning` | Amber `#fff3cd / #664d03` | Deadline approaching — act soon |
| `Info` | Blue `#cfe2ff / #084298` | Notable state — monitor, no urgent action |
| `Healthy` | Green `#d1e7dd / #0f5132` | Normal active state — no action needed |
| `Archived` | Gray `#e9ecef / #6c757d` | Inactive / historical — reference only |

---

---

# VISA STATES

Participant BO: `Visa`  
Evaluator: `VisaStateEvaluator` (BO states) | SQL View: `vw_VisaProcessStates` (planned)

---

### V-01 · Active

| Field | Value |
|---|---|
| Code | `Active` |
| Severity | Healthy |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Visa_ListView` filtered to active visas |

**Criteria**
- `Visa.IsActive = true`
- `Visa.IsCancelled = false`
- `Visa.IsExtended = false`
- `Visa.IsChanged = false`
- `Visa.ExpirationDate > Today + SystemSettings.DefaultExpiringSoonDays`

**Action required:** None.

---

### V-02a · Expiring Soon (On Extension)

| Field | Value |
|---|---|
| Code | `ExpiringSoon` |
| Severity | Info |
| Source | SQL (cross-BO: Visa + VisaExtensionStatus view) |
| Status | **Implemented** |
| Dashboard link | Opens `VisaExtensionStatus_ListView` filtered |

**Criteria**
- `Visa.IsActive = true`, `IsCancelled = false`, `IsExtended = false`, `ExtensionRequired = true`
- `Visa.ExpirationDate` within the expiring-soon window (`>= Today`, `<= Today + DefaultExpiringSoonDays`)
- `VisaExtensionStatus.CurrentState.Code` is not null AND not in (`PROCESS_CANCELLED`, `1_REVIEW_REJECTED`, `2_REVIEW_REJECTED`, `PROCESS_REJECTED`)

**Action required:** Monitor — extension application is in progress.

---

### V-02b · Expiring Soon (Extension Not Required)

| Field | Value |
|---|---|
| Code | `ExpiringSoonNotRequired` |
| Severity | Info |
| Source | BO + SQL count |
| Status | **Implemented** |
| Dashboard link | Opens `Visa_ListView` filtered |

**Criteria**
- `Visa.IsActive = true`, `IsCancelled = false`
- `Visa.ExpirationDate` within the expiring-soon window
- `Visa.ExtensionRequired = false` (user has manually opted out — employee leaving, contract ending, etc.)

**Implementation:** `ExtensionRequired` is a new `bool` property on `Visa`, defaulting to `true`. Evaluator returns `ExpiringSoonNotRequired` with `Info` severity when this flag is false and the visa is expiring soon. Dashboard count uses a direct LINQ/SQL query.

**Action required:** None — suppressed by user. Verify employee departure is confirmed.

---

### V-02c · Expiring Soon (Extension Cancelled)

| Field | Value |
|---|---|
| Code | `ExpiringSoon` |
| Severity | Warning |
| Source | SQL (cross-BO: Visa + Application) |
| Status | **Implemented** |
| Dashboard link | Opens `Visa_ListView` filtered |

**Criteria**
- `Visa.IsActive = true`, `IsCancelled = false`, `IsExtended = false`, `ExtensionRequired = true`
- `Visa.ExpirationDate` within the expiring-soon window
- An `ApplicationItem.CurrentVisa = this Visa` exists where `Application.ApplicationType` ∈ (`App_Cancel_Visa_Ext`, `App_Cancel_Visa_and_WP_Ext`) AND `Application.IsDeleted = false`

**Action required:** The extension cancellation application exists. Verify if the visa needs a new extension application or if the employee is departing.

---

### V-02d · Expiring Soon (Ext. Rejected)

| Field | Value |
|---|---|
| Code | `ExpiringSoonExtRejected` |
| Severity | Warning |
| Source | SQL (cross-BO: Visa + VisaExtensionStatus view) |
| Status | **Implemented** |
| Dashboard link | Opens `VisaExtensionStatus_ListView` filtered |

**Criteria**
- `Visa.IsActive = true`, `IsCancelled = false`, `IsExtended = false`, `ExtensionRequired = true`
- `Visa.ExpirationDate` within the expiring-soon window
- `VisaExtensionStatus.CurrentState.Code` ∈ (`1_REVIEW_REJECTED`, `2_REVIEW_REJECTED`, `PROCESS_REJECTED`)

**Exclusion from V-02a:** V-02a (On Extension) explicitly excludes all three rejection codes so a visa never appears in both states simultaneously.

**Action required:** The extension application was rejected. A new extension application must be submitted or departure arrangements made.

---

### V-03 · Extended

| Field | Value |
|---|---|
| Code | `Extended` |
| Severity | Info |
| Source | BO |
| Status | **Removed from dashboard** — superseded by V-15 (Extension Completed) |

**Note:** The `IsExtended` flag and evaluator state are retained on the Visa BO for row coloring and query filtering. The dashboard row was removed because V-15 (Extension Completed) captures the same fact from the application perspective with richer data.

---

### V-04 · Changed

| Field | Value |
|---|---|
| Code | `Changed` |
| Severity | Info |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Visa_ListView` filtered |

**Criteria**
- `Visa.IsActive = true`
- `Visa.IsCancelled = false`
- `Visa.IsChanged = true`
- `Visa.IsExtended = false`

**Action required:** Verify change details are recorded correctly.

---

### V-05 · Cancelled

| Field | Value |
|---|---|
| Code | `Cancelled` |
| Severity | Critical |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Visa_ListView` filtered |

**Criteria**
- `Visa.IsCancelled = true`

**Action required:** Investigate reason for cancellation. Employee may need a new visa application.

---

### V-06 · Expired

| Field | Value |
|---|---|
| Code | `Expired` |
| Severity | Critical |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Visa_ListView` filtered |

**Criteria**
- `Visa.IsActive = true`
- `Visa.IsCancelled = false`
- `Visa.ExpirationDate < Today`

**Action required:** Employee is non-compliant. Immediate renewal or departure required.

---

### V-07 · Archived

| Field | Value |
|---|---|
| Code | `Archived` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Visa_ListView` filtered |

**Criteria**
- `Visa.IsActive = false`
- `Visa.IsCancelled = false`

**Action required:** None — historical record.

---

### V-08 · No Visa

| Field | Value |
|---|---|
| Code | `NoVisa` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Visa_ListView` (unfiltered — no active visa record exists) |

**Criteria**
- No `Visa` record exists for the person, or all records are archived/cancelled.

**Action required:** Verify whether employee requires a visa.

---

### V-09a · Submitted to Ministry 1

| Field | Value |
|---|---|
| Code | `SubmittedToMinistry1` |
| Severity | Info |
| Source | SQL |
| Status | **Implemented** |
| ApplicationState code | `1_REVIEW_STARTED` |
| ApplicationTypes | `App_Visa_Ext`, `App_Visa_Ext_According_to_WP`, `App_Visa_Ext_FM`, `App_Visa_and_WP_Ext` |
| Dashboard link | Opens `VisaExtensionStatus_ListView` filtered by `CurrentState.Code = 1_REVIEW_STARTED` |

**Criteria**
- `Application.ApplicationType.Code` IN (`App_Visa_Ext`, `App_Visa_Ext_According_to_WP`, `App_Visa_Ext_FM`, `App_Visa_and_WP_Ext`)
- `Application.IsDeleted = false`
- Latest `ApplicationProgress.State.Code = 1_REVIEW_STARTED`

**Action required:** Monitor — await 1st ministry response.

---

### V-09b · Submitted to Ministry 2

| Field | Value |
|---|---|
| Code | `SubmittedToMinistry2` |
| Severity | Info |
| Source | SQL |
| Status | **Implemented** |
| ApplicationState code | `2_REVIEW_STARTED` |
| ApplicationTypes | `App_Visa_Ext`, `App_Visa_Ext_According_to_WP`, `App_Visa_Ext_FM`, `App_Visa_and_WP_Ext` |
| Dashboard link | Opens `VisaExtensionStatus_ListView` filtered by `CurrentState.Code = 2_REVIEW_STARTED` |

**Criteria**
- `Application.ApplicationType.Code` IN (`App_Visa_Ext`, `App_Visa_Ext_According_to_WP`, `App_Visa_Ext_FM`, `App_Visa_and_WP_Ext`)
- `Application.IsDeleted = false`
- Latest `ApplicationProgress.State.Code = 2_REVIEW_STARTED`

**Action required:** Monitor — await 2nd ministry response.

---

### V-10a · Ministry 1 Approved

| Field | Value |
|---|---|
| Code | `1_REVIEW_APPROVED` |
| Severity | Healthy |
| Source | SQL |
| Status | **Implemented** |
| Depends on | `View_VisaExtensionStatus` (`CurrentState.Code`) |

**Criteria**
- `CurrentState.Code = '1_REVIEW_APPROVED'`
- Ministry 1 approved; awaiting Ministry 2 submission

**Action required:** Submit to Ministry 2.

---

### V-10b · Ministry 2 Approved

| Field | Value |
|---|---|
| Code | `2_REVIEW_APPROVED` |
| Severity | Healthy |
| Source | SQL |
| Status | **Implemented** |
| Depends on | `View_VisaExtensionStatus` (`CurrentState.Code`) |

**Criteria**
- `CurrentState.Code = '2_REVIEW_APPROVED'`
- Both ministries approved; ready to start processing

**Action required:** Proceed to visa processing (`PROCESS_STARTED`).

---

### V-11a · Ministry 1 Rejected

| Field | Value |
|---|---|
| Code | `1_REVIEW_REJECTED` |
| Severity | Critical |
| Source | SQL |
| Status | **Implemented** |
| Depends on | `View_VisaExtensionStatus` (`CurrentState.Code`) |

**Criteria**
- `CurrentState.Code = '1_REVIEW_REJECTED'`
- Ministry 1 rejected; application never reached Ministry 2

**Action required:** Review rejection reason. Resubmit or escalate.

---

### V-11b · Ministry 2 Rejected

| Field | Value |
|---|---|
| Code | `2_REVIEW_REJECTED` |
| Severity | Critical |
| Source | SQL |
| Status | **Implemented** |
| Depends on | `View_VisaExtensionStatus` (`CurrentState.Code`) |

**Criteria**
- `CurrentState.Code = '2_REVIEW_REJECTED'`
- Ministry 1 passed; Ministry 2 rejected

**Action required:** Review rejection reason. Resubmit or escalate.

---

### V-12 · Process Started

| Field | Value |
|---|---|
| Code | `PROCESS_STARTED` |
| Severity | Info |
| Source | SQL |
| Status | **Implemented** |
| Depends on | `View_VisaExtensionStatus` (`CurrentState.Code`) |

**Criteria**
- `CurrentState.Code = 'PROCESS_STARTED'`
- Both ministry approvals received; visa processing has begun

**Action required:** Monitor.

---

### V-13 · Process Cancelled

| Field | Value |
|---|---|
| Code | `PROCESS_CANCELLED` |
| Severity | Warning |
| Source | SQL |
| Status | **Implemented** |
| Depends on | `View_VisaExtensionStatus` (`CurrentState.Code`) |

**Criteria**
- `CurrentState.Code = 'PROCESS_CANCELLED'`
- Processing was cancelled after it started

**Action required:** Investigate reason; re-initiate if appropriate.

---

### V-14 · Process Rejected

| Field | Value |
|---|---|
| Code | `PROCESS_REJECTED` |
| Severity | Critical |
| Source | SQL |
| Status | **Implemented** |
| Depends on | `View_VisaExtensionStatus` (`CurrentState.Code`) |

**Criteria**
- `CurrentState.Code = 'PROCESS_REJECTED'`
- Processing authority rejected the application

**Action required:** Review rejection reason. Resubmit or escalate.

---

### V-15 · Extension Completed

| Field | Value |
|---|---|
| Code | `PROCESS_ISSUED` |
| Severity | Healthy |
| Source | SQL |
| Status | **Implemented** |
| Depends on | `View_VisaExtensionStatus` (`IssuedVisaID` column) |

**Criteria**
- `CurrentState.Code = 'PROCESS_ISSUED'`
- AND `IssuedVisaID IS NOT NULL` (a `Visa` exists where `Visa.IssuingApplicationItemId = ApplicationItem.ID`)

**Implementation:** `IssuedVisaID` added to `View_VisaExtensionStatus` via subquery; `VisaExtensionStatus.IssuedVisaID` property added to BO; dashboard count overrides group-by result with filtered query.

**Action required:** None — visa extension complete.

---

### V-16 · Transfer Initiated

| Field | Value |
|---|---|
| Code | `PROCESS_STARTED` |
| Severity | Info |
| Source | SQL |
| Status | **Implemented** |
| ApplicationState code | `PROCESS_STARTED` |
| ApplicationTypes | `App_Change_Passport` |
| Depends on | `View_VisaTransferStatus` (`CurrentState.Code`) |
| Dashboard link | Opens `VisaTransferStatus_ListView` filtered by `CurrentState.Code = PROCESS_STARTED` |
| Test Scenario | `VisaTransferInitiated` (scenario 038) |

**Criteria**
- `Application.ApplicationType.Code = App_Change_Passport`
- `Application.IsDeleted = false`
- Latest `ApplicationProgress.State.Code = PROCESS_STARTED`

**Action required:** Monitor transfer progress.

---

### V-17 · Transfer Completed

| Field | Value |
|---|---|
| Code | `PROCESS_ISSUED` |
| Severity | Healthy |
| Source | SQL |
| Status | **Implemented** |
| ApplicationState code | `PROCESS_ISSUED` |
| ApplicationTypes | `App_Change_Passport` |
| Depends on | `View_VisaTransferStatus` (`IssuedVisaID` column) |
| Dashboard link | Opens `VisaTransferStatus_ListView` filtered by `CurrentState.Code = PROCESS_ISSUED AND IssuedVisaID != null` |
| Test Scenario | `VisaTransferCompleted` + `VisaTransferCompletedLink` (scenarios 040 + 041) |

**Criteria**
- `CurrentState.Code = 'PROCESS_ISSUED'`
- AND `IssuedVisaID IS NOT NULL` (a `Visa` exists where `Visa.IssuingApplicationItemId = ApplicationItem.ID`)

**Implementation:** `IssuedVisaID` added to `View_VisaTransferStatus` via subquery; dashboard count overrides group-by with filtered query (same pattern as V-15).

**Action required:** Verify new visa details are recorded in system.

---

### V-18 · Transfer Cancelled

| Field | Value |
|---|---|
| Code | `PROCESS_CANCELLED` |
| Severity | Warning |
| Source | SQL |
| Status | **Implemented** |
| ApplicationState code | `PROCESS_CANCELLED` |
| ApplicationTypes | `App_Change_Passport` |
| Depends on | `View_VisaTransferStatus` (`CurrentState.Code`) |
| Dashboard link | Opens `VisaTransferStatus_ListView` filtered by `CurrentState.Code = PROCESS_CANCELLED` |
| Test Scenario | `VisaTransferRejected` (scenario 039) |

**Criteria**
- `Application.ApplicationType.Code = App_Change_Passport`
- `Application.IsDeleted = false`
- Latest `ApplicationProgress.State.Code = PROCESS_CANCELLED`

**Action required:** Investigate cancellation reason. Resubmit or escalate.

---

---

# REGISTRATION STATES

Participant BOs: `AddressOfResidence`, `Person`  
Evaluator: `AddressOfResidenceStateEvaluator` (BO states) | SQL View: `vw_RegistrationStates` (planned)  
**Note:** Registration = address registration (propiska/§9) process.

---

### R-01 · Address Active

| Field | Value |
|---|---|
| Code | `Active` |
| Severity | Healthy |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `AddressOfResidence_ListView` filtered |

**Criteria**
- `AddressOfResidence.IsActive = true`
- `AddressOfResidence.ExpirationDate > Today + SystemSettings.DefaultExpiringSoonDays`  
  *(or ExpirationDate is null — no expiry set)*

**Action required:** None.

---

### R-02 · Address Expiring Soon

| Field | Value |
|---|---|
| Code | `ExpiringSoon` |
| Severity | Warning |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `AddressOfResidence_ListView` filtered |

**Criteria**
- `AddressOfResidence.IsActive = true`
- `AddressOfResidence.ExpirationDate >= Today`
- `AddressOfResidence.ExpirationDate <= Today + SystemSettings.DefaultExpiringSoonDays`

**Action required:** Initiate re-registration or extension before expiry.

---

### R-03 · Address Expired

| Field | Value |
|---|---|
| Code | `Expired` |
| Severity | Critical |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `AddressOfResidence_ListView` filtered |

**Criteria**
- `AddressOfResidence.IsActive = true`
- `AddressOfResidence.ExpirationDate < Today`

**Action required:** Employee registration has lapsed — re-register immediately.

---

### R-04 · Address Archived

| Field | Value |
|---|---|
| Code | `Archived` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `AddressOfResidence_ListView` filtered |

**Criteria**
- `AddressOfResidence.IsActive = false`

**Action required:** None — historical record.

---

### R-05 · Check-in Requested

| Field | Value |
|---|---|
| Code | `CheckInRequested` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- Check-in (initial registration at address) application submitted to FMS
- Not yet processed

**Action required:** Monitor FMS processing.

---

### R-06 · Check-in Completed

| Field | Value |
|---|---|
| Code | `CheckInCompleted` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- FMS has confirmed check-in registration
- Registration stamp/document issued

**Action required:** None — store registration document.

---

### R-07 · Check-in Rejected

| Field | Value |
|---|---|
| Code | `CheckInRejected` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- FMS has rejected the check-in application

**Action required:** Investigate rejection reason and resubmit.

---

### R-08 · Check-out Requested

| Field | Value |
|---|---|
| Code | `CheckOutRequested` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- Deregistration (check-out) application submitted
- Not yet processed

**Action required:** Monitor.

---

### R-09 · Check-out Completed

| Field | Value |
|---|---|
| Code | `CheckOutCompleted` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- Deregistration confirmed by FMS

**Action required:** None — if employee moving address, initiate new check-in.

---

### R-10 · Re-registration Required

| Field | Value |
|---|---|
| Code | `ReRegistrationRequired` |
| Severity | Warning |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- Periodic re-registration deadline is approaching (law requires re-registration every N days)
- No re-registration application submitted yet

**Action required:** Submit re-registration application before deadline.

---

### R-11 · Re-registration Submitted

| Field | Value |
|---|---|
| Code | `ReRegistrationSubmitted` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- Re-registration application submitted
- Not yet approved

**Action required:** Monitor.

---

### R-12 · Re-registration Completed

| Field | Value |
|---|---|
| Code | `ReRegistrationCompleted` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- Re-registration confirmed by FMS

**Action required:** None — update `AddressOfResidence.ExpirationDate` with new registration expiry.

---

### R-13 · Registration Expired

| Field | Value |
|---|---|
| Code | `RegistrationExpired` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- Registration period has lapsed per FMS records (source of truth: SQL, not BO ExpirationDate)

**Action required:** Immediate re-registration required — legal compliance breach.

---

### R-14 · No Registration

| Field | Value |
|---|---|
| Code | `NoRegistration` |
| Severity | Archived |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_RegistrationStates` |

**Criteria**
- No active registration record exists in FMS data

**Action required:** Verify whether employee is legally required to be registered at this address.

---

---

# PASSPORT STATES

Participant BO: `Passport`  
Evaluator: `PassportStateEvaluator`

---

### P-01 · Active

| Field | Value |
|---|---|
| Code | `Active` |
| Severity | Healthy |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Passport_ListView` filtered |

**Criteria**
- `Passport.IsActive = true`
- `Passport.ExpirationDate > Today + SystemSettings.DefaultExpiringSoonDays`

**Action required:** None.

---

### P-02 · Expiring Soon

| Field | Value |
|---|---|
| Code | `ExpiringSoon` |
| Severity | Warning |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Passport_ListView` filtered |

**Criteria**
- `Passport.IsActive = true`
- `Passport.ExpirationDate >= Today`
- `Passport.ExpirationDate <= Today + SystemSettings.DefaultExpiringSoonDays`

**Action required:** Notify employee to renew passport. Note: many visas require passport validity 6 months beyond visa expiry.

---

### P-03 · Expired

| Field | Value |
|---|---|
| Code | `Expired` |
| Severity | Critical |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Passport_ListView` filtered |

**Criteria**
- `Passport.IsActive = true`
- `Passport.ExpirationDate < Today`

**Action required:** Employee cannot travel or renew visa with expired passport. Immediate renewal required.

---

### P-04 · Archived

| Field | Value |
|---|---|
| Code | `Archived` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Passport_ListView` filtered |

**Criteria**
- `Passport.IsActive = false`

**Action required:** None — historical record.

---

### P-05 · No Passport

| Field | Value |
|---|---|
| Code | `NoPassport` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `Passport_ListView` (no active record exists) |

**Criteria**
- No active `Passport` record exists for the person

**Action required:** Verify whether employee passport needs to be recorded in the system.

---

---

# MEDICAL RECORD STATES

Participant BO: `MedicalRecord`  
Evaluator: `MedicalRecordStateEvaluator` | SQL View: `vw_MedicalStates` (planned)

---

### M-01 · Active

| Field | Value |
|---|---|
| Code | `Active` |
| Severity | Healthy |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `MedicalRecord_ListView` filtered |

**Criteria**
- `MedicalRecord.IsActive = true`
- `MedicalRecord.ExpirationDate > Today + threshold`  
  *(threshold = % of duration from IssueDate, via `SystemSettings`)*

**Action required:** None.

---

### M-02 · Expiring Soon

| Field | Value |
|---|---|
| Code | `ExpiringSoon` |
| Severity | Warning |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `MedicalRecord_ListView` filtered |

**Criteria**
- `MedicalRecord.IsActive = true`
- Remaining days as % of total duration (IssueDate → ExpirationDate) is below threshold
- `MedicalRecord.ExpirationDate >= Today`

**Action required:** Schedule employee medical examination for renewal.

**Notes:** Uses percentage-based threshold, not a fixed day count. Configured in `SystemSettings`.

---

### M-03 · Expired

| Field | Value |
|---|---|
| Code | `Expired` |
| Severity | Critical |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `MedicalRecord_ListView` filtered |

**Criteria**
- `MedicalRecord.IsActive = true`
- `MedicalRecord.ExpirationDate < Today`

**Action required:** Medical certificate has lapsed — renewal required immediately for compliance.

---

### M-04 · Archived

| Field | Value |
|---|---|
| Code | `Archived` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `MedicalRecord_ListView` filtered |

**Criteria**
- `MedicalRecord.IsActive = false`

**Action required:** None — historical record.

---

### M-05 · Renewed

| Field | Value |
|---|---|
| Code | `Renewed` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_MedicalStates` |

**Criteria**
- Medical certificate was renewed (new certificate issued within valid overlap window of previous)
- New `MedicalRecord` record exists with `IsActive = true` and start date ≤ previous expiry + grace days

**Action required:** None — verify new record is correctly linked to employee.

---

---

# INVITATION STATES

Participant BO: `Invitation` *(not yet modelled as a BO)*  
Source: SQL only — all states planned  
SQL View: `vw_InvitationStates` (planned)

---

### I-01 · Draft

| Field | Value |
|---|---|
| Code | `Draft` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Invitation letter drafted but not yet submitted to ministry.  
**Action required:** Review and submit invitation package.

---

### I-02 · Submitted

| Field | Value |
|---|---|
| Code | `Submitted` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Invitation application submitted to GUVM. Awaiting acknowledgement.  
**Action required:** Monitor.

---

### I-03 · Under Review

| Field | Value |
|---|---|
| Code | `UnderReview` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Ministry has opened review of invitation application.  
**Action required:** Monitor.

---

### I-04 · Approved

| Field | Value |
|---|---|
| Code | `Approved` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Ministry has approved invitation. Visa application can now proceed.  
**Action required:** Initiate visa application process.

---

### I-05 · Rejected

| Field | Value |
|---|---|
| Code | `Rejected` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Ministry has rejected invitation application.  
**Action required:** Review rejection reason; resubmit with corrections or escalate.

---

### I-06 · Issued

| Field | Value |
|---|---|
| Code | `Issued` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Invitation letter/document issued by ministry. Not yet collected.  
**Action required:** Collect and forward to invitee.

---

### I-07 · Collected

| Field | Value |
|---|---|
| Code | `Collected` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Invitee has received invitation document.  
**Action required:** None — process complete. Monitor subsequent visa application.

---

### I-08 · Expired

| Field | Value |
|---|---|
| Code | `InvitationExpired` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Invitation validity period has passed without the invitee using it.  
**Action required:** Reapply for invitation if employee still needs to enter.

---

### I-09 · Cancelled

| Field | Value |
|---|---|
| Code | `InvitationCancelled` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Invitation was cancelled (by company, ministry, or invitee).  
**Action required:** Investigate reason; apply for new invitation if required.

---

### I-10 · Extension Requested

| Field | Value |
|---|---|
| Code | `ExtensionRequested` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Extension of invitation validity submitted to ministry.  
**Action required:** Monitor.

---

### I-11 · Extension Approved

| Field | Value |
|---|---|
| Code | `ExtensionApproved` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Ministry approved invitation extension.  
**Action required:** Update invitation record with new expiry date.

---

### I-12 · Extension Rejected

| Field | Value |
|---|---|
| Code | `ExtensionRejected` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Ministry rejected invitation extension.  
**Action required:** Reapply for new invitation or escalate.

---

### I-13 · Transfer Initiated

| Field | Value |
|---|---|
| Code | `TransferInitiated` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Transfer of invitation to a new sponsoring organisation initiated.  
**Action required:** Monitor transfer process.

---

### I-14 · Transfer Completed

| Field | Value |
|---|---|
| Code | `TransferCompleted` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Invitation transfer completed successfully.  
**Action required:** Verify new sponsor details are recorded.

---

### I-15 · Transfer Rejected

| Field | Value |
|---|---|
| Code | `TransferRejected` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |

**Criteria:** Invitation transfer rejected by ministry.  
**Action required:** Investigate; reapply if necessary.

---

### I-16 · No Invitation

| Field | Value |
|---|---|
| Code | `NoInvitation` |
| Severity | Archived |
| Source | SQL |
| Status | **Planned** |

**Criteria:** No active invitation record exists for the person.  
**Action required:** Verify whether an invitation is required for this employee's visa type.

---

---

# WORK PERMIT STATES

Participant BO: `WorkPermitItem`  
Evaluator: `WorkPermitItemStateEvaluator` (BO states) | SQL View: `vw_WorkPermitStates` (planned)

---

### W-01 · Active

| Field | Value |
|---|---|
| Code | `Active` |
| Severity | Healthy |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `WorkPermitItem_ListView` filtered |

**Criteria**
- `WorkPermitItem.IsActive = true`
- `WorkPermitItem.IsCancelled = false`
- `WorkPermitItem.IsExtended = false`
- `WorkPermitItem.ExpirationDate > Today + SystemSettings.DefaultExpiringSoonDays`

**Action required:** None.

---

### W-02 · Expiring Soon

| Field | Value |
|---|---|
| Code | `ExpiringSoon` |
| Severity | Warning |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `WorkPermitItem_ListView` filtered |

**Criteria**
- `WorkPermitItem.IsActive = true`
- `WorkPermitItem.IsCancelled = false`
- `WorkPermitItem.IsExtended = false`
- `WorkPermitItem.ExpirationDate >= Today`
- `WorkPermitItem.ExpirationDate <= Today + SystemSettings.DefaultExpiringSoonDays`

**Action required:** Initiate work permit renewal or extension process with ministry.

---

### W-03 · Extended

| Field | Value |
|---|---|
| Code | `Extended` |
| Severity | Info |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `WorkPermitItem_ListView` filtered |

**Criteria**
- `WorkPermitItem.IsActive = true`
- `WorkPermitItem.IsCancelled = false`
- `WorkPermitItem.IsExtended = true`

**Action required:** Monitor — extension recorded. Watch for expiry of extended permit.

---

### W-04 · Cancelled

| Field | Value |
|---|---|
| Code | `Cancelled` |
| Severity | Critical |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `WorkPermitItem_ListView` filtered |

**Criteria**
- `WorkPermitItem.IsCancelled = true`

**Action required:** Employee cannot legally work. Investigate and apply for new permit immediately.

---

### W-05 · Expired

| Field | Value |
|---|---|
| Code | `Expired` |
| Severity | Critical |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `WorkPermitItem_ListView` filtered |

**Criteria**
- `WorkPermitItem.IsActive = true`
- `WorkPermitItem.IsCancelled = false`
- `WorkPermitItem.ExpirationDate < Today`

**Action required:** Employee cannot legally work — compliance breach. Renewal required immediately.

---

### W-06 · Archived

| Field | Value |
|---|---|
| Code | `Archived` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `WorkPermitItem_ListView` filtered |

**Criteria**
- `WorkPermitItem.IsActive = false`
- `WorkPermitItem.IsCancelled = false`

**Action required:** None — historical record.

---

### W-07 · No Work Permit

| Field | Value |
|---|---|
| Code | `NoWorkPermit` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `WorkPermitItem_ListView` (no active record exists) |

**Criteria**
- No active `WorkPermitItem` record exists for the person

**Action required:** Verify whether employee legally requires a work permit.

---

### W-08 · Extension Submitted

| Field | Value |
|---|---|
| Code | `ExtensionSubmitted` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Extension application submitted to ministry. Awaiting decision.  
**Action required:** Monitor.

---

### W-09 · Extension Under Review

| Field | Value |
|---|---|
| Code | `ExtensionUnderReview` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Ministry opened review of extension application.  
**Action required:** Monitor.

---

### W-10 · Extension Approved

| Field | Value |
|---|---|
| Code | `ExtensionApproved` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Ministry approved work permit extension. New expiry date issued.  
**Action required:** Update `WorkPermitItem` with new expiry date and set `IsExtended = true`.

---

### W-11 · Extension Rejected

| Field | Value |
|---|---|
| Code | `ExtensionRejected` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Ministry rejected extension application.  
**Action required:** Review rejection reason. Apply for new permit or escalate.

---

### W-12 · Extension Issued

| Field | Value |
|---|---|
| Code | `ExtensionIssued` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Extended work permit document issued. Not yet collected.  
**Action required:** Collect document and update BO record.

---

### W-13 · Transfer Initiated

| Field | Value |
|---|---|
| Code | `TransferInitiated` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Work permit transfer to new employer/sponsor initiated.  
**Action required:** Monitor.

---

### W-14 · Transfer Completed

| Field | Value |
|---|---|
| Code | `TransferCompleted` |
| Severity | Healthy |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Transfer completed; permit now under new employer.  
**Action required:** Update sponsorship details in system.

---

### W-15 · Transfer Rejected

| Field | Value |
|---|---|
| Code | `TransferRejected` |
| Severity | Critical |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Work permit transfer rejected by ministry.  
**Action required:** Investigate reason. Reapply or escalate.

---

### W-16 · Ministry Submitted

| Field | Value |
|---|---|
| Code | `MinistrySubmitted` |
| Severity | Info |
| Source | SQL |
| Status | **Planned** |
| Depends on | SQL view `vw_WorkPermitStates` |

**Criteria:** Initial work permit application submitted to ministry. No decision yet.  
**Action required:** Monitor.

---

---

# EMPLOYEE CONTRACT STATES

Participant BO: `EmployeeContract`  
Evaluator: `EmployeeContractStateEvaluator`

---

### C-01 · Active

| Field | Value |
|---|---|
| Code | `Active` |
| Severity | Healthy |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `EmployeeContract_ListView` filtered |

**Criteria**
- `EmployeeContract.IsActive = true`
- `EmployeeContract.ExpirationDate > Today + threshold`  
  *(threshold = % of duration from ContractStartDate, via `SystemSettings`)*  
  OR `ExpirationDate` is null (open-ended contract)

**Action required:** None.

---

### C-02 · Expiring Soon

| Field | Value |
|---|---|
| Code | `ExpiringSoon` |
| Severity | Warning |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `EmployeeContract_ListView` filtered |

**Criteria**
- `EmployeeContract.IsActive = true`
- Remaining days as % of total duration (ContractStartDate → ExpirationDate) is below threshold
- `EmployeeContract.ExpirationDate >= Today`

**Action required:** Initiate contract renewal discussion with employee.

**Notes:** Uses percentage-based threshold. Configured in `SystemSettings`.

---

### C-03 · Expired

| Field | Value |
|---|---|
| Code | `Expired` |
| Severity | Critical |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `EmployeeContract_ListView` filtered |

**Criteria**
- `EmployeeContract.IsActive = true`
- `EmployeeContract.ExpirationDate < Today`

**Action required:** Contract has lapsed — renew or terminate employment relationship formally.

---

### C-04 · Archived

| Field | Value |
|---|---|
| Code | `Archived` |
| Severity | Archived |
| Source | BO |
| Status | **Implemented** |
| Dashboard link | Opens `EmployeeContract_ListView` filtered |

**Criteria**
- `EmployeeContract.IsActive = false`

**Action required:** None — historical record.

---

---

# Change Log

| Date | Change | Author |
|---|---|---|
| 2026-04-20 | Initial document created; all BO states documented with exact criteria; SQL states documented as Planned | AI / Developer |
| 2026-04-20 | Added Architecture Rules AR-01/AR-02/AR-03. Audited all 79 states: no Source corrections needed — all cross-BO planned states (R-10, M-05) were already classified SQL | AI / Developer |
| 2026-04-20 | Documented SQL State Architecture pattern — dual *Status/*Tracking view-backed BOs. VisaExtensionStatus and WorkPermitExtensionStatus already exist and cover V-09–V-19 and W-08–W-16. RegistrationStatus, InvitationStatus, MedicalRenewalStatus marked as planned | AI / Developer |
| 2026-04-20 | Revised component model: components are per section, not per state. A state = one criteria branch (if/else in evaluator, CASE in SQL view). No new files per state — only new files when a section is set up for the first time | AI / Developer |
| 2026-04-20 | Added State Entry Fields table documenting all fields including Test Scenario. Added scenario testing step (Step 5) to IMPLEMENT_STATE_PROMPT.md Templates A/B/C — each state implementation now ends with a data.yaml scenario that verifies the state visually on the dashboard | AI / Developer |
| 2026-04-20 | V-09 split into V-09a (SubmittedToMinistry1 / 1_REVIEW_STARTED) and V-09b (SubmittedToMinistry2 / 2_REVIEW_STARTED) — two ministries may be involved in visa extension approval. Total states updated to 80. View_VisaExtensionStatus fixed to filter by ApplicationType. | AI / Developer |