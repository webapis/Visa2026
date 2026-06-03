# Business Object State — Temporal Types (`DaysRemaining` vs `DaysElapsed`)

> **Purpose:** Classify which business objects derive **states from time** and whether the clock counts **down to** a deadline or **up from** an event. Officers use these two axes for different actions (renew before expiry vs follow up because something has been waiting too long).
>
> **Related docs:**
> - [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) — state **codes** and business conditions per BO
> - [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) — visual registry; § How BO states are determined (sources A–H)
> - [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md) — inbox alerts (validity vs follow-up)
> - [`ExpirationAlertRule`](../Visa2026.Module/BusinessObjects/ExpirationAlertRule.cs) — officer-configurable **DaysRemaining** thresholds (tenant JSON seed)

---

## 1. Definitions

| Temporal type | Officer question | Anchor date | Metric (computed) | Typical action |
|---------------|------------------|-------------|-------------------|----------------|
| **`DaysRemaining`** | How many days **until** something must happen? | A **future** (or today) deadline, usually `ExpirationDate` | `(AnchorDate.Date − Today).Days` — positive while still valid | **Preventive:** extend, renew, register **before** the date |
| **`DaysElapsed`** | How many days **since** something happened? | A **past** event date (progress step, arrival, submission) | `(Today − AnchorDate.Date).Days` — zero on the event day, grows each day after | **Follow-up:** chase ministry, complete registration, escalate stuck case |

**Important distinctions**

- **`DaysRemaining < 0`** on a validity BO means **expired** (deadline passed). That is still the **DaysRemaining** axis, not DaysElapsed — the primary metric remains “distance to `ExpirationDate`”.
- **`DaysElapsed`** alerts often need a **precondition** (e.g. only while `ApplicationProgress.State` is non-terminal).
- Many BOs also have **non-temporal** states (flags, process codes) that do not use either metric — see §4.

---

## 2. Master registry

| Business object | Temporal type | Anchor property | State source | Config / code |
|-----------------|---------------|-----------------|--------------|---------------|
| **Visa** | `DaysRemaining` | `ExpirationDate` | `VisaValidityState`, `VisaStateEvaluator` | `ExpirationAlertRule` key `Visa` |
| **Passport** | `DaysRemaining` | `ExpirationDate` | `PassportStateEvaluator`, `ExpirationState` | key `Passport` |
| **WorkPermitItem** | `DaysRemaining` | `ExpirationDate` | `WorkPermitItemStateEvaluator` | key `WorkPermitItem` |
| **EmployeeContract** | `DaysRemaining` | `ExpirationDate` | `EmployeeContractStateEvaluator` | key `EmployeeContract` |
| **MedicalRecord** | `DaysRemaining` | `ExpirationDate` | `MedicalRecordStateEvaluator` | key `MedicalRecord` |
| **AddressOfResidence** | `DaysRemaining` | `ExpirationDate` | `AddressOfResidenceStateEvaluator` | key `AddressOfResidence` |
| **Invitation** | `DaysRemaining` | `ExpirationDate` | `ExpirationLogicHelper` → `ExpirationState` | key `Invitation` |
| **BorderZone** | `DaysRemaining` | `ExpirationDate` | `ExpirationLogicHelper` → `ExpirationState` | key `BorderZone` |
| **ApplicationProgress** | `DaysElapsed` | `Date` | `ApplicationState.Code`, `ApplicationLocation.Code` on each history row | Planned step SLA rules — see [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md) |
| **Application** (workflow) | `DaysElapsed` (derived) | Latest `ApplicationProgress.Date` | Latest progress row drives `CurrentState` / list filters | Same as progress |
| **Person** (registration compliance) | `DaysElapsed` | `ExternalArrival.TravelDate` | Spec in `BO_STATE_TRACKING.md` §9 | Threshold **N days** (TBD in settings) |
| **TravelHistory** (`ExternalArrival`, …) | `DaysElapsed` (anchor only) | `TravelDate` | Used by Person registration compliance | — |

**Read models (SQL views)** such as `VisaExtensionStatus`, `WorkPermitExtensionStatus` expose **`DaysRemainingOnVisa`** / **`DaysRemaining`** copied from linked document BOs — they inherit the **`DaysRemaining`** type from the underlying visa/work permit.

---

## 3. `DaysRemaining` — document validity states

### 3.1 Contract (`IExpirationLogic`)

All eight document BOs implement [`IExpirationLogic`](../Visa2026.Module/BusinessObjects/IExpirationLogic.cs):

