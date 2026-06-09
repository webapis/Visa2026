# person-employee-create-tk — scenario map

## 0. Header

| Field | Value |
|-------|--------|
| **Scenario id** | `person-employee-create-tk` |
| **Status** | Green local run (headed, :5052) |
| **Map version** | 0.1 |
| **Date** | 2026-06-08 |
| **YAML file** | [person-employee-create-tk.yaml](./person-employee-create-tk.yaml) |
| **Base scenario** | [`person-employee-create`](./person-employee-create.yaml) (en-US login + English lookup labels) |

---

## 1. Journey

Same as **person-employee-create**, but **Turkmen UI first**:

1. Open `/LoginPage`, switch culture to **tk-TM** via `login-language-switcher` + `select-listbox-item`.
2. Log in as **standarduser** (manual fill — not bundled `login:` step).
3. **New** employee, fill required scalars using **tk-TM combo display text**, **Save and Close**.

**Outcome:** employee `Person` saved under Turkmen-localized UI.

---

## 2. Navigation

| Item | Value |
|------|--------|
| **Base URL** | `http://localhost:5052` |
| **Auth** | manual logon after culture switch |
| **Culture** | `tk-TM` via switcher label `Türkmen Dili (Türkmenistan)` |
| **Paths** | `/LoginPage` → `/Person_ListView_Employees` → new `Person_DetailView_Employee` |

---

## 3. Hook inventory

Inherits all hooks from [person-employee-create_map.md](../../../.cursor/skills/visa2026-ui-scenarios/examples/person-employee-create_map.md) §3, plus:

| Hook id | Step uses | Status | Notes |
|---------|-----------|--------|-------|
| `login-language-switcher` | click | **verified** | Before logon |
| Culture list | `select-listbox-item` | **waived** | `env.targetCultureLabel` |

**Ready for YAML:** ☑ (pending first green run on `:5052`)

---

## 4. Proposed YAML

Authoritative: [person-employee-create-tk.yaml](./person-employee-create-tk.yaml).

**tk-TM `env` lookup labels** (from `LookupStrings.json` / `CountryLookupStrings.json`):

| env key | tk-TM value |
|---------|-------------|
| `employeeGender` | Erkek |
| `employeeMaritalStatus` | Aýrylşan |
| `employeeCountryOfBirth` | Türkiýe |
| `employeeNationality` | Türkiýe |
| `employeeForeignAddressCountry` | Türkiýe |
| `employeeVisaFamilyMembersText` | Ýok |
| `employeeProjectContract` | GT-15 *(tenant — unchanged)* |
| `employeeSubcontractor` | Çalyk Enerji *(tenant `LookupCatalogs/tenant/subcontractor.json`)* |

---

## 5. Blockers / run notes

| Topic | Notes |
|-------|--------|
| **Culture label** | Pin `targetCultureLabel` from DevTools if OS shows a different switcher string |
| **Unique PersonalNumber** | `E2E-CREATE-TK-010` — change suffix if duplicate validation |
| **Subcontractor combo** | If fill fails, capture exact dropdown text on `:5052` after tk-TM switch |
| **No `login:` step** | Culture must be set **before** credentials; cookie persists through logon |

---

## 6. Changelog

| Date | Change |
|------|--------|
| 2026-06-08 | Initial map + yaml — Turkmen-first variant of person-employee-create |
| 2026-06-08 | Green headed run after clean MDI / ephemeral model-difference store fix |
| 2026-06-09 | Save → **Save and Close**; assert `person-list-employees-new` after close |
