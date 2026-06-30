# VISA2015 → Visa2026 — on-prem IIS migration runbook

**Hosts:** legacy SQL **`10.100.128.15`** · Visa2026 IIS **`10.100.128.25`** (Prod / Staging / Demo)

**Canonical strategy:** [IMPORT_PLAN_AND_STRATEGY.md](./IMPORT_PLAN_AND_STRATEGY.md) · [import-strategy.yaml](../../Visa2026.DataImporter/legacy/visa2014/import-strategy.yaml)

**IIS slot layout:** [ON_PREM_WINDOWS_IIS.md](../ON_PREM_WINDOWS_IIS.md) · [visa2026-windows-iis-deploy](../../.cursor/skills/visa2026-windows-iis-deploy/SKILL.md)

**Import commands:** [import-practices.md](../../.cursor/skills/visa2014-to-visa2026-import/import-practices.md) · [order.yaml](../../Visa2026.DataImporter/legacy/visa2014/order.yaml)

---

## Summary

| Layer | Host | Role |
|-------|------|------|
| **Legacy (read-only)** | `10.100.128.15` | SQL Server · database **`VISA2015`** · user **`ReadOnlyUser`** |
| **Visa2026 Prod** | `10.100.128.25:80` | `Visa2026DbProd` — officer production after cutover |
| **Visa2026 Staging** | `10.100.128.25:8080` | `Visa2026DbStaging` — full-volume UAT before prod |
| **Visa2026 Demo** | `10.100.128.25:8081` | `Visa2026DbDemo` — training / familiarization |

**Write path:** `Visa2026.DataImporter --import-visa2014` → **OData only** (never direct SQL into Visa2026, never write to `VISA2015`).

**Do not** ship migration by copying LocalDB `.mdf` files from a developer machine.

---

## Parallel period (locked decision)

During the period when officers still work in the **legacy** system and use Visa2026 for **preview only**:

| Rule | Decision |
|------|----------|
| Officers write in legacy | **Yes** — `VISA2015` on `10.100.128.15` remains **system of record** |
| Officers write in Visa2026 | **No** — **view and search only** until cutover |
| Sync direction | **One-way:** legacy → Visa2026 only |
| Conflict policy | **Legacy wins** (safe because Visa2026 has no officer edits) |

**Enforcement (recommended):** assign officers a **read-only** XAF role on Prod/Staging during parallel period (Navigate + Read; deny Create/Write/Delete on business types). See [ROLE_PERMISSIONS_GUIDE.md](../ROLE_PERMISSIONS_GUIDE.md). Importer uses a separate service account with write access.

**Cutover:** stop legacy writes → run **final delta sync** → switch officers to full Prod role → disable scheduled sync jobs.

---

## Network and secrets

### Firewall

Import workstation (or scheduled-task host) must reach:

- `10.100.128.15:1433` (legacy SQL)
- `10.100.128.25:80`, `:8080`, `:8081` (Visa2026 OData)

### Secrets (never commit)

| Secret | Storage |
|--------|---------|
| `ReadOnlyUser` password | Windows **user** env `VISA2014_SQL_PASSWORD` on import machine |
| OData import user password | Windows user env or secure vault per slot |
| JWT / SQL on `.25` | `C:\visa2026\env\prod.env`, `staging.env`, `demo.env` on server |

Legacy connection override (prod source on LAN):

```powershell
$env:VISA2014_SQL_CONNECTION = "Server=10.100.128.15;Database=VISA2015;User Id=ReadOnlyUser;TrustServerCertificate=True;MultipleActiveResultSets=true"
$env:VISA2014_SQL_PASSWORD = [Environment]::GetEnvironmentVariable('VISA2014_SQL_PASSWORD','User')
```

---

## Id-map directories (per target slot)

Legacy GUID → Visa2026 OData `ID` maps are **per deployment**. Do not reuse LocalDB or staging maps on prod.

| Slot | Suggested folder (under `Visa2026.DataImporter/legacy/visa2014/`) |
|------|---------------------------------------------------------------------|
| Local dev | `id-maps/calik-energi/` (existing pilots) |
| Staging | `id-maps/calik-energi-onprem-staging/` |
| Production | `id-maps/calik-energi-onprem-prod/` |
| Demo | `id-maps/calik-energi-onprem-demo/` (or clone staging DB — see below) |

Pass explicit paths with `--id-map-output` and entity-specific flags (`--person-id-map`, `--application-id-map`, …). Keep maps and `import-logs/` **out of git** (PII).

Optional CLI profile: `--legacy-source calik-energi-onprem-staging` / `calik-energi-onprem-prod` in [`legacy-sources.yaml`](../../Visa2026.DataImporter/legacy/visa2014/legacy-sources.yaml).

---

## Phase 0 — Prerequisites (each slot)

1. IIS site running current `Visa2026.Blazor.Server` build ([ON_PREM_WINDOWS_IIS.md](../ON_PREM_WINDOWS_IIS.md)).
2. Target database exists (`Visa2026DbProd` / `Staging` / `Demo`); Module updaters have run once (lookups seeded).
3. **Empty** business data (or restore pre-migration empty backup).
4. Dedicated **OData import user** per slot (not officer accounts).
5. Officer users: **read-only** role during parallel period (Prod/Staging).
6. Backup target DB before first import.

---

## Phase 1 — Demo (`:8081`)

**Purpose:** Training and UI familiarization — not the authoritative migration test.

