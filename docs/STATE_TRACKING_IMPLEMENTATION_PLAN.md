# State Tracking — Implementation Plan

> **Officer inbox (bell + notifications list):** see [`STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md`](STATE_NOTIFICATIONS_IMPLEMENTATION_PLAN.md) for the UI prototype, data-completeness rules, and how this engine feeds the inbox. This document remains the **evaluation / snapshot / dispatcher** plan.

## Architecture Decision: Computed-on-Demand + Thin Snapshot Table

States are **computed live** (not stored as DB columns per BO) — consistent with how `ExpirationState` already works on `Visa`, `Passport`, `WorkPermitItem`, etc. The only thing persisted is a `PersonStateSnapshot` table that records the **last known state per person per BO type**, used purely to detect transitions and fire notifications exactly once.

---

## Architecture Decision: C# Evaluators vs SQL Server Views

**Rule: No business logic in SQL. SQL is the data layer only.**

| Layer | Responsibility |
|---|---|
| **C# Evaluators** | All state logic for single-BO states (Visa, Passport, WorkPermit, Contract, Medical, Address). These are pure functions: take a loaded object + `StateEvaluationSettings`, return `BoStateResult`. |
| **SQL Views (data layer)** | Cross-BO joins for §9 Registration Compliance and §10 Departure Compliance, where a single state requires joining Person → Visa → TravelHistory → Application. The view returns a pre-joined flat row; C# still makes the final state decision. |
| **SQL Views (read layer)** | Aggregate counts from `PersonStateSnapshot` for the dashboard tiles, so the dashboard reads one query instead of loading every BO into memory. |
| **`PersonStateSnapshot` table** | Stores last known state per (Person, BoType). Used for transition detection and notification deduplication. |

**Why SQL for §9/§10 data layer:**
Those states require joining 4+ tables (Person, Visa, TravelHistory, Application, ApplicationType, ApplicationStatus). Doing this in C# object-by-object per person causes N+1 queries at scale. A SQL view returns the pre-joined snapshot in a single query; C# reads one row per person and decides the state code.

**Why C# for single-BO states:**
State logic depends on configurable thresholds (`ExpirationWarningThreshold`, `DefaultExpiringSoonDays`) stored in `SystemSettings`. These cannot be parameterized easily in SQL views, and the percentage-based expiration logic is cleaner in C#.

---

## New File Structure

```
Visa2026.Module/
└── Services/
    └── StateEvaluation/
        ├── IPersonStateService.cs
        ├── PersonStateService.cs
        ├── PersonStateResult.cs
        ├── BoStateResult.cs
        ├── StateCode.cs
        ├── StateSeverity.cs
        ├── StateEvaluationSettings.cs
        ├── StateTransitionDetector.cs
        ├── PersonStateRecomputeQueue.cs
        └── Evaluators/
            ├── PassportStateEvaluator.cs
            ├── VisaStateEvaluator.cs
            ├── WorkPermitStateEvaluator.cs
            ├── EmployeeContractStateEvaluator.cs
            ├── MedicalRecordStateEvaluator.cs
            ├── AddressOfResidenceStateEvaluator.cs
            ├── InvitationEligibilityEvaluator.cs
            ├── RegistrationComplianceEvaluator.cs
            └── VisaDepartureComplianceEvaluator.cs

Visa2026.Module/BusinessObjects/
└── PersonStateSnapshot.cs          ← new EF Core entity

Visa2026.Blazor.Server/
└── Services/
    ├── PersonStateBackgroundService.cs   ← on-save queue drainer
    ├── NightlyStateRecomputeService.cs   ← midnight sweep
    └── Notifications/
        ├── IStateNotificationDispatcher.cs
        ├── XafNotificationDispatcher.cs
        └── EmailFallbackDispatcher.cs
```

---

## Step-by-Step Plan

### Step 1 — State Result Models *(pure C#, no XAF dependency)*

Create the shared contract all consumers depend on.

**`StateSeverity.cs`**
```csharp
public enum StateSeverity { None, Info, Warning, Critical, Breach }
```

