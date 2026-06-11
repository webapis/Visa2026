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
| Runtime language switcher | `login-language-switcher` | `e2e-login-language-switcher` | Controller + Action `LanguageSwitcher` | `LogonViewE2eSelectorsController` | verified |

**Note:** Logon controller must be registered via `Visa2026BlazorApplication.CreateLogonWindowControllers`.

**Language switcher:** DevExpress `ShowLanguageSwitcher` renders a toolbar **SingleChoice** button (family **D** chrome), not a combo. Native selectors: `[data-action-name="LanguageSwitcher"]`, `.language-switcher-test-container`. Visa2026 augments via `CustomizeControl` + `E2eActionControlSelectorSupport` (same as `Logon`).

---

## Person detail — scalar fields

Controller: `PersonDetailViewE2eSelectorsController` + `PersonE2eMemberHooks`  
Applicator: `E2ePropertySelectorApplicator` (text, date, lookup, bool, image, custom editors)  
Model backup: `[ModelDefault("CustomCSSClassName", "e2e-person-{member-kebab}")]` on **required / always-visible** scalars in `Person.cs` (not optional gear-hidden members).
Views: typed `Person` detail views — `Person_DetailView_Employee`, `Person_DetailView_FamilyMember`, `Person_DetailView_TemporaryVisitor` (`PersonDetailViewE2eSelectorsController.TargetViewId`)

**Naming:** `person-{member-kebab}` — shared across typed views (same hook id on every view where the field is visible).

| Member | test id / InputId | CSS class | Status |
|--------|-------------------|-----------|--------|
| FirstName | `person-first-name` / `#person-first-name` | `e2e-person-first-name` | verified |
| LastName | `person-last-name` / `#person-last-name` | `e2e-person-last-name` | verified |
| PersonalNumber | `person-personal-number` | `e2e-person-personal-number` | verified |
| DateOfBirth | `person-date-of-birth` | `e2e-person-date-of-birth` | verified |
| BirthPlace | `person-birth-place` | `e2e-person-birth-place` | verified |
| CountryOfBirth | `person-country-of-birth` | `e2e-person-country-of-birth` | verified |
| Gender | `person-gender` | `e2e-person-gender` | verified |
| Nationality | `person-nationality` | `e2e-person-nationality` | verified |
| ForeignAddress | `person-foreign-address` | `e2e-person-foreign-address` | verified |
| ForeignAddressCountry | `person-foreign-address-country` | `e2e-person-foreign-address-country` | verified |
| ProjectContract | `person-project-contract` | `e2e-person-project-contract` | verified |
| Subcontractor | `person-subcontractor` | `e2e-person-subcontractor` | verified |
| MaritalStatus | `person-marital-status` | `e2e-person-marital-status` | verified |
| VisaApplicationFamilyMembersText | `person-visa-application-family-members-text` | `e2e-person-visa-application-family-members-text` | verified |
| Relationship | `person-relationship` | `e2e-person-relationship` | verified |

### Per typed detail view (members hooked when on layout)

| DetailView Id | Scalar members |
|---------------|----------------|
| `Person_DetailView_Employee` | all 15 rows above |
| `Person_DetailView_FamilyMember` | shared 12 + `Relationship` (no `MaritalStatus`, `VisaApplicationFamilyMembersText` — hidden for non-employee role) |
| `Person_DetailView_TemporaryVisitor` | shared 12 only |

**Verify scenarios:** `person-employee-scalar-fields`, `person-family-member-scalar-fields`, `person-temporary-visitor-scalar-fields` (or legacy `person-scalar-fields` = employee).

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

## Sidebar navigation — Application group

Controller: `NavigationE2eSelectorsController` + `NavigationE2eHooks` (same family E as People)  
Mechanism: `ShowNavigationItemAction.CustomizeControl` → `DxAccordionAdapter` → `ItemHeaderTextTemplate` wrapper  
Stable ids: XAF navigation item **Id** (`Application`, `Application_DirectMigration`, …), not localized caption.

| Nav item Id | data-testid | CSS class | View (reference) | Status |
|-------------|-------------|-----------|------------------|--------|
| Application | `nav-application` | `.e2e-nav-application` | group (expand/collapse) | verified |
| Application_DirectMigration | `nav-application-direct-migration` | `.e2e-nav-application-direct-migration` | `Application_ListView_DirectMigration` | verified |
| Application_ViaMinistries | `nav-application-via-ministries` | `.e2e-nav-application-via-ministries` | `Application_ListView_ViaMinistries` | verified |

