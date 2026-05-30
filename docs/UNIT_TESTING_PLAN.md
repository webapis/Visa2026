# Visa2026 — Unit testing plan

Status: **Draft v0.1**  
Last updated: 2026-05-30

---

## 1. Purpose

This document answers: **which business objects, functions, and code should get unit tests** (and which should not). It is the implementation backlog for **`Visa2026.Module.Tests`**, separate from UI/E2E coverage.

| Document | Role |
|----------|------|
| [`TESTING_PLAN.md`](TESTING_PLAN.md) | Full pyramid (unit / integration / E2E), CI, E2E inventory |
| **This file** | **What to unit-test** in the Module — by type, file, BO, priority |
| [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) | Golden state **codes** and criteria for evaluator tests |
| [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md) | BR-xxx rules (especially BR-050–065) |
| [`.cursor/skills/visa2026-unit-tests/SKILL.md`](../.cursor/skills/visa2026-unit-tests/SKILL.md) | How to scaffold, run, and record experience (`learnings.md`) |

**Not in scope here:** EasyTest steps, Blazor editors, Selenium — see [`Visa2026.E2E.Tests/README.md`](../Visa2026.E2E.Tests/README.md).

---

## 2. Principles

1. **Unit-test pure Module logic** — static evaluators, string/date parsers, format builders, readiness maps — with in-memory BOs, no SQL.
2. **Do not unit-test the UI** — XAF controllers, Appearance, Blazor components → E2E or host test hooks only.
3. **Prefer one evaluator file = one test class** — branches mirror [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) state codes.
4. **BR-050+ belongs here, not in E2E** — ministry/process dimensions on `Visa` need many cases; E2E keeps one smoke path per journey.
5. **`RuleFromBoolProperty` on BOs** — unit-test only if logic is **`internal static`** / extracted helper; otherwise **integration** with `ObjectSpace` or defer.
6. **SQL dashboard states** — criteria live in views ([`SqlViewsUpdater.cs`](../Visa2026.Module/DatabaseUpdate/SqlViewsUpdater.cs)); cover with **integration** (IT-011), not duplicated unit tests unless logic is lifted to C#.

---

## 3. Test project (prerequisite)

| ID | Task | Status |
|----|------|--------|
| **UT-001** | Add `Visa2026.Module.Tests` (`net8.0`, xUnit, reference **Module only**), register in `Visa2026.slnx` | Not started |
| **UT-002** | Add `InternalsVisibleTo` for `Visa2026.Module.Tests` on Module (for `internal static` helpers) | Not started |
| **UT-003** | Add `TestSupport/` factories (`TestVisaFactory`, `TestWorkPermitItemFactory`, `StateEvaluationSettings` defaults) | Not started |

Commands and csproj template: [visa2026-unit-tests `reference.md`](../.cursor/skills/visa2026-unit-tests/reference.md).

---

## 4. Priority legend

| Priority | Meaning | Target |
|----------|---------|--------|
| **P0** | Compliance / dashboard trust; static evaluators already in repo | First sprint after UT-001 |
| **P1** | High-value pure helpers; BR-063–065 rejection rules | Second sprint |
| **P2** | Reports/zip naming; merge dictionaries; readiness maps | As reports/PDF work touches code |
| **P3** | Extract-and-test from BOs; planned SQL-only states | When implementing new STATE_SPECIFICATIONS |

---

## 5. P0 — State evaluators (unit)

One test class per evaluator under `Visa2026.Module.Tests/Services/StateEvaluation/`.  
Source of truth for **state codes**: [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) (match `BoStateResult.StateCode` exactly).

