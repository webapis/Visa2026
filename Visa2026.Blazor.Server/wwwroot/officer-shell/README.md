# Officer shell — interactive HTML prototype

**Plan:** [`docs/APPLICATION_PROFILE_HTML_PROTOTYPE_PLAN.md`](../../../docs/APPLICATION_PROFILE_HTML_PROTOTYPE_PLAN.md)  
**PNG reference:** [`docs/prototypes/`](../../../docs/prototypes/) (copies in `assets/png/`)

## Stack

- **Core:** vanilla HTML/CSS/ES modules (no React/Vue build step)
- **Wizard UI:** [Bootstrap 5.3](https://getbootstrap.com/) + [Bootstrap Icons](https://icons.getbootstrap.com/) via jsDelivr CDN — forms, tables, alerts, buttons
- **Custom:** `styles/wizard.css` + `js/wizard-ui.js` for PNG-specific stepper, badges, and section headers
- **Templates:** `js/template-catalog-ui.js` + `styles/template-catalog.css` (catalog list/grid + overview rail)
- **Pagination:** `js/pagination-ui.js` + `styles/pagination.css` (staged, in-process, templates)
- **Staged grouped:** `js/staged-workspace-ui.js` + `styles/staged-workspace.css` (`#/staged?group=template`)
- **Nav badges:** `js/nav-ui.js` (sidebar counts 18 / 24)
- **Case tabs:** `js/case-tabs-ui.js` + `styles/case-tabs.css` (people, progress, resminamalar, sla)

1. Start Blazor with profile **Visa2026 - PostgreSQL** (or any F5 profile).
2. Open: **`https://localhost:{port}/officer-shell/`**  
   (append `index.html` if needed: `/officer-shell/index.html`)

## Flows (mock data)

| Nav | Action |
|-----|--------|
| Staged profiles | Select ready rows → **Start process** → new in-process case; **Grouped** view for template sections |
| In process | Click row → workspace tabs |
| Profile templates | Catalog → overview → Configure → wizard → Publish |
| Reference mockups | Gallery of 22 PNGs |

**Deferred (H7):** Person DetailView staging actions.

## Slices

| Slice | Status |
|-------|--------|
| H0 Shell + mock store | Done |
| H1 Staged list/grid + merge | Done |
| H2 In-process list/grid | Done |
| H3 Case workspace (6 tabs) | Done |
| H4 Templates + overview | Done |
| H5 Template wizard (5 steps) | Done |
| H6 Gallery + README | Done |

**Parity:** [`parity/CHECKLIST.md`](parity/CHECKLIST.md) · **[`parity/COMPARISON.md`](parity/COMPARISON.md)** (22 PNG gap analysis)
