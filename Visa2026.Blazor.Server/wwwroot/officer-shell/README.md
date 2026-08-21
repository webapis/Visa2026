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
- **Template AI convert:** `js/template-convert-ui.js` + `js/template-convert-data.js` + `styles/template-convert.css` (Upload → Candidate check → Converting → Preview → Done)

1. Start Blazor with profile **Visa2026 - PostgreSQL** (or any F5 profile).
2. Open: **`https://localhost:{port}/officer-shell/`**  
   (append `index.html` if needed: `/officer-shell/index.html`)

## Flows (mock data)

| Nav | Action |
|-----|--------|
| Staged profiles | Select ready rows → **Start process** → new in-process case; **Grouped** view for template sections |
| In process | Click row → workspace tabs |
| Profile templates | Catalog → overview → Configure → wizard → Publish |
| Template AI convert | **Convert existing document** on the templates catalog (always) or on a case → Resminamalar tab (only when the topbar **Template convert editor** switch is on — spec L13) |
| Reference mockups | Gallery of 22 PNGs |

**Deferred (H7):** Person DetailView staging actions.

## Template AI convert (slice E7a)

**Interaction scenario (which control leads where):** [`docs/TEMPLATE_AI_CONVERT_UI_FLOW.md`](../../../docs/TEMPLATE_AI_CONVERT_UI_FLOW.md) — views V0–V11, guards, and the transition map. The prototype implements it; build E7b against it.

Deep-link a stage for review — `?convert=` accepts `upload`, `candidate`, `roster`, `converting`, `preview`, `done`, `help`, and `confirm`:

```text
#/templates?convert=candidate
#/templates?convert=roster
#/templates?convert=confirm
```

Mock shapes in `template-convert-data.js` mirror the shipped Module DTOs field-for-field
(`TemplateCandidateReport`, `TemplateValidationReport`, `DocumentRegion.WordSpan` / `.ExcelCell`),
so slice **E7b** swaps the store for real service calls instead of rewriting the views.
Try `Use passport number for the ID field` in the Preview chat to watch the gap become a match,
and `make the header bold` to see an out-of-scope request refused (L8).

Canonical UX: [`docs/TEMPLATE_AI_CONVERT_PRODUCT_SPEC.md`](../../../docs/TEMPLATE_AI_CONVERT_PRODUCT_SPEC.md) ·
contracts: [`docs/TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md`](../../../docs/TEMPLATE_AI_CONVERT_ENGINEERING_SPEC.md)

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
| E7a Template AI convert modal (PNGs 01–05) | Done — edge cases 06–16 pending |

**Parity:** [`parity/CHECKLIST.md`](parity/CHECKLIST.md) · **[`parity/COMPARISON.md`](parity/COMPARISON.md)** (22 PNG gap analysis)
