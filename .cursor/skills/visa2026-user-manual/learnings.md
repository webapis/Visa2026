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
