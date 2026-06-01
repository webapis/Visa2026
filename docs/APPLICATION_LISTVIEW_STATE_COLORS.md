# Application ListView — row background by current progress state

> **Status:** **Implemented.** `Application.PrimaryStateCode` (latest progress), `BoStateAppearanceColors`, `BoStateRowAppearanceRegistration`, `ApplicationProgressRowStateRefreshController`, Blazor `ApplicationProgressRowAppearanceController` + `site.css` row classes.
>
> **Related:**
> - [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) — color registry per `ApplicationState.Code`
> - [`APPLICATION_PROGRESS_DOMAIN_NOTES.md`](APPLICATION_PROGRESS_DOMAIN_NOTES.md) — Progress History UI and happy-path example
> - [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md) — progress timeline rules (separate concern)
> - [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md) — BR-037, BR-038

---

## 1. Requirement (officer / product)

On the **`Application` ListView**, each row’s **background color** must reflect **where the application is in processing now**.

That “current processing position” is **`CurrentState`**: the **`ApplicationProgress`** row that is **last in the timeline** — i.e. the most recently added / latest **`Date`** in **`ProgressHistory`** (tie-break: highest `ID`). Officers add progress rows manually; the list should **read** that latest step, not a separate manual flag.

| Surface | Behaviour |
|---------|-----------|
| **Application ListView** | Row `BackColor` (and optional `FontColor`) from **current** progress **state** |
| **Application DetailView** | No change required for this requirement (optional: show same color on header / current state field) |
| **ApplicationProgress nested ListView** | **Out of scope here** — each **history** row may use **its own** `State.Code` color later (see [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) § Application progress display) |

---

## 2. What `CurrentState` means

**Definition (canonical):**

```text
CurrentState = ApplicationProgressHelper.GetLatest(Application.ProgressHistory)
```

| Field on latest row | Used for |
|---------------------|----------|
| `State` (`ApplicationState`) | **Primary** input for row color — stable `State.Code` (e.g. `1_REVIEW_STARTED`, `PROCESS_ISSUED`) |
| `Location` (`ApplicationLocation`) | Secondary dimension in UI; **fallback** for color only when state is generic (see [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) § Application progress display) |
| `Date` | Anchor for **`DaysElapsed`** / SLA alerts — **not** the ListView color key |

**Not `CurrentState`:** any older row in `ProgressHistory`, `ApplicationStatus` enum (deprecated), or document validity on linked `Visa` / `Passport` (`DaysRemaining` — different BO, different ListView).

---

## 3. Model status in code (important)

| Item | Status |
|------|--------|
| `Application.ProgressHistory` | **Exists** — aggregated `IList<ApplicationProgress>` |
| `ApplicationProgress` (`State`, `Location`, `Date`) | **Exists** |
| `ApplicationProgressHelper.GetLatest` | **Exists** |
| `Application.CurrentState` property | **Not on `Application.cs` today** — legacy `CurrentStateID` column **dropped** by [`ApplicationLegacyColumnsCleanupUpdater`](../Visa2026.Module/DatabaseUpdate/ApplicationLegacyColumnsCleanupUpdater.cs) |
| `APPLICATION.md` still describes `CurrentState` | **Target behaviour** — docs ahead of implementation |

When this feature is implemented, prefer one of:

| Approach | Pros | Cons |
|----------|------|------|
| **A. `[NotMapped]` computed `CurrentState`** | No duplicate FK; always matches history | Criteria / Appearance must evaluate on each row load |
| **B. Persisted `CurrentState` / `CurrentStateID` + sync on progress save** | Fast ListView criteria; matches old design | Must keep in sync on insert/update/delete of progress rows |

Color wiring should not depend on officers maintaining two sources of truth. **BR-038** must hold after any approach.

---

## 4. How row color should be chosen (planned)

Follow [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) — one **tone** per `ApplicationState.Code` (and location fallback rules already drafted).

**Primary state code for Application ListView row:**

