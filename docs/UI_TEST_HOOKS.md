# UI test hooks — element access reference

**Prepared by:** [`.cursor/skills/visa2026-ui-test-hooks/`](../.cursor/skills/visa2026-ui-test-hooks/SKILL.md)

Catalog of **prepared and verified** UI accessibility — stable CSS selectors for business object properties, layout tabs, actions, and controls, proven on the **live rendered UI**.

1. **Access** — `document.querySelector(...)` returns a non-null node on the documented DOM target.
2. **Behavior** — the documented interaction works (focus, read/write `value`, `click()` switches tab, etc.).

**Do not add rows here from code review, naming conventions, or planned hooks.** Hooks may exist in source before they appear here — track those in skill [registry.md](../.cursor/skills/visa2026-ui-test-hooks/registry.md) as **implemented (pending verify)** until DevTools checks pass.

| Audience | Use this doc for |
|----------|------------------|
| **Developers** | Verified stable queries (`#InputId`, `[data-testid]`, `.e2e-*`) — not DevExpress `.dxbl-*` or captions |
| **Other Agent skills** | Read-only reference for **proven** element access; consumed by **visa2026-ui-scenarios** YAML |

**Not in scope:** E2E runners, scrapers, CI. **Implementation / pending hooks:** [registry.md](../.cursor/skills/visa2026-ui-test-hooks/registry.md). **How to add hooks:** [reference.md](../.cursor/skills/visa2026-ui-test-hooks/reference.md).

---

## How to read the tables

| Column | Meaning |
|--------|---------|
| **View** | XAF view Id where the element appears |
| **BO / target** | BO property, layout `LayoutGroup` Id, or Action Id |
| **Type** | `text-input`, `password-input`, `button`, `layout-tab`, … |
| **Access (primary)** | Preferred `document.querySelector` |
| **Access (alternates)** | `[data-testid="…"]`, `.e2e-*` |
| **DOM target** | Node to interact with |
| **Expected behavior** | What was verified in DevTools |
| **Verified** | Date or session note (optional) |

### Access query priority

1. **`#InputId`** — text/password inputs  
2. **`[data-testid="…"]`**  
3. **`.e2e-{slug}`**

### Adding a new row (Process step 3)

Only after **configure + test** (step 2) passes:

```text
User describes targets → hooks in code → DevTools access + behavior OK → add row HERE → registry.md verified
```

Remove a row if a later build breaks access or behavior.

**Optional automation:** [tools/VerifyUiTestHooks/README.md](../tools/VerifyUiTestHooks/README.md) (Playwright headless — same checks, does not auto-edit this file).

---

## Login

**View:** `AuthenticationStandardLogonParameters_Blazor_DetailView` (also `AuthenticationStandardLogonParameters_DetailView`)

| BO / target | Type | Access (primary) | Access (alternates) | DOM target | Expected behavior | Verified |
|-------------|------|------------------|---------------------|------------|-------------------|----------|
| `UserName` | text-input | `#login-user-name` | `[data-testid="login-user-name"]`, `.e2e-login-user-name` | `<input id="login-user-name">` | `focus()`; read/write `value` | 2026-06-06 |
| `Password` | password-input | `#login-password` | `[data-testid="login-password"]`, `.e2e-login-password` | `<input id="login-password">` | `focus()`; read/write `value` | 2026-06-06 |
| Action `Logon` | button | `[data-testid="login-submit"]` | `.e2e-login-submit` | Log In toolbar control | `click()` reaches button | 2026-06-06 |

**DevTools — verified snippet**

```javascript
const user = document.querySelector('#login-user-name');
const pass = document.querySelector('#login-password');
const submit = document.querySelector('[data-testid="login-submit"]');
console.log({ user, pass, submit }); // all non-null
user.focus();
user.value = 'officer';
pass.value = '***';
console.log(user.value, pass.value);
// submit.click(); // optional — triggers logon
```

---

## Person — collection tabs

**Views:** `Person_DetailView_Employee`, `Person_DetailView`, `Person_DetailView_FamilyMember`, `Person_DetailView_TemporaryVisitor`  
**Layout:** `TabbedGroup` Id = `Tabs`