**`StateCode.cs`** — string constants for every state code defined in BO_STATE_TRACKING.md:
- Passport: `Active`, `ExpiringSoon`, `Expired`, `Archived`
- Visa: `Active`, `ExpiringSoon`, `Expired`, `Cancelled`, `Changed`, `Extended`, `Archived`
- WorkPermit: `Active`, `ExtensionApplicationRequired`, `ExtensionInProgress`, `ExpiringSoon`, `Expired`, `Cancelled`, `Changed`, `Extended`, `Archived`
- EmployeeContract, MedicalRecord, AddressOfResidence: `Active`, `ExpiringSoon`, `Expired`, `Archived`
- Invitation eligibility: `InvitationEligible`, `InvitationIneligible_PassportExpiring`
- Registration compliance (§9): `NotPresent`, `ArrivedPendingRegistration`, `RegistrationInProgress`, `RegistrationOverdue`, `Registered`, `CheckedOut`
- Visa departure compliance (§10): `VisaValid`, `PassportRenewalRequired`, `ExtensionApplicationRequired`, `ExtensionInProgress`, `DepartureRequired`, `CheckOutRequired`, `CheckOutInProgress`, `CheckOutOverdue`, `CancelledCheckOutRequired`, `CancelledCheckOutOverdue`, `CheckedOut`, `VisaExtended`, `NoActiveVisa`

**`BoStateResult.cs`**
```csharp
public record BoStateResult(
    string BoType,          // "Passport", "Visa", "WorkPermit", "VisaDepartureCompliance", etc.
    string StateCode,       // from StateCode constants
    StateSeverity Severity,
    int? DaysRemaining,     // null if not applicable
    Guid? BoId,             // link to the record
    string Label            // human-readable, e.g. "Visa: Extension Required (22 days)"
);
```

**`PersonStateResult.cs`**
```csharp
public record PersonStateResult(
    Guid PersonId,
    string PersonFullName,
    DateTime ComputedAt,
    IReadOnlyList<BoStateResult> States,
    StateSeverity OverallSeverity,   // max severity across all States
    BoStateResult HighestPriorityState
);
```

**`StateEvaluationSettings.cs`** — immutable snapshot of SystemSettings thresholds:
```csharp
public record StateEvaluationSettings(
    int VisaExtensionWindowDays,          // 90
    int WpExtensionWindowDays,            // 90
    int RegistrationDeadlineDays,         // configurable
    int CheckOutGraceDays,                // 3 (legal requirement)
    int InvitationPassportMinMonths,      // 1
    decimal ExpirationWarningThreshold,   // e.g. 0.9 (percentage-based)
    int DefaultExpiringSoonDays           // fallback fixed days
);
// + static factory: StateEvaluationSettings.FromSystemSettings(SystemSettings s)
```

---

### Step 2 — Individual Evaluators *(no DB calls, pure logic)*

Each evaluator is a stateless class with a single `Evaluate(...)` method. Takes loaded objects + settings, returns a `BoStateResult`. No ObjectSpace dependency.

**Simple evaluators** (wrap existing `IExpirationLogic` + flag states):
- `PassportStateEvaluator` — uses `IssueDate`, `ExpirationDate`, `IsActive`
- `EmployeeContractStateEvaluator` — uses `ContractStartDate`, `ExpirationDate`, `IsActive`
- `MedicalRecordStateEvaluator` — uses `IssueDate`, `ExpirationDate`, `IsActive`
- `AddressOfResidenceStateEvaluator` — uses `StartDate`, `ExpirationDate`, `IsActive`

**Flag-extended evaluators** (expiration + IsCancelled/IsChanged/IsExtended):
- `VisaStateEvaluator` — §2
- `WorkPermitStateEvaluator` — §3 (includes 90-day extension window + passport constraint)

**Compliance evaluators** (complex joins pre-resolved by the service):
- `RegistrationComplianceEvaluator` — §9; inputs: latest `ExternalArrival`/`ExternalDeparture`, `CurrentRegistration`, `bool hasActiveCheckInApp`, `bool hasCompletedCheckInRegistration`
- `VisaDepartureComplianceEvaluator` — §10 (most complex); inputs:
  ```
  Visa currentVisa
  Passport currentPassport
  TravelHistory latestExternalMovement
  bool hasActiveExtensionApp
  bool hasCompletedCheckOutRegistration
  bool hasActiveCheckOutApp
  DateTime? visaCancellationDate
  bool hasActiveWp
  StateEvaluationSettings settings
  ```

**Note on `ExpirationLogicHelper`:** Add a new overload that accepts `StateEvaluationSettings` instead of `IObjectSpace`, so evaluators do not need an ObjectSpace. The existing overload continues to work for all existing callers.

---

### Step 3 — `IPersonStateService` and `PersonStateService`

**`IPersonStateService.cs`**
```csharp
public interface IPersonStateService
{
    PersonStateResult ComputeForPerson(Person person, StateEvaluationSettings settings);
    IReadOnlyList<PersonStateResult> ComputeForPersons(IEnumerable<Person> persons, StateEvaluationSettings settings);
    StateEvaluationSettings LoadSettings(IObjectSpace objectSpace);
    IReadOnlyList<PersonStateResult> ComputeAll(IObjectSpace objectSpace);
}
```

**Data loading strategy (3 queries for a full sweep — no N+1):**

