# Excel in-place editor — learnings (append-only)

Read **newest first** under **## Entries**. Promote repeat patterns to [SKILL.md](./SKILL.md) **Scenarios** ([MATURITY.md](./MATURITY.md)).

---

## Entries

### 2026-06-19 — Blazor re-render wiped iframe; reload appeared broken

**Symptom:** Reload from database did not show template; grid stayed blank below ribbon.

**Cause:** `<iframe src="about:blank">` in Panel markup — every `StateHasChanged` (status chip, dirty flag) reset `src` to `about:blank`.

**Fix:** Remove `src` from Razor; JS sets `src` from `data-src` (lazy load when tab visible). Reload uses `reloadSpreadsheetIframe` (`about:blank` → new URL).

**Verify:** Edit cell → status updates → grid still visible; Reload shows DB content.

---

### 2026-06-19 — Embed layout: ribbon OK, grid blank

**Symptom:** Initial load showed ribbon without cells; switching ribbon tabs sometimes showed content but ribbon collapsed.

**Cause:** Iframe page used `100vh` while iframe height was ~62vh; `SetHeight()` / wrong flex sizing left worksheet viewport at 0 height.

**Fix:** Embed CSS: `html/body height: 100%`, host `flex: 1 1 0; height: 0`, spreadsheet `position: absolute; inset: 0`. JS: `AdjustControl` + `urt-spreadsheet-resize` postMessage (no forced `SetHeight`). Lazy load iframe when IntersectionObserver sees visible tab.

**Verify:** Open Spreadsheet tab → ribbon + grid without tab-switch workaround.

---

### 2026-06-19 — Save button not visible

**Symptom:** Officers looked for save on main XAF toolbar.

**Cause:** Save lives on **Save to template** in Panel toolbar above iframe (not main Save).

**Fix:** Moved toolbar outside iframe (`embed=true` hides inner toolbar). Document in `EXCEL_TEMPLATE_INPLACE_EDITOR.md` and officer guide.

---

### 2026-06-19 — HTTP iframe: ValueManager / DI

**Symptom:** `ValueManagerContext.Storage is null`; singleton injecting scoped `HttpAccess`.

**Fix:** `UserReportTemplateSpreadsheetHttpAccess` for iframe requests; `INonSecuredObjectSpaceFactory` in file service; resolve user key in page, not in singleton session service.

---

### 2026-06-19 — Startup middleware order

**Symptom:** `Cannot change ValueManagerType` when enabling Spreadsheet.

**Fix:** `app.UseDevExpressControls()` **after** `app.UseXaf()`.

---
