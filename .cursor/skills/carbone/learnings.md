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

## Phase 0 tooling (2026-06-18)

- **`tools/CarboneSpike/`** — console CLI: `export-json`, `baseline-excel`, `baseline-word`, `inject-word`, `paths`.
- **`docker-compose.carbone-spike.yml`** — optional local Carbone EE container (`127.0.0.1:4000`).
- Row JSON uses production **`UserReportMergeDataHelper`** builders (Forma_16, Sanaw, Gurlusyk Excel list).
- **`export-json`** writes **unwrapped** JSON for Studio (`rows` at root). Carbone `{d.X}` = root field `X`, not a literal `"d"` JSON key. Use `--wrap-d` only for nested tests.
- **Studio empty preview (2026-06-18):** XLSX **merge works** but **Preview PDF** is blank when Chrome converter is used — Docker log: `XLSX document cannot be converted to PDF format with Chrome converter`. Use **HTML smoke**, `{o.converter=L}` in template (Z1), or download **XLSX** output. Also fixed: unwrapped JSON + tab-prefixed tags in cells.
- **Manual next step:** Carbone-tagged template copies → Studio render → compare to `tools/CarboneSpike/output/baseline-legacy-*`.
- Phase 0 **exit** not recorded yet — pending side-by-side Carbone vs legacy sign-off.
