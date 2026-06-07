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

## Implemented in code — not yet in this doc

These hooks exist in the repo but **failed or skipped DevTools verify** — **no selectors listed here** until verified.

| Target | View / BO | Registry status | Notes |
|--------|-----------|-----------------|-------|
| `Person.FirstName`, `Person.LastName` | Person detail views | implemented | Run DevTools on `#person-first-name`, `#person-last-name` |
| Other Person `Tabs` layout groups | Person detail views | implemented | Same pattern as Passports; verify each tab header individually |
| Toolbar Save / New / Delete | various | backlog | — |
| `Application` nested tabs | `Application_DetailView` | backlog | — |
| ListView / grid rows | various | backlog | — |

After DevTools verify: **move** the row into a section above with queries and snippet; update [registry.md](../.cursor/skills/visa2026-ui-test-hooks/registry.md) to **verified**.

---

## Entry template (after DevTools verify only)

```markdown
| `{target}` | {type} | `{primary}` | `{alternates}` | {dom target} | {expected behavior} | YYYY-MM-DD |
```

Naming: skill [SKILL.md § Naming contract](../.cursor/skills/visa2026-ui-test-hooks/SKILL.md).