| ID | Evaluator | Primary BO | Spec section | Implemented states (approx.) | First tests to write |
|----|-----------|------------|--------------|-------------------------------|----------------------|
| **UT-010** | [`VisaStateEvaluator`](../Visa2026.Module/Services/StateEvaluation/Evaluators/VisaStateEvaluator.cs) | `Visa` | Visa States | 23 | `NoVisa`, `Cancelled`, `Archived`, `Expired`, `ExpiringSoon`, `Extended`, `Changed`, `Active`; threshold edge cases via `StateEvaluationSettings` |
| **UT-011** | [`WorkPermitItemStateEvaluator`](../Visa2026.Module/Services/StateEvaluation/Evaluators/WorkPermitItemStateEvaluator.cs) | `WorkPermitItem` | Work Permit States | 7 of 16 | Same validity ladder as visa (cancelled / archived / expired / expiring / extended / active) |
| **UT-013** | [`PassportStateEvaluator`](../Visa2026.Module/Services/StateEvaluation/Evaluators/PassportStateEvaluator.cs) | `Passport` | Passport States | 5 | Expired, expiring soon, active, no passport |
| **UT-014** | [`MedicalRecordStateEvaluator`](../Visa2026.Module/Services/StateEvaluation/Evaluators/MedicalRecordStateEvaluator.cs) | `MedicalRecord` | Medical Record States | 4 of 5 | Validity + missing record |
| **UT-015** | [`EmployeeContractStateEvaluator`](../Visa2026.Module/Services/StateEvaluation/Evaluators/EmployeeContractStateEvaluator.cs) | `EmployeeContract` | Employee Contract States | 4 | Contract end / active / expired |
| **UT-016** | [`AddressOfResidenceStateEvaluator`](../Visa2026.Module/Services/StateEvaluation/Evaluators/AddressOfResidenceStateEvaluator.cs) | `AddressOfResidence` | Registration (address slice) | 4 of 14 registration | Private-house date range + expiry branches |