```csharp
DateTime? ExpirationDate { get; }
int DaysRemaining { get; }  // (ExpirationDate.Date - Today).Days
```

Shared helper: [`ExpirationLogicHelper`](../Visa2026.Module/BusinessObjects/IExpirationLogic.cs), evaluators under [`Services/StateEvaluation/Evaluators/`](../Visa2026.Module/Services/StateEvaluation/Evaluators/).

### 3.2 Implementing business objects

| BO | `ExpirationDate` source | Extra non-date states | Evaluator / helper |
|----|-------------------------|------------------------|-------------------|
| [`Visa`](../Visa2026.Module/BusinessObjects/Visa.cs) | `StartDate` + `ValidityDuration` | `IsCancelled`, `IsChanged`, `IsExtended`, `ExtensionRequired` | `VisaValidityState`, `VisaStateEvaluator` |
| [`Passport`](../Visa2026.Module/BusinessObjects/Passport.cs) | `IssueDate` / validity rules | Archived (not current for person) | `PassportStateEvaluator` |
| [`WorkPermitItem`](../Visa2026.Module/BusinessObjects/WorkPermitItem.cs) | WP validity | `IsCancelled`, extension band | `WorkPermitItemStateEvaluator` |
| [`EmployeeContract`](../Visa2026.Module/BusinessObjects/EmployeeContract.cs) | `ContractStartDate` + duration | Archived | `EmployeeContractStateEvaluator` |
| [`MedicalRecord`](../Visa2026.Module/BusinessObjects/MedicalRecord.cs) | issue + duration | Archived | `MedicalRecordStateEvaluator` |
| [`AddressOfResidence`](../Visa2026.Module/BusinessObjects/AddressOfResidence.cs) | `ExpirationDate` (private house; optional for lodging/hotel) | Archived; type hides expiry fields for non–private-house | `AddressOfResidenceStateEvaluator` |
| [`Invitation`](../Visa2026.Module/BusinessObjects/Invitation.cs) | `StartDate` + duration | `IsCancelled`, `IsChanged` | `ExpirationLogicHelper` |
| [`BorderZone`](../Visa2026.Module/BusinessObjects/BorderZone.cs) | `StartDate` + duration | — | `ExpirationLogicHelper` |

### 3.3 Typical state codes (validity dimension)

| Code | Condition (simplified) | Officer meaning |
|------|------------------------|-----------------|
| `Active` | `DaysRemaining` above alert window | Document still comfortably valid |
| `ExtensionApplicationRequired` | Within extension band (Visa, WorkPermitItem only) | Start extension application |
| `ExpiringSoon` / `Expiring` | `DaysRemaining ≤ ExpiringSoonDays` (per `ExpirationAlertRule`) | Urgent renewal window |
| `Expired` | `DaysRemaining < 0` | Document no longer valid |
| `Archived` | Not the person’s current item | Historical row — no validity action |

Thresholds: **System → Expiration alert rules** (`ExpiringSoonDays`, optional `ExtensionApplicationRequiredDays`). Fallback: `SystemSettings.DefaultExpiringSoonDays`.

### 3.4 Officer actions (examples)

| State | Example action |
|-------|----------------|
| `ExtensionApplicationRequired` | Open `App_Visa_and_WP_Ext` or `App_WP_Ext` |
| `ExpiringSoon` | Escalate — extension window may have been missed |
| `Expired` | Block new applications; open replacement workflow |

---

## 4. `DaysElapsed` — process and follow-up states

### 4.1 `ApplicationProgress`

Each [`ApplicationProgress`](../Visa2026.Module/BusinessObjects/ApplicationProgress.cs) row is a **point on a timeline**:

| Property | Role |
|----------|------|
| `Date` | **Anchor** for elapsed time in this step |
| `State` | Workflow outcome dimension (`ApplicationState` catalog, e.g. `PROCESS_STARTED`, `PROCESS_ISSUED`) |
| `Location` | Physical/logical location dimension (`ApplicationLocation` catalog, e.g. `AT_OFFICE`, `AT_THE_MINISTERY_1`) |

**Elapsed metric (per row or for current step):**

```text
DaysElapsed = (Today - ApplicationProgress.Date.Date).Days
```

The **state code** on the row (`State.Code`, `Location.Code`) is **not** computed from days alone — it is **what was recorded** when the officer updated progress. **DaysElapsed** turns the same row into a **follow-up signal** (“still at ministry 14 days after dispatch”).

