# Responsive UI — reference

Skill: [SKILL.md](./SKILL.md)

## Measure this PC

```powershell
Add-Type -AssemblyName System.Windows.Forms
$s = [System.Windows.Forms.Screen]::PrimaryScreen
"bounds=$($s.Bounds.Width)x$($s.Bounds.Height) working=$($s.WorkingArea.Width)x$($s.WorkingArea.Height)"
```

DPI: `GetDeviceCaps` LOGPIXELSX. **96 = 100%.** CSS pixels match the bounds at 100%.

Recorded minimum (2026-09-24): **1280×800**, working **1280×752**, **100%**.

## Where layout lives

| Piece | File |
|-------|------|
| Case columns, Overview tiles, progress, linked records | `Visa2026.Blazor.Server/wwwroot/css/officer-shell/case-workspace.css` |
| XAF sidebar width **320px** from 576px up | `Visa2026.Blazor.Server/wwwroot/css/site.css` (`.app .sidebar`) |
| Hide sidebar class | `#visa-app-shell.visa-app-shell--nav-collapsed-for-case` in `site.css` (same box model as `--nav-collapsed-for-slot`) |
| Hold / release calls | `OfficerShellCaseWorkspaceComponent.razor` → `visaCaseWorkspaceNav.hold` / `release` |
| Script tag | `Pages/_Host.cshtml` → `~/js/case-workspace-nav.js` |
| Overview markup | `OfficerShellCaseWorkspaceComponent.razor` `RenderOverview` |

Served stylesheet is `css/officer-shell/case-workspace.css` from `_Host.cshtml`. The copy under `wwwroot/officer-shell/styles/` is the prototype and is not linked.

## Containers

`.officer-case-workspace` is `container-name: officer-case`.

`.cw-overview` is `container-name: cw-overview`.

A container query cannot change the container's own grid. The rail drop is a query on **officer-case**, applied to `.cw-layout` inside it.

## XAF sidebar vs case pane

At **1280px** window width:

```text
1280 - 320 sidebar ≈ 960 case pane
960 - 300 section nav - 270 rail - gaps ≈ 360 summary
```

That 360px pane is why tiles crushed. Hiding the sidebar gives the summary the 320px back so fewer rows stack and more of the vertical page is visible.

## Left menu script

`wwwroot/js/case-workspace-nav.js`:

- `hold()` — increment a count. If a `.officer-case-workspace` has a client rect, add `visa-app-shell--nav-collapsed-for-case` on `#visa-app-shell`. Same on every window width.
- `release()` — decrement; at zero, remove the class so the menu returns.
- On `resize`, a capturing `click` (tab change), and DOM changes, re-apply. A hidden MDI tab must not keep the menu closed.
- Do not remove `visa-app-shell--nav-collapsed-for-slot` (preview slot uses that class).