1. Resolve `latest = GetLatest(ProgressHistory)`.
2. If no progress rows → **no row tint** (or neutral default).
3. If `latest.State.Code` is terminal reject/cancel → **Red** family (`1_REVIEW_REJECTED`, `2_REVIEW_REJECTED`, `PROCESS_REJECTED`, `PROCESS_CANCELLED`).
4. If `PROCESS_ISSUED` → **Green** family.
5. If `IS_BEING_PREPARED` or (non-terminal and `latest.Location.Code == AT_OFFICE`) → **Gold** family.
6. Else → **Blue** family using `latest.State.Code` (ministry / migration in progress).

Exact hex / XAF `BackColor` names: master tables in [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) § Quick reference (Blue, Gold, Green, Red).

**Precedence vs other dimensions:** For the **Application** list only, **workflow state** wins row background. Linked visa expiry (`ExpiringSoon`, etc.) stays on **Visa** / **Person** lists ([BR-049](BUSINESS_LOGIC_BASELINE.md)).

---

## 5. Implementation pattern (reference — do not build yet)

Existing date-bound BOs (`Visa`, `Passport`, `WorkPermitItem`) use a **severity bucket** + class-level `[Appearance]` on `StateSeverityLevel` (interim). **Target** for `Application` (per skill [`visa2026-bo-state-colors`](../.cursor/skills/visa2026-bo-state-colors/SKILL.md)):

1. **`IBoListRowState`** (or equivalent) on `Application` with `[NotMapped] string PrimaryStateCode` derived from latest progress `State.Code` (+ alias map if needed).
2. **`BoStateAppearanceColors`** registry keyed by code → `BackColor` / `FontColor`.
3. **`BoStateRowAppearanceRegistration`** at module startup — criteria like `PrimaryStateCode = 'PROCESS_ISSUED'` (same idea as [`SoftDeleteAppearanceRegistration`](../Visa2026.Module/)).
4. **Refresh:** when `ProgressHistory` changes, ListView refresh so row color updates (controller `ObjectChanged` / `CollectionChanged` — mirror [`ExpirationStateRefreshController`](../Visa2026.Module/Controllers/ExpirationStateRefreshController.cs) pattern).

**Avoid:** hard-coding eleven separate `[Appearance]` attributes on `Application` without registry — duplicates [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) and complicates new catalog codes.

**Alternative (minimal):** `[Appearance]` per `ApplicationState.Code` on `Application` using criteria that call into a computed property, e.g. `CurrentProgressStateCode = '1_REVIEW_STARTED'` — acceptable if registry class is deferred.

---

## 6. Acceptance criteria (for future implementation)

- [x] **Current state** column on Application ListView (`Application.CurrentState` — localized latest progress state @ location).
- [ ] Open **Application** ListView: row background matches **latest** `ProgressHistory` row’s `State` (verified after adding a new progress row).
- [ ] Change latest progress (new row with later `Date`): ListView updates without requiring full app restart.
- [ ] Terminal states (`PROCESS_ISSUED`, `PROCESS_REJECTED`, `PROCESS_CANCELLED`, `*_REVIEW_REJECTED`) use **Green** or **Red** per registry.
- [ ] Application with **empty** `ProgressHistory`: documented neutral behaviour (no crash, consistent color).
- [ ] Soft-deleted applications: existing **Gainsboro** delete appearance still wins over state color (priority).
- [ ] Colors match [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) (no one-off colors outside registry).

---

## 7. Out of scope (this requirement)

- SLA / overdue highlighting (`DaysElapsed` > max) — separate **follow-up** signal (notification, badge); may **add** border or icon later, not replace state color.
- **ApplicationProgress** nested list row colors — separate row per history entry.
- Auto-advance progress when SLA expires — see [`APPLICATION_PROGRESS_STATE_VALIDATION.md`](APPLICATION_PROGRESS_STATE_VALIDATION.md).
- Dashboard tile counts — [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md).

---

## 8. Changelog

| Date | Note |
|------|------|
| 2026-06-01 | Requirement captured: Application ListView row color from latest `ApplicationProgress` / `CurrentState`; implementation explicitly deferred |
