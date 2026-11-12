# Application Profile Instance — ListView progress row colors

> **Status:** **Implemented.** Canonical officer coloring strategy for **Application Profile Instance** ListViews (registry, CSS, precedence, Turkmen labels).
>
> **Related:** [`APPLICATION_LISTVIEW_STATE_COLORS.md`](APPLICATION_LISTVIEW_STATE_COLORS.md) (history / wiring), [`BO_STATE_COLORS.md`](BO_STATE_COLORS.md) (global registry), skill [`visa2026-bo-state-colors`](../.cursor/skills/visa2026-bo-state-colors/SKILL.md).

---

## 1. Purpose

Each ListView row background shows **where the case is in processing now**, from the **latest** `ApplicationProgress` step (plus a few ListView-only overrides such as Via Ministries issuance green and last-approval blue).

Turkmen labels below are the officer-facing names used in seed / localization (`LookupCatalogStrings`, `application-state.json`).

---

## 2. Palette (Turkmen)

| Officer label (tk) | Progress / appearance code | Background | Text |
|--------------------|----------------------------|------------|------|
| **Ofisde** | `IS_BEING_PREPARED` / `AT_OFFICE` | `#FEDEAF` | `#7c4a03` |
| **Ministrlige iberlen** · ara ylalaşyklar | `N_REVIEW_STARTED`, intermediate `N_REVIEW_APPROVED`, `AT_THE_MINISTERY_N` | `#3FE0D0` | `#134e4a` |
| **Ministrlikden çykan** | Last-leg `N_REVIEW_APPROVED` → ListView code `LAST_APPROVAL_COMPLETE` | `#4169E2` | `#ffffff` |
| **Migrasiýa gullugyna tabşyryldy** | `SUBMITTED_MIGRATION` | `#FFF59D` (light yellow) | `#854d0e` |
| **CO belgisi alyndy** | `PROCESS_STARTED` | `#FED700` (darker yellow) | `#1a1a1a` |
| **Resmileşdirildi** | `PROCESS_ISSUED` (also Via Ministries green gate) | `#86efac` | `#15803d` |
| **Garşylyk berildi** · **Ministrlikden gelmedi** | `PROCESS_REJECTED` / `N_REVIEW_REJECTED` | `#fca5a5` | `#b91c1c` |
| **Ýüz tutma ýatyryldy** | `PROCESS_CANCELLED` | `#fecaca` | `#991b1b` |

**Yellow pair rule:** Submitted→migration stays **lighter** (`#FFF59D`); AS/CO received stays **darker** (`#FED700`) so the two steps stay distinguishable.

---

## 3. Precedence (ListView appearance)

Resolved in `ApplicationListViewDisplayState.Resolve`. Milestone colors **override** SLA tint for forced steps:

1. **Via Ministries issuance green** — latest is `PROCESS_STARTED` **and** required Result chips are complete → paint as `PROCESS_ISSUED` (`ApplicationWorkspaceViaMinistryIssuanceGreenGate`). Invitation profiles ignore Visa chip.
2. **CO belgisi alyndy** — `PROCESS_STARTED` → `#FED700`
3. **Migrasiýa gullugyna tabşyryldy** — `SUBMITTED_MIGRATION` → `#FFF59D`
4. **Ministrlikden çykan** — last ministry leg `N_REVIEW_APPROVED` → `LAST_APPROVAL_COMPLETE` → `#4169E2`
5. **Approval-leg teal** — other `N_REVIEW_STARTED` / intermediate `N_REVIEW_APPROVED` / `AT_THE_MINISTERY_N` → `#3FE0D0`
6. **Ofisde peach** — `IS_BEING_PREPARED` / `AT_OFFICE` → `#FEDEAF`
7. Else SLA / completed-route appearance (warning / overdue / other registry codes)

All reject outcomes share one red (`#fca5a5`). Cancelled stays the lighter coral (`#fecaca`).

---

## 4. Code touchpoints

| Layer | Location |
|-------|----------|
| Precedence + CSS class | `Visa2026.Module/BusinessObjects/ApplicationListViewDisplayState.cs` |
| Last-approval / approval-leg helpers | `Visa2026.Module/BusinessObjects/ApplicationProgressLegCodes.cs` |
| Via Ministries green gate | `Visa2026.Module/Services/ApplicationWorkspace/ApplicationWorkspaceViaMinistryIssuanceGreenGate.cs` |
| Hex / XAF registry | `Visa2026.Module/Appearance/BoStateAppearanceColors.cs` |
| Blazor row CSS | `Visa2026.Blazor.Server/wwwroot/css/site.css` (`.visa-progress-row--state-*`) |
| Turkmen state names | `LookupCatalogStrings.json`, `LookupCatalogs/application-state.json` |

---

## 5. Changelog

| Date | Note |
|------|------|
| 2026-11-12 | Strategy captured: palette, Turkmen labels, precedence, yellow pair contrast (`#FFF59D` vs `#FED700`), Ministrlikden çykan blue, Ministrlige iberlen rename |
| 2026-11-12 | Applied: palette/precedence wired; last-approval ListView label Ministrlikden çykan; unit tests lock hex + Resolve |

## 6. Color filter tabs (ListView)

Above every Application Profile Instance ListView grid:

- **Ähli** always; every color bucket always visible (count may be **0**; empty grid when selected)
- One tab **per painted color** (swatch + Turkmen label + count)
- Filter uses `ProgressColorTabKey` from `ListRowAppearanceStateCode` (Via Ministries green gate and Ministrlikden çykan included)
- Remembers last tab **per ListView id** in browser `localStorage`; unknown key → fall back to **Ähli**
- Left nav (Via ministry / Direct migration) unchanged
- **Gözleg/Search** tab: appears when searching (auto-selected); count = matches across all colors; clearing the box keeps Search at 0 until a color tab is chosen; color tabs stay visible

| Layer | Location |
|-------|----------|
| Bucket map / labels | `ApplicationProfileInstanceProgressColorTab` |
| BO key | `ApplicationProfileInstance.ProgressColorTabKey` |
| UI + criteria | `ApplicationProfileInstanceProgressColorTabsController` + `ApplicationProfileInstanceProgressColorTabs.razor` |
| Persistence | `wwwroot/js/progress-color-tabs.js` |

Prototype: [`docs/prototypes/application-profile-instance-listview-process-color-tabs-prototype-dark.jpg`](prototypes/application-profile-instance-listview-process-color-tabs-prototype-dark.jpg)

| 2026-11-12 | Color filter tabs: per-ListView localStorage, Ähli fallback, one tab per painted color |
| 2026-11-12 | Always show zero-count color tabs (incl. Rejected/Cancelled); click keeps filter / empty grid |
