---
name: visa2026-application-progress
description: >-
  Manages Visa2026 ApplicationProgress workflow: append-only progress history,
  ministry legs on ProjectContract, route/transition validation, ministry decision
  letter file (MinistryLetterFile), contract snapshots, officer UX on progress
  detail, and unit tests for resolver/transition helpers. Use for approval process
  bugs, illegal next steps, ministry name labels, schema drift on ApplicationProgresses,
  contract leg configuration — not ListView row colors (visa2026-bo-state-colors).
  Always read learnings.md first; append after verified fixes.
disable-model-invocation: false
---

# Visa2026 — Application progress & ministry approval

**User prompts:** [prompts.md](./prompts.md)

## Agent workflow (every task — mandatory)

1. **Read** [learnings.md](./learnings.md) (**## Entries**, newest first) and **Scenarios** below.
2. **Classify** — workflow/validation/contract/ministry file (**this skill**) vs row color registry (**[visa2026-bo-state-colors](../visa2026-bo-state-colors/SKILL.md)**).
3. **Fix** in **Visa2026.Module** (BOs, helpers, controllers, updaters, catalogs) — Blazor only for progress ListView appearance controllers.
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; add/update tests under `Visa2026.Module.Tests/BusinessObjects/` when logic changes.
5. **Record** — append [learnings.md](./learnings.md) after **verified** fix ([MATURITY.md](./MATURITY.md)).
6. **Promote** — same root cause twice → **Scenarios** row; three times → [reference.md](./reference.md) or triage bullet here.

## Canonical docs

| Doc | Topic |
|-----|--------|
| [`docs/APPLICATION_PROGRESS_STATE_VALIDATION.md`](../../../docs/APPLICATION_PROGRESS_STATE_VALIDATION.md) | Transition graphs, SLA, validation layers |
| [`docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md`](../../../docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md) | Ministry legs, contract model, officer rules |
| [`docs/APPLICATION_PROGRESS_DOMAIN_NOTES.md`](../../../docs/APPLICATION_PROGRESS_DOMAIN_NOTES.md) | Route-on-type design |
| [`docs/DEPRECATED.md`](../../../docs/DEPRECATED.md) | **`ApplicationStatus` enum** — do not extend |

**Related skills:**

| Topic | Skill |
|-------|--------|
| ListView row colors, `BoStateAppearanceColors`, `PrimaryStateCode` tint | [visa2026-bo-state-colors](../visa2026-bo-state-colors/SKILL.md) |
| Tenant `ProjectContract` / `ApprovingMinistry` JSON seed | [visa2026-lookup-data](../visa2026-lookup-data/SKILL.md) |
| `Invalid column name`, Docker deploy, `FORCE_XAF_DB_UPDATE` | [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) |
| Officer E2E on Applications list | [visa2026-easytest-e2e](../visa2026-easytest-e2e/SKILL.md) |

**Long reference:** [reference.md](./reference.md) · **Experience log:** [learnings.md](./learnings.md) · **Maturity:** [MATURITY.md](./MATURITY.md)

---

## Scope

| In scope | Out of scope |
|----------|----------------|
| `ApplicationProgress` rows (`State`, `Location`, `Date`, `MinistryLetterFile`) | Resminamalar / Word reports |
| `ApplicationProgressTransitionHelper` legal next steps | PDF form mapping |
| `ProjectContract` + `ProjectContractMinistryLeg` + `ApprovingMinistry` | `ApplicationStatus` enum (deprecated) |
| `ApplicationApprovalLegSnapshot` on save | Non-progress BO state evaluators |
| Progress detail UX (datasources, defaults, letter upload visibility) | Dashboard SQL views |
| Unit tests for resolver / leg codes / transitions | |

---

## Mental model

1. **Append-only history** — officers add rows to `Application.ProgressHistory`; effective position = latest by `Date`, then `ID` (`ApplicationProgressHelper.GetLatest`).
2. **Two axes** — `ApplicationType.ApplicationProgressRoute` (ministries vs direct migration) + **leg count** from contract/snapshot/type default.
3. **One contract row = one process** — `ProjectContract` carries ordered `MinistryLegs`; multiple rows may share the same `Code` (e.g. GT-15 one-leg vs three-leg variants).
4. **Snapshot on contract change** — `ProjectContractMinistryHelper.ApplySnapshot` copies leg labels to `Application.ApprovalLegSnapshots` (never `ObservableCollection.Clear()` on snapshots — EF rejects Reset).
5. **Ministry letter** — optional `FileData MinistryLetterFile` on progress rows where `ApplicationProgressLegCodes.IsMinistryDecisionStateCode` (`*_REVIEW_APPROVED` / `*_REVIEW_REJECTED`); hidden by `[Appearance]` otherwise.

---

## Scenarios (check first)

| Symptom | First step | Likely fix area |
|---------|------------|-----------------|
| `Invalid column name 'MinistryLetterFileID'` | [learnings.md](./learnings.md); confirm `ApplicationProgressMinistryLetterFileSchemaUpdater` in `Module.cs` | Pre-schema SQL updater |
| Illegal next state/location on save | Trace `ApplicationProgressTransitionHelper.TryValidateProgressStep` | Leg count vs transition graph |
| Contract required / legs required message | `ApplicationProgressProfileResolver.TryValidate*` | `ProjectContract` + `MinistryLegs` |
| Ministry column empty on progress | `ApprovalLegSnapshots` + `ApprovalLegProfileMinistryHelper.GetMinistryShortNameForProgressStep` (falls back to live profile legs) | Snapshot missing on import; heal via `EnsureSnapshots` on Application save |
| Letter upload hidden when it should show | `IsMinistryDecisionStateCode` + `[Appearance]` on `MinistryLetterFile` | State code or criteria |
| Application list row color wrong | **`visa2026-bo-state-colors`** — `PrimaryStateCode`, `BoStateAppearanceColors` | Not transition helper |
| Cannot edit contract legs | `ProjectContractMinistryController` — duplicate contract row instead | Structural immutability |
| New ministry leg count (e.g. 4th) | Add catalog rows `4_REVIEW_*`, `AT_THE_MINISTERY_4`; bump manifest; extend loop in helpers (max `MaxLegCount`) | Catalog + helpers |

---

## Common tasks

### Add or change a progress state code

1. Add row to `DatabaseUpdate/LookupCatalogs/application-state.json` (and `application-location.json` if new location).
2. Bump global `manifest.json` version if DB already current.
3. Register color in [`docs/BO_STATE_COLORS.md`](../../../docs/BO_STATE_COLORS.md) (**bo-state-colors** skill).
4. If ministry leg pattern `{n}_REVIEW_*` — prefer `ApplicationProgressLegCodes` builders; avoid hard-coded branches per leg in C#.
5. Extend transition graph only if not covered by dynamic loop in `ApplicationProgressTransitionHelper.GetTransitions`.

### Change ministry leg configuration

1. Edit **Lookup → Organization → Project contracts** (child **MinistryLegs**) or tenant seed.
2. Active contract must have ≥1 leg (`ProjectContractMinistryController`).
3. Referenced contracts: **duplicate** row to change leg order/count — structural edits blocked.
4. Run `GenerateModelLocalization` if new UI strings.

### Ministry decision letter (scalar file)

- Property: `ApplicationProgress.MinistryLetterFile` (`FileData`, `[FileAttachment]`).
- Visible when `IsMinistryDecisionStep` (approved/rejected ministry states only).
- Validation: size via `SystemSettings.MaxDocumentSizeInMB`; content via `DocumentFileUploadConstraints`.
- Schema: `ApplicationProgressMinistryLetterFileSchemaUpdater` + `ApplicationProgressMinistryLetterFileSchemaSql` (idempotent SQL before EF sync).

### Unit tests

Add tests under `Visa2026.Module.Tests/BusinessObjects/` (project not in slnx yet — build `.csproj` directly):

```powershell
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug -p:EnableSourceLink=false
```

Patterns: `ApplicationProgressProfileResolverTests`, `ApplicationProgressTransitionHelperThreeLegTests`, `ApplicationProgressLegCodesDecisionTests`.

---

## Definition of done

- [ ] Logic in **Module**; Blazor only if ListView appearance controller needs change
- [ ] Transition + contract validation still pass for direct-migration and 1…N ministry routes
- [ ] UI strings in `UiStrings.messages.json` / `entities.json` + regenerate localization when user-facing
- [ ] Schema change has **ModuleUpdater** SQL if EF might run before column exists
- [ ] Unit test for new branch in resolver, leg codes, or transitions
- [ ] Append [learnings.md](./learnings.md) when fix was non-obvious

---

## Additional resources

- [reference.md](./reference.md) — file map, validation pipeline, resolver order
- [prompts.md](./prompts.md) — copy-paste chat openers
