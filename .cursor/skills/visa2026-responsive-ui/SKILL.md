---
name: visa2026-responsive-ui
description: >-
  Visa2026 officer-shell responsive layout: measure the minimum screen, reflow
  Application Profile instance sections, stop tiles stretching, and hide the XAF
  left menu on a narrow instance detail so more of the page is on screen.
  Use for responsive UI, screen size, 1280×800, Overview tiles, vertical scroll,
  or closing the left navigation while a case is open. Not profile domain rules
  (visa2026-application-profile) or #visa-preview-slot (visa2026-preview-slot).
  Read learnings.md first; append after verified fixes.
disable-model-invocation: false
---

# Visa2026 — Responsive UI

**User prompts:** [prompts.md](./prompts.md) (`@visa2026-responsive-ui`).

**Widths and files:** [reference.md](./reference.md).

## Agent workflow

1. Read [learnings.md](./learnings.md) (newest first) and **Scenarios**.
2. Measure the officer window if the task names "this PC" or "smallest screen". Do not invent a phone width.
3. Change layout in the section the officer named. Overview is first; other case tabs are not on these cuts yet.
4. Hard-refresh the Blazor host. Compare the **1280×800** window and one wider window. Tiles must hug their text. More of Progress and Linked records should be on screen without a tall empty tile.
5. Append [learnings.md](./learnings.md) after a verified fix ([MATURITY.md](./MATURITY.md)).

## Layouts

Two layouts. Not a list of named devices.

| Layout | When |
|--------|------|
| **Wide** | Case pane wider than **1120px** and the summary wider than **760px**. Three tile columns, identity column on the side, Readiness on the right. |
| **Minimum** | This officer PC: **1280×800** at **100%** scale (working area **1280×752**). XAF sidebar is **320px**, so the case pane is about **960px**. |

Cuts inside Overview (the pane's own width, not the monitor name):

| Pane | Change |
|------|--------|
| Case pane ≤ **1120px** | Readiness, Quick actions, and Activity move **under** the summary. Section nav stays **300px**. |
| Summary ≤ **760px** | Number, date, and process number in **one row** on top. Other tiles in **two columns**. |
| Summary ≤ **420px** | One column. Only if the window is dragged smaller than this PC. |

## Rules

1. **Minimum is 1280×800.** Do not design Overview for a phone.
2. **Do not shrink** the 300px section nav. Türkmençe labels stay on one line.
3. **Tiles stay content height.** `.cw-summary-body` uses `align-items: flex-start`. Grids use `align-content: start`. A short tile must not grow to match a taller column.
4. **Key off the case pane**, not the full browser window. The 320px sidebar is outside `.officer-case-workspace`.
5. **One section at a time.** Overview first (`RenderOverview` / `.cw-overview`). Do not restack People, Progress, or Resminamalar until that section is named.
6. **Left menu while an instance is open.** On every screen size, hide `app .sidebar` while an Application Profile instance detail is visible, so the summary can use that width. When the detail closes, or its tab is hidden, show the menu again.

## Scenarios

| Symptom | First step |
|---------|------------|
| Tiles are tall empty boxes | `.cw-summary-body` stretched to the identity column. Keep `align-items: flex-start`. |
| Readiness still beside a crushed summary on this PC | Container is `.officer-case-workspace` at **1120px**, not `@media (max-width: 1280px)` on the window. |
| Left menu still open on an instance detail | `visaCaseWorkspaceNav` in `wwwroot/js/case-workspace-nav.js`. It closes the menu on every screen size while `.officer-case-workspace` is visible. Hard-refresh so `_Host.cshtml` loads the script. |
| Section nav labels wrap | Do not shrink the 300px column. |
| Resminamalar or Document copies titles stack one letter per line when Preview is open | `.ct-resmi-split` and `.dc-split` must be one column. The old second column is empty. Under an 820px case pane, section buttons wrap in a row so the catalog keeps the width. |
| Application result is squeezed when New Rejection is open | Same 820px case-pane rule, on `.cw-layout:has(.cw-result-board)`. Reset `grid-column` from the overview rule or the cards stay in a second column. |

## Scope

| In | Out |
|----|-----|
| Case workspace CSS, instance detail left menu | Profile seed, lock, progress rules |
| Measuring the officer screen | Preview-slot resize (`visa2026-preview-slot`) |
| Overview reflow | Phone-first breakpoints as the design target |

**Related:** [application-profile](../visa2026-application-profile/SKILL.md) · [preview-slot](../visa2026-preview-slot/SKILL.md).
