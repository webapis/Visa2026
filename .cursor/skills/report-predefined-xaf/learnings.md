# Learnings (append-only): predefined reports in Visa2026

Purpose: capture recurring pitfalls and proven patterns discovered while creating/updating predefined reports.  
This file is intended to evolve frequently. Keep `SKILL.md` stable; promote items into `SKILL.md` only when they become true standards.

## How to use

After finishing a report task, append:

- **Date** (YYYY-MM-DD)
- **Report(s)** affected
- **What happened** (symptom)
- **Root cause**
- **Fix**
- **Prevent** (how to avoid next time)

Template:

```markdown
### YYYY-MM-DD — <ReportClassName> (or area)

- **Symptom**:
- **Root cause**:
- **Fix**:
- **Prevent**:
```

---

## Entries

### 2026-05-09 — `AppInvAndWPBorcnamaItemReport`

- **Symptom**: Scanned Borçnama includes local representative passport and mobile; model has no `LocalEmployee` passport/phone fields.
- **Root cause**: `Representative` expat path uses `Person.CurrentPassport`; local path has no parallel data.
- **Fix**: `Representative_PassportLine` and `Representative_Phone` only populate for expat representative; operators can extend BO later or store free text in `Company.TaxInformation` patterns if needed.
- **Prevent**: When a form needs local-ID fields, confirm `LocalEmployee` (or `Representative`) schema before binding.

### 2026-05-05 — report-predefined-xaf skill added

- **Symptom**: Report creation/update steps were being repeated manually across many sessions.
- **Root cause**: No single enforced workflow + gates for map approval, Turkmen QA, and `.resx` sync.
- **Fix**: Added project skill + references; added this learnings log for continuous improvement.
- **Prevent**: Append new pitfalls here; promote to `SKILL.md` only when repeated and stable.