| LayoutGroup Id | Type | Access (primary) | Access (alternates) | DOM target | Expected behavior | Verified |
|----------------|------|------------------|---------------------|------------|-------------------|----------|
| *(strip)* | layout-tab-strip | `[data-testid="person-employee-tabs"]` | `.e2e-person-employee-tabs` | tab strip container | query non-null (container only) | 2026-06-06 |
| `Educations` | layout-tab | `[data-testid="person-employee-tab-educations"]` | `.e2e-person-employee-tab-educations` | tab **header** | `click()` activates Educations panel | 2026-06-06 |
| `Passports` | layout-tab | `[data-testid="person-employee-tab-passports"]` | `.e2e-person-employee-tab-passports` | tab header | `click()` activates Passports panel | 2026-06-06 |

**DevTools — verified snippet (Passports)**

```javascript
const tab = document.querySelector('[data-testid="person-employee-tab-passports"]')
  ?? document.querySelector('.e2e-person-employee-tab-passports');
console.log('access', tab); // non-null
tab.click(); // Passports collection panel visible
```

Use **tab header** for navigation, not `.e2e-{test-id}-content` on the panel wrapper.

---

## Sidebar navigation — People

**Context:** Main window accordion nav (after logon). Expand **People** if collapsed.  
**Stable ids:** XAF navigation item **Id** (`Employees`, `FamilyMembers`, …), not localized caption.

| Nav item Id | Type | Access (primary) | Access (alternates) | DOM target | Expected behavior | Verified |
|-------------|------|------------------|---------------------|------------|-------------------|----------|
| `People` | nav-group | `[data-testid="nav-people"]` | `.e2e-nav-people` | wrapper around group header text | query non-null; click expands/collapses group | 2026-06-06 |
| `Employees` | nav-item | `[data-testid="nav-people-employees"]` | `.e2e-nav-people-employees` | wrapper around Employees header | query non-null; `click()` opens `Person_ListView_Employees` | 2026-06-06 |
| `FamilyMembers` | nav-item | `[data-testid="nav-people-family-members"]` | `.e2e-nav-people-family-members` | wrapper around Family Members header | query non-null; `click()` opens `Person_ListView_FamilyMembers` | 2026-06-06 |
| `TemporaryVisitors` | nav-item | `[data-testid="nav-people-temporary-visitors"]` | `.e2e-nav-people-temporary-visitors` | wrapper around Temporary visitor header | query non-null; `click()` opens `Person_ListView_TemporaryVisitors` | 2026-06-06 |

**DevTools — verified snippet**

```javascript
document.querySelector('.e2e-nav-people-employees');
// → <div class="e2e-nav-people-employees" data-testid="nav-people-employees">…</div>

document.querySelector('.e2e-nav-people-family-members');
// → <div class="e2e-nav-people-family-members" data-testid="nav-people-family-members">…</div>

// Optional navigation:
// document.querySelector('[data-testid="nav-people-employees"]')?.click();
```

Hooks sit in **light DOM** under `.xaf-navmenu` (plain `document.querySelector` is enough for these wrappers).

---

## Sidebar navigation — Application

**Context:** Main window accordion nav (after logon). Expand **Application** if collapsed.  
**Stable ids:** XAF navigation item **Id** (`Application_DirectMigration`, `Application_ViaMinistries`, …), not localized caption.

| Nav item Id | Type | Access (primary) | Access (alternates) | DOM target | Expected behavior | Verified |
|-------------|------|------------------|---------------------|------------|-------------------|----------|
| `Application` | nav-group | `[data-testid="nav-application"]` | `.e2e-nav-application` | wrapper around group header text | query non-null; click expands/collapses group | 2026-06-07 |
| `Application_DirectMigration` | nav-item | `[data-testid="nav-application-direct-migration"]` | `.e2e-nav-application-direct-migration` | wrapper around direct migration header | query non-null; `click()` opens `Application_ListView_DirectMigration` | 2026-06-07 |
| `Application_ViaMinistries` | nav-item | `[data-testid="nav-application-via-ministries"]` | `.e2e-nav-application-via-ministries` | wrapper around via ministry header | query non-null; `click()` opens `Application_ListView_ViaMinistries` | 2026-06-07 |

**DevTools — verified snippet**

