# carbone skill — Visa2026 notes (append-only)

Upstream: [carboneio/carbone-skill](https://github.com/carboneio/carbone-skill) v1.4.0 (installed under `.cursor/skills/carbone/`).

## Coexistence with production merge

- **Shipped user templates today** use **DocxTemplater** `{{ds.*}}` / `{{#ds.rows}}` — see **`visa2026-user-report-templates`** and **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`**. Do **not** rewrite those to `{d.…}` unless the project explicitly migrates to Carbone runtime.
- **Carbone skill** is for **syntax reference**, template design spikes, and **`carbone` branch** exploration — not Resminamalar officer UX.
- **Skill ≠ MCP**: this folder teaches tags; **`carbone-mcp`** (optional) calls the Carbone API to render/upload.
- **Integration plan:** [`docs/CARBONE_INTEGRATION_PLAN.md`](../../docs/CARBONE_INTEGRATION_PLAN.md) — phases, architecture, schema, deployment.

## Approved decisions (2026-06-18)

| Topic | Decision |
|-------|----------|
| Prod | **On-prem Carbone** only (no Cloud render for PII) |
| Excel | **Carbone** for Word **and** Excel (sunset ClosedXML after migration) |
| Photos | **Keep `WordUserReportImageInjector`** after Carbone DOCX merge |
| Dual-stack | **Temporary** — per-template `MergeEngine` until Phase 5b removes DocxTemplater/ClosedXML |
| Studio access | **VisaOffice** (+ admins with template Write) — not general officers |
| SQL `FileData` | **Snapshot on publish** — Carbone merges; SQL copy via **Sync from Carbone** (audit/backup only) |