**Verify (after logon, expand Application if collapsed):**

```javascript
document.querySelector('[data-testid="nav-application"]');
document.querySelector('.e2e-nav-application-direct-migration')?.click();
document.querySelector('[data-testid="nav-application-via-ministries"]');
```

Or: `Invoke-UiHookVerify.ps1 -Scenario nav-application`

---

## Person ListView — New / Delete actions (family C)

Controller: `PersonListViewE2eActionSelectorsController` + `PersonListViewE2eActionHooks`  
Script: `wwwroot/js/visa2026-e2e-hooks.js` (shadow pierce + `data-testid` on toolbar **New** / **Delete** `<button>`)  
Mechanism: `ViewController<ListView>` → `OnActivated` / `OnViewControlsCreated` → JS applies hooks when toolbar renders. Not `CustomizeControl` (does not reach TabbedMDI view toolbar in 25.2.6 Ribbon). URL pathname is source of truth in TabbedMDI.

| ListView Id | Action Id | data-testid | CSS class | Status |
|-------------|-----------|-------------|-----------|--------|
| `Person_ListView_Employees` | `New` | `person-list-employees-new` | `.e2e-person-list-employees-new` | verified |
| `Person_ListView_FamilyMembers` | `New` | `person-list-family-members-new` | `.e2e-person-list-family-members-new` | verified |
| `Person_ListView_TemporaryVisitors` | `New` | `person-list-temporary-visitors-new` | `.e2e-person-list-temporary-visitors-new` | verified |
| `Person_ListView_Employees` | `Delete` | `person-list-employees-delete` | `.e2e-person-list-employees-delete` | verified |
| `Person_ListView_FamilyMembers` | `Delete` | `person-list-family-members-delete` | `.e2e-person-list-family-members-delete` | verified |
| `Person_ListView_TemporaryVisitors` | `Delete` | `person-list-temporary-visitors-delete` | `.e2e-person-list-temporary-visitors-delete` | verified |

**Verify (after logon, open each ListView):**

```javascript
document.querySelector('[data-testid="person-list-employees-new"]');
document.querySelector('[data-testid="person-list-employees-delete"]');
document.querySelector('[data-testid="person-list-family-members-new"]');
document.querySelector('[data-testid="person-list-family-members-delete"]');
document.querySelector('[data-testid="person-list-temporary-visitors-new"]');
document.querySelector('[data-testid="person-list-temporary-visitors-delete"]');
```

Or: `Invoke-UiHookVerify.ps1 -Scenario person-list-employees-new` (Playwright).

---

## Person DetailView — Save actions (family C)

Controller: `PersonDetailViewE2eActionSelectorsController` + `PersonDetailViewE2eActionHooks`  
Script: `wwwroot/js/visa2026-e2e-hooks.js` (shadow pierce + `data-testid` on toolbar **Save** / **SaveAndClose** / **SaveAndNew** `<button>`)  
Mechanism: same as Person ListView toolbar hooks; URL pathname is source of truth in TabbedMDI.

| DetailView Id | Action Id | data-testid | CSS class | Status |
|---------------|-----------|-------------|-----------|--------|
| `Person_DetailView_Employee` | `Save` | `person-detail-employee-save` | `.e2e-person-detail-employee-save` | verified |
| `Person_DetailView_FamilyMember` | `Save` | `person-detail-family-member-save` | `.e2e-person-detail-family-member-save` | verified |
| `Person_DetailView_TemporaryVisitor` | `Save` | `person-detail-temporary-visitor-save` | `.e2e-person-detail-temporary-visitor-save` | verified |
| `Person_DetailView_Employee` | `SaveAndClose` | `person-detail-employee-save-and-close` | `.e2e-person-detail-employee-save-and-close` | verified |
| `Person_DetailView_FamilyMember` | `SaveAndClose` | `person-detail-family-member-save-and-close` | `.e2e-person-detail-family-member-save-and-close` | verified |
| `Person_DetailView_TemporaryVisitor` | `SaveAndClose` | `person-detail-temporary-visitor-save-and-close` | `.e2e-person-detail-temporary-visitor-save-and-close` | verified |
| `Person_DetailView_Employee` | `SaveAndNew` | `person-detail-employee-save-and-new` | `.e2e-person-detail-employee-save-and-new` | verified |
| `Person_DetailView_FamilyMember` | `SaveAndNew` | `person-detail-family-member-save-and-new` | `.e2e-person-detail-family-member-save-and-new` | verified |
| `Person_DetailView_TemporaryVisitor` | `SaveAndNew` | `person-detail-temporary-visitor-save-and-new` | `.e2e-person-detail-temporary-visitor-save-and-new` | verified |

