# PNG ↔ HTML parity comparison

**Compared:** 2026-08-10  
**Interactive:** `/officer-shell/` (hash routes below)  
**PNG source:** `docs/prototypes/` · copies in `assets/png/`  
**Gallery:** `#/mockups` (side-by-side in browser)

**Legend**

| Symbol | Meaning |
|--------|---------|
| ✅ | **Close** — structure + key copy present; polish/CSS gaps only |
| 🟡 | **Partial** — route works; missing major UI blocks from PNG |
| 🔴 | **Stub** — placeholder or wrong layout vs PNG |
| ⬜ | **Not built** — no route or doc-only |

**Overall (22 PNGs):** ✅ 5 · 🟡 12 · 🔴 4 · ⬜ 1

---

## Quick route map

| PNG group | Open in browser |
|-----------|-----------------|
| Staged list/grid | `#/staged` (toggle List/Grid) |
| Staged grouped workspace | `#/staged?group=template` (toggle **Grouped**) |
| In-process list/grid | `#/in-process` |
| Case workspace + tabs | `#/case/p1/overview` … `#/case/p1/sla` |
| Templates catalog | `#/templates` |
| Template overview | `#/templates/t1` |
| Template wizard steps 1–5 | `#/templates/wizard/0` … `/4` |
| PNG gallery | `#/mockups` |

---

## 1. Shell & navigation (2 PNGs)

### `visa2026-custom-left-navigation-shell-mockup.png` — 🟡 Partial

| Aspect | PNG | HTML (`os-*` shell) | Gap |
|--------|-----|---------------------|-----|
| Navy sidebar + light content | ✓ | ✓ | — |
| VISA2026 branding | Globe / wordmark variants in PNGs | `V26` square logo + text | Logo art differs |
| Nav groups | DASHBOARD, PEOPLE, APPLICATION PROFILES, … | Same group labels | ✓ |
| Top breadcrumb bar | In content on some PNGs | `os-topbar` crumb | ✓ |
| User chip bottom | Name + role + office | ✓ mock user | ✓ |
| Extra nav items | Document copies queue, Reports, Admin collapsed | Report Dashboard, SLA monitor only | IA simplified |
| Collapse control | Some PNGs show « collapse | Missing | Not implemented |

**Route:** all non-wizard pages.

### `application-profiles-navigation-sidebar-mockup.png` — ✅ Close

| Aspect | PNG | HTML | Gap |
|--------|-----|------|-----|
| Staged badge **18** | Orange count on nav item | Dynamic **18** (`staged.length`) | ✓ |
| In process badge **24** | Blue count | Dynamic **24** (`inProcess.length`) | ✓ |
| Profile templates sub | “Configuration · Visa office admin” | ✓ | `nav-ui.js` |
| Flow diagram overlay | Templates → Staged → In process callout | Missing | Doc-only annotation |
| Staged page toolbar | + New profile, Merge, More | Only Start process in header | Extra actions missing |

**Route:** `#/staged`, sidebar on all pages.

---

## 2. Staged profiles (3 PNGs)

### `staged-profiles-listview-table-mockup.png` — 🟡 Partial

| Aspect | PNG | HTML `#/staged` list | Gap |
|--------|-----|----------------------|-----|
| Title + subtitle | ✓ | ✓ (has extra `prototype` tag) | Remove tag for parity |
| Search + dropdowns | ✓ | ✓ (non-functional) | — |
| **Template family filter chips** | All (18), Registration (5), Invitation (6), … | **Missing** | High priority |
| Color legend under chips | ✓ | Missing | — |
| Table columns | 8 incl. row chevron | 7, no action column | Add › column |
| Readiness labels | “Awaiting data” | `awaiting` pill text | Label casing |
| Row count | 18 rows | 7 mock rows | Data volume |
| Warning banner | Orange, specific copy | Similar banner when blocked | ✓ |
| Pagination | Rows per page + numbered pages | ✓ | `pagination-ui.js` (P6) |
| Start process position | Toolbar right | Header right | Minor layout |

### `staged-profiles-grid-cards-mockup.png` — 🟡 Partial

