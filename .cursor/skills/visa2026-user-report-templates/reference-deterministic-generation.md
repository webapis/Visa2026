# Deterministic user report generation

**Principle:** Same **scan** + same **approved map** + same **template file** + same **application data** ⇒ same **output document**.

Maps are the contract; templates and code must not drift from them.

---

## What “deterministic” means here

| Input (fixed) | Output (stable) |
|---------------|-----------------|
| `Application` + active `ApplicationItems` (DB) | Item set and order from `GetActiveApplicationItems` (sorted by `ApplicationItemName`) |
| `UserReportTemplate` file bytes | Extracted placeholder list |
| Approved `*_map.md` §6 tokens | Bind keys passed to DocxTemplater / ClosedXML |
| Map §2 family + §7 loop tokens | `rows` vs `ApplicationItems` vs scalar |
| Golden values (§12) | Preview / QA only — production uses live BO data |

**Not deterministic today (document in map Notes):** manual signature cells, ad-hoc Word edits without re-Extract, Release DB stale template bytes, §8 fields marked `_manual_`.

---

## Agent enforcement

1. **One map format** — All `*_map.md` files use **`docs/USER_REPORT_MAP_STANDARD.md`** section order (§0–§15). No custom sections.
2. **Exact tokens** — §6 **Placeholder token** column is copy-paste authoritative; Extract in app must find every §7 token.
3. **No improvisation** — If §6 lists `{{ds.rows.Person_FullName}}`, do not suggest `{{ds.Person_FullName}}` or `{{.Person_FullName}}` unless §7 allows dot form inside `rows` loop (equivalent — pick one per map and stick to it).
4. **Golden values** — §12 is the only source for preview preset literals; transcribe from scan once.
5. **Version bump** — Any §6/§7 change ⇒ increment **Map version** in §0 ⇒ user updates `.docx`/`.xlsx` ⇒ **Extract + Validate**.
6. **Status gate** — `Draft` → user **Approved** → Word/Excel authoring → seed `Implemented`.

---

## Word families → deterministic binding

| Family | §2 collection | §7 required | Generator path |
|--------|---------------|-------------|----------------|
| `AppScalar` | `none` | no `#` loops | `data[scalar]` from `Application` |
| `ItemRows` | `rows` | `#ds.rows` … `/ds.rows` | `PopulateSyntheticRowsIfNeeded` → row dicts |
| `ItemRoster` | `ApplicationItems` | `#ds.ApplicationItems` | `ApplicationItemPhotoMergeRow` or dict rows |
| `ItemScalar` | `none` | no loops | `GenerateAsync(template, applicationItem)` only |

Resminamalar zip: always `Application` merge root except future per-item zip entries.

---

## Excel (deterministic)

| Field | Rule |
|-------|------|
| §10 `ExcelMergeMode` | `ItemList` for production Resminamalar |
| Header | Fixed cells `{{ds.*}}` on `Application` |
| Data row | Exactly one `{{#ds.rows}}` row; `{{.Column}}` per §6 |
| Golden row | §12 one row transcribed from scan |

---

## Verification checklist (§11)

Run in order after template change:

1. Map §6 row count ≈ Extract placeholder count (minus static §7 control tokens).
2. Validate — all green for declared root BO.
3. Preview with §12 golden data (Word preset / Excel spike).
4. Resminamalar on test application — compare to scan layout.
5. Second run on same app — byte-identical output (or document known float fields: dates, empty photo).

---

## Related

- **`docs/USER_REPORT_MAP_STANDARD.md`** — mandatory sections
- **`reference-map-contract.md`** — scan → map → approve workflow
- **`reference-template-families.md`** — layout IDs
