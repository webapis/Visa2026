# VISA2014 → Visa2026 — import best practices

Applies when **loading** data into Visa2026 (Phase 3+). **Extract** always from **`VISA2015`** (not inferred from VISA2014 repo).

**Prerequisite:** [IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md) **`approved`** in `import-strategy.yaml`. Phase 1 discovery **`complete`** and Phase **1b** **`importConfirmed: true`** per entity — see [VISA2014_MIGRATION.md § Import confirmation gate](../../../docs/VISA2014_MIGRATION.md). Do not implement import code or POST until gates pass.

**Experience:** read [learnings.md](./learnings.md) before work; append after verified sessions ([MATURITY.md](./MATURITY.md)).

Discovery/mapping rules live in [SKILL.md](./SKILL.md) and [VISA2014_MIGRATION.md](../../../docs/VISA2014_MIGRATION.md).

**Reuse target patterns from:** [visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) · [IMPORTING.md](../../../Visa2026.DataImporter/IMPORTING.md) · `Visa2026.DataImporter/ApiClient.cs` · `BaseImporter.cs`

---

## Principles

| # | Practice | Why |
|---|----------|-----|
| 0 | **Strategy approved** | `import-strategy.yaml` before any import **implementation** |
| 0b | **Confirm before implement** | Excel preview reviewed; `importConfirmed: true` per BO; read/append **learnings.md** each session |
| 1 | **OData only** into Visa2026 | XAF validation, security, audit — same as seed import |
| 2 | **Never write** to `VISA2015` | Legacy is read-only source |
| 3 | **Dependency order** from `order.yaml` | FKs and id-map must exist before children |
| 4 | **Three-layer mapping** at transform time | Table → column → lookup value before POST |
| 5 | **Idempotent runs** | Re-run safe via natural-key upsert + id-map |
| 6 | **Pilot → reconcile → expand** | One **confirmed** BO on disposable DB before full prod cutover |
| 7 | **Log skips and failures** | `skip_row`, unmapped lookups, OData 400 — no silent drops |

---

## Before first POST (target environment)

Run in order:

0. **`import-strategy.yaml`** → `status: approved` ([IMPORT_PLAN_AND_STRATEGY.md](../../../docs/VISA2014_MIGRATION/IMPORT_PLAN_AND_STRATEGY.md)).
1. **`importConfirmed: true`** — dossier + `order.yaml` for every entity in this batch ([SKILL.md § Phase 1b](./SKILL.md)); **Excel preview** reviewed ([EXCEL_PREVIEW_EXPORT.md](../../../docs/VISA2014_MIGRATION/EXCEL_PREVIEW_EXPORT.md)).
2. **Target DB** — `Visa2026DbDev` or empty disposable DB; **not** production on first pass.
3. **Start `Visa2026.Blazor.Server`** once so Module updaters seed lookups, ApplicationType, org singletons ([LOOKUP_SEEDING.md](../../../docs/LOOKUP_SEEDING.md)).
4. **Wait for server** — importer polls until `https://localhost:5001` responds (see `ApiClient.WaitForServerAsync`).
5. **Verify prerequisite lookups** — Country, Department, Position, ApplicationType, etc. exist via OData GET (DataImporter Phase 2 pattern). **Abort** if critical catalogs empty.
6. **Org singletons** — `CompanyProfile`, default `ProjectContract` if BO validation requires them (IMPORTING.md Phase 3).
7. **OData exposure** — every imported BO registered in `WebApiServiceExtensions.cs`.
8. **Lookup translations complete** — layer 3 mapped for every `lookupCatalog` used in current batch.
9. **ApplicationType visibility** — if importing `Application` / `ApplicationItem`, catalog `Show*` flags should match Module ([visa2026-dataimporter](../visa2026-dataimporter/SKILL.md) visibility preflight). Fix catalog or DB before bulk import; avoid `--skip-visibility-preflight` except debug.

---

## Extract → transform → load (ETL)

```text
VISA2015 (read SQL)
  → dedupe by field-map deduplication.keys
  → apply column transforms + lookup-translations.yaml
  → resolve FKs via id-map/ (legacy GUID → new OData ID)
  → upsert OData (POST or PATCH)
  → append id-map for new rows
  → reconcile counts
```

| Step | Best practice |
|------|----------------|
| Extract | Parameterized SQL; filter soft-delete (`GCRecord`, `rowFilter`) |
| Dedupe | One canonical row per business key **before** POST |
| Transform | Layer 3 **before** OData lookup query — never `$filter` with raw legacy string on target |
| Load | One entity batch per `order.yaml` row; children after parents |
| Map IDs | Write `id-map/{Entity}-{legacyGuid}.json` on every successful create |

