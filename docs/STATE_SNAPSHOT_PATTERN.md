# State Snapshot Pattern

This document defines a practical pattern for tracking complex Business Object (BO) states in a way that stays consistent across dashboard counts, list navigation, notifications, and automation.

It is intended as a shared reference for both developers and AI assistants working in this repository.

---

## 1) Problem This Pattern Solves

State logic becomes hard to maintain when:

- state is recalculated in multiple places (UI, controllers, SQL views, services)
- state depends on relationships across multiple BOs (`Visa`, `Registration`, `Application`, `ApplicationProgress`, etc.)
- dashboard count logic and click-through list logic diverge
- no durable explanation exists for why a record matched a state

The result is frequent mismatches, difficult debugging, and fragile changes.

---

## 2) Pattern Name

Use this standard naming in code/docs:

- **Primary name:** `State Snapshot Pattern`
- **Architectural alias:** `Derived State Projection`

Recommended combined phrase:

- **State Snapshot (Derived State Projection)**

---

## 3) Core Concept

Treat state as **materialized derived domain data**:

1. Build a normalized facts view for each tracked owner BO.
2. Evaluate deterministic rules in one place.
3. Persist **all matched states** to a snapshot store.
4. Read the snapshot everywhere (dashboard, list filters, notifications).

This turns state into a stable read model rather than repeated ad-hoc query logic.

### Important modeling choice for this project

- States are **non-exclusive**.
- One BO can match multiple states at the same time.
- This pattern does **not** require a primary state or partition rules.

---

## 4) Pattern Components

### 4.1 Snapshot Store

A persisted entity/table per tracked owner type (or a generalized table) containing one row **per matched state**:

- `OwnerId` (e.g., `VisaId`)
- `BoType` (if generalized)
- `StateCode` (one matched code per row)
- `Severity`
- `EvaluatedAtUtc`
- `RuleVersion`
- `Reason`
- optional dependency pointers (`LastCheckoutApplicationId`, `LatestProgressId`, etc.)

### 4.2 Facts Builder

A query layer that gathers all inputs needed for evaluation into a single facts model.

Example for visa departure states:

- visa base flags and dates
- person linkage
- registration linkage to current visa
- latest relevant checkout application
- latest application progress state

### 4.3 State Evaluator

A pure deterministic evaluator:

- input: `Facts`
- output: `MatchedStates[]` where each item has `StateCode`, `Severity`, `Reason`
- explicit rule priority
- no side effects

### 4.4 Recompute Orchestrator

Detects changes in dependent BOs and recomputes affected owners.

Typical triggers:

- on-save hooks
- background queue worker
- nightly full or incremental sweep

### 4.5 Snapshot Writer (Upsert/Synchronize)

Synchronizes evaluator results atomically and idempotently:

- insert newly matched states
- update still-matched states
- deactivate/remove states that no longer match

### 4.6 Read Model Consumers

All UX flows read snapshots only:

- dashboard counts (by `StateCode`)
- click-through list criteria
- notification generation

No duplication of business criteria in UI.

### 4.7 Observability + Governance

- state transition history (optional but recommended)
- diagnostics via `Reason`
- rule versioning and regression tests

---

## 5) `Reason` Property

`Reason` is a compact explanation of why a specific matched state row exists.

Use deterministic key-value style, not prose paragraphs.

Examples:

- `Rule=V06B; CheckoutLatestState=PROCESS_STARTED; WorkingDays=5`
- `Rule=V06A; NoCheckoutApp=true; WorkingDays=2; HasRegistrationForVisa=true`
- `Rule=Excluded; MissingRegistrationForCurrentVisa=true`

Benefits:

- fast debugging/support
- clear auditability of state assignment
- easier validation of seeded scenarios and edge cases

---

## 6) Lifecycle (End-to-End)

1. A dependency BO changes (e.g., `ApplicationProgress`).
2. Orchestrator resolves impacted owners (e.g., affected `VisaId`s).
3. Facts Builder loads normalized facts.
4. Evaluator computes the matched state set deterministically.
5. Snapshot Writer synchronizes state rows for that owner.
6. UI and notifications consume snapshot state.

---

## 7) Implementation Rules (Project Guidance)

1. **One evaluator per owner BO type** (or per logical state family).
2. **One source of truth** for rule priority.
3. **No rule duplication in UI controllers/components.**
4. **Dashboard and navigation must query the same snapshot state.**
5. **Include `RuleVersion`** in snapshots when changing rule logic over time.
6. **Enforce deterministic behavior** for identical facts inputs.
7. **Do not force exclusivity.** Persist all matched states.

---

## 8) Suggested Snapshot Schema (Example)

For `Visa` state tracking:

- `VisaStateSnapshot` (one row per matched state)
  - `Visa` (FK)
  - `StateCode`
  - `IsActive`
  - `Severity`
  - `Reason`
  - `RuleVersion`
  - `EvaluatedAtUtc`
  - `LastCheckoutApplicationId` (nullable)
  - `LastCheckoutStateCode` (nullable)

Suggested unique key:

- (`VisaId`, `StateCode`)

This can be generalized later if needed.

---

## 9) Testing Strategy

Prefer evaluator-level tests with scenario fixtures:

- build facts for each business scenario
- assert full matched state set (`StateCode[]`) and per-state `Reason`
- cover precedence conflicts and null/missing relationship paths

Then add integration tests for:

- recompute trigger correctness
- snapshot write idempotency
- dashboard/list consistency against snapshot rows

---

## 10) Rollout Plan (Low Risk)

1. Implement snapshot for one problematic family first (e.g., Visa V-06a..d).
2. Keep old logic temporarily for comparison (dual-read diagnostics if needed).
3. Switch dashboard/list for that family to snapshot source.
4. Validate with seeded scenarios.
5. Expand to additional BO state families incrementally.

---

## 11) When Not To Use This Pattern

This pattern may be unnecessary when:

- state is trivial and local to one BO with no relationship dependencies
- state is read rarely and performance/consistency constraints are low

For cross-BO operational states, this pattern is strongly preferred.

---

## 12) Summary

The State Snapshot Pattern converts fragile distributed state logic into:

- a deterministic evaluator,
- a durable and explainable multi-state snapshot,
- and a single read model for all consumers.

For this project, it is the recommended approach for relationship-dependent operational states.
