# Case workspace Overview hybrid (2014 feel)

Officers used to VISA2014 see **Case** and **people** on one screen (`Ýüz tutma` + nested people/results), with **Progress** on a sibling tab (`Ýagdaýy`). Document copies and Resminamalar were reached via **XAF report-style actions**, not separate primary destinations. Visa2026 today splits these into left-nav sections.

This prototype proposes **hybrid C** with VISA2014-like **two-level tabs** on Overview, plus **report-like preview actions** on People & links.

**Status:** implemented (hybrid Overview shell + People preview actions; left-nav Document copies / Resminamalar kept).

## Locked decisions

| Decision | Choice |
|----------|--------|
| Level 1 tabs | **Case Summary** \| **Progress** (siblings) |
| Under Case Summary | Case field tiles, then level-2 tabs |
| Level 2 tabs | **People & links** then **one tab per result type** (Invitation, Visa, Rejection, …) |
| People tab content | **People on this case** table only — no **Linked records by person** |
| Progress content | Full Progress UI on the Progress level-1 tab |
| Separate Progress page | **No** — Progress is the Overview level-1 tab; left-nav Progress is a shortcut |
| Sidebar shortcuts | Overview / People & links / Progress / Application result jump into the same Overview tabs |
| Left-nav Document copies | **Keep** for now (full section as today) — may revisit after implementation |
| Left-nav Resminamalar | **Keep** — generate / edit new and existing templates |
| People & links preview | Document copies + Resminamalar opened **only for preview** into `#visa-preview-slot`, docked as a **full-height right column** (not a bottom-corner card), not for template authoring |

### People & links — report-like access (VISA2014 feel)

| Control | Behavior |
|---------|----------|
| Toolbar **Preview document copies** | Opens a **submenu under the button** (section list for checked people). Choosing an item opens `#visa-preview-slot` **preview-only** |
| Toolbar **Preview Resminamalar** | Opens a **submenu under the button** (template list for checked people). Choosing an item opens slot **preview-only** |
| Header checkbox | Select all / clear all rows (VISA2014-like) |
| Row checkbox | Include person in toolbar multi-preview |
| Per-row preview icons | **Removed** — selection is via checkboxes + toolbar submenus |

## Screen

| File | State |
|------|--------|
| [case-workspace-overview-hybrid-2014-feel.png](./case-workspace-overview-hybrid-2014-feel.png) | Case Summary \| Progress; nested People \| result types; people toolbar + checkboxes + row preview icons; full-height right preview column |

## Implementation notes

- Blazor: `OfficerShellCaseWorkspaceComponent.razor` hybrid level-1/level-2 tabs; sidebar Overview / People / Progress / Application result are shortcuts into the same shell
- People toolbar + row actions open `#visa-preview-slot` (Person document copies / Resminamalar catalog for checked or single person)
- Left-nav Document copies / Resminamalar sections kept (authoring / full catalog)

## Success criteria

- Case + people + per-type results feel like one VISA2014 place under Overview
- Progress is one click beside Case Summary
- Document copies / Resminamalar preview from People & links feels like XAF report actions
- Template authoring remains on the Resminamalar section