**Verify (after logon, open a Person on each typed detail view):**

```javascript
document.querySelector('[data-testid="person-detail-employee-save"]');
document.querySelector('[data-testid="person-detail-employee-save-and-close"]');
document.querySelector('[data-testid="person-detail-employee-save-and-new"]');
document.querySelector('[data-testid="person-detail-family-member-save"]');
document.querySelector('[data-testid="person-detail-temporary-visitor-save"]');
```

Or: `Invoke-UiHookVerify.ps1 -Scenario person-detail-employee-save -StartUrl "/Person_DetailView_Employee/{guid}"`.

---

## Person detail — nested collection New (family C, scoped)

Controller: `PassportListViewE2eActionSelectorsController` + `PassportListViewE2eActionHooks`  
Script: `visa2026-e2e-hooks.js` group `personDetailNestedPassports` — pierce **New** on embedded `Passport_ListView` when URL contains `Person_DetailView`.

| ListView Id | Tab (layout) | data-testid | Status |
|-------------|--------------|-------------|--------|
| `Passport_ListView` | `Passports` | `person-employee-tab-passports-new` | verified |

**Verify:** open saved employee Person → click Passports tab → `deepQuery('[data-testid="person-employee-tab-passports-new"]')` (not top-level `document.querySelector` — shadow DOM).  
`Invoke-UiHookVerify.ps1 -Scenario person-employee-tab-passports-new -StartUrl "/Person_DetailView_Employee/{guid}"`.

---

## Passport detail — scalar fields (family A)

Controller: `PassportDetailViewE2eSelectorsController` + `PassportE2eMemberHooks`  
Model backup: `[ModelDefault("CustomCSSClassName", "e2e-passport-{member-kebab}")]` on required scalars in `Passport.cs`  
View: `Passport_DetailView`

| Member | data-testid | CSS class | Status |
|--------|-------------|-----------|--------|
| `PassportNumber` | `passport-passport-number` | `.e2e-passport-passport-number` | verified |
| `PassportType` | `passport-passport-type` | `.e2e-passport-passport-type` | verified |
| `IssueDate` | `passport-issue-date` | `.e2e-passport-issue-date` | verified |
| `ExpirationDate` | `passport-expiration-date` | `.e2e-passport-expiration-date` | verified |
| `Authority` | `passport-authority` | `.e2e-passport-authority` | verified |
| `IssuedCountry` | `passport-issued-country` | `.e2e-passport-issued-country` | verified |

**Excluded:** `Person` (waived when auto-filled), `IsCancelled`, collections, computed members.

**Batch verify:** see [SKILL.md § Batch verify](./SKILL.md#step-3--batch-verify-multiple-hooks-on-one-view).

**Verify:** `Invoke-UiHookVerify.ps1 -Scenario passport-scalar-fields -StartUrl "/Passport_DetailView/{guid}"` (set `VISA2026_HOOK_VERIFY_PASSPORT_URL`).

---

## Passport DetailView — Save (family C)

Controller: `PassportDetailViewE2eActionSelectorsController` + `PassportDetailViewE2eActionHooks`  
Script: `visa2026-e2e-hooks.js` group `passportDetail`

| DetailView Id | Action Id | data-testid | Status |
|---------------|-----------|-------------|--------|
| `Passport_DetailView` | `Save` | `passport-detail-save` | verified |

---

## Backlog (not hooked)

| Area | Notes |
|------|--------|
| Save / Delete actions (other views) | Standard XAF actions — add per journey |
| New / Delete on other ListViews | Reuse `PersonListViewE2eActionSelectorsController` + `visa2026-e2e-hooks.js` pattern |
| ApplicationType quick code editor | Custom editor — needs dedicated controller |
| Application nested tabs | `Application_DetailView` `TabbedGroup` `Tabs` |
| MDI top tabs | Dynamic captions — ViewId + object key |
| ListView rows | Grid column hooks TBD |
