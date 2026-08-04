# User manual — learnings (append-only)

**Skill:** [SKILL.md](./SKILL.md) · **Plan:** [docs/USER_MANUAL_IMPLEMENTATION_PLAN.md](../../../docs/USER_MANUAL_IMPLEMENTATION_PLAN.md)

Record verified outcomes after catalog generator changes, CI fixes, guide publications, screenshot baselines, or publish pipeline work.

---

## Template (copy per entry)

```markdown
### YYYY-MM-DD — <short title>

- **Phase:** 0 | 1 | 2 | 3 | 4 | 5
- **Area:** generator | validator | mkdocs | guide | screenshots | publish
- **What worked:**
- **Gotcha / drift:**
- **Follow-up:**
```

---

## Entries

### 2026-08-04 — Skill and plan created

- **Phase:** 0 (planning)
- **Area:** skill
- **What worked:** Two-layer model (catalog vs guides) documented in `docs/USER_MANUAL_IMPLEMENTATION_PLAN.md` and `.cursor/skills/visa2026-user-manual/`.
- **Gotcha / drift:** `user-manual/` folder and generator not scaffolded yet — skill paths are planned until Phase 0 lands.
- **Follow-up:** Run Phase 0 scaffold; first learning entry after `mkdocs build` succeeds locally.

### 2026-08-04 — Skill scope expanded (create / update / plan / fix / track)

- **Phase:** 0 (planning)
- **Area:** skill
- **What worked:** Added `tracking.md` as living status board; SKILL.md activity routing for full documentation lifecycle.
- **Gotcha / drift:** Update `tracking.md` whenever phase, guide status, or doc debt changes — not only learnings.
- **Follow-up:** Phase 0 scaffold; flip infrastructure rows in tracking when paths exist.

### 2026-08-04 — Roadmap + E2E media contract

- **Phase:** 0 (planning)
- **Area:** plan + interlock
- **What worked:** `docs/USER_MANUAL_ROADMAP.md` timeline; `docs/USER_MANUAL_E2E_MEDIA.md` links guides via `e2eScenarioId`, `Record-EasyTest.ps1`, planned `UserManualMediaCapture`.
- **Gotcha / drift:** easytest-e2e **produces** MP4/PNG; user-manual **consumes** — update both skills' learnings when media pipeline changes.
- **Follow-up:** Promote `person-employee-create` scenario; first video for `person/register` after storage decision.

### 2026-08-04 — Video storage left open

- **Phase:** 0 (planning)
- **Area:** plan
- **What worked:** Documented options A–E in `USER_MANUAL_E2E_MEDIA.md` §5.1 (embed, static, object, Postgres/FileData, hybrid); open decision #6 in tracking + implementation plan.
- **Gotcha / drift:** EasyTest still **produces** MP4; only **publish target** is undecided — do not block Phase 0–2 on video storage.
- **Follow-up:** Decide in Phase 3 with product + IT; set `videoStorage` on guides.

### 2026-08-04 — Advisory workflow (advise before implement)

- **Phase:** 0 (planning)
- **Area:** skill
- **What worked:** Added `advisory.md` — when/to document, options menus A–E, phase gates, pre-flight questions; SKILL.md mandates advise-first.
- **Gotcha / drift:** Agents must offer paths before Phase 0 scaffold or bulk guides — user may only need planning (B3).
- **Follow-up:** Use advisory §5 on every new manual request.

### 2026-08-04 — Curriculum CRUD → templates

- **Phase:** 0 (planning)
- **Area:** plan
- **What worked:** `curriculum.md` tiers 0–7; tracking inventory reordered; Phase 2 = tiers 0–4 only; tier 7 template generation last.
- **Gotcha / drift:** Do not publish Resminamalar/templates before person register + application create guides.
- **Follow-up:** Add `tier` + `prerequisiteSlugs` to guide `_template.md` in Phase 2.

### 2026-08-04 — Status hub (roadmap + changelog + next inline)

- **Phase:** 0 (planning)
- **Area:** docs
- **What worked:** `docs/USER_MANUAL_STATUS.md` — single entry for snapshot, consolidated changelog, Phase 0 queue §4.
- **Gotcha / drift:** Keep STATUS §4 in sync when shipping; tracking.md holds detailed inventory.
- **Follow-up:** Phase 0 task 1 — MkDocs scaffold in `user-manual/`.

### 2026-08-04 — Four manual locales (en/tr/tk/ru)

- **Phase:** policy + Phase 0 scaffold
- **Area:** localization
- **What worked:** `localization.md` — mkdocs-static-i18n, folder per locale, aligned with `LOCALIZATION_PLAN.md`.
- **Gotcha / drift:** English pilots first; tr/tk/ru need per-locale officer review; screenshots per locale when app UI i18n ships.
- **Follow-up:** Phase 0 mkdocs.yml includes all four languages in switcher.

### 2026-08-04 — Pre-implementation decisions recorded

- **Phase:** 0
- **Area:** governance
- **What worked:** D1 on-prem Docker; D2 tech publish; D11 minimal pilot; D17 deploy gate.
- **Gotcha / drift:** D2 tech publish + D8 officer review — workflow must be `review` → officer OK → tech `published`.
- **Follow-up:** Assign reviewer names (D5) before Phase 2; design manual Docker service in compose.

### 2026-08-04 — Phase 0 scaffold shipped

