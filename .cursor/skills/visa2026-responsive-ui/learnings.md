# Learnings (append-only): Responsive UI

**Read before every responsive-ui task.** Newest first.

**After a verified fix:** append one entry. Do not edit or delete prior entries.

```markdown
### YYYY-MM-DD — <short title>

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: responsive-ui | application-profile | —
```

---

## Entries

### 2026-09-24 — Issued visa form left empty space in the slot

- **Symptom**: New Issued visa did not use the free space on the right of the preview. Action labels on the result page ran into the next button.
- **Try**: `.issue-issued-visa-slot` was `max-width: 42rem`. Action buttons used `minmax(200px, 1fr)`.
- **Test**: Officer screenshot of New Issued visa beside Application result. Not rechecked after this change.
- **Root cause**: The 42rem cap stopped the visa fields short of the slot edge. A 200px action column is narrower than icon + name + “Add Rejection”.
- **Fix**: Visa form `max-width: none`. Action row minimum is `17.5rem`, so a button wraps to the next line before its label collides.
- **Prevent**: Do not cap the issued-visa form narrower than the preview slot.
- **Cross-skill**: responsive-ui | preview-slot

### 2026-09-24 — Invitation form left empty space on the right of the slot

- **Symptom**: Edit invitation beside Application result left unused space to the right of the form.
- **Try**: `.issue-issued-header-slot` was `width: 100%` and `max-width: 40rem`.
- **Test**: Officer screenshot of Edit invitation CO322188. The result cards and section row look right. Form width not rechecked after this change.
- **Root cause**: The 40rem cap stopped the form short of the preview slot's right edge.
- **Fix**: `max-width: none`, so the header fields and the people table use the full slot width.
- **Prevent**: Do not cap the issued-header form narrower than the preview slot.
- **Cross-skill**: responsive-ui | preview-slot

### 2026-09-24 — Application result squeezed when New Rejection opens

- **Symptom**: Application result on instance 5/-840 stayed readable until New Rejection opened. Then the section list stayed a vertical column and Rejection / Issued visa were squeezed.
- **Try**: Result markup is inside `.cw-overview`, so the 1120px rule keeps `300px` plus the content. It is not a wide tab, so the Document copies row did not apply.
- **Test**: Officer screenshots. Fine with the slot closed. Not rechecked after this fix.
- **Root cause**: Half-window New Rejection leaves about a 640px case pane. 300px section nav plus the quick-actions rail leaves a narrow result column.
- **Fix**: Under an 820px case pane, `.cw-layout:has(.cw-result-board)` becomes one column. Section buttons wrap in a row. Quick actions move under the result cards. `grid-column` is reset so the overview rule cannot keep a second column.
- **Prevent**: A new case tab that reuses `.cw-overview` still needs its own narrow-pane rule. Reset `grid-column` when stacking.
- **Cross-skill**: responsive-ui

### 2026-09-24 — Document copies preview layout confirmed

- **Symptom**: Preview crushed the Document copies list.
- **Try**: One column for `.dc-split`. Under an 820px case pane, section buttons wrap in a row, same as Resminamalar.
- **Test**: Officer screenshots on `localhost:5001` with a passport preview open. Narrow window: section buttons in a row, Passport and Visa stay on one line beside Ready and Preview. Wider window: section nav stays on the left and the list stays beside the preview. Officer said it seems fine.
- **Root cause**: An empty 300px column left the list a few pixels wide.
- **Fix**: Keep `.dc-split` on one column. Share the narrow-pane section row with Resminamalar.
- **Prevent**: Do not reserve a second column in `.dc-split`.
- **Cross-skill**: responsive-ui | preview-slot

### 2026-09-24 — Document copies catalog collapses when Preview opens

- **Symptom**: On Document copies, Preview turned Passport and the address into one letter per line. The section nav stayed a vertical column.
- **Try**: Same shape as Resminamalar. `.dc-split` was `1fr 300px` with only `.dc-list` as a child. The 300px column is empty. The narrow-pane row of section buttons applied only to `.ct-resmi-page`.
- **Test**: Officer screenshots. Catalog is fine until Preview. Not rechecked after this fix.
- **Root cause**: A leftover inline-preview column, plus the 300px section nav still beside that narrow pane. `overflow-wrap: anywhere` on the record label.
- **Fix**: One column for `.dc-split`. The 820px case-pane rule now includes `.dc-page`, so section buttons wrap and the catalog uses the pane width. Labels wrap on spaces.
- **Prevent**: Do not reserve a second column in `.dc-split`. Do not use `overflow-wrap: anywhere` on document names.
- **Cross-skill**: responsive-ui | preview-slot

