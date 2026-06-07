# UI test hooks registry

Implementation tracking: controllers, mechanisms, verify status.

| Document | Contents |
|----------|----------|
| [`docs/UI_TEST_HOOKS.md`](../../../docs/UI_TEST_HOOKS.md) | **Verified only** — DevTools-confirmed selectors and behavior |
| **This file** | All hooks in code, including **implemented (pending verify)** |

**Verify:** see [SKILL.md](./SKILL.md) § Isolated verify server (`Invoke-UiHookVerify.ps1`), or run [tools/VerifyUiTestHooks](../../../tools/VerifyUiTestHooks/README.md) against an existing host.

**Mechanism families (A–F):** [SKILL.md § UI element categories](./SKILL.md#ui-element-categories-mechanism-families) — **classify → match → reuse | discover** before code ([SKILL.md § Before writing code](./SKILL.md#before-writing-code-classify--match--reuse--discover)).

| Status | Meaning | In `docs/UI_TEST_HOOKS.md`? |
|--------|---------|----------------------------|
| **verified** | DevTools access + behavior confirmed | **Yes** — full selector row |
| **implemented** | Code merged; DevTools verify pending | **No** — listed under “Implemented in code — not yet in this doc” or backlog only |

**Member scope:** hook **required / always-visible direct scalar** detail fields only — not `IList<T>` collections (use tab rows below), not computed/hidden members, not **optional gear-hidden** scalars unless the developer explicitly requested them. See [SKILL.md § BO member scope](./SKILL.md#bo-member-scope--what-to-hook) and [`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md).

---

## Login (`AuthenticationStandardLogonParameters_Blazor_DetailView`)

| Element | test id / InputId | CSS class | Mechanism | Controller / model | Status |
|---------|-------------------|-----------|-----------|-------------------|--------|
| User Name | `login-user-name` / `#login-user-name` | `e2e-login-user-name` | Controller + Model | `LogonViewE2eSelectorsController`, `Model.xafml` | verified |
| Password | `login-password` / `#login-password` | `e2e-login-password` | Controller + Model | same | verified |
| Log In | `login-submit` | `e2e-login-submit` | Controller + ActionDesign | same | verified |

**Note:** Logon controller must be registered via `Visa2026BlazorApplication.CreateLogonWindowControllers`.

---

## Person detail — scalar fields

Controller: `PersonDetailViewE2eSelectorsController` + `PersonE2eMemberHooks`  
Applicator: `E2ePropertySelectorApplicator` (text, date, lookup, bool, image, custom editors)  
Model backup: `[ModelDefault("CustomCSSClassName", "e2e-person-{member-kebab}")]` on **required / always-visible** scalars in `Person.cs` (not optional gear-hidden members).
Views: all `Person` detail views (`Person_DetailView_Employee`, `Person_DetailView`, `Person_DetailView_FamilyMember`, `Person_DetailView_TemporaryVisitor`)

**Naming:** `person-{member-kebab}` — e.g. `CountryOfBirth` → `person-country-of-birth`.

| Member | test id / InputId | CSS class | Status |
|--------|-------------------|-----------|--------|
| FirstName | `person-first-name` / `#person-first-name` | `e2e-person-first-name` | implemented |
| LastName | `person-last-name` / `#person-last-name` | `e2e-person-last-name` | implemented |
| PersonalNumber | `person-personal-number` | `e2e-person-personal-number` | implemented |
| DateOfBirth | `person-date-of-birth` | `e2e-person-date-of-birth` | implemented |
| BirthPlace | `person-birth-place` | `e2e-person-birth-place` | implemented |
| CountryOfBirth | `person-country-of-birth` | `e2e-person-country-of-birth` | implemented |
| Gender | `person-gender` | `e2e-person-gender` | implemented |
| MaritalStatus | `person-marital-status` | `e2e-person-marital-status` | implemented |
| Nationality | `person-nationality` | `e2e-person-nationality` | implemented |
| ForeignAddress | `person-foreign-address` | `e2e-person-foreign-address` | implemented |
| ForeignAddressCountry | `person-foreign-address-country` | `e2e-person-foreign-address-country` | implemented |
| ProjectContract | `person-project-contract` | `e2e-person-project-contract` | implemented |
| VisaApplicationFamilyMembersText | `person-visa-application-family-members-text` | `e2e-person-visa-application-family-members-text` | implemented |
| Relationship | `person-relationship` | `e2e-person-relationship` | implemented |
| Subcontractor | `person-subcontractor` | `e2e-person-subcontractor` | implemented |

**Excluded:** collections (`Passports`, …), computed/hidden members (`FullName`, `Age`, `PersonRole`, …), optional-field gear (`ShowOptionalFields`), **optional gear-hidden scalars** (`MiddleName`, `Email`, `Photo`, `HireDate`, `IsArchived`, `SponsoringEmployee`) — hook only when developer explicitly opts in; see [`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md).

**Verify (example):**

```javascript
document.querySelector('#person-first-name');
document.querySelector('[data-testid="person-date-of-birth"]');
document.querySelector('.e2e-person-gender');
```

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

## Sidebar navigation — People group

Controller: `NavigationE2eSelectorsController` + `NavigationE2eHooks`  
Support: `NavigationE2eSelectorSupport` (parallel ChoiceActionItem / NavMenuItem tree walk)  
Mechanism: `ShowNavigationItemAction.CustomizeControl` → `DxAccordionAdapter.ComponentModel.ItemHeaderTextTemplate` wrapper with `data-testid` + `.e2e-*`  
Stable ids: XAF navigation item **Id** (`People`, `Employees`, …), not localized caption.

| Nav item Id | data-testid | CSS class | View (reference) | Status |
|-------------|-------------|-----------|------------------|--------|
| People | `nav-people` | `.e2e-nav-people` | group (expand/collapse) | verified |
| Employees | `nav-people-employees` | `.e2e-nav-people-employees` | `Person_ListView_Employees` | verified |
| FamilyMembers | `nav-people-family-members` | `.e2e-nav-people-family-members` | `Person_ListView_FamilyMembers` | verified |
| TemporaryVisitors | `nav-people-temporary-visitors` | `.e2e-nav-people-temporary-visitors` | `Person_ListView_TemporaryVisitors` | verified |

**Verify (after logon, expand People if collapsed):**

```javascript
document.querySelector('[data-testid="nav-people"]');
document.querySelector('[data-testid="nav-people-employees"]')?.click();
document.querySelector('.e2e-nav-people-family-members');
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