| Aspect | PNG | HTML grid toggle | Gap |
|--------|-----|------------------|-----|
| 4-column card grid | ✓ | `auto-fill` grid | ✓ |
| Top color stripe | ✓ | ✓ | ✓ |
| Type badge on card | “Registration upon arrival” pill | Only dot + label in meta | Badge missing |
| Project / staged icons | Briefcase + calendar icons | Plain text meta | Icons missing |
| Mid-card missing hint | Orange warning line | Not on cards | — |
| Footer readiness + chevron | ✓ | ✓ simplified | — |
| Filter chips + legend | ✓ | Missing | Same as list |
| Bottom orange banner | Full width below grid | Banner above table only | Grid banner position |

### `staged-application-profiles-workspace-mockup.png` — ✅ Close

Grouped-by-template workspace: accordion sections per template family, avatars, row meta badges, readiness dots, selection bar, Start process in toolbar. Route `#/staged?group=template` or **Grouped** view toggle — `staged-workspace-ui.js`.

---

## 3. In-process profiles (2 PNGs)

### `process-started-profiles-listview-table-mockup.png` — 🟡 Partial

| Aspect | PNG | HTML `#/in-process` list | Gap |
|--------|-----|--------------------------|-----|
| Title + subtitle | ✓ | ✓ | ✓ |
| Search + filters | Search, All templates, Newest first | Search only | Filters missing |
| **Family filter chips** | All (24), Visa extension (8), … | **Missing** | High priority |
| Row checkboxes | ✓ | **Missing** | — |
| Column “Project / Contract” | ✓ | “Project” only | Header text |
| SLA column | “12 days” / “5 days” green-orange | Generic ready/awaiting pill | Wrong SLA presentation |
| Sort icons on headers | ✓ | Missing | — |
| Pagination | Full | ✓ | `pagination-ui.js` |
| Row click → workspace | ✓ | ✓ | ✓ |

### `process-started-profiles-list-cards-mockup.png` — 🟡 Partial

| Aspect | PNG | HTML grid | Gap |
|--------|-----|-----------|-----|
| Card layout with SLA + step | Rich cards | Minimal meta | Card density low |
| Filter chips | ✓ | Missing | — |
| Multi-select checkboxes | Some PNGs | Missing | — |

---

## 4. Case workspace (7 PNGs)

**Route base:** `#/case/p1/{tab}` — tabs: `overview`, `people`, `progress`, `documents`, `resminamalar`, `sla`

### `process-started-application-profile-workspace-mockup.png` — 🔴 Stub

| Aspect | PNG | HTML overview tab | Gap |
|--------|-----|-------------------|-----|
| Header | № + SLA “12 days remaining” + In process badge | Basic badges | SLA copy/format |
| Person strip | Avatar initials + names + “Merged from 3…” | Plain name pills | Avatars missing |
| **Case summary** | Icon grid (visa type, category, period, project, entry) | Bullet list | Layout wrong |
| **Progress stepper** | Horizontal 4-step with Completed/In progress/Pending badges | Simple 4 boxes | Stepper style |
| **Linked records** | Tile row with counts + chevrons | Bullet list | Layout wrong |
| Right rail | **Readiness** card + Quick actions + **Activity** timeline | Quick actions only | Readiness + Activity missing |
| Footer | Template line | Missing | — |

### `process-started-nav-overview.png` — 🔴 Stub

Alt overview layout; HTML uses single simplified overview (same gaps as above).

### `process-started-nav-people-links.png` — ✅ Close

| PNG | HTML `people` tab |
|-----|-------------------|
| People table + linked records matrix + summary rail | `case-tabs-ui.js` — per-person record cards, Valid/Expired states |

### `process-started-nav-progress.png` — ✅ Close

| PNG | HTML `progress` tab |
|-----|---------------------|
| Vertical timeline + ministry step detail + advance UX | Vertical stepper, upload zone, progress rail with SLA ring |

### `process-started-nav-document-copies.png` — ✅ Close

| PNG | HTML `documents` tab |
|-----|----------------------|
| Readiness summary bar, per-person accordion, preview pane, package actions | `document-copies-ui.js` (P3) |

### `process-started-nav-resminamalar.png` — ✅ Close

| PNG | HTML `resminamalar` tab |
|-----|-------------------------|
| Catalog groups, readiness chips, preview slot | Grouped table + preview pane + ZIP actions |

### `process-started-nav-sla-deadlines.png` — ✅ Close

| PNG | HTML `sla` tab |
|-----|----------------|
| SLA metrics, timeline, deadlines table, alerts | `case-tabs-ui.js` SLA dashboard |

---

## 5. Profile templates (9 PNGs)