| Query | Loads |
|---|---|
| Q1 | All non-archived Persons with `CurrentVisa`, `CurrentPassport`, `CurrentWorkPermitItem`, `CurrentEmployeeContract`, `CurrentMedicalRecord`, `CurrentAddressOfResidence`, `CurrentRegistration` via EF Core Include chains |
| Q2 | Per-person application flags — single JOIN across `ApplicationItems → Applications → ApplicationTypes → ApplicationProgresses`; returns `Dictionary<Guid, PersonApplicationFlags>` keyed by PersonId |
| Q3 | Latest external `TravelHistory` per person (`ExternalArrival` or `ExternalDeparture`) |

`PersonApplicationFlags` carries pre-resolved booleans:
- `bool HasActiveVisaExtensionApp`
- `bool HasActiveWpExtensionApp`
- `bool HasActiveCheckOutApp`
- `bool HasCompletedCheckOutRegistration`
- `bool HasActiveCheckInApp`
- `bool HasCompletedCheckInRegistration`
- `DateTime? VisaCancellationDate`

**Terminal state detection:** An `ApplicationState` is terminal when its `Name` matches `"Issued"`, `"Completed"`, `"Rejected"`, or `"Cancelled"`. Q2 filters on `!terminalNames.Contains(app.CurrentState.State.Name)` for in-progress checks.

**`ComputeForPerson` assembles results** by calling all evaluators and returns a `PersonStateResult` with all `BoStateResult` entries and the computed `OverallSeverity`.

---

### Step 4 — `PersonStateSnapshot` Entity

New EF Core entity — logical key `(PersonId, BoType)`:

```csharp
public class PersonStateSnapshot : BaseObject
{
    public virtual Person Person { get; set; }
    public virtual string BoType { get; set; }
    public virtual string StateCode { get; set; }
    public virtual StateSeverity Severity { get; set; }
    public virtual DateTime LastComputedAt { get; set; }
    public virtual DateTime? LastTransitionAt { get; set; }  // when StateCode last changed
    public virtual bool NotificationSent { get; set; }       // deduplication flag
}
```

- Added to `Visa2026EFCoreDbContext` as `DbSet<PersonStateSnapshot>`
- EF Core migration generated; or added to `SqlViewsUpdater` as raw DDL
- Not exposed in XAF navigation (internal system table)

---

### Step 5 — `StateTransitionDetector`

Pure logic class. Accepts a new `PersonStateResult` and the previous `PersonStateSnapshot` rows for that person. Returns `IReadOnlyList<StateTransition>`:

```csharp
public record StateTransition(
    Guid PersonId,
    string PersonFullName,
    string BoType,
    string OldStateCode,    // null if first time seen
    string NewStateCode,
    StateSeverity Severity
);
```

A transition fires when `NewStateCode != OldStateCode` (including null → first state). Only transitions to states with `Severity >= Warning` are dispatched as notifications.

---

### Step 6 — On-Save Queue + Background Drainer

**`PersonStateRecomputeQueue.cs`** (singleton `ConcurrentQueue<Guid>`)
```csharp
public class PersonStateRecomputeQueue
{
    private readonly ConcurrentQueue<Guid> _queue = new();
    public void Enqueue(Guid personId) => _queue.Enqueue(personId);
    public bool TryDequeue(out Guid personId) => _queue.TryDequeue(out personId);
}
```

**Enqueue calls** added inside `OnSaving()` of each tracked BO:
- `Visa.OnSaving()` — enqueues `Passport?.Person?.ID`
- `WorkPermitItem.OnSaving()` — enqueues `Person?.ID`
- `ApplicationProgress.OnSaving()` — enqueues all `Application.ApplicationItems[].Person.ID`
- `Registration.OnSaving()` — enqueues `Person?.ID`
- `TravelHistory.OnSaving()` — enqueues `Person?.ID`
- `Passport.OnSaving()` — enqueues `Person?.ID`
- `EmployeeContract.OnSaving()` — enqueues `Person?.ID`
- `MedicalRecord.OnSaving()` — enqueues `Person?.ID`
- `AddressOfResidence.OnSaving()` — enqueues `Person?.ID`

**`PersonStateBackgroundService.cs`** (`BackgroundService`):
- Drains `PersonStateRecomputeQueue` in a loop (100ms poll interval)
- Per dequeued PersonId: creates a scoped ObjectSpace via `IObjectSpaceFactory`, loads person, calls `IPersonStateService.ComputeForPerson(...)`, runs `StateTransitionDetector`, updates `PersonStateSnapshot` rows, calls `IStateNotificationDispatcher` for any transitions
- Deduplicates: if same PersonId appears multiple times in queue, coalesces into one computation