Parent [`Application`](../Visa2026.Module/BusinessObjects/Application.cs) **`CurrentState`** (when present) references the `ApplicationProgress` row with the **latest `Date`** ([`ApplicationProgressHelper.GetLatest`](../Visa2026.Module/BusinessObjects/ApplicationProgressHelper.cs)). The application’s effective workflow state is therefore **DaysElapsed-type**, anchored on that row’s `Date`.

See also: [`ApplicationProgress.md`](../Visa2026.Module/BusinessObjects/ApplicationProgress.md), [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) §8c.

### 4.2 Typical use (spec / planned alerts)

| Scenario | Anchor | Precondition | Example alert |
|----------|--------|--------------|---------------|
| Stuck at ministry | Latest progress `Date` | `Location` = ministry, state not terminal | “Sent to ministry **N+** days ago” |
| Application open too long | `ApplicationDate` | No terminal progress | “Application open **N+** days” |
| Registration overdue | `ExternalArrival.TravelDate` | No completed `App_Reg_Check_In` | `RegistrationOverdue` ([`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) §9) |

**Note:** Follow-up thresholds for `ApplicationProgress` are **not** yet in `ExpirationAlertRule` (v1 covers **`DaysRemaining`** on eight validity BOs only). Elapsed rules are the planned **`FollowUpState`** category in the state notifications inbox.

### 4.3 Officer actions (examples)

| Signal | Example action |
|--------|----------------|
| Many days elapsed at ministry | Call / visit authority; update `ApplicationProgress` |
| Registration overdue (elapsed since arrival) | Open `App_Reg_Check_In` immediately |
| Terminal progress reached | No elapsed follow-up — process complete |

---

## 5. Non-temporal and hybrid states

### 5.1 Non-temporal (no `DaysRemaining` / `DaysElapsed` on that dimension)

| Source | Examples |
|--------|----------|
| Persisted flags | `Visa.IsCancelled`, `InvitationItem.IsUsed`, `WorkPermitItem.IsChanged` |
| Catalog codes alone | `ApplicationState`, `ApplicationLocation` **labels** (the code is stored; elapsed time is a separate concern) |
| Cross-BO linkage | `ExtensionInProgress`, `OnExtension`, rejection evidence |
| Current vs historical | `Archived` via `PersonCurrentItems` |

### 5.2 Hybrid BOs (multiple dimensions)

| BO | Dimensions |
|----|------------|
| **Visa** | `DaysRemaining` validity + flags (`IsExtended`, `IsChanged`, …) + optional process linkage |
| **Application** | `DaysElapsed` workflow (progress) + optional `ApplicationDate` elapsed + item/document linkage |
| **Person** | Aggregated compliance (`DaysElapsed` since arrival) + current child documents (`DaysRemaining` on `CurrentVisa`, etc.) |

**UI rule:** one **row background** color from the highest-priority state; show **separate columns** when both validity (`DaysRemaining`) and process (`DaysElapsed`) matter ([`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) § Multi-dimensional display).

---

## 6. Configuration today vs planned

| Temporal type | Officer UI today | Seed / defaults |
|---------------|------------------|-----------------|
| **`DaysRemaining`** | **System → Expiration alert rules** | `tenant/expiration-alert-rules.json`, `InsertOnly` |
| **`DaysElapsed`** | Not yet — document thresholds in specs | Planned: follow-up rule BO or extended temporal rules with `Direction = AfterAnchor` |

---

## 7. When adding a new state

1. Classify: **`DaysRemaining`**, **`DaysElapsed`**, or **non-temporal**.
2. Name the **anchor property** and formula.
3. Document state codes in [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md).
4. Register colors in [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md).
5. If countdown: add or extend a row in **Expiration alert rules** / tenant JSON.
6. If elapsed: specify precondition + threshold for follow-up inbox (future).

---

## 8. Code index

| Area | Path |
|------|------|
| DaysRemaining interface | `Visa2026.Module/BusinessObjects/IExpirationLogic.cs` |
| Alert rules BO | `Visa2026.Module/BusinessObjects/ExpirationAlertRule.cs` |
| Rule keys | `Visa2026.Module/BusinessObjects/ExpirationAlertBusinessObjectKeys.cs` |
| Evaluation | `Visa2026.Module/Services/StateEvaluation/` |
| Progress timeline | `Visa2026.Module/BusinessObjects/ApplicationProgress.cs`, `ApplicationProgressHelper.cs` |
