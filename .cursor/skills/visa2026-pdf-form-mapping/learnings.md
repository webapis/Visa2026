# Learnings (append-only): PDF form mapping

Purpose: **XFA fill, PdfFormMapping rules, template, gates, converters** — not Document copies dialog layout.

**Read before every task:** skim **## Entries** (newest first).  
**Maturity:** [MATURITY.md](./MATURITY.md).

**Document copies UX / scan preview:** [visa2026-document-copies/learnings.md](../visa2026-document-copies/learnings.md).

**After a verified fix:** append one entry. **Do not** edit or delete prior entries.

```markdown
### YYYY-MM-DD — <short title> (mapping | gate | template | filler)

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: pdf-form-mapping | document-copies | lookup-data | lifecycle-docker | —
```

---

## Entries

### 2026-06-05 — Family members mapped to item 18, not _241 (mapping)

- **Symptom**: Visa PDF showed `ESRA AKSOY; 12.10.1989; AYALY` under item 24 (work experience); item 18 family lines empty.
- **Try**: Dump XFA keys near `_18` / `_24`; compare with officer reference PDF.
- **Test**: Document copies application form download — family text under item 18 (`_181`–`_183`), item 24 blank.
- **Root cause**: `PdfFormMapping` pointed `Pdf_FamilyMembersAggregateText` at `_241[0]` (work/profession text near item 24) and used semicolon aggregate format.
- **Fix**: Map `Pdf_FamilyMembersMaritalLine1/2/3` → `_181`–`_183`; delete `_241` mapping via `FamilyMembersPdfFormMappingUpdater`; format `REL NAME DATE` with country on last segment (`VisaFamilyMemberLinesHelper.FormatForVisaPdfMaritalFamilyBlock`).
- **Prevent**: Pdf field reference documents `_241` as work text only; seed + migration updater for existing DBs.
- **Cross-skill**: —

### 2026-06-05 — Stale `_241` DB row bypassed item-18 fix (mapping)

- **Symptom**: After code fix, format correct (`AYALY ESRA AKSOY…`) but still under item 24 / `_241`; item 18 lines empty.
- **Try**: Inspect XFA template — `_181`–`_183` tooltips = Maşgala ýagdaýy; `_241` tooltip = Wezipe boýunça iş tejribesi.
- **Test**: Rebuild + regenerate PDF without DB migration — still wrong until mapping load normalized.
- **Root cause**: `CreateMappingIfNotExists` left legacy `_241` → `Pdf_FamilyMembersAggregateText` in DB; `FamilyMembersPdfFormMappingUpdater` had not run (no module version bump / restart).
- **Fix**: `PdfMappingHelper.GetMappings` normalizes mappings at fill time (drop `_241` family row, force `_181`–`_183` line paths); `PdfFormMappingUpdater` + `FamilyMembersPdfFormMappingUpdater` persist same correction.
- **Prevent**: Do not rely on seed-only for mapping corrections — always migrate existing rows or normalize at load.
- **Cross-skill**: lifecycle-docker (FORCE_XAF_DB_UPDATE if admin rows still stale in UI)

### 2026-06-05 — Item 21 Okan ýeri empty (expression mapping)

- **Symptom**: Field 19–20 (Bilimi, Hünäri) filled; item 21 Okan ýeri blank on generated PDF.
- **Try**: Compare DB `PdfFormMapping` for `_21` — legacy `Expression` with `Concat(CurrentEducation.EducationCountry.Name, …)`.
- **Test**: Employee with `CurrentEducation` set → item 21 shows `TUR, INSTITUTION NAME` (uppercased by PDF normalizer).
- **Root cause**: Criteria `Concat` on nested navigations unreliable; used `Name` instead of country `Code`.
- **Fix**: `ApplicationItem.Pdf_EducationPlaceOfStudy` (`{Code}, {Institution NameTm-first}`); map `_21` as Property; runtime + `EducationPlacePdfFormMappingUpdater` rewrite legacy rows.
- **Prevent**: Prefer computed `Pdf_*` properties over expressions for multi-hop BO paths; match `Education_CountryCode` / `Education_InstitutionName` report pattern.
- **Cross-skill**: —

### 2026-06-06 — Classify mapping vs document copies bugs

- **Symptom**: Officer reports “Document copies application form wrong.”
- **Try**: Confirm download succeeds; open PDF and inspect **which fields** are empty vs wrong value.
- **Test**: Fix mapping → Document copies row works without dialog code changes.
- **Root cause**: Same generator (`ApplicationFilledFormPdfGenerator`) used by dialog and batch worker; UI skill is wrong owner for field content.
- **Fix**: Route to **pdf-form-mapping**; check `PdfFormMapping`, logs, `PdfMappingSourceGate`.
- **Prevent**: Scenarios tables in both skills cross-link; do not change Blazor for mapping-only issues.
- **Cross-skill**: document-copies

### 2026-06-06 — PdfMappingSourceGate skips hidden slots

- **Symptom**: PDF field empty though related BO has data; application type hides document slot in UI.
- **Try**: Compare `ApplicationType` Show* flags with mapping `PropertyPath` / expression references.
- **Test**: After enabling slot or setting link, field fills; gate skip logged at Debug.
- **Root cause**: Intentional — PDF should not show out-of-scope slots (matches XAF Appearance).
- **Fix**: Admin expectation: add mapping only for in-scope paths; or officer must set ApplicationItem link.
- **Prevent**: Document in `PdfFormMapping.md` §3.1; triage table in SKILL.md.
- **Cross-skill**: lookup-data (ApplicationType flags)

### 2026-06-06 — XFA merge: do not use MergeFiles

- **Symptom**: Merged PDF shows XFA “Please wait…” placeholder; filled data lost.
- **Try**: Search for `MergeFiles` or indiscriminate Spire merge on filled streams.
- **Test**: Page-by-page import per `XFA_PDF_Integration.md`.
- **Root cause**: `MergeFiles` reconstructs XFA layer from streams.
- **Fix**: Use page import loop documented in XFA integration guide.
- **Prevent**: Any new merge code for filled forms must follow XFA doc — not scan merger (PdfSharpCore) patterns.
- **Cross-skill**: —
