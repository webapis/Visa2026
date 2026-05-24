# State Notifications — Implementation Plan

Status: **Phase 1 complete (UI prototype)** · Phase 2+ planned  
Branch: `bo-state-notification` (commit with inbox UI + sample data)  
Last updated: 2026-05-24

---

## 1. Purpose

Visa officers need a **single inbox** for actionable issues: document validity / process states **and** missing profile data — without opening every `Person` record manually.

This plan covers the **State notifications** feature (header bell + full inbox). It complements:

| Document | Role |
|----------|------|
| [`BO_STATE_TRACKING.md`](BO_STATE_TRACKING.md) | Canonical **state codes** and business conditions per BO |
| [`STATE_TRACKING_IMPLEMENTATION_PLAN.md`](STATE_TRACKING_IMPLEMENTATION_PLAN.md) | Backend **evaluation engine**, snapshots, on-save queue, XAF push dispatcher |
| [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) | **State Dashboard** tiles (separate UI; counts must stay consistent with evaluators) |
| [`STATE_SNAPSHOT_PATTERN.md`](STATE_SNAPSHOT_PATTERN.md) | Snapshot pattern for dashboard ↔ list parity |
| [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md) | BR-010–012 (dashboard), **BR-013** (invitation prerequisites → data completeness) |

**Distinction**

| Surface | User job |
|---------|----------|
| **State Dashboard** | “How many records are in state X?” → click tile → filtered list |
| **State notifications** | “What needs my attention **now**?” → prioritized inbox → open person/document |

Both must use the **same evaluators / rules** so counts and messages never disagree.

---

## 2. Goals and UX principles

### 2.1 Officer outcomes

- See **critical** issues from anywhere (header badge).
- Review **all open** items in one place (inbox), filter by severity, status, and **category**.
- Distinguish **date/process states** from **missing data** at a glance.
- Jump to the **Person** or related BO to fix the issue.
- **Snooze** temporarily (optional policy per category — TBD in Phase 2).
- **No manual “Mark done”** — an item clears when **Sync states** shows the condition no longer applies (auto-resolve).

### 2.2 Notification categories

| `BoStateNotificationCategory` | Meaning | Example |
|-------------------------------|---------|---------|
| `ValidityState` | Expiry, extension window, registration/checkout compliance | Passport expired, WP extension required |
| `DataCompleteness` | Missing BO, missing attachment, incomplete profile for applications | No `CurrentPassport`, education without diploma copies |

### 2.3 Severity and status (inbox row)

| Field | Values (prototype) | Notes |
|-------|-------------------|--------|
| `Severity` | Critical, Warning, Info | Maps from evaluator `StateSeverity` / completeness rule |
| `Status` | Open, Snoozed, Done | **Done** = auto-resolved after sync, not user-dismissed |
| `StateCode` / `StateLabel` | Stable code + display label | e.g. `MissingPassport`, `ExpiringSoon` |
| `MissingItemLabel` | Data completeness only | e.g. `Passport`, `Diploma copies` |

Header badge shows **open critical count only**; inbox shows all severities.

---

## 3. Phase 1 — UI prototype (done)

**Goal:** Validate layout, copy, filters, and navigation before persisting notifications or wiring live evaluators.

### 3.1 What was built

| Area | Implementation |
|------|----------------|
| Navigation | **Operations → State notifications** (`BoStateNotificationInboxHost` detail view) |
| Inbox UI | Blazor `BoStateNotificationInboxComponent` (custom property editor) |
| Header | `StateNotificationHeaderBadge.razor` — bell + critical count → opens inbox (critical filter when count &gt; 0) |
| Data | `BoStateNotificationPrototypeData` — 12 sample rows (validity + missing data + snoozed/resolved) |
| Summary | `IBoStateNotificationSummaryService` → `BoStateNotificationPrototypeSummaryService` |
| Permissions | `Updater.cs` — User role: Operations nav + read on inbox host |
| Styling | `wwwroot/css/site.css` — `bo-state-inbox-*`, `bo-state-header-badge-*` |

### 3.2 Prototype limitations (intentional)

- **Sync states** — UI only (spinner + toast); no recompute.
- **Open person / Open record** — toast; no `DetailView` navigation yet.
- **Snooze** — toggles local prototype state only (not persisted).
- **Sample data** — not tied to real `Person` / `Passport` rows in the database.

### 3.3 File map (Phase 1)

