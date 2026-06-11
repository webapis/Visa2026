# person-employee-passport-create — scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `person-employee-passport-create` |
| **Status** | YAML authored — green-run pending |
| **Map version** | 0.4 |
| **Date** | 2026-06-08 |
| **YAML file** | `person-employee-passport-create.yaml` (draft in `examples/` + `tools/UiScenarioRunner/scenarios/`) |
| **Template for** | Other Person-child BO scenarios (`Education`, `MedicalRecord`, …) |

---

## 1. Journey

Log in, create and **save** a new **employee** `Person` (Phase A — same required scalars as `person-employee-create`), **return to the Employees list** (`goto` — Save and Close is not on the new-record toolbar), **reopen** the employee from `Person_ListView_Employees` (by `PersonalNumber`), open the **Passports** tab, click **New** on the nested passports list, fill **required** `Passport` detail fields, and **Save** the passport.

**Workaround (v1):** `person-employee-tab-passports-new` is **not reliable** on the first detail mount immediately after create/save; it **is** available after close → reopen. Phase A2 encodes that path until toolbar hook is fixed at render time.

**Outcome:** a persisted `Passport` row linked to the new employee; `PassportNumber` unique among active passports; `ExpirationDate > IssueDate`.

**Prerequisite:** saved `Person` with `PersonRole = Employee` — created **in-scenario** (no fixture GUID; CI uses `-FreshDatabase`).

**Scope (v1):**

