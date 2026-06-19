# Carbone Phase 0 spike tooling

Prove Carbone can replace DocxTemplater/ClosedXML for **one Excel + one Word** ministry template before Phase 1 schema work.

Canonical plan: [`docs/CARBONE_INTEGRATION_PLAN.md`](../../docs/CARBONE_INTEGRATION_PLAN.md).

## Prerequisites

- .NET 8 SDK
- Ministry seed templates under `Visa2026.Module/Resources/Templates/`
- Carbone access for manual render:
  - **Dev:** [Carbone Cloud](https://account.carbone.io) or local Docker (see below)
  - **Prod path:** on-prem only (decided) — spike may use Cloud **without real PII**; sample JSON uses fake names

## Quick start

From repo root:

```powershell
dotnet run --project tools/CarboneSpike -- paths

dotnet run --project tools/CarboneSpike -- export-json --scenario gurlusyk --items 3
dotnet run --project tools/CarboneSpike -- export-json --scenario gurlusyk --items 3 --sample-rows
dotnet run --project tools/CarboneSpike -- export-json --scenario sanaw --items 3

dotnet run --project tools/CarboneSpike -- baseline-excel --items 3
dotnet run --project tools/CarboneSpike -- baseline-word --scenario sanaw --items 3
dotnet run --project tools/CarboneSpike -- baseline-word --scenario forma16 --items 1
```

Outputs go to **`tools/CarboneSpike/output/`** (gitignored except `.gitkeep`).

## Phase 0 workflow

### 1. Legacy baseline (today’s merge)

| Command | Produces |
|---------|----------|
| `baseline-excel` | ClosedXML-filled `433_gurlusyk_uzt.xlsx` |
| `baseline-word --scenario sanaw` | DocxTemplater-filled `Sanaw_uzt.docx` |
| `baseline-word --scenario forma16` | DocxTemplater + **image injector** on `Forma_16.docx` |

Open in Word/Excel or convert to PDF for comparison.

### 2. Carbone-tagged template copies

```powershell
dotnet run --project tools/CarboneSpike -- retag-gurlusyk
```

Produces **`tools/CarboneSpike/templates/spike/433_gurlusyk_uzt.carbone.xlsx`** — upload this in Studio.

Or copy manually and retag per migration doc:

- DocxTemplater `{{ds.Field}}` → Carbone `{d.Field}`
- Loop rows: see [`templates/CARBONE_TAG_MIGRATION.md`](templates/CARBONE_TAG_MIGRATION.md)
- Keep `{{IMAGE:Person_Photo}}` literals for post-merge injector (decided)

Upload the Carbone copy to **Carbone Studio** (VisaOffice accounts only).

### 3. Export JSON

```powershell
dotnet run --project tools/CarboneSpike -- export-json --scenario gurlusyk --items 18 --sample-rows
```

Use **`--sample-rows`** for Carbone Studio preview (fake person names in table columns). JSON is **unwrapped** at root (`FullApplicationNumber`, `rows`, …) — Carbone `{d.Field}` maps to those keys. Do **not** wrap in `{"d":…}` unless testing with `--wrap-d`.

Paste JSON into Studio → preview → export DOCX/XLSX/PDF.

### 4. Word photos after Carbone

If Carbone output is DOCX and template still has `{{IMAGE:Person_Photo}}`:

```powershell
dotnet run --project tools/CarboneSpike -- inject-word --in tools/CarboneSpike/output/carbone-forma16.docx
```

### 5. Pass/fail

| Check | Pass |
|-------|------|
| Excel row count | Same as legacy baseline |
| Sanaw table columns | Text matches sample JSON |
| Forma 16 photo | Injector replaces token (tiny PNG in spike) |
| Side-by-side PDF | Acceptable layout vs legacy |

Record results in [`.cursor/skills/carbone/learnings.md`](../../.cursor/skills/carbone/learnings.md).

## Optional: local Carbone Docker

```powershell
docker compose -f docker-compose.carbone-spike.yml up -d
```

Open **http://127.0.0.1:4000** — you should get **Carbone Studio** (template upload, JSON preview, render). If you only see `{"success":true,...}`, Studio is off: set `CARBONE_EE_STUDIO=true` in compose and recreate the container.

Templates persist under `tools/CarboneSpike/carbone-data/template/`. Requires Carbone EE license in `CARBONE_EE_LICENSE` (user env or `.env.dev`).

## Commands reference

```
export-json [--scenario gurlusyk|sanaw|forma16] [--items N] [--sample-rows] [--out path]
baseline-excel [--items N] [--template path]
baseline-word [--scenario sanaw|forma16] [--items N] [--template path]
inject-word --in path [--out path]
paths
```