### 2026-09-24 — Resminamalar preview layout confirmed

- **Symptom**: Preview crushed the template catalog.
- **Try**: One column for `.ct-resmi-split`. Under an 820px case pane, section buttons wrap in a row.
- **Test**: Officer screenshots of instance 5/-840 on `localhost:5001` with Preview open. Narrow window: section buttons in a row, titles on one line, Ready and Preview on the card. Wider window: section nav stays on the left and the catalog stays beside the preview. Officer said it seems fine.
- **Root cause**: An empty 280px column left the catalog a few pixels wide.
- **Fix**: Keep the catalog on one column. Do not reserve a second preview column in the case tab.
- **Prevent**: Do not put `overflow-wrap: anywhere` back on template titles.
- **Cross-skill**: responsive-ui | preview-slot

### 2026-09-24 — Resminamalar catalog collapses when Preview opens

- **Symptom**: On instance 5/-840, Preview turned template names into one letter per line and stacked the toolbar.
- **Try**: `.ct-resmi-split` was `1fr 280px`. The case tab has only the catalog child. The 280px column is empty. Preview is the global slot at half the window, so the case pane is about 640px and the catalog column falls to a few pixels. `overflow-wrap: anywhere` then breaks every letter.
- **Test**: Officer screenshots. Catalog is fine until Preview. Not rechecked after this fix.
- **Root cause**: A leftover inline-preview column, plus the 300px section nav still beside that narrow pane.
- **Fix**: One column for `.ct-resmi-split`. Under an 820px case pane, section buttons wrap in a row and the catalog uses the pane width. Titles wrap on spaces.
- **Prevent**: Do not reserve a second column in `.ct-resmi-split`. Do not use `overflow-wrap: anywhere` on template titles.
- **Cross-skill**: responsive-ui | preview-slot

### 2026-09-24 — Compact summary tiles confirmed

- **Symptom**: Case summary properties were tall boxes.
- **Try**: Icon on the left of the label and value, 26px, padding 7px 10px.
- **Test**: Officer screenshots of instance 5/-840 on two window sizes at `localhost:5001`. Both keep short rows. On the wider window, Progress and Linked records sit on the same screen. Officer said it seems fine.
- **Root cause**: The icon had its own row.
- **Fix**: Keep the icon beside the text.
- **Prevent**: Do not put the summary icon back in its own row.
- **Cross-skill**: responsive-ui

### 2026-09-24 — Case summary tiles were tall because the icon had its own row

- **Symptom**: Overview tiles on instance 5/-840 looked like tall boxes. The icon sat above the label.
- **Try**: `.cw-sum-tile` was a column: 32px icon, then label, then value, with 12px padding.
- **Test**: Officer close-up of Visa type / Project / Border zone and the number column.
- **Root cause**: The icon occupied a full row, so each property was about as tall as it was wide.
- **Fix**: Icon sits on the left and spans the label and value. Padding is 7px 10px; icon is 26px.
- **Prevent**: Do not put the summary icon back in its own row.
- **Cross-skill**: responsive-ui

### 2026-09-24 — Left menu confirmed closed on an open instance

- **Symptom**: Sidebar stayed open on the instance detail.
- **Try**: `visaCaseWorkspaceNav.hold` adds `visa-app-shell--nav-collapsed-for-case` on every window width while `.officer-case-workspace` is visible.
- **Test**: Officer screenshot of instance 5/-844 on `localhost:5001`. The XAF accordion is gone; Home / View / Misc and the case page fill the width. Readiness stays on the right of Overview.
- **Root cause**: The earlier 1280px gate skipped this window.
- **Fix**: No width check. Closing the detail or leaving the tab calls `release` and shows the menu again.
- **Prevent**: Do not gate this menu on window width.
- **Cross-skill**: responsive-ui

### 2026-09-24 — Close the left menu on every screen size