| Approach | When |
|----------|------|
| **A — Clone staging** | After staging UAT passes: backup `Visa2026DbStaging` → restore to `Visa2026DbDemo` |
| **B — Subset import** | `--max-rows` on selected entities for a small sandbox |

Do **not** run continuous scheduled sync into Demo from live legacy unless you accept frequent overwrites.

---

## Phase 2 — Staging (`:8080`) — required before prod

### 2.1 Pre-flight

- [ ] Per-entity `importConfirmed: true` in [`order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml)
- [ ] Excel preview reviewed for critical entities
- [ ] Staging backup taken
- [ ] `VISA2014_SQL_CONNECTION` points at `10.100.128.15`

### 2.2 Initial full import

Run **one entity at a time** in `order.yaml` order from the import workstation:

```powershell
$repo = "C:\path\to\Visa2026"
$mapRoot = "$repo\Visa2026.DataImporter\legacy\visa2014\id-maps\calik-energi-onprem-staging"
$base = "http://10.100.128.25:8080"
$user = "<staging-import-user>"
$pass = [Environment]::GetEnvironmentVariable('VISA2026_STAGING_IMPORT_PASSWORD','User')

$common = @(
  "--import-visa2014",
  "--legacy-source", "calik-energi-onprem-staging",
  "--api-base-url", $base,
  "--user", $user,
  "--password", $pass,
  "--no-wait"
)

Set-Location $repo
dotnet run --project Visa2026.DataImporter -c Debug -- @common --entity Person `
  --id-map-output "$mapRoot\Person.json"

# Continue per order.yaml — pass parent id-map flags for child entities, e.g. ApplicationItem:
dotnet run --project Visa2026.DataImporter -c Debug -- @common --entity ApplicationItem `
  --id-map-output "$mapRoot\ApplicationItem.json" `
  --person-id-map "$mapRoot\Person.json" `
  --application-id-map "$mapRoot\Application.json" `
  --passport-id-map "$mapRoot\Passport.json"
```

Log to `import-logs/staging-<Entity>-<date>.log` (create directory first).

### 2.3 Reconciliation

After each wave ([IMPORT_PLAN_AND_STRATEGY.md](./IMPORT_PLAN_AND_STRATEGY.md) §7):

- Legacy row counts vs OData `$count`
- Spot-check 5–10 records via id-map
- Officer **read-only** UAT: search, lists, document preview

### 2.4 Sign-off gate

- [ ] Reconciliation signed
- [ ] Rollback `.bak` retained until prod cutover completes

---

## Phase 3 — Production (`:80`)

**Only after staging sign-off.**

Same procedure as staging with:

- `--api-base-url http://10.100.128.25` (port 80)
- `--legacy-source calik-energi-onprem-prod`
- `id-maps/calik-energi-onprem-prod/`
- Officers remain **read-only** until cutover day

### Cutover day

1. Announce legacy **read-only** (or stop legacy app).
2. Run **final sync** pass (all entities + attachments).
3. Reconcile counts; smoke-test critical flows.
4. Promote officers to full write role on Prod.
5. Stop scheduled sync tasks.
6. Archive id-maps and logs securely.

### Rollback

- Restore `Visa2026DbProd` from pre-import backup.
- **Never** write to `VISA2015` on `10.100.128.15`.

---

## Scheduled one-way sync (parallel period)

**Status:** **Planned** — tooling today supports **initial load** and **new-row catch-up** (id-map skip) on some entities; **full delta sync** (PATCH updated legacy rows) requires a future `--sync-visa2014` / `--sync-since` implementation.

Because officers **do not write** in Visa2026 during parallel period, **legacy-wins** scheduled sync is **safe** and **feasible** once delta upsert is implemented.

### Recommended schedule

| Slot | Schedule | Notes |
|------|----------|-------|
| **Staging** | Nightly (e.g. 02:00) | Keeps UAT data fresh for read-only officer preview |
| **Production** | Nightly off-peak | After initial prod load; before cutover only |
| **Demo** | Manual / weekly clone from staging | Avoid live sync |

### Planned sync job shape

```powershell
# Future — not implemented yet
# dotnet run --project Visa2026.DataImporter -- `
#   --sync-visa2014 --legacy-source calik-energi-onprem-prod `
#   --api-base-url http://10.100.128.25 `
#   --sync-state-dir C:\visa2026-import\sync-state\prod `
#   --user ... --password ...
```

Until `--sync-visa2014` exists, **interim catch-up:**

- Re-run entity imports in `order.yaml` order — entities with id-map skip import **new** legacy rows only (`Application`, `ApplicationProgress`, `ApplicationItem`).
- **Does not** update changed fields on rows already imported.
- **Person** re-run may duplicate without upsert — avoid repeating full Person import on prod.

Use Windows **Task Scheduler** on the import workstation; alert on non-zero exit.

### Sync implementation backlog

| Feature | Purpose |
|---------|---------|
| `--sync-since <timestamp>` | Legacy rows changed after last successful run |
| Upsert (PATCH if in id-map) | Propagate edits made in legacy |
| Per-slot watermark file | `sync-state/<slot>.json` |
| Legacy soft-delete | `GCRecord` → target archive policy |
| Attachments wave | Less frequent schedule (nightly scalars, weekly files) |

---

## Entity order reference

Follow [`order.yaml`](../../Visa2026.DataImporter/legacy/visa2014/order.yaml) strictly. Attachments / file blobs **last**.

---

## Revision log

| Date | Change |
|------|--------|
| 2026-06-21 | Initial runbook — `.25` IIS slots, `.15` legacy SQL, read-only Visa2026 parallel period, planned nightly sync |
