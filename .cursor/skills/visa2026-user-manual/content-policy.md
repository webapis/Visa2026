# User manual — content policy (officers only)

**Applies to:** everything under `user-manual/docs/` that officers read on the published site.

**Does not apply to:** `docs/USER_MANUAL_*.md`, `.cursor/skills/`, generator code, CI, or E2E — those are **developer** surfaces.

**Skill:** [SKILL.md](./SKILL.md) · **Advisory:** [advisory.md](./advisory.md)

Applies to **all locales** (en, tr, tk, ru). Rules are the same in every language.

---

## 1. Core rule

The officer manual describes **what to click, what to type, and what to expect on screen** — not how the application is built.

| Officer manual (`user-manual/`) | Developer docs (`docs/`, skills, code) |
|---------------------------------|----------------------------------------|
| UI labels, menus, screenshots | Architecture, CI, E2E, generators |
| Plain-language steps | C#, SQL, OData, APIs |
| Roles as shown in the app | Property names, type names, file paths |

**If content belongs in `docs/` or requires reading source code, it does not belong in the manual.**

### 1.1 Verification (green tick only)

Officers may see a **green tick** when automated tests passed for that guide. Full test results (TRX, matrices, CI links) live in **`manual-test-reports/`** — not in guide prose. See [testing-evidence.md](./testing-evidence.md).

---

## 2. Forbidden in officer-facing pages

Do **not** publish (in guide body, reference pages, or sidebar-visible text):

| Category | Examples |
|----------|----------|
| **Programming / query languages** | C#, JavaScript, SQL, OData, PowerShell, YAML snippets |
| **Implementation identifiers** | `Person.FirstName`, `ApplicationItem`, `ObjectSpace`, BO class names |
| **Repo & tooling** | `Visa2026.Module`, git paths, `dotnet test`, EasyTest, MkDocs, CI workflows |
| **Infrastructure** | Connection strings, Docker, IIS, PostgreSQL, deploy runbooks |
| **Developer plans** | Links to `docs/VISA2014_MIGRATION.md`, implementation plans, agent skills |
| **Test logs / TRX / CI output** | Full report in `manual-test-reports/` only |
| **API & integration** | Web API, Swagger, OData endpoints, import pipelines |

**Fenced code blocks** (` ``` `) in `user-manual/docs/guides/` and `user-manual/docs/reference/` are **not allowed** on publish (validator **fail** in Phase 1+). Exception: none for v1 — use prose and screenshots instead.

---

## 3. Required style

| Use | Not |
|-----|-----|
| **Menu path:** *Employees → New* | `NavigateEmployeesList()` |
| **Field label:** *First name* (from catalog `displayName`) | `FirstName` |
| **Button:** *Save*, *Document copies* | Controller or action class names |
| **Role:** *Visa Officer* (as in UI) | `PermissionPolicyRole` |
| **Screenshot** with caption | ASCII diagram of code flow |

**Source of truth for labels:** generated `bo-catalog.json` **display names** and E2E scenario map §3 captions — never invent; never expose internal property names on the site.

---

## 4. Frontmatter and build metadata

Guide YAML frontmatter (`slug`, `bo`, `e2eScenarioId`, `sourceDocs`, …) is for **CI and agents only**. It must **not** appear in rendered HTML.

- Configure MkDocs / theme so frontmatter is not shown to readers.
- `sourceDocs` traces developer sources for authors — **never** link or quote them in officer prose.

---

## 5. Auto-generated reference (Layer A)

Catalog-driven reference pages for officers show:

- Human **type title** (e.g. *Person*, *Application*)
- **Navigation path** in UI terms
- **Fields** as on-screen labels, required/optional, short officer-facing help
- Links to how-to guides

They must **not** show: `fullName`, CLR types, EF mappings, validation rule class names, or `[Appearance]` implementation detail.

---

## 6. Adapting from `docs/`

When drafting from developer docs (`PERSON_DOSSIER.md`, `APPLICATION_ITEM_DOCUMENT_COPIES.md`, etc.):

1. Extract **workflow and business rules** only.
2. Replace every technical term with the **UI label** from catalog or E2E map.
3. Remove phases, file paths, and “planned / deferred” language.
4. Add screenshots from EasyTest — officers trust UI, not architecture diagrams.

---

## 7. Administration & templates (tier 7)

Template guides are still **officer/admin UI** docs, not developer docs.

| Allowed | Forbidden |
|---------|-----------|
| “Open **User report templates**”, “Click **Edit template**” | `UserReportTemplateSeedGate`, embed pipeline |
| “Insert the **First name** merge field from the list” | Raw `{{Person.FirstName}}` unless product UI shows exactly that string |
| Staging folder path **only if** the app shows the same path to admins | UNC / server paths not visible in UI |

Prefer **screenshots of the Word/Excel UI** over placeholder syntax.

---

## 8. Review checklist (officers)

Before `status: published`:

- [ ] No code blocks or programming syntax in the page
- [ ] No class names, property names, or repo paths
- [ ] Every field name matches what appears on screen (English manual)
- [ ] Steps match screenshots (no video — D21)
- [ ] No links to `docs/` developer pages

---

## 9. CI validation (Phase 1+)

| Check | Severity |
|-------|----------|
| Fenced code block in `user-manual/docs/guides/` or `reference/` | **Fail** |
| Guide body contains `Visa2026.Module`, `dotnet `, ````csharp` | **Fail** |
| Reference page uses CLR `fullName` | **Fail** |
| Link target under `docs/` (developer) | **Warn** → fail after Phase 2 |

---

## 10. Changelog

| Date | Change |
|------|--------|
| 2026-08-04 | Initial policy — officer manual must not contain code-related content |