- Required / always-visible `Passport` scalars only ([`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md)).
- **Excluded:** gear-hidden `IsCancelled`; `Documents` / `Visas` / `Images` collections; `DaysRemaining` (computed); `Person` lookup if auto-filled on nested **New**.

**Parent scenario reuse:** Phase A mirrors [person-employee-create_map.md](./person-employee-create_map.md): **Save** then **`goto: /Person_ListView_Employees`** (not Save and Close — hook absent on new employee detail).

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5052` (UI Scenarios launch profile) |
| **Auth** | `login` — user `Admin` or `standarduser`, password from env / empty in dev |
| **List path** | `/Person_ListView_Employees` |
| **Parent detail** | `/Person_DetailView_Employee/{oid}` — OID from **New** + save |
| **Child detail** | `Passport_DetailView` *(nested — confirm popup vs MDI tab in DevTools)* |
| **Fixture** | **None** — parent created in-scenario |
| **Env vars** | `VISA2026_SCENARIO_*`; tenant lookup display strings; unique `passportNumber`, `employeePersonalNumber` |

**Nav sequence (YAML v1):**

```text
login
→ goto /Person_ListView_Employees
→ click person-list-employees-new
→ fill {Person required scalars}
→ click person-detail-employee-save              # Phase A — persist parent
→ goto /Person_ListView_Employees                # close detail (no SaveAndClose on new record)
→ wait-for person-list-employees-new
→ click-text ${employeePersonalNumber}           # Phase A2 — reopen (hook workaround)
→ wait-for person-employee-tabs
→ select-tab person-employee-tab-passports       # Phase B
→ click person-employee-tab-passports-new
→ fill {Passport required scalars}
→ click passport-detail-save                     # Phase C
→ (assert) Passports tab + nested New visible
```

**Waived:** sidebar `nav-people-employees` (use `goto` list path — same as `person-employee-create`).

---

## 3. Hook inventory

Status legend: **verified** = in [`UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md); **implemented** = code exists, DevTools pending; **missing** = not started; **waived** = not used in YAML v1.

### 3a. Login + Person parent (reuse)

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `login-user-name` | Logon `UserName` | login | **verified** | |
| `login-password` | Logon `Password` | login | **verified** | |
| `login-submit` | Action `Logon` | login | **verified** | |
| `person-list-employees-new` | `Person_ListView_Employees` action `New` | click | **verified** | Phase A |
| `person-detail-employee-save` | `Person_DetailView_Employee` action `Save` | click | **verified** | Phase A — stay on detail |
| `person-first-name` | `Person.FirstName` | fill | **verified** | |
| `person-last-name` | `Person.LastName` | fill | **verified** | |
| `person-personal-number` | `Person.PersonalNumber` | fill | **verified** | Unique per run |
| `person-date-of-birth` | `Person.DateOfBirth` | fill | **verified** | |
| `person-birth-place` | `Person.BirthPlace` | fill | **verified** | If on employee layout |
| `person-country-of-birth` | `Person.CountryOfBirth` | fill | **verified** | Lookup |
| `person-gender` | `Person.Gender` | fill | **verified** | Lookup display text in `env` |
| `person-marital-status` | `Person.MaritalStatus` | fill | **verified** | |
| `person-nationality` | `Person.Nationality` | fill | **verified** | |
| `person-foreign-address` | `Person.ForeignAddress` | fill | **verified** | |
| `person-foreign-address-country` | `Person.ForeignAddressCountry` | fill | **verified** | |
| `person-project-contract` | `Person.ProjectContract` | fill | **verified** | |
| `person-subcontractor` | `Person.Subcontractor` | fill | **verified** | |
| `person-visa-application-family-members-text` | `Person.VisaApplicationFamilyMembersText` | fill | **verified** | e.g. `"Ýok"` |
| `person-detail-employee-save-and-close` | Save and Close | click | **verified** | **waived** v1 — not on **new** employee detail toolbar; use Save + `goto` list |

### 3b. Person tab (reuse)

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `person-employee-tabs` | Tab strip `Tabs` | wait-for | **verified** | Optional sanity check |
| `person-employee-tab-passports` | LayoutGroup `Passports` | select-tab | **verified** | [UI_TEST_HOOKS.md § Person collection tabs](../../../docs/UI_TEST_HOOKS.md) |

### 3c. Nested Passports list toolbar (new)

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `person-employee-tab-passports-new` | Nested `Passports` ListView action `New` | click | **verified** | **After detail reopen only** (workaround v1); not on first mount post-create |
| `person-employee-tab-passports-delete` | Nested `Delete` | click | **missing** | **waived** v1 — not in create journey |

**Open question (DevTools):** nested **New** opens `Passport_DetailView` as MDI tab, modal, or inline nested detail — affects wait-for / assert steps in §4.

### 3d. Passport detail — required scalars (new)

Mechanism: family **A** — `PassportE2eMemberHooks` + `PassportDetailViewE2eSelectorsController` + `E2ePropertySelectorApplicator` (mirror `Person` pattern).

| Hook id | BO member | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `passport-passport-number` | `Passport.PassportNumber` | fill | **verified** | DevTools batch 2026-06-08 |
| `passport-passport-type` | `Passport.PassportType` | fill | **verified** | Lookup combo |
| `passport-issue-date` | `Passport.IssueDate` | fill | **verified** | `dd.MM.yyyy` mask |
| `passport-expiration-date` | `Passport.ExpirationDate` | fill | **verified** | Must be **after** `IssueDate` |
| `passport-authority` | `Passport.Authority` | fill | **verified** | Max 100 chars |
| `passport-issued-country` | `Passport.IssuedCountry` | fill | **verified** | Restored on layout; DevTools batch 2026-06-08 |
| `passport-person` | `Passport.Person` | fill | **waived** | Pre-filled from nested **New** |

**Excluded from v1 (optional / gear / collections):**

| Member | Reason |
|--------|--------|
| `IsCancelled` | Gear-hidden optional scalar |
| `Documents`, `Visas`, `Images` | Collections — separate scenarios if needed |
| `DaysRemaining`, `ShowOptionalFields` | Computed / UI chrome |
| `PersonalNumber` | `[Browsable(false)]` on BO |

### 3e. Passport detail — Save (new)

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| `passport-detail-save` | `Passport_DetailView` action `Save` | click | **verified** | Nested new passport (URL stays `Person_DetailView`); DevTools `.e2e-passport-detail-save` 2026-06-08 |
| `passport-detail-save-and-close` | `SaveAndClose` | click | **missing** | **waived** v1 — prefer Save + assert on parent tab |

### 3f. Assertion (optional v1)

| Hook id | UI target | Step uses | Status | Notes |
|---------|-----------|-----------|--------|-------|
| *(grid row)* | Passports nested ListView row | assert-visible | **missing** | **waived** v1 — assert tab + saved parent instead; add row hook later |

**Ready for YAML:** ☑ — §3 complete; YAML authored 2026-06-08; pending green run on `:5052`

---

## 4. Proposed YAML

Sketch only — do **not** promote until §3 complete and green run on `:5052`.

```yaml
id: person-employee-passport-create
description: Create employee Person, open Passports tab, add passport, save
requiresAuth: true

env:
  # Phase A — Person (unique per run)
  employeePersonalNumber: E2E-PPT-001
  employeeFirstName: Passport
  employeeLastName: Scenario
  employeeDateOfBirth: "01.01.1990"
  employeeBirthPlace: Ashgabat
  employeeCountryOfBirth: Türkmenistan
  employeeGender: Male
  employeeMaritalStatus: Single
  employeeNationality: Türkmenistan
  employeeForeignAddress: E2E foreign address
  employeeForeignAddressCountry: Türkmenistan
  employeeProjectContract: REPLACE-TENANT-PROJECT
  employeeSubcontractor: REPLACE-TENANT-SUBCONTRACTOR
  employeeVisaFamilyMembersText: "Ýok"
  # Phase C — Passport
  passportNumber: E2E-PPT-NO-001
  passportIssueDate: "01.01.2024"
  passportExpirationDate: "01.01.2034"
  passportAuthority: E2E Migration Authority
  passportType: REPLACE-TENANT-PASSPORT-TYPE-DISPLAY
  passportIssuedCountry: REPLACE-TENANT-COUNTRY-DISPLAY

steps:
  - login:
      user: Admin
      password: ""

  # --- Phase A: saved employee Person ---
  - goto: /Person_ListView_Employees
  - wait-for: person-list-employees-new
  - click: person-list-employees-new
  - wait-for: person-first-name
  - fill:
      person-first-name: ${employeeFirstName}
      person-last-name: ${employeeLastName}
      person-personal-number: ${employeePersonalNumber}
      person-date-of-birth: ${employeeDateOfBirth}
      person-birth-place: ${employeeBirthPlace}
      person-country-of-birth: ${employeeCountryOfBirth}
      person-gender: ${employeeGender}
      person-marital-status: ${employeeMaritalStatus}
      person-nationality: ${employeeNationality}
      person-foreign-address: ${employeeForeignAddress}
      person-foreign-address-country: ${employeeForeignAddressCountry}
      person-project-contract: ${employeeProjectContract}
      person-subcontractor: ${employeeSubcontractor}
      person-visa-application-family-members-text: ${employeeVisaFamilyMembersText}
  - click: person-detail-employee-save
  - goto: /Person_ListView_Employees
  - wait-for: person-list-employees-new
  - click-text: ${employeePersonalNumber}
  - wait-for: person-employee-tabs

  # --- Phase B: Passports tab + nested New ---
  - select-tab: person-employee-tab-passports
  - wait-for: person-employee-tab-passports-new
  - click: person-employee-tab-passports-new
  - wait-for: passport-passport-number

  # --- Phase C: fill + save Passport ---
  - fill:
      passport-passport-number: ${passportNumber}
      passport-passport-type: ${passportType}
      passport-issue-date: ${passportIssueDate}
      passport-expiration-date: ${passportExpirationDate}
      passport-authority: ${passportAuthority}
      passport-issued-country: ${passportIssuedCountry}
  - click: passport-detail-save

  # --- Assert (adjust after DevTools confirms nested UX) ---
  - select-tab: person-employee-tab-passports
  - wait-for: person-employee-tab-passports-new
```

---

## 5. Run-time notes

| Topic | Notes |
|-------|--------|
| **Fresh DB / CI** | Parent **must** be created in-scenario; no fixture Person OID |
| **Unique identifiers** | Distinct `employeePersonalNumber` and `passportNumber` per run (`E2E-PPT-*` prefix) |
| **Date validation** | `ExpirationDate` strictly after `IssueDate` (`Passport_DateRange` rule) |
| **Passport number uniqueness** | `IsPassportNumberUniqueAmongActive` — avoid reusing numbers across runs on non-fresh DB |
| **OnCreated defaults** | `PassportType` / `IssuedCountry` may pre-fill — omit `fill` for those hooks if defaults present on greenfield seed |
| **Lookup `fill`** | Combo boxes use tenant display text in `env` (same pattern as `person-employee-create`) |
| **Save parent before child** | Nested collection **New** may require persisted `Person` FK — Phase A **Save** is mandatory |
| **Passports New hook (workaround)** | Save + `goto` list + reopen from list before Passports tab; `click-text` opens row by `employeePersonalNumber` |
| **Nested detail UX** | Confirm popup vs MDI tab before final assert/wait steps |
| **Gear fields** | Do not expand optional fields in v1 |

---

## 6. Blockers

1. ~~Nested Passports list `New` hook~~ — **implemented** (2026-06-08); run `Invoke-UiHookVerify.ps1 -Scenario person-employee-tab-passports-new`.
2. ~~`Passport` detail scalar hooks~~ — **implemented**; verify with `-Scenario passport-scalar-fields` + `VISA2026_HOOK_VERIFY_PASSPORT_URL`.
3. ~~`Passport_DetailView` Save hook~~ — **verified** (2026-06-08; nested new passport).
4. ~~YAML~~ — **authored**; green-run: `.\scripts\local\Invoke-UiScenarioRun.ps1 -Scenario person-employee-passport-create -FreshDatabase`
5. **Nested detail presentation** — assert uses Passports tab + nested **New** (grid row hook waived v1).
6. **Optional: grid row hook** — waived v1.

---

## 7. Handoff — @visa2026-ui-test-hooks

**Implemented (2026-06-08).** Verify then return here for YAML.

Copy-paste if hooks need rework:

```markdown
Prepare hooks for scenario `person-employee-passport-create`:

**View context:** `Person_DetailView_Employee` → tab `Passports` → nested ListView **New** → `Passport_DetailView`.

**Nested list toolbar (family C):**
- `person-employee-tab-passports-new` — action `New` on embedded Passports list (shadow pierce, scoped to Passports tab panel)

**Passport detail scalars (family A — required / always-visible only):**
- `passport-passport-number` → `Passport.PassportNumber`
- `passport-passport-type` → `Passport.PassportType`
- `passport-issue-date` → `Passport.IssueDate`
- `passport-expiration-date` → `Passport.ExpirationDate`
- `passport-authority` → `Passport.Authority`
- `passport-issued-country` → `Passport.IssuedCountry`
- Waive `Passport.Person` if auto-filled on nested New

**Passport detail Save (family C):**
- `passport-detail-save` → `Passport_DetailView` action `Save`

Do not hook: `IsCancelled`, collections (`Documents`, `Visas`, `Images`), computed `DaysRemaining`.
Verify on `http://localhost:5051` or `:5052` after employee Person is saved and Passports tab is active.
Update `docs/UI_TEST_HOOKS.md` and `hooks-manifest.json`.
```

After hooks verified → return to **visa2026-ui-scenarios** for YAML + green run.

---

## 8. Changelog

| Date | Change |
|------|--------|
| 2026-06-08 | Initial map — Person-child pilot; layered §3 hook inventory; handoff block for ui-test-hooks |
| 2026-06-08 | §3c–§3e hooks implemented in Blazor.Server; status **Hooks implemented — verify pending** |
| 2026-06-08 | All §3 hooks verified; `person-employee-passport-create.yaml` authored; status **YAML authored** |
| 2026-06-08 | v0.4 — Save + `goto` list + reopen by `PersonalNumber` (`click-text`) for `person-employee-tab-passports-new` hook workaround |
| 2026-06-08 | v0.4.1 — drop SaveAndClose step (not on new employee detail); same pattern as `person-employee-create` |