- **Phase:** 0
- **Area:** infra
- **What worked:** `user-manual/` MkDocs Material + mkdocs-static-i18n (en/tr/tk/ru); `Build-UserManual.ps1 -SkipE2E`; generator stub in solution; `user-manual.yml` CI.
- **Gotcha / drift:** Windows dev box may lack Python — build script tries `python`, `python3`, then `py -3`. Validator script must use ASCII punctuation (em dash broke Windows PowerShell 5.1 parse).
- **Follow-up:** Phase 0 acceptance — CI green on PR; Phase 1 catalog generator + `UserManualDocs` tests.

### 2026-08-04 — Phase 1 catalog generator shipped

- **Phase:** 1
- **Area:** infra + Module
- **What worked:** `[UserDocumentation]` on Person/Application/ApplicationItem/ApplicationProgress; generator reflects XAF display names + required fields; `bo-catalog.json` + `navigation-tree.json` committed; validator fails unknown `bo:`; 3 `UserManualDocs` xUnit tests.
- **Gotcha / drift:** Generator uses project reference to Module (`typeof(Person).Assembly`) — `Assembly.LoadFrom` alone fails without DevExpress deps in output folder. Generated `business-objects.md` is copied into docs at build time (gitignored).
- **Follow-up:** Phase 2 pilot guides (login + `person/register`); assign reviewer names (D5).

### 2026-08-04 — Login guide draft (Phase 2)

- **Phase:** 2
- **Area:** guides
- **What worked:** `getting-started/login` in en/tr/tk/ru — officer UI labels (`User Name`, `Password`, `Log In`, `Report Dashboard`); linked `e2eScenarioId: person-officer-journey`; screenshot paths for v2026.08.
- **Gotcha / drift:** PNG assets not captured yet — images break until Phase 3 E2E copy or manual staging; split login vs navigation per curriculum (navigation guide still backlog).
- **Follow-up:** Capture login/navigation screenshots; `person/register` guide; officer review before `published`.

### 2026-08-04 — Navigation guide draft (Phase 2)

- **Phase:** 2
- **Area:** guides
- **What worked:** `getting-started/navigation` in en/tr/tk/ru — shell, Report Dashboard, left menu labels from model/UiStrings, list/detail/toolbar pattern, State notifications via Operations + bell; prerequisite link to login guide.
- **Gotcha / drift:** Screenshot paths `navigation-step-01`…`04` not captured yet; some nav items (Border zone) role-dependent — guide uses "may differ" disclaimer.
- **Follow-up:** E2E screenshot copy from `person-officer-journey` (`02-employees-list` etc.); tier 1 `person/open-and-search`.

### 2026-08-04 — Local preview host (Serve-UserManual.ps1)

- **Phase:** 0–2
- **Area:** infra
- **What worked:** `scripts/local/Serve-UserManual.ps1` bootstraps portable Python under `user-manual/.tools/`, runs `Build-UserManual.ps1`, then `mkdocs serve` at **http://127.0.0.1:8765/manual/** with live reload.
- **Gotcha / drift:** Windows Store `python.exe` alias breaks detection — probe with `$ErrorActionPreference = SilentlyContinue`. MkDocs Material has no `tk.html` — added `user-manual/overrides/partials/languages/tk.html` (English chrome, Turkmen content). Missing screenshot PNGs warn but do not block build.
- **Follow-up:** GitHub Pages deploy job on `master` push; on-prem Docker static host per D1 later.

### 2026-08-04 — Login/navigation screenshots (EasyTest → manual assets)

- **Phase:** 2
- **Area:** screenshots
- **What worked:** `Record-EasyTest.ps1 -NoRecord -Screenshots` on `person-officer-journey`; `Copy-EasyTestManualScreenshots.ps1` maps `00-logon-page`…`04-employee-detail` to guide PNGs; English UI replicated to tr/tk/ru (D12); `Sync-ManualAssets` copies `user-manual/assets` → `docs/assets` before mkdocs build.
- **Gotcha / drift:** EasyTest filenames include UTC timestamp suffix — copy script matches by label prefix (`00-logon-page*.png`). `navigation-step-02-left-menu` reuses post-login dashboard capture until a dedicated nav shot exists.
- **Follow-up:** Per-locale EasyTest when app UI i18n ships; refresh on UI change (`screenshotsVersion` bump).

### 2026-08-04 — person/register guide (Phase 2)

- **Phase:** 2
- **Area:** guides
- **What worked:** `person/register` draft en/tr/tk/ru; required field table from E2E captions; screenshots from `02/03/04` EasyTest milestones; mkdocs **Guides** nav; validator slug key per locale.
- **Gotcha / drift:** No empty-form screenshot before fill — step 2 is prose-only; `bo: Person` ties to catalog anchor.
- **Follow-up:** `person/edit-employee` guide; officer review before `published`.

### 2026-08-04 — person/add-passport guide (Phase 2)

- **Phase:** 2
- **Area:** guides
- **What worked:** `person/add-passport` en/tr/tk/ru; **Passports** tab + **New Passport** toolbar; fields from `E2ETestPassportFieldCaptions`; screenshots `05`–`07` + employee detail `04`.
- **Gotcha / drift:** Passport BO not in catalog yet — guide uses `bo: Person`; nested list UX may show **New** vs **New Passport**.
- **Follow-up:** Visa nested create guide; add `Passport` to `[UserDocumentation]` catalog when reference page ships.