---

## Upsert and idempotency

Mirror [IMPORTING.md](../../../Visa2026.DataImporter/IMPORTING.md) scenario idempotency and [Excelmappings.cs](../../../Visa2026.DataImporter/Excelmappings.cs) upsert keys:

1. Define **natural keys** in field-map (`upsertKey: true` on stable columns — e.g. `PassportNumber`, `VisaNumber`).
2. **Before POST:** OData `$filter` on upsert key(s); if found → **PATCH** (or skip if unchanged); if not → POST.
3. **Never** reuse VISA2014 GUIDs as Visa2026 `ID` unless explicitly verified.
4. **Duplicate legacy rows** → one OData row; all legacy IDs in group → same `id-map` target.
5. **Re-run policy:** second full import should not duplicate business keys — upsert must be deterministic.

**PATCH vs POST:**

| Situation | Prefer |
|-----------|--------|
| First prod migration into empty DB | POST + id-map |
| Re-run / fix mapping / dev refresh | Upsert (GET filter → PATCH or POST) |
| Changed many child rows | Clear target scope or disposable DB — avoid partial PATCH mess (see `--clear-scenario` pattern in dataimporter skill) |

---

## Lookup resolution

1. Read legacy column value.
2. Translate with `lookup-translations.yaml` (`legacy` → `target`).
3. Resolve target row: OData GET on catalog with `$filter={targetMatchProperty} eq '...'`.
4. **Cache** lookups in memory per run (`BaseImporter` `_lookupCache` pattern) — one GET per distinct translated value.
5. **Unmapped legacy value:** honor `unmappedPolicy` (`block_row` for required FKs — do not POST null and hope).

**Do not** POST new global lookup rows during prod migration unless explicitly planned — use existing Module catalogs + translation table.

---

## Batching, logging, and errors

| Practice | Detail |
|----------|--------|
| **Batch by entity** | Finish Person batch + reconcile before Application |
| **Counts** | Log `success`, `failed`, `skipped`, `dedupeMerged` per entity |
| **Verbose pilot** | `--verbose` or equivalent on first BO — log payloads |
| **OData 400** | Stop batch; fix mapping or Web API exposure — do not blindly continue |
| **Partial failure** | Record last successful legacy key; document in `learnings.md` |
| **No silent catch** | Every `skip_row` and `block_row` increments a counter in import summary |

---

## Reconciliation (after each batch)

| Check | How |
|-------|-----|
| Row count | Legacy distinct keys (after dedupe) vs OData `$count` or SQL MCP on target |
| Spot records | 5–10 random legacy IDs via id-map — field parity |
| Lookup coverage | Zero unmapped legacy values in batch log |
| Duplicate groups | `dedupeMerged` matches discovery SQL duplicate count |
| FK integrity | Sample child rows — parent resolves via id-map |

---

## Safety and cutover

- **PII:** treat VISA2014 backup and import logs as sensitive.
- **Prod cutover:** only after pilot + UAT on staging clone; runbook with rollback (restore target DB snapshot).
- **Attachments last** — after owning BO and id-map exist.
- **Service account:** use dedicated import user with officer/admin role — not ad-hoc sa on OData.
- **Freeze window:** avoid officers editing same BOs during cutover import.

---

## Anti-patterns (do not)

- Direct `INSERT` into Visa2026 SQL bypassing XAF
- String-match legacy lookup to target without layer 3
- Import ApplicationItem before Person id-map populated
- Parallel OData POST across entities with FK dependencies
- Invent ministry ApplicationType / catalog rows at import time
- `--skip-visibility-preflight` on production migration without sign-off
- Import entire prod legacy in one run without pilot reconciliation
- Commit `id-map/` with prod GUIDs to git

---

## Commands (current + planned)

**Today (target seed import — same OData stack):**

```powershell
# Start host (lookups + updaters)
dotnet run --project Visa2026.Blazor.Server

# Reference importer (scenarios — not VISA2014 yet)
dotnet run --project Visa2026.DataImporter
# or
.\scripts\local\Run-DataImporter.ps1
```

**Planned (VISA2014 legacy):**

```powershell
dotnet run --project Visa2026.DataImporter -- --import-visa2014 --entity Person --verbose
dotnet run --project Visa2026.DataImporter -- --import-visa2014 --dry-run --entity Person
```

Until `--import-visa2014` exists, pilot via documented manual OData POST following sections above.

---

## Related

- [SKILL.md](./SKILL.md) — phases, discovery, mapping
- [reference.md](./reference.md) — SQL, paths, pick-next-BO
- [learnings.md](./learnings.md) — verified fixes