```
Visa2026.Module/
  BusinessObjects/StateNotifications/
    BoStateNotificationInboxHost.cs      # Non-persistent shell
    BoStateNotificationItem.cs           # In-memory row DTO
    BoStateNotificationCategory.cs
    BoStateNotificationSeverity.cs
    BoStateNotificationStatus.cs
    BoStateNotificationPrototypeData.cs  # Remove/replace in Phase 2
  Controllers/
    BoStateNotificationInboxNavigationController.cs
    BoStateNotificationInboxViewController.cs       # Hides Save/Delete
  DatabaseUpdate/
    BoStateNotificationInboxModelUpdater.cs
    BoStateNotificationInboxDetailViewUpdater.cs
  Editors/
    BoStateNotificationInboxEditorAliases.cs
  Services/StateNotifications/
    IBoStateNotificationSummaryService.cs
    BoStateNotificationSummary.cs
    BoStateNotificationPrototypeSummaryService.cs
    BoStateNotificationInboxFilterService.cs        # Pending critical-only from header

Visa2026.Blazor.Server/
  Components/StateNotificationHeaderBadge.razor
  Editors/
    BoStateNotificationInboxComponent.razor
    BoStateNotificationInboxPropertyEditor.cs
    BoStateNotificationInboxModel.cs
  Services/BoStateNotificationNavigationHelper.cs
  Pages/_Host.cshtml                              # Registers header badge
  Startup.cs                                      # DI registrations
  wwwroot/css/site.css
```

---

## 4. Data completeness rules (Phase 2 evaluators)

Aligned with **BR-013** (invitation prerequisites) and current `Person` / `Education` model.

| Rule code | Category | Severity | Condition | Target navigation |
|-----------|----------|----------|-----------|-------------------|
| `MissingPassport` | DataCompleteness | Critical | `Person.CurrentPassport == null` | `Person` detail |
| `MissingEducation` | DataCompleteness | Warning | No row in `Person.Educations` (policy: at least one education for invitation/WP packages) | `Person` detail |
| `MissingMedicalRecord` | DataCompleteness | Critical | No active / any `MedicalRecord` on person (define: current vs any — match BR-013) | `Person` detail |
| `MissingDiplomaCopies` | DataCompleteness | Warning | `Education` exists but `Education.Documents` has no diploma-type attachment | `Education` detail |
| `PassportInsufficientValidity` | DataCompleteness | Warning | `CurrentPassport` exists but validity &lt; 6 months (BR-013) | `Passport` or `Person` |

**Future (not in prototype samples):** missing photo, missing CV (`Person.Documents`), missing `CurrentVisa` when required by process, etc.

**Evaluator home (proposed):**

```
Visa2026.Module/Services/StateNotifications/
  IDataCompletenessEvaluator.cs
  PersonDataCompletenessEvaluator.cs
  EducationAttachmentCompletenessEvaluator.cs
```

Run after person-linked saves (same queue as validity recompute — see §5).

---

## 5. Validity and process rules (Phase 2+)

Reuse existing **per-BO evaluators** under `Visa2026.Module/Services/StateEvaluation/Evaluators/`:

- `PassportStateEvaluator`, `VisaStateEvaluator`, `WorkPermitItemStateEvaluator`
- `MedicalRecordStateEvaluator`, `EmployeeContractStateEvaluator`, `AddressOfResidenceStateEvaluator`
- Registration / departure compliance (when `PersonStateService` exists — see [`STATE_TRACKING_IMPLEMENTATION_PLAN.md`](STATE_TRACKING_IMPLEMENTATION_PLAN.md))

**Emit inbox rows when:**

- Evaluated `StateSeverity` is **Warning or higher** (Info optional — product decision).
- State **changed** since last snapshot **or** row is still open and severity unchanged (nightly refresh).

**Existing persistence:** `BoStateSnapshot` (`OwnerType`, `OwnerId`, `StateCode`, `Severity`, …) — extend or align with notification deduplication; the older plan name `PersonStateSnapshot` in `STATE_TRACKING_IMPLEMENTATION_PLAN.md` refers to the same concept.

---

## 6. Phase 2 — Persistence and live inbox

### 6.1 Entity (proposed)

```csharp
// Visa2026.Module/BusinessObjects/StateNotifications/StateNotification.cs
public class StateNotification : BaseObject
{
    public virtual BoStateNotificationCategory Category { get; set; }
    public virtual BoStateNotificationSeverity Severity { get; set; }
    public virtual BoStateNotificationStatus Status { get; set; }
    public virtual string StateCode { get; set; }
    public virtual string StateLabel { get; set; }
    public virtual string Message { get; set; }
    public virtual string MissingItemLabel { get; set; }
    public virtual Person Person { get; set; }
    public virtual string TargetBoTypeName { get; set; }
    public virtual Guid? TargetBoId { get; set; }
    public virtual DateTime? EventDate { get; set; }
    public virtual int? DaysRemaining { get; set; }
    public virtual DateTime DetectedAt { get; set; }
    public virtual DateTime? HandledAt { get; set; }
    public virtual string HandledBy { get; set; }  // "State sync" | username
    public virtual DateTime? SnoozedUntil { get; set; }
    // Optional: fingerprint hash (PersonId + StateCode + TargetBoId) for dedup
}
```

