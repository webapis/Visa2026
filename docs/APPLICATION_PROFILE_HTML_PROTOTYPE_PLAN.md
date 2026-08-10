# Application Profile — HTML prototype plan (PNG → interactive UI)

**Status:** **H0–H6 shipped** (2026-08-10) · **H7 deferred**  
**PNG source of truth:** [`docs/prototypes/`](prototypes/) — 22 mockups (2026-08-10) · inventory in [`APPLICATION_PROFILE_PLAN.md`](APPLICATION_PROFILE_PLAN.md) §9  
**Agent skill:** [`.cursor/skills/visa2026-application-profile/`](../.cursor/skills/visa2026-application-profile/) — update `IMPLEMENTATION_PLAN.md` when a slice ships  
**Goal:** Build **interactive HTML + mock data** that matches the PNGs **exactly** (layout, colors, typography, labels, spacing, states). This shell becomes the **canonical custom officer UI** and replaces native XAF navigation/layout for Application Profile flows.

---

## 1. Outcomes

| Outcome | Definition of done |
|---------|-------------------|
| **Visual parity** | Side-by-side with PNG: same structure, colors, component states (active nav, badges, disabled checkboxes, SLA chips). |
| **Clickable flows** | Officer can navigate shell → staged → merge → in-process → workspace tabs → templates → wizard without XAF. |
| **Mock data** | One shared in-memory dataset; actions mutate mock state (stage, merge, publish template). |
| **Blazor-ready** | CSS tokens + BEM/class names documented for port to Razor components under `Visa2026.Blazor.Server`. |

**Non-goals (HTML phase):** real API, auth, EF, validation rules engine, i18n (except preserving Turkmen names in mock rows), print/PDF.

---

## 2. Locked decisions (2026-08-10)

| Topic | Decision |
|-------|----------|
| **Delivery shape** | **One SPA** (`index.html` + JS router + CSS) inside Blazor `wwwroot` |
| **Host path** | **`Visa2026.Blazor.Server/wwwroot/officer-shell/`** — create here from slice **H0** (no `docs/prototypes-html/` staging copy) |
| **Local URL** | `https://localhost:{port}/officer-shell/` (F5 Blazor profile) |
| **Stack** | Vanilla HTML/CSS/ES modules; **Bootstrap 5.3** + **Bootstrap Icons** (CDN) for wizard parity |
| **22 PNG → routes** | **13 logical screens** — list/grid = toggle on same route (§4) |
| **Language** | English UI chrome; Turkmen person names in mock data |
| **Theme** | **Light only — as in PNG mockups** (no dark mode in v1) |
| **Person DetailView staging** | **Deferred** (not HTML v1) — no Stage invitation / Stage visa extension actions; sidebar **People** = placeholder stub only |
| **Domain model in UI** | Template → Staged profile → In-process profile (merge assigns number/date) |
| **Parity method** | Per-screen checklist + compare to PNG at 1440×900 |
| **PNG assets** | Stay in [`docs/prototypes/`](../prototypes/); copy into `wwwroot/officer-shell/assets/png/` for QA gallery only (optional H6) |

### Former open questions — resolved

1. Person staging actions → **later** (post–v1 HTML).  
2. Dark mode → **no**; match images exactly (light).  
3. Hosting → **`wwwroot/officer-shell/` immediately**.

---

## 3. Design system (extract from PNGs — lock before slice 1)

Extract tokens from mockups and record in `Visa2026.Blazor.Server/wwwroot/officer-shell/styles/tokens.css`:

| Token | Value (from PNGs — verify with color picker) |
|-------|-----------------------------------------------|
| Nav background | `#0f1b2d` / navy |
| Nav active | `#1d6ef2` + `rgba(29,110,242,0.18)` fill |
| Page background | `#eef2f7` |
| Panel / card | `#ffffff`, border `#d8e0ea` |
| Text primary | `#142033` |
| Text muted | `#5c6b7a` |
| Success / Ready | green chips (~`#0f9d58` / `#e6f6ee`) |
| Warning / Awaiting | amber (~`#b45309` / `#fff4e5`) |
| Error / Incomplete | red (~`#c62828` / `#fdecec`) |
| Template accent — Registration | blue `#3b82f6` |
| Template accent — Invitation | green `#22c55e` |
| Template accent — Visa extension | amber `#f59e0b` |
| Template accent — Work permit | purple `#a855f7` |
| Font | `Segoe UI`, `IBM Plex Sans`, system-ui |
| Sidebar width | 268px |
| Radius | 8–10px cards, 999px pills |
| Top bar height | 56px |

**Components to implement once (shared):**