> **Why queue, not synchronous?** XAF's ObjectSpace is not committed when `OnSaving` fires — related objects may not yet reflect their new state. Deferring to after-commit via the background drainer ensures the computation sees the fully saved state.

---

### Step 7 — Nightly Background Job

**`NightlyStateRecomputeService.cs`** (`BackgroundService`):
- Timer fires daily at configurable time (default: 00:05 local time)
- Creates a scoped ObjectSpace, calls `IPersonStateService.ComputeAll(objectSpace)`
- For each `PersonStateResult`, runs `StateTransitionDetector` against existing `PersonStateSnapshot` rows
- Updates all snapshots, dispatches notifications for any transitions
- Logs counts: persons evaluated, transitions detected, notifications sent

---

### Step 8 — Notification Dispatch

**`IStateNotificationDispatcher.cs`**
```csharp
public interface IStateNotificationDispatcher
{
    Task DispatchAsync(IReadOnlyList<StateTransition> transitions, IObjectSpace objectSpace);
}
```

**`XafNotificationDispatcher.cs`** — in-app browser push via XAF `NotificationsModule`. Creates a `Notification` object per transition targeting the relevant application users. Message format:
> `"[Person Name] — [BO Type] is now [State Label]. [N days remaining if applicable.]"`
> Direct link to Person record included.

**`EmailFallbackDispatcher.cs`** — sends email when push notification has not been acknowledged within a configurable window (e.g. 30 minutes). Uses the existing mail service configured in the project.

**Recipients:** All active `ApplicationUser` records (HR coordinators, compliance officers). Future enhancement: role-based routing (e.g. `CheckOutOverdue` → compliance officer only).

---

## Consumers Summary

| Consumer | API Called | Notes |
|---|---|---|
| **Dashboard tiles** | `ComputeAll()` → group by `StateCode` → count per state | Fresh on each dashboard load |
| **Push notifications** | `StateTransitionDetector` → `IStateNotificationDispatcher` | Driven by on-save queue + nightly job |
| **List view color coding** | `BoStateResult.Severity` → CSS color map | `Critical` → red, `Warning` → orange, `Info` → blue |
| **Person detail badges** | `ComputeForPerson()` on detail view open | Returns all BO states for the person |

## Severity → Color Map

| Severity | Color | Examples |
|---|---|---|
| `None` | Default | `Active`, `Archived`, `NotPresent` |
| `Info` | Blue | `ExtensionInProgress`, `CheckOutInProgress`, `RegistrationInProgress` |
| `Warning` | Orange | `ExpiringSoon`, `ExtensionApplicationRequired`, `CheckOutRequired`, `ArrivedPendingRegistration` |
| `Critical` | Red | `Expired`, `DepartureRequired`, `CancelledCheckOutRequired`, `RegistrationOverdue` |
| `Breach` | Dark red / bold | `CheckOutOverdue`, `CancelledCheckOutOverdue`, `DepartureOverdue` |

---

## Files to Create or Modify

| File | Action |
|---|---|
| `Visa2026.Module/Services/StateEvaluation/` (entire folder) | **Create** |
| `Visa2026.Module/BusinessObjects/PersonStateSnapshot.cs` | **Create** |
| `Visa2026.Module/BusinessObjects/ExpirationLogicHelper.cs` | **Modify** — add `StateEvaluationSettings` overload |
| `Visa2026.Module/BusinessObjects/SystemSettings.cs` | **Modify** — add threshold fields; add `StateEvaluationSettings.FromSystemSettings` factory |
| `Visa2026.Module/BusinessObjects/Visa.cs` | **Modify** — add queue enqueue to `OnSaving` |
| `Visa2026.Module/BusinessObjects/WorkPermitItem.cs` | **Modify** — add queue enqueue |
| `Visa2026.Module/BusinessObjects/Passport.cs` | **Modify** — add queue enqueue |
| `Visa2026.Module/BusinessObjects/EmployeeContract.cs` | **Modify** — add queue enqueue |
| `Visa2026.Module/BusinessObjects/MedicalRecord.cs` | **Modify** — add queue enqueue |
| `Visa2026.Module/BusinessObjects/AddressOfResidence.cs` | **Modify** — add queue enqueue |
| `Visa2026.Module/BusinessObjects/Registration.cs` | **Modify** — add queue enqueue |
| `Visa2026.Module/BusinessObjects/TravelHistory.cs` | **Modify** — add queue enqueue |
| `Visa2026.Module/DatabaseUpdate/Visa2026EFCoreDbContext.cs` | **Modify** — add `DbSet<PersonStateSnapshot>` |
| `Visa2026.Blazor.Server/Startup.cs` | **Modify** — register services and hosted services |
| `Visa2026.Blazor.Server/Services/` (notification + background services) | **Create** |