```javascript
document.querySelector('.e2e-nav-application-direct-migration');
// → <div class="e2e-nav-application-direct-migration" data-testid="nav-application-direct-migration">…</div>

document.querySelector('[data-testid="nav-application-via-ministries"]');
// → <div class="e2e-nav-application-via-ministries" data-testid="nav-application-via-ministries">…</div>

// Optional navigation:
// document.querySelector('[data-testid="nav-application-direct-migration"]')?.click();
```

Same family **E** mechanism as People — light DOM under `.xaf-navmenu`. **Restart the host** after rebuilding so hook map changes load.

---

## Person ListView — New / Delete actions

**Context:** Typed Person list views (after logon). View toolbar **New** and **Delete** buttons (`Action` Id `New` / `Delete`). Hooks applied by `PersonListViewE2eActionSelectorsController` via `wwwroot/js/visa2026-e2e-hooks.js` (shadow pierce; test id follows **current URL** in TabbedMDI; re-applied on activate, history change, and toolbar re-render).

| ListView Id | Action Id | Type | Access (primary) | Access (alternates) | DOM target | Expected behavior | Verified |
|-------------|-----------|------|------------------|---------------------|------------|-------------------|----------|
| `Person_ListView_Employees` | `New` | button | `[data-testid="person-list-employees-new"]` | `.e2e-person-list-employees-new` | toolbar **New** `<button>` | query non-null after view load; `click()` opens new Person detail | 2026-06-07 |
| `Person_ListView_FamilyMembers` | `New` | button | `[data-testid="person-list-family-members-new"]` | `.e2e-person-list-family-members-new` | toolbar **New** `<button>` | same (family member detail) | 2026-06-07 |
| `Person_ListView_TemporaryVisitors` | `New` | button | `[data-testid="person-list-temporary-visitors-new"]` | `.e2e-person-list-temporary-visitors-new` | toolbar **New** `<button>` | same (temporary visitor detail) | 2026-06-07 |
| `Person_ListView_Employees` | `Delete` | button | `[data-testid="person-list-employees-delete"]` | `.e2e-person-list-employees-delete` | toolbar **Delete** `<button>` | query non-null after view load; enabled when row selected | 2026-06-07 |
| `Person_ListView_FamilyMembers` | `Delete` | button | `[data-testid="person-list-family-members-delete"]` | `.e2e-person-list-family-members-delete` | toolbar **Delete** `<button>` | same | 2026-06-07 |
| `Person_ListView_TemporaryVisitors` | `Delete` | button | `[data-testid="person-list-temporary-visitors-delete"]` | `.e2e-person-list-temporary-visitors-delete` | toolbar **Delete** `<button>` | same | 2026-06-07 |

**DevTools — verified snippet (Employees New)**

```javascript
document.querySelector('[data-testid="person-list-employees-new"]');
// → <button class="… e2e-person-list-employees-new" data-testid="person-list-employees-new" data-action-name="New">…</button>

// Delete (after restart + open Employees list):
document.querySelector('[data-testid="person-list-employees-delete"]');
// → <button class="… e2e-person-list-employees-delete" data-testid="person-list-employees-delete" data-action-name="Delete">…</button>

// Optional — re-apply if toolbar re-rendered:
// visa2026E2eHooks.applyNewActionTestId('person-list-employees-new');
// visa2026E2eHooks.applyDeleteActionTestId('person-list-employees-delete');

// Optional navigation:
// document.querySelector('[data-testid="person-list-employees-new"]')?.click();
```

On Family Members / Temporary visitor lists, swap the test id slug (`person-list-family-members-new`, `person-list-temporary-visitors-new`, and `-delete` variants).

---

## Person DetailView — Save actions

**Context:** Typed Person detail views (after logon, open a Person record). View toolbar modification buttons (`Action` Ids `Save`, `SaveAndClose`, `SaveAndNew`). Hooks applied by `PersonDetailViewE2eActionSelectorsController` via `wwwroot/js/visa2026-e2e-hooks.js` (same shadow pierce + URL sync as ListView toolbar actions).