- `AppShell` — sidebar + topbar + content outlet  
- `NavItem` — icon, title, subtitle, badge, active state  
- `PageHeader` — title, subtitle, primary/secondary actions  
- `Toolbar` — search, filters, List/Grid toggle  
- `FilterChips` — template family counts  
- `DataTable` — zebra, sort indicators, row hover, chevron  
- `CardGrid` / `ProfileCard` — color stripe, checkbox, status pill  
- `StatusPill` — ready, incomplete, awaiting, in-process, locked, draft  
- `Banner` — warning (cannot start process), success (ready to publish)  
- `WizardChrome` — left stepper + footer (Back, Save draft, Next/Publish)  
- `WorkspaceLayout` — case header, person strip, left tab nav, main, right rail  
- `Stepper` — horizontal (overview) and vertical (progress)  

---

## 4. Route map (PNG → HTML)

| # | Route ID | PNG reference(s) | Notes |
|---|----------|------------------|-------|
| 1 | `shell` | `visa2026-custom-left-navigation-shell-mockup.png` | Baseline chrome; all routes use this shell |
| 2 | `nav-ia` | `application-profiles-navigation-sidebar-mockup.png` | Optional help page diagram; or doc-only |
| 3 | `staged` | `staged-profiles-listview-table-mockup.png` + `staged-profiles-grid-cards-mockup.png` | List ↔ Grid toggle; grouped variant optional (`staged-application-profiles-workspace-mockup.png`) as **group-by-template** mode |
| 4 | `in-process` | `process-started-profiles-listview-table-mockup.png` + `process-started-profiles-list-cards-mockup.png` | List ↔ Grid toggle |
| 5 | `case/:id` tab `overview` | `process-started-application-profile-workspace-mockup.png`, `process-started-nav-overview.png` | Pick sharper PNG as primary |
| 6 | `case/:id` tab `people` | `process-started-nav-people-links.png` | |
| 7 | `case/:id` tab `progress` | `process-started-nav-progress.png` | |
| 8 | `case/:id` tab `documents` | `process-started-nav-document-copies.png` | |
| 9 | `case/:id` tab `resminamalar` | `process-started-nav-resminamalar.png` | |
| 10 | `case/:id` tab `sla` | `process-started-nav-sla-deadlines.png` | |
| 11 | `templates` | `application-profile-templates-listview-mockup.png` + `application-profile-templates-grid-mockup.png` | List ↔ Grid |
| 12 | `templates/:id` | `application-profile-template-overview-mockup.png` | Left rail template list + overview |
| 13 | `templates/wizard` step 1–5 | `application-profile-template-wizard-mockup.png` … `step5` | Single wizard route; `#/templates/wizard/{0-4}` |

**PNG count:** 22 files → **13 logical screens** (with toggles and tabs).

---

## 5. Mock data model

Single module `mock-data.js` (exported constants + mutators):

```text
MockStore
├── user: { name, role, office, initials }
├── templates[]: ApplicationProfileTemplate
│     id, name, code, actionFamily, progressRoute, audience, status, stagedUses, inProcessUses
│     + config snapshot (legs, sla, produce/cancel, person toggles, template files)
├── stagedProfiles[]: StagedApplicationProfile
│     id, templateId, personId, personName, projectName, stagedOn, missingFields[], readiness
├── inProcessProfiles[]: InProcessApplicationProfile
│     id, number, date, templateId, people[], project, started, currentStep, slaDays, status
│     mergedFromStagedIds[]
├── people[]: (minimal for labels)
└── actions: stageFromTemplate(), startProcess(selectedIds), publishTemplate(), …
```

**Seed data** must mirror PNG rows (Aýgul Berdiýewa, Maksat Orazow, № 2026-0147, INV_WP_EMP, etc.).

**Readiness rules (mock):**

- Staged row `selectable` only when `readiness === 'ready'`.  
- **Start process** disabled if any selected row not ready; show orange banner (PNG copy).  
- Merge creates new `inProcessProfiles` entry, removes staged rows, navigates to `case/:id/overview`.

---

## 6. File structure (canonical path)

```text
Visa2026.Blazor.Server/wwwroot/officer-shell/
├── index.html                 # shell + router outlet — entry: /officer-shell/
├── README.md                  # F5 URL, parity notes (dev-only)
├── assets/
│   └── png/                   # optional copies of 22 PNGs for in-app QA gallery
├── styles/
│   ├── tokens.css
│   ├── shell.css
│   ├── components.css
│   └── pages.css
├── js/
│   ├── mock-data.js           # seed data + mutators
│   └── main.js                # hash router + page renders
└── parity/
    └── CHECKLIST.md           # per-PNG sign-off (can live in docs/ if preferred)
```

**Static files:** served by existing `UseStaticFiles()` in `Startup.cs` — no MapFallback change required (`/officer-shell/index.html` direct).

**UTF-8 without BOM** for all text files (repo rule).

---

## 7. Implementation slices

