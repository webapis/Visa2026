# Business Object State Tracking Specification

This document defines the trackable Business Objects (BOs), their named states, and the conditions that determine each state. It serves as the canonical reference for notification logic, dashboard indicators, and workflow automation.

> **Temporal classification:** Each date-driven BO uses either a **`DaysRemaining`** (countdown to deadline) or **`DaysElapsed`** (time since an event) model. See **[`BO_STATE_TEMPORAL_TYPES.md`](BO_STATE_TEMPORAL_TYPES.md)** for the master registry, anchor dates, and officer-action mapping.

---

## Shared State Framework

Most date-bound BOs implement `IExpirationLogic`, which yields four base states computed by `ExpirationLogicHelper`. The **warning threshold** (when `Active` transitions to `ExpiringSoon`) is configured per object type in `SystemSettings`.

| State Name | Code | Base Condition |
|---|---|---|
| **Active** | `Active` | Current date is before the warning threshold |
| **Expiring Soon** | `ExpiringSoon` | Current date is within the warning threshold window before `ExpirationDate` |
| **Expired** | `Expired` | Current date is past `ExpirationDate` |
| **Archived** | `Archived` | `IsActive = false` (manually or system-deactivated) |

Domain-specific states are layered on top of the base states where applicable.

---

## Tracked Business Objects

---

### 1. Passport

**Purpose:** Official travel document tied to a `Person`. Triggers renewal workflows and blocks visa applications when expired.

| State Name | Code | Condition |
|---|---|---|
| **Active** | `Active` | `ExpirationDate` is beyond the warning threshold from today |
| **Expiring Soon** | `ExpiringSoon` | Today is within `N` days/% of `ExpirationDate` (threshold from `SystemSettings`) |
| **Expired** | `Expired` | Today ≥ `ExpirationDate` |
| **Archived** | `Archived` | `IsActive = false` — passport superseded by a newer one |

**Key fields:** `IssueDate`, `ExpirationDate`, `PassportType`, `Person`
**Suggested actions:**
- `ExpiringSoon` → notify passport owner and HR to initiate renewal
- `Expired` → block new visa/application submissions using this passport
- `Archived` → no action needed; retained for historical record

---

### 2. Visa

**Purpose:** Entry/residency authorization linked to a `Passport`. Expiry affects the person's legal right to stay.

| State Name | Code | Condition |
|---|---|---|
| **Active** | `Active` | `ExpirationDate` is beyond the warning threshold from today; `IsCancelled = false` |
| **Expiring Soon** | `ExpiringSoon` | Today is within the warning threshold window before `ExpirationDate`; `IsCancelled = false` |
| **Expired** | `Expired` | Today ≥ `ExpirationDate`; `IsCancelled = false` |
| **Cancelled** | `Cancelled` | `IsCancelled = true` |
| **Changed** | `Changed` | `IsChanged = true` — visa details amended after issuance |
| **Extended** | `Extended` | `IsExtended = true` — validity period prolonged |
| **Archived** | `Archived` | `IsActive = false` — superseded by a newer visa |

