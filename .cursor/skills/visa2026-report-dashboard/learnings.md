# Report Dashboard — Learnings (append-only)

Date format: `YYYY-MM-DD`

---

## 2026-07-14 — Initial dashboard implementation

**What was built:**
- Full prototype with mock data for all 7 categories (Visa, Invitation, Registration, WorkPermit, Travel, BorderZone, Passport)
- Overview mode: card grid with conic-gradient donut + horizontal mini-bars per category
- Category detail mode: bar/pie/list chart, sub-report tabs, preview table
- Date range picker (6m–3y), project chips, person type tabs
- Full-page CSS overriding XAF chrome (no border-left, no border-bottom, 0 padding)

**Known patterns:**
- `Status` field in `ReportDashboardPreviewRow` drives ALL chart grouping — make sure mock and real data use human-readable labels, not codes
- `ReportDashboardPropertyEditor` must use a separate persistent `EF ObjectSpace` for DB queries — the DetailView`s `IObjectSpace` is a `NonPersistentObjectSpace` and throws when queried for EF entities
- UTF-8 (no BOM) is required for all `.cs` and `.razor` files — the `StrReplace` tool may produce UTF-16; use `[System.IO.File]::WriteAllText` with `New-Object System.Text.UTF8Encoding $false` when rewriting files
- `ComponentModelBase` is the correct base for the Blazor component model in XAF 25.2; `Disposable` is not the right base

**Pending:**
- All categories still on mock data — no SQL views created yet
- Real `ReportDashboardQueryService` methods exist but only the snapshot count logic is complete; `Load*` methods return mock-equivalent structure from EF objects