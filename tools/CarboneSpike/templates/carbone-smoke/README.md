# Carbone Studio smoke test (not Visa2026)

Use these **before** debugging ministry templates.

## Generate

```powershell
dotnet run --project tools/CarboneSpike -- create-smoke-sample
```

## Why PDF preview was blank (XLSX)

Docker logs show:

`The XLSX document cannot be converted to PDF format with Chrome converter`

Studio **Preview PDF** defaults to the **Chrome** engine. **Excel → PDF** needs **LibreOffice** (`{o.converter=L}`). Merge still works; only PDF preview fails.

**Fixes (pick one):**

| Approach | Steps |
|----------|--------|
| **HTML smoke (easiest)** | Upload `carbone-smoke-minimal.html` + paste `carbone-smoke-minimal.json` → PDF preview works |
| **XLSX + LibreOffice PDF** | Upload `carbone-smoke-minimal.xlsx` (has `{o.converter=L}` in **Z1**) + same JSON |
| **XLSX output only** | Upload xlsx + JSON → use toolbar **download** as `.xlsx`, not PDF preview |

API proof (merge works): `tools/CarboneSpike/output/smoke-xlsx-out.xlsx` from local render tests.

## Test A — HTML (recommended first)

| Upload | Data JSON |
|--------|-----------|
| `carbone-smoke-minimal.html` | `carbone-smoke-minimal.json` |

**Pass:** PDF shows “Carbone smoke OK” + subtitle.

## Test B — Excel loop

| Upload | Data JSON |
|--------|-----------|
| `carbone-smoke-loop.xlsx` | `carbone-smoke-loop.json` |

**Pass:** Title + 3 rows (Alice, Bob, Carol). Use XLSX download if PDF still blank.

## JSON rules

- Paste **unwrapped** JSON at root (`title`, `rows`, …) — no `{"d":{...}}` wrapper.
- Template `{d.title}` = root field `title`.

## Gurlusyk after smoke passes

```powershell
dotnet run --project tools/CarboneSpike -- retag-gurlusyk
dotnet run --project tools/CarboneSpike -- export-json --scenario gurlusyk --items 3 --sample-rows
```

Upload `templates/spike/433_gurlusyk_uzt.carbone.xlsx` — includes `{o.converter=L}` for PDF preview.