### 6.2 Services

| Service | Responsibility |
|---------|----------------|
| `IStateNotificationInboxService` | Load open/snoozed/done rows for current user/tenant |
| `IStateNotificationSyncService` | **Sync states** — recompute all rules, upsert/close rows |
| `StateNotificationFactory` | Map `BoStateResult` + completeness results → `StateNotification` |
| Replace `BoStateNotificationPrototypeSummaryService` | Query DB for `IBoStateNotificationSummaryService` |

### 6.3 Inbox component changes

- Load from `IStateNotificationInboxService` instead of `BoStateNotificationPrototypeData`.
- Wire **Sync** to `IStateNotificationSyncService` (show progress, refresh list).
- Snooze → persist `SnoozedUntil`; hide until date (or show in Snoozed filter).

---

## 7. Phase 3 — Recompute triggers (align with state engine)

Follow [`STATE_TRACKING_IMPLEMENTATION_PLAN.md`](STATE_TRACKING_IMPLEMENTATION_PLAN.md) triggers:

| Trigger | Action |
|---------|--------|
| On-save queue | Enqueue `PersonId` when Passport, Visa, WorkPermitItem, MedicalRecord, Education, Registration, TravelHistory, ApplicationProgress change |
| Nightly job | Full sweep for date-driven transitions |
| **Sync states** (manual) | Officer-triggered full or incremental recompute + inbox refresh |

After recompute:

1. Compare new results to open `StateNotification` rows (keyed by person + state code + target).
2. **Close** rows whose condition no longer applies → `Status = Done`, `HandledBy = "State sync"`.
3. **Create** rows for new conditions.
4. Optionally fire **XAF push** only on **new** Critical/Warning (transition), not on every sync.

---

## 8. Phase 4 — Navigation and actions

| Action | Behavior |
|--------|----------|
| Open person | `DetailView` for `Person` (`TargetBoId`) |
| Open education | `DetailView` for `Education` |
| Open record | Generic: resolve `TargetBoTypeName` + `TargetBoId` via XAF types info |
| From header bell | `BoStateNotificationNavigationHelper.OpenStateNotifications(criticalOnly: true)` — already wired |

Remove prototype toasts in `BoStateNotificationInboxPropertyEditor`.

---

## 9. Phase 5 — Polish and integration

- **Localization:** move inbox strings to `VisaUiMessages` (Turkmen/Russian/English).
- **Header:** resolve duplicate bells (XAF notifications vs state badge); adjust `right` offset in CSS if needed.
- **Roles:** restrict Operations nav; optional per-role notification subsets.
- **Email fallback:** optional for Critical only (see state tracking plan Step 8).
- **Tests:** unit tests for completeness evaluators; E2E smoke for nav + sync (EasyTest).

---

## 10. Consistency checklist (dashboard vs inbox)

Before marking Phase 2 complete for a given rule:

- [ ] Same predicate as State Dashboard criteria / evaluator branch.
- [ ] Same severity mapping.
- [ ] Inbox count for “open + state code X” matches dashboard drill-down count (for overlapping rules).
- [ ] Document edge cases in [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) or BO_STATE_TRACKING if new.

---

## 11. Implementation order (recommended)

1. **PersonDataCompletenessEvaluator** + sync creates/closes `StateNotification` rows (high value, simpler than full visa compliance).
2. Wire inbox UI to DB + **Sync states**.
3. **Open person/education/record** navigation.
4. Integrate **Passport / Visa / WP** validity rows from existing evaluators.
5. Registration / departure compliance rows.
6. Transition-based XAF push (optional parallel track).
7. Remove `BoStateNotificationPrototypeData` and prototype badge label.

---

## 12. Open decisions

| Topic | Options | Notes |
|-------|---------|-------|
| Missing medical | “any record” vs “valid non-expired current” | BR-013 implies valid medical |
| Missing education | Zero rows vs no “primary” education | Confirm with domain |
| Diploma copies | Filter `EducationDocument` by type/category | Confirm document type codes in DB |
| Info-level inbox rows | Show vs hide | Prototype includes Info for medical expiring soon |
| Snooze on missing data | Allow vs disable | Missing data may need fix, not delay |
| Per-user vs global inbox | Shared queue for all officers | Default: shared operational queue |

---

## 13. Related commits and smoke test

**Smoke test (prototype):**

1. Log in as User role → **Operations → State notifications**.
2. Confirm summary tiles, category chips, missing-data cards.
3. Click header bell → inbox opens with critical filter when count &gt; 0.
4. **Sync states** → toast (prototype).
5. **Open person** → toast (prototype).

**After Phase 2:** repeat with a real person missing passport; fix data; **Sync** → row moves to Resolved.
