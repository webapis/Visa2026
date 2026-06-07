# UI test hooks registry

Implemented hooks in the repo. Update this file when adding or removing hooks.

**Verify command template:** see [SKILL.md](./SKILL.md) § Verify checklist.

| Status | Meaning |
|--------|---------|
| **verified** | Confirmed in DevTools on running app |
| **implemented** | Code merged; DevTools verify pending |

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

| LayoutGroup Id | data-testid | Header CSS (click target) |
|----------------|-------------|---------------------------|
| *(strip)* | `person-employee-tabs` | `.e2e-person-employee-tabs` |
| Educations | `person-employee-tab-educations` | `.e2e-person-employee-tab-educations` |
| Passports | `person-employee-tab-passports` | `.e2e-person-employee-tab-passports` |
| PositionHistory | `person-employee-tab-position-history` | `.e2e-person-employee-tab-position-history` |
| MedicalRecords | `person-employee-tab-medical-records` | `.e2e-person-employee-tab-medical-records` |
| AddressesOfResidence | `person-employee-tab-addresses-of-residence` | `.e2e-person-employee-tab-addresses-of-residence` |
| Documents | `person-employee-tab-documents` | `.e2e-person-employee-tab-documents` |
| FamilyRelationDocuments | `person-employee-tab-family-relation-documents` | `.e2e-person-employee-tab-family-relation-documents` |
| TravelHistories | `person-employee-tab-travel-histories` | `.e2e-person-employee-tab-travel-histories` |
| WorkDuties | `person-employee-tab-work-duties` | `.e2e-person-employee-tab-work-duties` |
| Salaries | `person-employee-tab-salaries` | `.e2e-person-employee-tab-salaries` |
| WorkPermitItems | `person-employee-tab-work-permit-items` | *(if tab visible on view)* |
| FamilyMembers | `person-employee-tab-family-members` | *(if tab visible on view)* |
| InvitationItems | `person-employee-tab-invitation-items` | *(if tab visible on view)* |

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
