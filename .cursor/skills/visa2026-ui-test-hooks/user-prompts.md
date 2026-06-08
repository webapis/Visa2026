# UI test hooks — user prompts

Copy-paste messages to invoke [visa2026-ui-test-hooks](./SKILL.md) in Cursor. Prefer **`@visa2026-ui-test-hooks`** (or `@.cursor/skills/visa2026-ui-test-hooks`) so the agent loads this skill.

**Not this skill:** multi-step YAML journeys → [visa2026-ui-scenarios](../visa2026-ui-scenarios/SKILL.md); EasyTest / CI E2E → [`docs/TESTING_PLAN.md`](../../../docs/TESTING_PLAN.md).

---

## Configure hooks (step 1 → 4)

| Goal | User prompt |
|------|-------------|
| **Classify + match first** | `@visa2026-ui-test-hooks I need hooks for **{target}**. Classify family A–F, match prior art in registry/reference, then **Reuse** or **Discover** before coding.` |
| **Single field** | `@visa2026-ui-test-hooks Add UI test hooks for **Person.FirstName** on employee detail view.` |
| **Named list** | `@visa2026-ui-test-hooks Hook **Person.FirstName**, **Person.LastName**, and **Person.DateOfBirth** with stable selectors.` |
| **Required scalars on a BO** | `@visa2026-ui-test-hooks Set up CSS selector access for all **required / always-visible** direct fields on **Person** (exclude collections, optional gear-hidden, computed, hidden).` |
| **Another BO** | `@visa2026-ui-test-hooks Add hooks for required scalar fields on **ApplicationItem** detail (respect optional-detail gear rules).` |
| **Layout tab** | `@visa2026-ui-test-hooks Add a hook for the **Passports** tab on **Person** detail (`TabbedGroup` **Tabs**).` |
| **All tabs on a view** | `@visa2026-ui-test-hooks Hook all collection **tab headers** on **Person_DetailView_Employee** (`Tabs` layout groups).` |
| **Toolbar action** | `@visa2026-ui-test-hooks Add a selector for **Save** on Person detail.` |
| **Logon** | `@visa2026-ui-test-hooks Ensure login **UserName**, **Password**, and **Log In** have stable selectors.` |
| **Manual IDE host** | Run **`Visa2026 - Hook Verify (LocalDB)`** from launchSettings (http://localhost:5051, DB Visa2026HookVerify) for DevTools without stopping daily dev on :5000. |
| **Sidebar nav (one group)** | `@visa2026-ui-test-hooks Add UI test hooks for **People** sidebar navigation (**People**, **Employees**, **Family Members**, **Temporary visitor**). Use **family E** — extend nav hook map, reuse shared controller.` |
| **Sidebar nav (another group)** | `@visa2026-ui-test-hooks Add **family E** hooks for **Application** sidebar nav (Ids: …). Extend **NavigationE2eHooks** map only.` |

---

## Optional fields (explicit opt-in only)

Default bulk hooks **exclude** gear-hidden optional scalars ([`docs/OPTIONAL_DETAIL_FIELDS.md`](../../../docs/OPTIONAL_DETAIL_FIELDS.md)). Use these when you **want** them hooked:

| Goal | User prompt |
|------|-------------|
| **One optional member** | `@visa2026-ui-test-hooks Also hook optional field **Person.Email** (gear-hidden — include verify with gear expanded).` |
| **All optional on a BO** | `@visa2026-ui-test-hooks Hook **all optional gear-hidden** scalar fields on **Person**, including **MiddleName**, **Email**, **Photo**, **HireDate**, **IsArchived**, **SponsoringEmployee**.` |

---

## Verify (step 2 — no new hooks unless missing)

| Goal | User prompt |
|------|-------------|
| **DevTools checklist** | `@visa2026-ui-test-hooks I kept the server with **Invoke-UiHookVerify -KeepServer**. Give me DevTools snippets for **nav-people-employees** on :5051.` |
| **Isolated verify (default)** | `@visa2026-ui-test-hooks Run **Invoke-UiHookVerify.ps1** for **login** and **nav-people** (no IDE host).` |
| **Playwright only** | `@visa2026-ui-test-hooks Verify **person-employee-scalar-fields** via **Invoke-UiHookVerify** with **-StartUrl** `/Person_DetailView_Employee/{guid}`.` |
| **Family / temporary scalars** | `@visa2026-ui-test-hooks Verify **person-family-member-scalar-fields** / **person-temporary-visitor-scalar-fields** with matching detail **-StartUrl**.` |
| **Login only** | `@visa2026-ui-test-hooks Verify login hooks: **.\scripts\local\Invoke-UiHookVerify.ps1 -Scenario login**` |

---

## Record (step 3 — after verify passes)

| Goal | User prompt |
|------|-------------|
| **Promote to catalog** | `@visa2026-ui-test-hooks DevTools verify passed for **Person.FirstName** and **Person.LastName**. Update **docs/UI_TEST_HOOKS.md** and **registry.md** to **verified**.` |
| **After green Playwright** | `@visa2026-ui-test-hooks **person-scalar-fields** VerifyUiTestHooks all PASS — record verified rows in **UI_TEST_HOOKS.md**.` |

---

## Inventory / status (read-only)

| Goal | User prompt |
|------|-------------|
| **What's verified?** | `@visa2026-ui-test-hooks What Person field hooks are **verified** vs **implemented only**? Check **UI_TEST_HOOKS.md** and **registry.md**.` |
| **Gap for a scenario** | `@visa2026-ui-scenarios List hook gaps in **person-employee-minimal** map §3 vs **UI_TEST_HOOKS.md**; give **@visa2026-ui-test-hooks** prompts for missing ids.` |
| **Plan scenario (hand back)** | `@visa2026-ui-scenarios Plan **person-employee-create**: login → nav-people-employees → person-list-employees-new → fill → person-detail-employee-save. Map first — see **visa2026-ui-scenarios/user-prompts.md**.` |

---

## Prompts that look related but use a different skill

| User intent | Use instead |
|-------------|-------------|
| Plan/run login → fill Person YAML | `@visa2026-ui-scenarios` — map first, then hand hook gaps here |
| Run **UiScenarioRunner** / CI scenario | `@visa2026-ui-scenarios` |
| Unit test evaluators in Module | `@visa2026-unit-tests` |
| Add EasyTest E2E test class | E2E project — not ui-test-hooks |

---

## Minimal template (fill in blanks)

```text
@visa2026-ui-test-hooks [Configure | Verify | Record] UI test hooks for
**{ViewId or BO}** — target(s): **{Member | LayoutGroup Id | Action Id}**.
[Optional: classify as family A–F and reuse prior pattern if matched.]
[Optional: include/exclude optional gear-hidden fields.]
[Optional: app restarted / VerifyUiTestHooks scenario / DevTools already passed.]
```

**Example (filled):**

```text
@visa2026-ui-test-hooks Configure UI test hooks for Person employee detail —
required scalar fields only (no optional, no collections).
Then verify with **Invoke-UiHookVerify.ps1 -Scenario person-scalar-fields -StartUrl**
/Person_DetailView_Employee/8a3c...
```
