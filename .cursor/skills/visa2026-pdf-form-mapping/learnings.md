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
