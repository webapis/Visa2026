# Business Object State Tracking Specification

This document defines the trackable Business Objects (BOs), their named states, and the conditions that determine each state. It serves as the canonical reference for notification logic, dashboard indicators, and workflow automation.

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

**Key fields:** `StartDate`, `ExpirationDate`, `IsCancelled`, `IsChanged`, `IsExtended`, `Passport`, `VisaType`
**Suggested actions:**
- `ExpiringSoon` → alert HR and employee to begin renewal/extension process
- `Expired` → flag person's record; block registration submissions
- `Cancelled` → notify relevant departments; initiate replacement process if needed

---

### 3. Work Permit Item

**Purpose:** Individual work authorization for an employee. Expiry makes the employee's work status non-compliant.

| State Name | Code | Condition |
|---|---|---|
| **Active** | `Active` | `ExpirationDate` is beyond the warning threshold; `IsCancelled = false` |
| **Expiring Soon** | `ExpiringSoon` | Today is within the warning threshold window before `ExpirationDate`; `IsCancelled = false` |
| **Expired** | `Expired` | Today ≥ `ExpirationDate`; `IsCancelled = false` |
| **Cancelled** | `Cancelled` | `IsCancelled = true` |
| **Changed** | `Changed` | `IsChanged = true` |
| **Extended** | `Extended` | `IsExtended = true` |
| **Archived** | `Archived` | `IsActive = false` |

**Key fields:** `StartDate`, `ExpirationDate`, `IsCancelled`, `IsChanged`, `IsExtended`, `Person`, `WorkPermit`
**Suggested actions:**
- `ExpiringSoon` → initiate renewal application with relevant ministry
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

**Key fields:** `StartDate`, `ExpirationDate`, `ValidityDuration`, `IsCancelled`, `IsChanged`, `Application`
**Suggested actions:**
- `ExpiringSoon` → notify applicant coordinator to plan person's travel before expiry
- `Expired` → flag; new invitation required before travel can proceed
- `Cancelled` → notify affected persons; initiate replacement invitation if still needed

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

**Purpose:** The central process record that drives the entire workflow. Its state reflects both document validity and workflow progress.

#### 8a. Expiration States (document validity)

| State Name | Code | Condition |
|---|---|---|
| **Valid** | `Active` | `ExpirationDate` is beyond the warning threshold; `IsActive = true` |
| **Validity Expiring** | `ExpiringSoon` | Today is within the warning threshold window before `ExpirationDate`; `IsActive = true` |
| **Validity Expired** | `Expired` | Today ≥ `ExpirationDate` |
| **Closed** | `Archived` | `IsActive = false` |

#### 8b. Workflow States (progress tracking via `ApplicationProgress`)

| State Name | Code | Condition |
|---|---|---|
| **Submitted** | `Submitted` | Latest `ApplicationProgress.State` = Submitted |
| **Under Review** | `UnderReview` | Latest `ApplicationProgress.State` = Under Review |
| **Approved** | `Approved` | Latest `ApplicationProgress.State` = Approved |
| **Rejected** | `Rejected` | Latest `ApplicationProgress.State` = Rejected |
| **Issued** | `Issued` | Latest `ApplicationProgress.State` = Issued |
| **Completed** | `Completed` | All `ApplicationItems` have their primary document issued; no pending items |

**Key fields:** `ApplicationDate`, `ExpirationDate`, `IsActive`, `CurrentState` (latest `ApplicationProgress`), `ApplicationType`
**Suggested actions:**
- `ValidityExpiring` → notify coordinator to follow up with authority
- `ValidityExpired` → escalate to manager; re-submission may be required
- `Rejected` → notify applicant and coordinator with rejection reason

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