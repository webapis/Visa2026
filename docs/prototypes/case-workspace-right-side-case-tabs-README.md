# Case workspace — Resminamalar / SLA / Organization right-anchored tabs

**Case summary | Progress** stay left on the level-1 tab row. **Resminamalar | SLA | Organization** are horizontal tabs on the **same row, right-anchored**. Selecting them fills a **right column** only; Overview hybrid stays visible in the center. **Left nav is hidden entirely**. `#visa-preview-slot` still docks further right when open.

**Status:** implemented in `OfficerShellCaseWorkspaceComponent` (`cw-layout--no-nav` / `cw-layout--right-case`).

## Locked decisions

| Decision | Choice |
|----------|--------|
| Tab chrome | Horizontal; left = Case summary / Progress; right-anchored = Resminamalar / SLA / Organization |
| Right-tab content | Right column only; Overview stays visible |
| Left nav | **Hidden entirely** |
| Preview slot | Still docks further right when open |

## Screen

| File | Notes |
|------|--------|
| [case-workspace-right-side-case-tabs-prototype.jpg](./case-workspace-right-side-case-tabs-prototype.jpg) | No left nav; right-anchored tabs; Resminamalar in right column |