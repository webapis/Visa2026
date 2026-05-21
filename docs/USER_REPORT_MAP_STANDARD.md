# User report map standard (`*_map.md`)

Every user report under **`Visa2026.Module/Resources/Templates/`** (Word) or **`Templates/Excel/`** (Excel) must have **`<Basename>_map.md`** that follows this document **exactly** — same section order, same tables, same level of detail.

**Goal:** Same scan + same map + same template tokens → **same merge output** (deterministic Resminamalar / preview).

Copy **`Resources/Templates/_map_TEMPLATE.md`** when creating a new map. Agents use **`.cursor/skills/visa2026-user-report-templates/reference-map-contract.md`** for workflow gates.

---

## Co-located trio (same basename)

| File | Example |
|------|---------|
| Scan | `Forma_16.png` |
| Map | `Forma_16_map.md` |
| Template | `Forma_16.docx` or `Excel/433_gurlusyk_uzt.xlsx` |

---

## Mandatory sections (fixed order)

| § | Section | Word | Excel |
|---|---------|:----:|:-----:|
| 0 | Document header (status, version, files) | ✓ | ✓ |
| 1 | Identity | ✓ | ✓ |
| 2 | Determinism specification | ✓ | ✓ |
| 3 | Reference image(s) | ✓ | ✓ |
| 4 | Output specification (Resminamalar) | ✓ | ✓ |
| 5 | Page / sheet layout | ✓ | ✓ |
| 6 | Placeholder contract (master table) | ✓ | ✓ |
| 7 | Loop and control tokens | ✓ | ✓ |
| 8 | Static text inventory | ✓ | ✓ |
| 9 | Photos / images | ✓ | N/A text |
| 10 | Excel merge (or N/A) | N/A | ✓ |
| 11 | Deterministic verification | ✓ | ✓ |
| 12 | Golden sample values (from scan) | ✓ | ✓ |
| 13 | Cross-check | ✓ | ✓ |
| 14 | Waiver | ✓ | ✓ |
| 15 | Changelog | ✓ | ✓ |

Do not omit a section — write **N/A — Word-only** (or Excel-only) where not applicable.

---

## Determinism rules (summary)

1. **Exact tokens** — Placeholder contract lists strings **copy-paste ready** (e.g. `{{ds.rows.Person_FullName}}`), not descriptions only.
2. **Template family** — One of `AppScalar` | `ItemRows` | `ItemRoster` | `ItemScalar` (see skill `reference-template-families.md`).
3. **Loop markers** — §7 lists every `{{#…}}`, `{{/…}}`, `{{:PageBreak}}`, `{{IMAGE:…}}` exactly as in Word/Excel.
4. **Item set** — “All active non-deleted `ApplicationItems` from DB query” (default Resminamalar) unless map states otherwise.
5. **Golden values** — §12 transcribes scan once; preview presets and QA use **only** these values (no ad-hoc filler).
6. **Map version** — Bump **Map version** in §0 when contract changes; re-Extract/Validate template after token edits.
7. **Approved gate** — Status **Approved** required before production `.docx`/`.xlsx` tokens; **Implemented** after seed registered.

---

## Master table columns (§6)

Every row is one dynamic cell. Required columns:

| Column | Description |
|--------|-------------|
| **ID** | Stable id (e.g. `F01`, `HDR-1`, `ROW-Person_FullName`) |
| **Region** | Where on form (scan § / cell) |
| **Static label** | Turkmen label text from scan (literal in Word) |
| **Placeholder token** | Exact `{{…}}` string |
| **BO property** | `ApplicationItem.Person_FullName` etc. |
| **Data type** | string, byte[], date text, … |
| **Golden value** | Value from primary scan for QA/preview |
| **Notes** | Static punctuation between tokens, manual-only, phase-2 |

---

## Related docs

- **`docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md`** — allowed property names
- **`docs/EXCEL_PLACEHOLDER_REFERENCE.md`** — Excel list templates
- **`docs/USER_TEMPLATE_AUTHOR_GUIDE.md`** — author workflow