- **Symptom**: Instance detail 5/-840 on `localhost:5001` still showed the XAF sidebar. Readiness stayed beside the summary, so the window was wider than 1280px.
- **Try**: `visaCaseWorkspaceNav` collapsed the sidebar only when `window.innerWidth <= 1280`.
- **Test**: Officer screenshot after the script existed. Sidebar still open.
- **Root cause**: The width gate skipped every window above 1280px. The officer wants the same close/open behavior on every size.
- **Fix**: `hold` adds `visa-app-shell--nav-collapsed-for-case` whenever a case workspace is visible. `release` and a hidden tab remove it. No width check.
- **Prevent**: Do not gate this menu on window width.
- **Cross-skill**: responsive-ui

### 2026-09-24 — visaCaseWorkspaceNav.hold was undefined

- **Symptom**: Opening an Application Profile instance threw `JSException`: `visaCaseWorkspaceNav` was undefined, at `OfficerShellCaseWorkspaceComponent.OnAfterRenderAsync`.
- **Try**: The razor and `_Host.cshtml` already called `visaCaseWorkspaceNav.hold`. The script file was not on disk.
- **Test**: Not rechecked in the browser after adding the file. A hard refresh is required so the new script loads.
- **Root cause**: `wwwroot/js/case-workspace-nav.js` was missing, and `OnAfterRenderAsync` did not catch `JSException`.
- **Fix**: Added the script (collapse the XAF sidebar only when the window is ≤ 1280px and the case workspace is visible; `release` restores it). Catch `JSException` around hold and release.
- **Prevent**: Do not call `visaCaseWorkspaceNav` without the script tag and file. Do not let a missing interop function fail the detail view.
- **Cross-skill**: responsive-ui

### 2026-09-24 — Left-menu script is referenced but missing

- **Symptom**: Instance detail on this PC still shows the XAF left menu, so Overview stays narrow and Progress / Linked records sit below the fold.
- **Try**: `OfficerShellCaseWorkspaceComponent` calls `visaCaseWorkspaceNav.hold` / `release`. `_Host.cshtml` loads `~/js/case-workspace-nav.js`. `site.css` hides `.sidebar` when `#visa-app-shell` has `visa-app-shell--nav-collapsed-for-case`.
- **Test**: The script file is not in the repo. Hold throws at runtime. Not verified in the browser.
- **Root cause**: The script was never added.
- **Fix**: Add `wwwroot/js/case-workspace-nav.js` as in [reference.md](./reference.md). Collapse only when the window is ≤ 1280px and the case workspace is visible. Remove the class when the detail closes.
- **Prevent**: Do not hide the menu on a wide monitor. Do not reuse `visa-app-shell--nav-collapsed-for-slot` (that class belongs to the preview slot).
- **Cross-skill**: application-profile

### 2026-09-24 — Overview tiles stretched; rail drop used the window width

- **Symptom**: Narrow window: Urgency was a tall empty tile and Readiness stayed beside the summary. Wide window: case tiles grew to the height of the identity column.
- **Try**: `align-items: stretch` on `.cw-summary-body`. Rail drop was `@media (max-width: 1280px)`.
- **Test**: Officer screenshots on `localhost:5001` (instance 5/-840). Stretch was visible on both widths. The 1280px window rule did not run when the browser window was wider than 1280 even though the case pane was already narrow.
- **Root cause**: Grid rows stretched to the taller column. The sidebar (320px) sits outside the case pane, so a full-window media query misses that pane.
- **Fix**: `align-items: flex-start` and `align-content: start`. Rail drop is `@container officer-case (max-width: 1120px)` on Overview only.
- **Prevent**: Do not set `align-items: stretch` on `.cw-summary-body`. Do not key the Overview rail off the full window width.
- **Cross-skill**: application-profile

### 2026-09-24 — Minimum screen is this PC at 1280×800

- **Symptom**: Overview tiles crushed (place names wrapped, process number at the bottom edge) on the officer's smallest screen.
- **Try**: `[System.Windows.Forms.Screen]::PrimaryScreen` plus LOGPIXELSX.
- **Test**: Primary bounds **1280×800**, working **1280×752**, DPI **96** (100%).
- **Root cause**: Three tile columns plus a 280px identity column plus a 270px rail do not fit beside a 320px XAF sidebar and a 300px section nav.
- **Fix**: Treat **1280×800** as the minimum. Wider monitors keep the three-column summary. One-column layout is only under a 420px summary pane.
- **Prevent**: Do not add a phone width as the Overview design target. Do not shrink the 300px section nav.
- **Cross-skill**: application-profile