| DetailView Id | Action Id | Type | Access (primary) | Access (alternates) | DOM target | Expected behavior | Verified |
|---------------|-----------|------|------------------|---------------------|------------|-------------------|----------|
| `Person_DetailView_Employee` | `Save` | button | `[data-testid="person-detail-employee-save"]` | `.e2e-person-detail-employee-save` | toolbar **Save** `<button>` | query non-null after detail load; `click()` persists changes | 2026-06-07 |
| `Person_DetailView_FamilyMember` | `Save` | button | `[data-testid="person-detail-family-member-save"]` | `.e2e-person-detail-family-member-save` | toolbar **Save** `<button>` | same | 2026-06-07 |
| `Person_DetailView_TemporaryVisitor` | `Save` | button | `[data-testid="person-detail-temporary-visitor-save"]` | `.e2e-person-detail-temporary-visitor-save` | toolbar **Save** `<button>` | same | 2026-06-07 |
| `Person_DetailView_Employee` | `SaveAndClose` | button | `[data-testid="person-detail-employee-save-and-close"]` | `.e2e-person-detail-employee-save-and-close` | toolbar **Save and Close** `<button>` | query non-null; `click()` saves and closes detail | 2026-06-07 |
| `Person_DetailView_FamilyMember` | `SaveAndClose` | button | `[data-testid="person-detail-family-member-save-and-close"]` | `.e2e-person-detail-family-member-save-and-close` | toolbar **Save and Close** `<button>` | same | 2026-06-07 |
| `Person_DetailView_TemporaryVisitor` | `SaveAndClose` | button | `[data-testid="person-detail-temporary-visitor-save-and-close"]` | `.e2e-person-detail-temporary-visitor-save-and-close` | toolbar **Save and Close** `<button>` | same | 2026-06-07 |
| `Person_DetailView_Employee` | `SaveAndNew` | button | `[data-testid="person-detail-employee-save-and-new"]` | `.e2e-person-detail-employee-save-and-new` | toolbar **Save and New** `<button>` | query non-null; `click()` saves and opens new detail | 2026-06-07 |
| `Person_DetailView_FamilyMember` | `SaveAndNew` | button | `[data-testid="person-detail-family-member-save-and-new"]` | `.e2e-person-detail-family-member-save-and-new` | toolbar **Save and New** `<button>` | same | 2026-06-07 |
| `Person_DetailView_TemporaryVisitor` | `SaveAndNew` | button | `[data-testid="person-detail-temporary-visitor-save-and-new"]` | `.e2e-person-detail-temporary-visitor-save-and-new` | toolbar **Save and New** `<button>` | same | 2026-06-07 |

**DevTools — verify snippet (Employee detail)**

```javascript
document.querySelector('[data-testid="person-detail-employee-save"]');
document.querySelector('[data-testid="person-detail-employee-save-and-close"]');
document.querySelector('[data-testid="person-detail-employee-save-and-new"]');
// → <button … data-action-name="Save|SaveAndClose|Save and Close|SaveAndNew|Save and New" data-testid="person-detail-employee-…">…</button>

// Optional — re-apply if toolbar re-rendered:
// visa2026E2eHooks.applyPersonDetailActionTestId('SaveAndClose', 'person-detail-employee-save-and-close');
```

Open a record on each typed detail view (`/Person_DetailView_Employee/{key}`, etc.) before querying.

---

## Implemented in code — not yet in this doc

These hooks exist in the repo but **failed or skipped DevTools verify** — **no selectors listed here** until verified.

| Target | View / BO | Registry status | Notes |
|--------|-----------|-----------------|-------|
| Required / always-visible `Person.*` scalars (15 members) | Person detail views | implemented | See [registry.md](../.cursor/skills/visa2026-ui-test-hooks/registry.md) § Person detail — scalar fields; run `VerifyUiTestHooks --scenario person-scalar-fields` |
| Other Person `Tabs` layout groups | Person detail views | implemented | Same pattern as Passports; verify each tab header individually |
| Toolbar Save (other views) | various | backlog | Person detail **Save** actions — see section above |
| `Application` nested tabs | `Application_DetailView` | backlog | — |
| ListView / grid rows | various | backlog | — |

After DevTools verify: **move** the row into a section above with queries and snippet; update [registry.md](../.cursor/skills/visa2026-ui-test-hooks/registry.md) to **verified**.

---

## Entry template (after DevTools verify only)

```markdown
| `{target}` | {type} | `{primary}` | `{alternates}` | {dom target} | {expected behavior} | YYYY-MM-DD |
```

Naming: skill [SKILL.md § Naming contract](../.cursor/skills/visa2026-ui-test-hooks/SKILL.md).