### `application-profile-templates-listview-mockup.png` — ✅ Close

| Aspect | PNG | HTML `#/templates` list | Gap |
|--------|-----|---------------------------|-----|
| + New template (green) | ✓ | ✓ | ✓ |
| Search + action family + sort dropdowns | ✓ | ✓ | Styled (mock) |
| **Action family chips** | All (12), Issuance (4), … | ✓ | — |
| Status pills | Active / Locked / Draft with icons | ✓ | `template-catalog-ui.js` |
| Column headers | “Template name”, “Staged uses”, … | ✓ | — |
| Pagination | ✓ | ✓ | Shared pager + rows per page |

### `application-profile-templates-grid-mockup.png` — ✅ Close

Grid cards: color stripe, icon circle, code badge, action/via rows, status + hint, staged/in-process stats, Configure footer — `template-catalog-ui.js` + `template-catalog.css`.

### `application-profile-template-overview-mockup.png` — ✅ Close

| Aspect | PNG | HTML `#/templates/t1` | Gap |
|--------|-----|----------------------|-----|
| Left template rail | Search + cards with status dots | ✓ | `tc-rail` cards |
| 4 numbered summary columns | Identity / Results / Process / Templates | ✓ | `tc-cols` |
| Usage stats bar | 24 staged · 8 in process + last configured | ✓ | `tc-usage` |
| Lock hint footer | Blue info + padlock | ✓ | `tc-lock-banner` |
| Top badges | Active, Published | ✓ | — |
| Top breadcrumbs | Home › Profile templates › … | Only topbar crumb | Minor |

### Wizard steps (5 PNGs) — ✅ Close (after Bootstrap rebuild)

| PNG | Route | Status | Remaining gaps |
|-----|-------|--------|----------------|
| `application-profile-template-wizard-mockup.png` | `#/templates/wizard/0` | ✅ | Icon-rail sidebar vs PNG crest logo; minor spacing |
| `…-wizard-step2-mockup.png` | `/wizard/1` | ✅ | Step 2 sidebar labels differ in one PNG variant |
| `…-wizard-step3-mockup.png` | `/wizard/2` | ✅ | Step 3 PNG uses different top bar (admin) — wizard chrome unified |
| `…-wizard-step4-mockup.png` | `/wizard/3` | ✅ | — |
| `…-wizard-step5-mockup.png` | `/wizard/4` | ✅ | Step 5 PNG has dark top bar variant — footer/actions match |

Wizard uses **Bootstrap 5 + Bootstrap Icons** + `wizard-ui.js` / `wizard.css`.

---

## 6. Priority backlog (PNG parity)

Ordered by impact across multiple screens:

| P | Item | PNGs affected | Status |
|---|------|---------------|--------|
| 1 | **Template family filter chips** + legend | Staged list/grid, In-process list, Templates catalog | **Done** (2026-08-10) |
| 2 | **Case workspace overview** | workspace + nav-overview | **Done** (2026-08-10) — `case-workspace-ui.js` |
| 3 | **Document copies tab** | nav-document-copies | **Done** (2026-08-10) — `document-copies-ui.js` |
| 4 | **Template catalog filters** | templates list/grid | **Done** (2026-08-10) — `template-catalog-ui.js` |
| 5 | **Template overview rail** | template-overview | **Done** (2026-08-10) — same module |
| 6 | **Pagination component** | All list views | **Done** (2026-08-10) — `pagination-ui.js` |
| 7 | **SLA column formatting** | in-process list | **Done** — `N days` green/amber chips |
| 8 | **Staged grouped workspace** | staged-application-profiles-workspace | **Done** (2026-08-10) — `staged-workspace-ui.js` |
| 9 | **Nav badge counts** | nav sidebar | **Done** (2026-08-10) — seed 18/24 + `nav-ui.js` |
| 10 | **People / progress / resminamalar / SLA tabs** | 4 nav PNGs | **Done** (2026-08-10) — `case-tabs-ui.js` |

---

## 7. How to verify

1. F5 → `https://localhost:{port}/officer-shell/`
2. Open `#/mockups` — PNG reference gallery
3. Walk routes in §Quick route map at **1440×900**, 100% zoom
4. Tick `CHECKLIST.md` when a row reaches ✅

**Wizard vs rest:** Wizard is Bootstrap-heavy; list/workspace screens still use custom `os-*` CSS — expect the largest visual gap outside wizard until backlog P1–P5 land.