**Planned evaluators (no C# file yet — unit tests follow implementation):**

| Spec section | BO(s) | Notes |
|--------------|-------|--------|
| Registration States (remaining) | `Visa`, `ApplicationItem`, registration tracking BOs | Many states are **SQL**-backed today — see §8 |
| Invitation States | `Invitation`, `InvitationItem` | 16 states **Planned** in spec; add evaluator + UT when implemented |
| Work Permit States (remaining 9) | `WorkPermitItem` | Extend UT-011 as branches land in evaluator |
| Visa process dimensions BR-053–061 | `Visa` + linked `ApplicationProgress` | Requires **fixture graph** (visa + application + progress); start with **unit** only after logic is extracted from BO/SQL into testable C# |

**Shared test concerns (all P0 evaluators):**

- `DateTime.Today` — use fixed “today” in arrange (same calendar date for start/expiry) or document flakiness; consider injecting clock only if production code is refactored.
- `DaysRemaining` on BOs — set `StartDate` / `ExpirationDate` explicitly in factories.
- `StateEvaluationSettings.ExpirationWarningThreshold` and `DefaultExpiringSoonDays` — table-driven cases.

---

## 6. P1 — Pure services and helpers (unit)

No `IObjectSpace` required (or test only the pure methods).

| ID | Type / class | What to test | Example cases |
|----|--------------|--------------|---------------|
| **UT-020** | [`CommaSeparatedSelectionHelper`](../Visa2026.Module/Services/CommaSeparatedSelectionHelper.cs) | `ParseSelected`, `FormatSelected`, `IsNoneValue`, `ReplaceLabel` | `Ýok`, empty, duplicates, trim, comma lists |
| **UT-021** | [`BorderZoneSelectionHelper`](../Visa2026.Module/Services/BorderZoneSelectionHelper.cs) | Alias delegates to UT-020 | Smoke delegate tests optional |
| **UT-022** | [`VisaFamilyMemberLinesHelper.Parse`](../Visa2026.Module/Services/VisaFamilyMemberLinesHelper.cs) | Line parser DTOs | Valid lines, bad dates, empty input |
| **UT-023** | [`ZipEntryFileNameSanitizer`](../Visa2026.Module/Services/ZipEntryFileNameSanitizer.cs) | `Sanitize`, `BuildReportEntryName`, `ToBundleEntryName`, `EnsureUnique` | Invalid chars, `3/-433`, long names, collision `_2` |
| **UT-024** | [`ApplicationTypeDevelopmentReadiness`](../Visa2026.Module/Services/ApplicationTypeDevelopmentReadiness.cs) | `GetStatus`, `CanSelectOnApplicationForm` | `101` → Pending; unknown → NotReady |
| **UT-025** | [`ApplicationTypeCodePickerHelper`](../Visa2026.Module/Services/ApplicationTypeCodePickerHelper.cs) | `GetSelectionCodeGroupKey` (via InternalsVisibleTo) | `101` → `"1"`, invalid codes → null |
| **UT-026** | [`Education.IsValidGraduationYear`](../Visa2026.Module/BusinessObjects/Education.cs) | `internal static` | blank OK, `1949` fail, `now+11` fail, trim |
| **UT-027** | **Extract** ministry selection code + `BuildFullNumber` | Today private on `Application` / controller | Refactor to `ApplicationNumberingHelper` + `ApplicationTypeSelectionHelper`; see **§7.1** |

**BOs touched by P1 helpers (not full BO tests):** `ApplicationItem.BorderZoneLocation`, `WorkPermitItem` permitted locations, `Visa` family member text fields.

---

## 7. P1 — Business rules needing unit coverage (logic TBD in C#)

These BRs are **confirmed** in [`BUSINESS_LOGIC_BASELINE.md`](BUSINESS_LOGIC_BASELINE.md) but logic may still live in SQL or BO properties. **Unit-test as soon as code exists in Module.**

| ID | BR | Topic | BOs | Unit approach |
|----|-----|-------|-----|---------------|
| **UT-030** | BR-063–065 | Rejection person vs application | `Rejection`, `RejectionItem`, `Application`, `Visa` | Pure function: given application `PROCESS_REJECTED` + optional `RejectionItem` for person → visa/person rejected flag |
| **UT-031** | BR-050–052 | Visa changed / archived / no visa | `Visa` | Extend UT-010; superseded visa must not drive expiring-soon |
| **UT-032** | BR-053–058 | Ministry review substates | `Visa`, `ApplicationProgress` | Latest progress code → `AtMinistry1`, `Ministry1Rejected`, etc. (extract from BO computed properties if needed) |
| **UT-033** | BR-059–061 | Extension cancellation branch | `Visa`, `Application`, `ApplicationType` | Dedicated codes `ExtensionCancellationInProgress`, `ExtensionCancelled` |

**BO validation (person-in-parent checks)** — today on `RuleFromBoolProperty`:

| BO | Property / rule | Unit strategy |
|----|-----------------|---------------|
| `ApplicationItem` | `ApplicationItem_PersonUniqueInApplication` | **UT-036:** extract `ApplicationItemValidation.IsPersonUniqueInApplication(...)` |
| `WorkPermitItem` | `WorkPermitItem_EmployeeIsValid`, person unique | **P3:** same pattern |
| `InvitationItem` | Person valid / unique in invitation | **P3** |
| `RejectionItem` | Person valid / unique in rejection | **P3** — ties to UT-030 |
| `Passport` | Active passport number unique | **Integration** (DB query) unless extracted |
| `Person` | Personal number unique among active | **Integration** |

### 7.1 `Application` and `ApplicationItem` (BO-specific)

Sources: [`Application.cs`](../Visa2026.Module/BusinessObjects/Application.cs), [`ApplicationItem.cs`](../Visa2026.Module/BusinessObjects/ApplicationItem.cs).

**Do not unit-test:** generic CRUD/Save, XAF `Appearance` / tab visibility, or thin `[NotMapped]` pass-throughs (`Person_FirstName` → `Person?.FirstName`). Use E2E for type selection UI (E2E-012, E2E-020) and merge helpers (UT-040) for report placeholders.

#### `Application` — unit candidates

| ID | Target | Refactor | Test focus |
|----|--------|----------|------------|
| **UT-027a** | `BuildFullNumber` | → `ApplicationNumberingHelper` | `{PREFIX}`, `{YEAR}`, `{YEAR2}`, `{MONTH2}`, `{NUMBER}`, empty format |
| **UT-027b** | `TryGetDefaultVisaLookupKeys` | → `ApplicationTypeDefaultsHelper` | `App_Inv` → Month1/BS1/Double; `App_Inv_And_WP` → Month6/Multiple/WP; unknown → false |
| **UT-028** | `ExpirationLogicHelper` | Already in [`IExpirationLogic.cs`](../Visa2026.Module/BusinessObjects/IExpirationLogic.cs) | Used by `Application.ExpirationState`; archived / expired / % threshold |
| **UT-034** | `NumberToTurkmenWords`, `JoinTurkmenList`, `AddTurkmenCase` | → `ApplicationTurkmenTextHelper` (optional) | Count words for cancel letters; vowel harmony (genitive/ablative/dative) |
| **UT-035** | `CancelVisaCount`, `CancelWPCount`, `CancelInvCount`, `BusinessTripDurationDays` | Test on in-memory `Application` + items | Skip `IsDeleted`; visa slot counting; inclusive trip days |

**Integration (not unit):**

| ID | Target | Why |
|----|--------|-----|
| **IT-015** | `Application.OnSaving` auto-number | `ObjectSpace` query + `ModifiedObjects` max logic |
| **IT-012** | `CurrentState` / progress history | Latest `ApplicationProgress` wins |

#### `ApplicationItem` — unit candidates

| ID | Target | Refactor | Test focus |
|----|--------|----------|------------|
| **UT-036** | `IsPersonUniqueInApplication` | → `ApplicationItemValidation` static | Duplicate person on same application; ignore self and `IsDeleted` |
| **UT-037** | `IsSpouseRelationship` | → same helper or keep static public | `SPOUSE`, `aýaly`, `adamsy`, null |
| **UT-038** | `FormatVisaDateText`, `JoinVisaFieldLines` | → `ApplicationItemReportLineHelper` | Cancel visa stacked blocks |
| **UT-039** | `JoinRegistrationGelmeginFamilyMemberLine`, `PreferLookupTmThenName` | Same helper | Forma 16 §8 dash line; NameTm vs Name |
| **UT-039b** | `FM_EducationLevelTm` | — | Employee vs age &lt; 18 → `Çaga` / `Orta` |
| **UT-039c** | `UpdateApplicationItemName` | — | `{Person.FullName} - {FullApplicationNumber}` |

**Optional P2 (constructed `Person` graphs, no DB):** `BuildPdfFamilyMembersAggregateText`, `BuildSahsyKagyzFamilyStatusText`, `PdfEmployeeForHouseholdOnVisaForm` — or cover via UT-022 + UT-040.

**Defer on `ApplicationItem`:** `OnCreated` border-zone default (needs `ObjectSpace`); 50+ nav-only NotMapped report fields.

#### Suggested test classes

```
ApplicationNumberingHelperTests
ApplicationTypeDefaultsTests
ApplicationTurkmenTextHelperTests
ApplicationCancellationCountTests
ApplicationItemValidationTests
ApplicationItemSpouseRelationshipTests
ApplicationItemReportLineHelperTests
```

Shared: `ExpirationLogicHelperTests` (also used by `Visa`, `WorkPermitItem`, etc.).

---

## 8. Integration-only (same test project, different fixture)

Documented here so unit work does **not** duplicate them. Details: [`TESTING_PLAN.md`](TESTING_PLAN.md) §4.2.

| ID | Area | Why not unit |
|----|------|--------------|
| **IT-010** | `LookupCatalogSyncUpdater` + JSON catalogs | EF + `ObjectSpace` |
| **IT-011** | Dashboard SQL views vs list filters | SQL Server + seeded rows |
| **IT-012** | `ApplicationProgress` latest-wins process state | Timeline + DB |
| **IT-013** | Organization singletons `TryGetInstance` | `IObjectSpace` + tenant seed |
| **IT-014** | `ModuleUpdater` migrations (one-off) | Full schema |
| **IT-015** | `Application.OnSaving` auto-number | `ObjectSpace` + DB/uncommitted max (see §7.1) |

**SQL-backed state sections** (Registration, part of Invitation/WP): assert via IT-011 using fixtures aligned with [`STATE_SPECIFICATIONS.md`](STATE_SPECIFICATIONS.md) `Test Scenario` column.

---

## 9. P2 — Reports, PDF, and merge data (unit, golden)

High value for regressions; use **small frozen inputs** (one `Application` + one `ApplicationItem` graph).

| ID | Class | What to assert |
|----|-------|----------------|
| **UT-040** | [`UserReportMergeDataHelper`](../Visa2026.Module/Services/UserReports/UserReportMergeDataHelper.cs) | Selected keys in `BuildRegistrationForm16RowDictionary`, `BuildSanawyRowDictionary`, `BuildItemRowDictionary` — not full DOCX |
| **UT-041** | [`UserReportPlaceholderBindingHelper`](../Visa2026.Module/Services/UserReports/UserReportPlaceholderBindingHelper.cs) | Path → type resolution edge cases |
| **UT-042** | [`PdfMappingHelper`](../Visa2026.Module/Services/PdfMappingHelper.cs) | Field mapping for one canonical `PdfFormMapping` (if logic is isolatable without Spire) |
| **UT-043** | [`ApplicationSupportingDocumentsPacker.BuildItemSlug`](../Visa2026.Module/Services/ApplicationSupportingDocumentsPacker.cs) | Slug rules, `ParseGraduationYearForSort` |

**Defer:** [`WordUserReportImageInjector`](../Visa2026.Module/Services/UserReports/WordUserReportImageInjector.cs) — OpenXML; manual / tool validation per [`USER_REPORT_MAP_STANDARD.md`](USER_REPORT_MAP_STANDARD.md).

---

## 10. Explicitly defer (do not add to Module.Tests)

| Area | Reason | Where to test |
|------|--------|---------------|
| Blazor editors (`ApplicationTypeQuickCodeComponent`, etc.) | UI discovery | E2E |
| XAF controllers (`*Controller.cs`) | `Frame`, `View`, `ObjectSpace` | E2E or narrow integration |
| `ConditionalAppearance`, model-driven visibility | XAF model | E2E smoke per type |
| [`CommaSeparatedCatalogHelper.LoadCatalogNames`](../Visa2026.Module/Services/CommaSeparatedCatalogHelper.cs) | Requires `ObjectSpace` | Integration or E2E |
| [`UserReportVisibilityService`](../Visa2026.Module/Services/UserReports/UserReportVisibilityService.cs) | Criteria strings on live instances | Integration |
| [`CrossObjectSyncHelper`](../Visa2026.Module/BusinessObjects/CrossObjectSyncHelper.cs) / `StateChangeTrackingHelper` | Sync rules + evaluator expressions | Integration |
| DevExpress Reports / Word generation end-to-end | Binary output | `tools/PreviewWordReports`, E2E-041 |
| `DatabaseUpdate/*Updater` except pure static parsers | DB side effects | IT-010, IT-014 |

---

## 11. Phased roadmap

### Phase 0 — Foundation (week 1)

- [ ] UT-001 – UT-003: project + `InternalsVisibleTo` + `TestSupport`
- [ ] UT-010: `VisaStateEvaluatorTests` (~15–25 facts)
- [ ] UT-020: `CommaSeparatedSelectionHelperTests`

### Phase 1 — Validity evaluators (week 2–3)

- [ ] UT-011, UT-013, UT-014, UT-015, UT-016
- [ ] UT-024, UT-025, UT-026
- [ ] Wire PR CI: `dotnet test Visa2026.Module.Tests` on `windows-latest` or `ubuntu-latest` (no browser)

### Phase 2 — Business rules in code (when refactored)

- [ ] UT-027a–b, UT-028, UT-034–039: `Application` / `ApplicationItem` helpers (§7.1)
- [ ] UT-030 – UT-033: rejection + visa process dimensions
- [ ] IT-011: first dashboard parity test
- [ ] IT-015: `Application.OnSaving` auto-number (DB + uncommitted objects)

### Phase 3 — Reports / packer (ongoing)

- [ ] UT-040 – UT-043 as templates change

### Phase 4 — Spec catch-up

- [ ] New evaluator files from STATE_SPECIFICATIONS **Planned** rows → matching test class before merge

---

## 12. Traceability (starter)

Update **Status** when tests merge. Full E2E traceability: [`TESTING_PLAN.md`](TESTING_PLAN.md) §8.

| BR / concern | Unit IDs | Integration | E2E |
|--------------|----------|-------------|-----|
| BR-001 validity (visa/WP/passport) | UT-010, UT-011, UT-013 | IT-011 | E2E-030 |
| BR-010–012 dashboard parity | — | IT-011 | E2E-023 |
| BR-050–058 visa process substates | UT-031, UT-032 | IT-012 | — |
| BR-059–061 extension cancellation | UT-033 | IT-012 | — |
| BR-063–065 rejection granularity | UT-030 | — | — |
| Application numbering | UT-027a–b, IT-015 | IT-013 | E2E-020 |
| Application cancel counts / Turkmen report text | UT-034–035 | — | E2E-040 (smoke) |
| ApplicationItem person unique / PDF lines | UT-036–039 | — | E2E-021 |
| Border / WP location strings | UT-020 | — | — |
| Graduation year validation | UT-026 | — | — |

---

## 13. Maintenance

- New **static evaluator** branch → add **Fact** + row in §5; update STATE_SPECIFICATIONS `Test Scenario` when fixture exists.
- New **pure helper** in `Visa2026.Module/Services` → add row §6 or §9 before merge.
- Changes to **`Application` / `ApplicationItem`** save or report logic → check **§7.1** first.
- Promote repeated test patterns to [`.cursor/skills/visa2026-unit-tests/learnings.md`](../.cursor/skills/visa2026-unit-tests/learnings.md).
- Do **not** grow this file with copy-pasted test code — keep inventories and IDs; code lives in `Visa2026.Module.Tests`.

---

## 14. Summary counts (targets)

| Category | Types / files | Target unit test classes (initial) |
|----------|---------------|----------------------------------|
| State evaluators (existing) | 6 | 6 |
| Pure helpers (P1) | 8 | 7 (+ 1 after extract) |
| `Application` / `ApplicationItem` (§7.1) | 2 BOs | 7 (+ optional family/PDF) |
| BR-driven (after extract) | 4 rule groups | 4 |
| Report merge (P2) | 4 | 4 |
| **Total initial goal** | — | **~27 test classes**, **180–280** facts (estimate) |

E2E remains **~12–20** journeys; unit suite should be **fast (&lt; 30 s)** on CI without SQL.
