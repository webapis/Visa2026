---
name: visa2026-excel-inplace-editor
description: >-
  In-browser DevExpress ASP.NET Core Spreadsheet editor on UserReportTemplate DetailView
  (Spreadsheet tab): iframe Razor host, Save to template / Reload from database, lazy load,
  embed layout, HTTP auth for iframe requests, session document generations. Use for blank
  grid, missing ribbon, reload not showing content, save not persisting, npm/Startup/middleware
  issues — not repo seed .xlsx under Resources/Templates/Excel (visa2026-user-report-templates),
  not Resminamalar ZIP/preview (visa2026-resminamalar), not ClosedXML merge logic bugs at
  generation time unless caused by corrupt saved blob. Read learnings.md first; append after
  verified fixes.
disable-model-invocation: false
---

# Visa2026 — Excel in-place editor (User Report Template)

## Agent workflow (every task)

1. **Read** [learnings.md](./learnings.md) (**## Entries**, newest first) and **Scenarios** below.
2. **Classify** — Spreadsheet tab / iframe / save-reload (**this skill**) vs seed maps/embed in repo (**[user-report-templates](../visa2026-user-report-templates/SKILL.md)**) vs Resminamalar dialog (**[resminamalar](../visa2026-resminamalar/SKILL.md)**) vs role denied (**[security-access](../visa2026-security-access/SKILL.md)**).
3. **Fix** with minimal diff; host wiring stays in **Blazor.Server**, BO host property in **Module** (see [reference.md](./reference.md)).
4. **Verify** — `dotnet build Visa2026.slnx -c Debug`; `npm ci` in `Visa2026.Blazor.Server` if assets missing; manual: Spreadsheet tab → grid + ribbon → edit → **Save to template** → General **Extract**.
5. **Record** — append [learnings.md](./learnings.md) after verified fix ([MATURITY.md](./MATURITY.md)).

## Canonical doc

**[`docs/EXCEL_TEMPLATE_INPLACE_EDITOR.md`](../../../docs/EXCEL_TEMPLATE_INPLACE_EDITOR.md)** — officer workflow, prerequisites, limitations, QA checklist.

**Related skills (do not duplicate):**

| Topic | Skill |
|-------|--------|
| Seed `.xlsx` in `Resources/Templates/Excel/`, maps, `EnsureExcelTemplateExists`, never edit layout in repo | [visa2026-user-report-templates](../visa2026-user-report-templates/SKILL.md) |
| Resminamalar catalog, ZIP batch, preview | [visa2026-resminamalar](../visa2026-resminamalar/SKILL.md) |
| `UserReportTemplate` / `FileData` write denied | [visa2026-security-access](../visa2026-security-access/SKILL.md) |
| Docker / deploy / `node_modules` in image | [visa2026-lifecycle-docker](../visa2026-lifecycle-docker/SKILL.md) |

**Long reference:** [reference.md](./reference.md) · **Experience log:** [learnings.md](./learnings.md) · **Maturity:** [MATURITY.md](./MATURITY.md)

---

## Scenarios (check first)

| Symptom | First step | Likely owner |
|---------|------------|--------------|
| Ribbon visible, **grid blank** / white area below ribbon | Embed CSS (`100%` not `100vh`); JS `urt-spreadsheet-resize`; lazy load when tab visible — [learnings.md](./learnings.md) | **This skill** |
| **Reload from database** does nothing / still blank | Parent toolbar calls `reloadSpreadsheetIframe`; URL has `reload=true&embed=true`; no Blazor `src="about:blank"` reset on re-render | **This skill** |
| Officer expects top **Save** to save cells | Main XAF Save does **not** persist spreadsheet — **Save to template** above grid | **This skill** (UX/doc) |
| Spreadsheet tab missing | `Model.xafml` `ExcelSpreadsheetHost`; `[VisibleInDetailView]` on host property; Excel output format | **This skill** |
| `ValueManagerContext.Storage is null` in iframe | Use `UserReportTemplateSpreadsheetHttpAccess` + non-secured OS — not `SecuritySystem` on HTTP request | **This skill** |
| `Cannot change ValueManagerType` at startup | `UseDevExpressControls()` **after** `UseXaf()` | **This skill** |
| Ribbon only, no npm / 404 on `dx.all.js` | `npm ci` in `Visa2026.Blazor.Server`; `Startup` static files for `/node_modules` | **This skill** |
| Saved file wrong at Resminamalar but editor OK | ClosedXML merge / placeholders — after **Extract** on General | **user-report-templates** |
| Read-only, no Save button | `UserReportTemplateEditAccess` / role write on `UserReportTemplate` | **security-access** |

---

## Scope

| In scope | Out of scope |
|----------|----------------|
| `UserReportTemplateSpreadsheet.cshtml` host + `DxSpreadsheetRequest` / Save POST | Authoring new seed templates in git |
| `UserReportTemplateExcelSpreadsheetPropertyEditor` + Panel iframe | Resminamalar dialog UX |
| Toolbar **Save to template** / **Reload from database** | Word templates |
| `UserReportTemplateSpreadsheetFileService` load/save `FileData` | Changing `ExcelReportGenerator` merge rules without blob evidence |
| Lazy load, resize postMessage, unsaved close guard | EasyTest for iframe (deferred v1) |
| `ExcelSpreadsheetRoundTripTests` (DevExpress SaveCopy → ClosedXML extract) | XFA PDF mapping |

---

## Officer reminder (one line)

**General** tab: upload + Extract/Validate · **Spreadsheet** tab: edit cells → **Save to template** (not top Save) → back to General → **Extract** if tokens changed.

---

## Build / assets

```powershell
# From repo root
dotnet build Visa2026.slnx -c Debug

# Spreadsheet npm assets (host)
cd Visa2026.Blazor.Server
npm ci
```

Round-trip test:

```powershell
dotnet test Visa2026.Module.Tests/Visa2026.Module.Tests.csproj -c Debug --filter ExcelSpreadsheetRoundTrip
```
