# Case workspace — professional tabs & case UI

Visual polish only: quieter underlines, flatter record chips, cleaner form/toolbar/table. **Tab locations stay as today** — do not move controls.

**Status:** implemented in `case-workspace.css` + people toolbar/row actions in `OfficerShellCaseWorkspaceComponent`.

## Layout lock (must keep)

| Region | Placement (unchanged) |
|--------|------------------------|
| Level-1 left | Application \| Progress |
| Level-1 right (same row) | Templates \| SLA & deadlines \| Organization |
| Hybrid record strip | Below Application form: People & links \| Invitation \| Rejection \| Issued visa |
| Case chrome | Title + Case locked + Delete case; Edit on form |

## Style direction

| Decision | Choice |
|----------|--------|
| Level-1 tabs | Quiet text + thin accent underline; right utilities stay text links on the same row |
| Record tabs | Same place; flatter chips, muted counts, no emoji |
| Form | Label-left, lighter borders, even gaps; slim summary column |
| People toolbar / table | Aligned row; one primary CTA; quieter secondary + row actions |
| Accent | Existing purple family, used sparingly — no glow / wash / rearranged nav |

## Screen

| File | Notes |
|------|--------|
| [case-workspace-professional-tabs-ui-prototype.jpg](./case-workspace-professional-tabs-ui-prototype.jpg) | Same tab positions; professional visual polish |