**Passport extension constraint:** A visa extension application cannot set a new expiration date beyond `Passport.ExpirationDate`. If `Passport.ExpirationDate ≤ Visa.ExpirationDate` (passport expires at or before the visa's current end), extension is **blocked** — the passport must be renewed first (see §1). This constraint applies to all visa extension types (`App_Visa_Ext`, `App_Visa_Ext_FM`, `App_Visa_and_WP_Ext`, `App_Visa_Ext_According_to_WP`).

**Key fields:** `StartDate`, `ExpirationDate`, `IsCancelled`, `IsChanged`, `IsExtended`, `Passport`, `Passport.ExpirationDate`, `VisaType`
**Suggested actions:**
- `ExpiringSoon` → alert HR and employee to begin renewal/extension process; verify `Passport.ExpirationDate` allows extension first
- `Expired` → flag person's record; block registration submissions
- `Cancelled` → notify relevant departments; initiate replacement process if needed

---

### 3. Work Permit Item

**Purpose:** Individual work authorization for an employee. Expiry makes the employee's work status non-compliant. Extension must be applied for 90 days before expiration, always together with the visa extension via `App_Visa_and_WP_Ext`.

**Precondition — scope:** Extension states apply only to `Person.CurrentWorkPermitItem` (`IsActive = true`). Archived work permit items are not evaluated.

| State Name | Code | Condition |
|---|---|---|
| **Active** | `Active` | `ExpirationDate > Today + 90 days`; `IsCancelled = false`; `IsExtended = false`; no extension application in progress |
| **Extension Application Required** | `ExtensionApplicationRequired` | `(Today + 90 days) ≥ ExpirationDate > Today` AND `IsCancelled = false` AND `IsExtended = false` AND no extension application submitted — within the 90-day window; must apply via `App_WP_Ext` (WP only) or `App_Visa_and_WP_Ext` (WP + visa together) |
| **Extension In Progress** | `ExtensionInProgress` | An `App_WP_Ext` or `App_Visa_and_WP_Ext` application exists for this person and its `CurrentState` has not yet reached a terminal state (Issued / Completed) |
| **Expiring Soon** | `ExpiringSoon` | Today is within the `SystemSettings` warning threshold before `ExpirationDate`; `IsCancelled = false`; `IsExtended = false`; no extension in progress — 90-day window was missed |
| **Expired** | `Expired` | Today ≥ `ExpirationDate`; `IsCancelled = false`; `IsExtended = false` |
| **Cancelled** | `Cancelled` | `IsCancelled = true` |
| **Changed** | `Changed` | `IsChanged = true` |
| **Extended** | `Extended` | `IsExtended = true` — extension approved via `App_Visa_and_WP_Ext` |
| **Archived** | `Archived` | `IsActive = false` |

**Key fields:** `StartDate`, `ExpirationDate`, `IsCancelled`, `IsChanged`, `IsExtended`, `Person`, `WorkPermit`, `Person.ApplicationItems` (for `App_Visa_and_WP_Ext` lookup)

**Passport extension constraint:** A work permit extension application cannot set a new expiration date beyond `Passport.ExpirationDate`. If `Passport.ExpirationDate ≤ WorkPermitItem.ExpirationDate` (passport expires at or before the work permit's current end), extension is **blocked** — the passport must be renewed first (see §1). This constraint applies to both `App_WP_Ext` and `App_Visa_and_WP_Ext`.

**Extension application type selection:**
- `App_WP_Ext` — extends the work permit only; use when the visa is already extended or being handled separately
- `App_Visa_and_WP_Ext` — extends both visa and work permit simultaneously; use when both need renewal at the same time

A visa-only extension (`App_Visa_Ext`, etc.) does **not** extend the work permit.

**Suggested actions:**
- `ExtensionApplicationRequired` → notify HR coordinator; use `App_Visa_and_WP_Ext` if visa also needs extending, otherwise `App_WP_Ext`
- `ExtensionInProgress` → monitor application progress with the ministry
- `ExpiringSoon` → escalate to HR; 90-day extension window was missed; urgent action required
- `Expired` → escalate to compliance officer; employee cannot legally work
- `Cancelled` → notify employee and manager; begin replacement permit process

---

### 4. Employee Contract

**Purpose:** Tracks the legal employment agreement for an employee. Expiry requires contract renewal or termination.

| State Name | Code | Condition |
|---|---|---|
| **Active** | `Active` | `ExpirationDate` is beyond the warning threshold |
| **Expiring Soon** | `ExpiringSoon` | Today is within the warning threshold window before `ExpirationDate` |
| **Expired** | `Expired` | Today ≥ `ExpirationDate` |
| **Archived** | `Archived` | `IsActive = false` — superseded by a new contract |

**Key fields:** `ContractStartDate`, `ExpirationDate`, `ValidityDuration`, `Person`, `PositionHistory`
**Suggested actions:**
- `ExpiringSoon` → alert HR to prepare contract renewal or termination paperwork
- `Expired` → escalate to HR manager; employee's legal employment status unclear

---

### 5. Medical Record

**Purpose:** Health clearance certificate required for residency and work permits. Expired records block dependent document renewals.

| State Name | Code | Condition |
|---|---|---|
| **Active** | `Active` | `ExpirationDate` is beyond the warning threshold |
| **Expiring Soon** | `ExpiringSoon` | Today is within the warning threshold window before `ExpirationDate` |
| **Expired** | `Expired` | Today ≥ `ExpirationDate` |
| **Archived** | `Archived` | `IsActive = false` |

**Key fields:** `IssueDate`, `ExpirationDate`, `ValidityDuration`, `Person`
**Suggested actions:**
- `ExpiringSoon` → notify employee to schedule medical examination
- `Expired` → block work permit and registration submissions that require a valid medical record

---

### 6. Invitation

**Purpose:** Official invitation letter batch for a set of persons. Expiry invalidates use of the invitation for entry.

| State Name | Code | Condition |
|---|---|---|
| **Active** | `Active` | `ExpirationDate` is beyond the warning threshold; `IsCancelled = false` |
| **Expiring Soon** | `ExpiringSoon` | Today is within the warning threshold window before `ExpirationDate`; `IsCancelled = false` |
| **Expired** | `Expired` | Today ≥ `ExpirationDate`; `IsCancelled = false` |
| **Cancelled** | `Cancelled` | `IsCancelled = true` |
| **Changed** | `Changed` | `IsChanged = true` |
| **Archived** | `Archived` | `IsActive = false` |

**Passport eligibility constraint (per InvitationItem person):** A person cannot be added to an invitation application (`App_Inv`, `App_Inv_FM`, `App_Inv_And_WP`, `App_Inv_According_to_WP`) if their `CurrentPassport.ExpirationDate ≤ Today + 1 month`. An invitation is valid for 6 months — including a person whose passport expires within 1 month would mean the invitation outlives the passport, which is not permitted. The person's passport must be renewed first (§1) before they can be included.

| Person Eligibility State | Code | Condition |
|---|---|---|
| **Eligible for Invitation** | `InvitationEligible` | `Person.CurrentPassport.ExpirationDate > Today + 1 month` — passport has sufficient validity; person may be added to invitation |
| **Ineligible — Passport Expiring** | `InvitationIneligible_PassportExpiring` | `Person.CurrentPassport.ExpirationDate ≤ Today + 1 month` — passport expires too soon; person cannot be added until passport is renewed |

**Key fields:** `StartDate`, `ExpirationDate`, `ValidityDuration`, `Application`, `InvitationItems[].IsCancelled` / `IsChanged` / `IsUsed`, `InvitationItems[].Person.CurrentPassport.ExpirationDate`
**Suggested actions:**
- `ExpiringSoon` → notify applicant coordinator to plan person's travel before expiry
- `Expired` → flag; new invitation required before travel can proceed
- `Cancelled` → notify affected persons; initiate replacement invitation if still needed
- `InvitationIneligible_PassportExpiring` → notify HR to renew the person's passport (§1) before submitting the invitation application

---

### 7. Address of Residence

**Purpose:** Records where a person is legally registered to reside. Expiry means the registration is overdue for renewal.

| State Name | Code | Condition |
|---|---|---|
| **Active** | `Active` | `ExpirationDate` is beyond the warning threshold |
| **Expiring Soon** | `ExpiringSoon` | Today is within the warning threshold window before `ExpirationDate` |
| **Expired** | `Expired` | Today ≥ `ExpirationDate` |
| **Archived** | `Archived` | `IsActive = false` |

**Key fields:** `StartDate`, `ExpirationDate`, `Type` (Lodging / Hotel / PrivateHouse), `Person`, `Region`, `City`
**Suggested actions:**
- `ExpiringSoon` → remind HR or lodging manager to prepare renewal registration
- `Expired` → flag person's record; registration renewal overdue

---

### 8. Application

**Purpose:** The central process record that drives the entire workflow. Its state has three independent dimensions: document validity (expiration), physical location (which authority currently holds the application), and workflow outcome.

---

#### 8a. Expiration States (document validity)

| State Name | Code | Condition |
|---|---|---|
| **Valid** | `Active` | `ExpirationDate` is beyond the warning threshold; `IsActive = true` |
| **Validity Expiring** | `ExpiringSoon` | Today is within the warning threshold window before `ExpirationDate`; `IsActive = true` |
| **Validity Expired** | `Expired` | Today ≥ `ExpirationDate` |
| **Closed** | `Archived` | `IsActive = false` |

---

#### 8b. Location States (`ApplicationStatus` enum)

Tracks which authority currently holds the application. Stored as `ApplicationStatus` on the `Application` record.

| State Name | Code | Enum Value | Meaning |
|---|---|---|---|
| **At Office** | `Office` | `0` | Application is being prepared or held at the company office; not yet submitted to any authority |
| **Sent to Ministry** | `ToMinistry` | `1` | Application has been submitted to a ministry (first or subsequent); awaiting ministry decision |
| **Processed** | `Processed` | `2` | Application has completed processing at the current authority and is ready for the next step or final outcome |

---

#### 8c. Workflow Progress States (via `ApplicationProgress`)

Each `ApplicationProgress` entry records a **state** (`ApplicationState`) and **location** (`ApplicationLocation`) at a point in time. `Application.CurrentState` always points to the latest entry. These states use the **`DaysElapsed`** temporal model (time since `ApplicationProgress.Date`).

**Validation, SLA time scopes, allowed transitions, and officer manual advance** are specified in **[`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md)**.

These states apply to all application types; the **routing** (which authorities are visited) varies by `ApplicationType` — see §8d.

| State Name | Code | Condition |
|---|---|---|
| **At Office** | `AtOffice` | Latest `ApplicationProgress.Location` = Office — application prepared, not yet dispatched |
| **Sent to Ministry** | `SentToMinistry` | Latest `ApplicationProgress` records dispatch to a Ministry location |
| **Sent to Second Ministry** | `SentToSecondMinistry` | A second Ministry `ApplicationProgress` entry exists after the first — some types require two ministries |
| **Sent to Migration Service** | `SentToMigrationService` | Latest `ApplicationProgress.Location` = Migration Service |
| **Completed** | `Completed` | Latest `ApplicationProgress.State` = Completed / Issued — all authorities have processed; documents issued |
| **Rejected** | `Rejected` | Latest `ApplicationProgress.State` = Rejected — refused by an authority |
| **Cancelled** | `Cancelled` | Latest `ApplicationProgress.State` = Cancelled — withdrawn or voided |

---

#### 8d. Workflow Routing by Application Type

The sequence of authorities visited differs per `ApplicationType`. The table below shows the standard routing for known types:

| Application Type | Routing |
|---|---|
| Invitation (`App_Inv`, etc.) | Office → Ministry → Migration Service → Completed / Rejected / Cancelled |
| Visa (`App_Visa`, `App_Visa_Ext`, etc.) | Office → Ministry → Migration Service → Completed / Rejected / Cancelled |
| Work Permit (`App_WP`, `App_WP_Ext`, etc.) | Office → Ministry → Migration Service → Completed / Rejected / Cancelled |
| Visa + WP Extension (`App_Visa_and_WP_Ext`) | Office → Ministry → Migration Service → Completed / Rejected / Cancelled |
| Check-In Registration (`App_Reg_Check_In`) | Office → Migration Service (directly) → Completed / Rejected / Cancelled |
| Check-Out Registration (`App_Reg_Check_Out`) | Office → Migration Service (directly) → Completed / Rejected / Cancelled |
| Internal Movements (`App_Reg_Check_In_Internal`, `App_Reg_Check_Out_Internal`) | Office → Migration Service (directly) → Completed / Rejected / Cancelled |
| Cancellation (`App_Cancel_Visa`, `App_Cancel_Visa_and_WP`, etc.) | Office → Ministry → Migration Service → Completed / Rejected / Cancelled |

> **Note:** Some application types may optionally route through a **second ministry** between the first ministry and the migration service. This is tracked by a second `ApplicationProgress` entry with a different `Ministry` location.

---

**Key fields:** `ApplicationDate`, `ExpirationDate`, `IsActive`, `CurrentState` (latest `ApplicationProgress`), `ApplicationType`, `ApplicationStatus`

**Suggested actions:**
- `ValidityExpiring` → notify coordinator to follow up with the current authority
- `ValidityExpired` → escalate to manager; re-submission may be required
- `Rejected` → notify applicant and coordinator with the rejection reason; determine if re-application is possible
- `Cancelled` → close all related person items; update `ApplicationItem` flags accordingly

---

### 9. Person — Arrival Registration Compliance

**Purpose:** Tracks whether a person currently present in Turkmenistan has fulfilled the mandatory registration requirement. Upon external arrival, the person must be registered with the migration authority by submitting an `Application` of type `App_Reg_Check_In`, which produces a `Registration` record linked to an `ExternalArrival` travel entry.

**Related application types and their movement records:**

| `ApplicationType.Name` | Movement Record Created | Meaning |
|---|---|---|
| `App_Reg_Check_In` | `ExternalArrival` | Person entered Turkmenistan; registration required |
| `App_Reg_Check_Out` | `ExternalDeparture` | Person departed Turkmenistan |
| `App_Reg_Check_In_Internal` | `InternalArrival` | Person arrived at an internal location |
| `App_Reg_Check_Out_Internal` | `InternalDeparture` | Person departed an internal location |

**States:**

| State Name | Code | Condition |
|---|---|---|
| **Not Present** | `NotPresent` | Person's latest `TravelHistory` is an `ExternalDeparture`, or no travel history exists — person is not in country |
| **Arrived — Pending Registration** | `ArrivedPendingRegistration` | Latest `TravelHistory` is an `ExternalArrival` AND no `Registration` linked to an `App_Reg_Check_In` application exists yet |
| **Registration In Progress** | `RegistrationInProgress` | An `App_Reg_Check_In` `Application` exists for this person (via `ApplicationItems`) and its `CurrentState` has not yet reached a terminal state (Issued / Completed) |
| **Registration Overdue** | `RegistrationOverdue` | Latest `ExternalArrival.TravelDate` is more than **N days** ago AND no completed `App_Reg_Check_In` registration exists — person has missed the registration deadline |
| **Registered** | `Registered` | An active `Registration` linked to a completed `App_Reg_Check_In` application exists (`Person.CurrentRegistration` points to it) |
| **Checked Out** | `CheckedOut` | Person's latest `Registration` is linked to an `App_Reg_Check_Out` application — person has been formally checked out |

**Key fields:** `Person.TravelHistories` (latest `ExternalArrival` / `ExternalDeparture`), `Person.Registrations`, `Person.CurrentRegistration`, `Registration.Application.ApplicationType.Name`, `Registration.MovementRecord`

**Suggested actions:**
- `ArrivedPendingRegistration` → alert coordinator to open an `App_Reg_Check_In` application immediately
- `RegistrationOverdue` → escalate to compliance officer; registration deadline breached
- `RegistrationInProgress` → remind coordinator to complete and submit the application
- `Registered` → no action; record is compliant
- `CheckedOut` → no action; person has left the country

---

### 10. Person — Visa Expiry Departure Compliance

**Purpose:** Tracks whether a person currently in the country must depart due to an expiring or expired visa. This is distinct from the Visa's own `ExpiringSoon`/`Expired` states (§2) — it represents a **physical legal obligation** to leave, and after expiry, an obligation to apply for `App_Reg_Check_Out` at the migration service. The obligation only applies when the visa has **not** been extended; if `IsExtended = true`, the departure requirement is lifted regardless of the expiration date.

**Precondition — scope:** All states in this section apply only when **both** conditions are met:
1. `Person.IsArchived = false` — archived persons are excluded from all departure compliance tracking
2. `Person.CurrentVisa` is not null and `IsActive = true` — only the active visa is evaluated; archived or superseded visas are ignored

If either precondition fails, no state is evaluated. If `Person.CurrentVisa = null` (but `IsArchived = false`), the state is `NoActiveVisa`.

**States:**

| State Name | Code | Condition |
|---|---|---|
| **Visa Valid** | `VisaValid` | `CurrentVisa.ExpirationDate > Today + 90 days`; `IsCancelled = false`; `IsExtended = false`; no extension application in progress |
| **Extension Application Required** | `ExtensionApplicationRequired` | `(Today + 90 days) ≥ CurrentVisa.ExpirationDate > Today` AND `IsCancelled = false` AND `IsExtended = false` AND no extension application submitted AND `Passport.ExpirationDate > CurrentVisa.ExpirationDate` — within the 90-day window and passport allows extension. **If `Person.CurrentWorkPermitItem` is also active** → use `App_Visa_and_WP_Ext`; **if no active WP** → use `App_Visa_Ext`, `App_Visa_Ext_FM`, `App_Visa_Ext_According_to_WP`, or `App_Visa_and_WP_Ext` |
| **Passport Renewal Required (Blocks Extension)** | `PassportRenewalRequired` | `(Today + 90 days) ≥ CurrentVisa.ExpirationDate` AND `Passport.ExpirationDate ≤ CurrentVisa.ExpirationDate` — visa extension is blocked because passport expires at or before the visa; passport must be renewed first (see §1) before any extension application can be submitted |
| **Extension In Progress** | `ExtensionInProgress` | An extension application (`App_Visa_Ext`, `App_Visa_Ext_FM`, `App_Visa_and_WP_Ext`, or `App_Visa_Ext_According_to_WP`) exists for this person and its `CurrentState` has not yet reached a terminal state (Issued / Completed) |
| **Departure Required** | `DepartureRequired` | `CurrentVisa.ExpirationState = ExpiringSoon` AND `IsCancelled = false` AND `IsExtended = false` AND no extension application in progress AND no `App_Reg_Check_Out` started — extension window passed without action; person must leave before `ExpirationDate` |
| **Check-Out Required** | `CheckOutRequired` | `CurrentVisa.ExpirationDate < Today` AND `IsCancelled = false` AND `IsExtended = false` AND no `App_Reg_Check_Out` application submitted AND `(Today − ExpirationDate) ≤ 3 days` — visa just expired naturally; person must apply for `App_Reg_Check_Out` within the 3-day grace window |
| **Check-Out In Progress** | `CheckOutInProgress` | An `App_Reg_Check_Out` application exists for this person and its `CurrentState` has not yet reached a terminal state (Issued / Completed) |
| **Check-Out Overdue** | `CheckOutOverdue` | `CurrentVisa.ExpirationDate < Today` AND `IsCancelled = false` AND `IsExtended = false` AND no completed `App_Reg_Check_Out` exists AND `(Today − ExpirationDate) > 3 days` — 3-day grace window missed; serious compliance breach |
| **Cancellation Check-Out Required** | `CancelledCheckOutRequired` | `CurrentVisa.IsCancelled = true` AND no `App_Reg_Check_Out` application submitted — visa was cancelled via `App_Cancel_Visa_and_WP` or `App_Cancel_Visa`; check-out must be applied for **immediately**, no grace window |
| **Cancellation Check-Out Overdue** | `CancelledCheckOutOverdue` | `CurrentVisa.IsCancelled = true` AND no completed `App_Reg_Check_Out` exists AND cancellation date < Today — any delay past cancellation day is a breach |
| **Checked Out** | `CheckedOut` | A `Registration` linked to a completed `App_Reg_Check_Out` application exists — person has been formally checked out |
| **Visa Extended** | `VisaExtended` | `CurrentVisa.IsExtended = true` — visa validity prolonged; no departure or check-out obligation |
| **No Active Visa** | `NoActiveVisa` | `CurrentVisa = null` — no current visa on record |

**Key fields:** `Person.CurrentVisa`, `Visa.ExpirationDate`, `Visa.ExpirationState`, `Visa.IsExtended`, `Visa.IsCancelled`, `Person.TravelHistories` (latest `ExternalArrival` / `ExternalDeparture`), `Person.ApplicationItems` (for `App_Reg_Check_Out` lookup), `Person.Registrations`

**Full state flow:**
```
VisaValid (> 90 days to expiry)
    │
    ├─ 90 days before ExpirationDate
    │       ↓
    │  ExtensionApplicationRequired
    │       ├─ extension application submitted → ExtensionInProgress
    │       │       └─ approved (IsExtended = true) → VisaExtended ✓
    │       │
    │       └─ no extension applied, visa enters ExpiringSoon window
    │               ↓
    │          DepartureRequired  (must leave before ExpirationDate)
    │
    ├─ Visa expires naturally (ExpirationDate < Today, IsCancelled = false, IsExtended = false)
    │       ↓
    │  CheckOutRequired (days 1–3)
    │       ├─ App_Reg_Check_Out submitted → CheckOutInProgress → CheckedOut ✓
    │       └─ no action after 3 days → CheckOutOverdue ✗
    │
    └─ Visa cancelled (IsCancelled = true, via App_Cancel_Visa_and_WP or App_Cancel_Visa)
            ↓
       CancelledCheckOutRequired (immediate, day 0)
            ├─ App_Reg_Check_Out submitted same day → CheckOutInProgress → CheckedOut ✓
            └─ any delay → CancelledCheckOutOverdue ✗
```

**Relationship to §2 (Visa states):** The Visa's own `ExpiringSoon` triggers `DepartureRequired` here only when `IsExtended = false`. Once `ExpirationDate` passes, the Visa enters `Expired` (§2) while the Person enters `CheckOutRequired` / `CheckOutOverdue` (§10) — two parallel but distinct states.

**Suggested actions:**
- `PassportRenewalRequired` → alert HR urgently; passport expires before the visa — renew passport first (§1), then proceed with visa/WP extension
- `ExtensionApplicationRequired` → notify HR coordinator; **if `Person.CurrentWorkPermitItem` is active** → submit `App_Visa_and_WP_Ext`; **if no active WP** → submit `App_Visa_Ext`, `App_Visa_Ext_FM`, or `App_Visa_Ext_According_to_WP`
- `ExtensionInProgress` → monitor application; follow up with authority until Issued/Completed
- `DepartureRequired` → notify person and HR coordinator; extension window missed — person must depart before `ExpirationDate`
- `CheckOutRequired` → alert coordinator urgently; submit `App_Reg_Check_Out` to migration service within the 3-day grace window
- `CheckOutInProgress` → monitor application progress; ensure it reaches Issued/Completed before grace window closes
- `CheckOutOverdue` → escalate to compliance officer; 3-day window has been missed
- `CancelledCheckOutRequired` → alert coordinator immediately; visa cancelled — submit `App_Reg_Check_Out` the same day, no grace window applies
- `CancelledCheckOutOverdue` → escalate to compliance officer urgently; cancelled visa check-out not submitted on time
- `CheckedOut` → no action; person is formally checked out
- `VisaExtended` → no departure action; monitor new expiry date via §2 Visa states
- `NoActiveVisa` → investigate; person should not be in country without a valid visa

---

## State Priority Rules

When a BO has multiple applicable states (e.g., `Expired` AND `Cancelled`), the following priority order determines the displayed/alerted state:

1. `Cancelled` — overrides all others; the document is void
2. `Expired` — document is past validity
3. `ExpiringSoon` — document requires attention
4. `Extended` or `Changed` — modifier on an otherwise Active document
5. `Active` — normal state
6. `Archived` — inactive; historical record only

---

## Notes

- Warning thresholds (the `N` days that define `ExpiringSoon`) are configured in `SystemSettings` per BO type.
- All date comparisons use `DateTime.Today` (date only, no time component).
- `Archived` is set only via explicit user action or system deactivation — it is never auto-set by date logic alone.
- BOs not listed here (e.g., `Education`, `TravelHistory`) do not have time-bound states requiring proactive tracking.
- `Registration` is not tracked directly — it is the *output* of a completed `App_Reg_Check_In` application. The compliance state is tracked at the `Person` level (see §9).
- The registration deadline (`N days` in §9 `RegistrationOverdue`) is to be defined in `SystemSettings`.
- The check-out grace window (3 days in §10 `CheckOutRequired` / `CheckOutOverdue`) is a fixed legal requirement; store in `SystemSettings` so it can be adjusted if regulations change.
- The visa extension application window (90 days in §10 `ExtensionApplicationRequired`) is a process rule; store in `SystemSettings`.
- The invitation passport validity threshold (1 month in §6 `InvitationIneligible_PassportExpiring`) should be stored in `SystemSettings`.

---

## Implementation Strategy

### Surfacing States (priority order)

1. **Dashboard tiles** — aggregated counts per state (e.g. "5 passports expiring", "2 departure overdue"); primary entry point for coordinators
2. **State notifications inbox** — prioritized officer queue (validity + missing profile data); see [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md)
3. **Push notifications** — browser push delivered to logged-in application users when a state transition occurs
4. **List view color coding** — rows colored by state severity in Person/Visa/WorkPermitItem list views
5. **Person detail view badges** — inline state badges on the Person detail view next to each tracked BO

### State Recalculation — Hybrid Approach

| Trigger | Frequency | Purpose |
|---|---|---|
| **On-save** | Immediately when a tracked BO is saved | Catches event-driven transitions (cancellation, extension approval, new registration) |
| **Nightly background job** | Once per day at midnight/early morning | Catches date-driven transitions that occur overnight with no data change (e.g. `ExpirationDate` passing) |
| **On dashboard load** | Each time the dashboard is opened | Ensures tiles always reflect the current moment for the viewing user |

### Notification Channels

| Channel | When used |
|---|---|
| **Browser push notification** | Primary channel; delivered to application users when a state transition is detected |
| **Email** | Fallback; sent when the user is offline or has not acknowledged the push notification within a defined window |

**Recipients:** Application users (HR coordinators, compliance officers) — not the persons themselves directly.

**Notification content should include:** person name, BO type, state name, days remaining (if applicable), and a direct link to the relevant record.