| Slice | Deliverable | PNG parity target | Status |
|-------|-------------|-------------------|--------|
| **H0** | Tokens + shell + router + mock store seed | `visa2026-custom-left-navigation-shell-mockup.png` | **Done** |
| **H1** | Staged list + grid + Start process merge | `staged-profiles-*.png` | **Done** |
| **H2** | In-process list + grid | `process-started-profiles-*.png` | **Done** |
| **H3** | Case workspace — 6 tabs | `process-started-nav-*.png`, workspace header PNG | **Done** |
| **H4** | Templates catalog + overview | `application-profile-templates-*.png`, overview PNG | **Done** |
| **H5** | Template wizard steps 1–5 | `application-profile-template-wizard*.png` | **Done** |
| **H6** | Parity pass + README + PNG gallery in shell | All 22 | **Done** (checklist pending officer sign-off) |
| **H7** | Person DetailView staging actions (invitation / visa extension) | Person UX PNGs TBD | **Deferred** (post–v1) |

**Verify each slice:** F5 Blazor → `https://localhost:{port}/officer-shell/`, 1440×900, compare to PNG, no console errors.

---

## 8. Parity checklist (per PNG)

For each mockup file, before marking slice Done:

- [ ] Header title + subtitle text matches  
- [ ] Primary button label + disabled state matches  
- [ ] Nav item active + badges (18 / 24) match  
- [ ] Table columns / card anatomy match  
- [ ] Status pill colors and labels match  
- [ ] Template color stripe / dot matches family  
- [ ] Warning banner copy matches (staged merge gate)  
- [ ] Wizard step labels and footer buttons match  
- [ ] Spacing “looks the same” at 100% zoom (subjective sign-off by officer/dev)

Record sign-off in `parity/CHECKLIST.md` with date and slice ID. **Gap analysis:** [`parity/COMPARISON.md`](../Visa2026.Blazor.Server/wwwroot/officer-shell/parity/COMPARISON.md) (22 PNGs vs HTML).

---

## 9. Navigation flows (must work in HTML)

```text
Sidebar: Staged profiles → staged list/grid → select → Start process → in-process case/overview
Sidebar: In process → list/grid → row click → case workspace (any tab)
Sidebar: Profile templates → list/grid → row → overview → Configure → wizard
Wizard: Publish → templates catalog (status draft→active for new)
Topbar: search (client filter mock rows only)
Sidebar: Reference mockups (optional) — gallery of 22 PNGs for QA
```

Sidebar: People → placeholder stub (staging actions **not** in v1)

---

## 10. Path to real Blazor custom UI

| HTML artifact | Future Blazor home | Status (2026-08-10) |
|---------------|-------------------|---------------------|
| `styles/*.css` | `wwwroot/css/officer-shell/` (import from `_Host.cshtml`) | **Done** (B0) |
| `render-shell.js` layout | `OfficerShellComponent.razor` + `OfficerShellHost` | **Done** (B0) |
| `pages/staged.js` | Staged queue in shell + `IOfficerShellStagedQueryService` | **Done** (B0 — heuristic filter) |
| `pages/case-workspace.js` | `ApplicationWorkspaceComponent` embedded in shell | **Done** (B0) |
| `pages/template-wizard.js` | `ApplicationProfileWizardComponent.razor` (existing) | **Done** (via catalog Configure) |
| `mock-data.js` | Module query services + `ObjectSpace` | **Partial** (B1–B2) |

**Rule:** Do not fork design — Blazor port is a **lift-and-shift** of HTML/CSS structure, then wire real `ObjectSpace`.

---

## 11. Dependencies & risks

| Risk | Mitigation |
|------|------------|
| PNGs are AI mockups — small inconsistencies between screens | Pick one PNG per component family as master; document deltas |
| Plan doc still describes live-FK `Application` model | HTML follows **PNG / staged-merge** model; update `APPLICATION_PROFILE_PLAN.md` when product locks pivot |
| Scope creep (all XAF modules in shell) | Sidebar links outside Application Profile → placeholder only |
| UTF-16 on Windows | PowerShell UTF-8 no BOM for bulk writes; verify first bytes after create |

---

## 12. Documentation updates when HTML ships

- Link this plan from [`APPLICATION_PROFILE_PLAN.md`](APPLICATION_PROFILE_PLAN.md) §9  
- Add slice rows **H0–H7** to [`.cursor/skills/visa2026-application-profile/IMPLEMENTATION_PLAN.md`](../.cursor/skills/visa2026-application-profile/IMPLEMENTATION_PLAN.md)  
- Append entry to `learnings.md` after first officer walkthrough  

---

## 13. Suggested first task (when implementation starts)

1. Create `Visa2026.Blazor.Server/wwwroot/officer-shell/` scaffold.  
2. Implement **H0** shell — verify at `/officer-shell/`.  
3. Seed `mock-data.js` from staged ListView PNG rows.  
4. Implement **H1** staged ListView → grid toggle → Start process.

**Deferred to H7:** Person DetailView staging (invitation / visa extension filtered by `ProjectContract`).
