# Carbone tag migration — Phase 0 spike templates

Work on **copies** under `tools/CarboneSpike/templates/spike/` (create locally). Do **not** change seeded ministry files in `Visa2026.Module/Resources/Templates/` until migration is approved.

## Pilot templates

| Spike scenario | Source seed | Focus |
|----------------|-------------|--------|
| `gurlusyk` | `Excel/433_gurlusyk_uzt.xlsx` | `{d.rows[i].…}` loop + header `{d.FullApplicationNumber}` |
| `sanaw` | `Sanaw_uzt.docx` | Table loop (14 columns) |
| `forma16` | `Forma_16.docx` | Loop + **`{{IMAGE:Person_Photo}}`** (injector after Carbone) |

## Syntax cheat sheet

| DocxTemplater (today) | Carbone (spike copy) |
|-----------------------|----------------------|
| `{{ds.FullApplicationNumber}}` | `{d.FullApplicationNumber}` |
| `{{#ds.rows}}` … `{{/ds.rows}}` | Two-row loop pattern — see [Carbone skill](../../.cursor/skills/carbone/SKILL.md) § loops |
| `{{.Person_LastName}}` inside loop | `{d.rows[i].Person_LastName}` (Excel) or loop row alias |
| `{{IMAGE:Person_Photo}}` | **Keep unchanged** — post-merge injector |

## Excel (`433_gurlusyk_uzt.xlsx`)

1. Copy file to `templates/spike/433_gurlusyk_uzt.carbone.xlsx` — or run `dotnet run --project tools/CarboneSpike -- retag-gurlusyk`.
2. Replace header cells `{{ds.*}}` → `{d.*}` (automated by `retag-gurlusyk`).
3. On the data row (where `{{#ds.rows}}` sits), use Carbone repetition for columns (see Carbone xlsx docs / skill `references/xlsx-tips.md`).
4. Export JSON: `export-json --scenario gurlusyk --items 18`.

## Word (`Sanaw_uzt.docx`)

1. Copy to `templates/spike/Sanaw_uzt.carbone.docx`.
2. Retag header/footer scalars to `{d.…}`.
3. Retag table loop row to Carbone loop syntax.
4. Export JSON: `export-json --scenario sanaw --items 3`.

## Word with photo (`Forma_16.docx`)

1. Copy to `templates/spike/Forma_16.carbone.docx`.
2. Retag text fields to `{d.…}` / loop rows.
3. **Do not** replace `{{IMAGE:Person_Photo}}` with Carbone image formatters.
4. After Carbone render → `inject-word --in …`.

## JSON shape

`export-json` writes:

```json
{
  "FullApplicationNumber": "3/-433",
  "rows": [ { "RowNumber": 1, "Person_LastName": "…" } ]
}
```

`export-json` writes this shape for **Carbone Studio** (`{d.*}` in templates = root fields). Use `--wrap-d` only if you need legacy `{"d":{...}}` nesting.
