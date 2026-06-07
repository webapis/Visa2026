# UI test hooks registry

Implementation tracking: controllers, mechanisms, verify status.

| Document | Contents |
|----------|----------|
| [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) | **Verified only** — DevTools-confirmed selectors and behavior |
| **This file** | All hooks in code, including **implemented (pending verify)** |

**Verify:** see [SKILL.md](./SKILL.md) § DevTools verify checklist, or run [tools/VerifyUiTestHooks](../../../tools/VerifyUiTestHooks/README.md) (Phase 2).

| Status | Meaning | In `docs/UI_TEST_HOOKS.md`? |
|--------|---------|----------------------------|
| **verified** | DevTools access + behavior confirmed | **Yes** — full selector row |
| **implemented** | Code merged; DevTools verify pending | **No** — listed under “Implemented in code — not yet in this doc” or backlog only |

---

## Login (`AuthenticationStandardLogonParameters_Blazor_DetailView`)

| Element | test id / InputId | CSS class | Mechanism | Controller / model | Status |
|---------|-------------------|-----------|-----------|-------------------|--------|
| User Name | `login-user-name` / `#login-user-name` | `e2e-login-user-name` | Controller + Model | `LogonViewE2eSelectorsController`, `Model.xafml` | verified |
| Password | `login-password` / `#login-password` | `e2e-login-password` | Controller + Model | same | verified |
| Log In | `login-submit` | `e2e-login-submit` | Controller + ActionDesign | same | verified |

**Note:** Logon controller must be registered via `Visa2026BlazorApplication.CreateLogonWindowControllers`.

---

## Person detail — fields

| View | Element | test id / InputId | CSS class | Mechanism | Status |
|------|---------|-------------------|-----------|-----------|--------|
| Person detail views | FirstName | `person-first-name` / `#person-first-name` | `e2e-person-first-name` | `PersonDetailViewE2eSelectorsController` + `Person.cs` ModelDefault | implemented |
| Person detail views | LastName | `person-last-name` / `#person-last-name` | `e2e-person-last-name` | same | implemented |

---

## Person detail — collection tabs (`TabbedGroup` Id=`Tabs`)

Controller: `PersonDetailViewE2eTabSelectorsController`  
Model backup: `Person_DetailView_Employee` → `Model.xafml` `CustomCSSClassName` on tab groups  
Views: `Person_DetailView_Employee`, `Person_DetailView`, `Person_DetailView_FamilyMember`, `Person_DetailView_TemporaryVisitor`

| LayoutGroup Id | data-testid | Header CSS | Status |
|----------------|-------------|------------|--------|
| *(strip)* | `person-employee-tabs` | `.e2e-person-employee-tabs` | verified |
| Educations | `person-employee-tab-educations` | `.e2e-person-employee-tab-educations` | verified |
| Passports | `person-employee-tab-passports` | `.e2e-person-employee-tab-passports` | verified |
| PositionHistory | `person-employee-tab-position-history` | `.e2e-person-employee-tab-position-history` | implemented |
| MedicalRecords | `person-employee-tab-medical-records` | `.e2e-person-employee-tab-medical-records` | implemented |
| AddressesOfResidence | `person-employee-tab-addresses-of-residence` | `.e2e-person-employee-tab-addresses-of-residence` | implemented |
| Documents | `person-employee-tab-documents` | `.e2e-person-employee-tab-documents` | implemented |
| FamilyRelationDocuments | `person-employee-tab-family-relation-documents` | `.e2e-person-employee-tab-family-relation-documents` | implemented |
| TravelHistories | `person-employee-tab-travel-histories` | `.e2e-person-employee-tab-travel-histories` | implemented |
| WorkDuties | `person-employee-tab-work-duties` | `.e2e-person-employee-tab-work-duties` | implemented |
| Salaries | `person-employee-tab-salaries` | `.e2e-person-employee-tab-salaries` | implemented |
| WorkPermitItems | `person-employee-tab-work-permit-items` | `.e2e-person-employee-tab-work-permit-items` | implemented |
| FamilyMembers | `person-employee-tab-family-members` | `.e2e-person-employee-tab-family-members` | implemented |
| InvitationItems | `person-employee-tab-invitation-items` | `.e2e-person-employee-tab-invitation-items` | implemented |

**Tab verify (Passports):**

```javascript
document.querySelector('[data-testid="person-employee-tab-passports"]')?.click();
document.querySelector('.e2e-person-employee-tab-passports')?.click();
```

---

## Backlog (not hooked)

| Area | Notes |
|------|--------|
| Save / New / Delete actions | Standard XAF actions — add per journey |
| ApplicationType quick code editor | Custom editor — needs dedicated controller |
| Application nested tabs | `Application_DetailView` `TabbedGroup` `Tabs` |
| MDI top tabs | Dynamic captions — ViewId + object key |
| ListView rows | Grid column hooks TBD |
