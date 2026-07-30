# Learnings (append-only): Person document copies

Purpose: **Person resolver, sectioned catalog, preview slot occupant, ListView entry** — not ApplicationItem ministry ZIP.

**Read when implementing or fixing Person document copies.**

**Canonical design:** [docs/PERSON_DOCUMENT_COPIES.md](../../../docs/PERSON_DOCUMENT_COPIES.md)

**Maturity:** [MATURITY.md](./MATURITY.md)

**After a verified fix:** append one entry below. **Do not** edit or delete prior entries.

```markdown
### YYYY-MM-DD — <short title> (phase N)

- **Symptom**:
- **Try**:
- **Test**:
- **Root cause**:
- **Fix**:
- **Prevent**:
- **Cross-skill**: person-document-copies | preview-slot | document-copies | —
```

---

## Entries

_No verified implementation entries yet. Design drafted 2026-06-06._

### 2026-07-30 - Person dossier becomes a 4th entry point (phase 2)

- **Ask**: From a person search result, open the dossier ("dosye") and every file copy (passport, visa, invitation, diploma, work permit, CV) for that person.
- **Try**: New read-only dossier page (`PersonDossierHost` + `PersonDossierPropertyEditor` + `PersonDossierComponent`), toolbar **Document copies** button calling `OpenPersonDocumentCopiesAsync`.
- **Root cause (of the design question)**: The ask looked like new work, but sections Passports / Visas / Education / WorkPermit / Invitation / Person files already exist in `PersonLinkedDocumentsResolver`. Only an entry point was missing.
- **Fix**: Reused the existing occupant unchanged. Entry points are now DetailView toolbar, ListView toolbar, ListView **Copies** column, **and the dossier**. Dossier `RecordKey` format is deliberately identical to `PersonLinkedDocumentRecord.RecordKey` (`Passport:{id}`, `Passport:{pid}/Visa:{vid}`, `Education:{id}`, ...) so a dossier row can be deep-linked to its copies row later.
- **Prevent**: Before building a "show me the person's files" surface, check this resolver first - the catalog is BO-family complete; what is usually missing is a caller.
- **Not verified in a running app session**: `dotnet build Visa2026.slnx -c Debug` clean; no UI run yet. Per-section paperclip deep-link still open.
- **Cross-skill**: preview-slot, report-dashboard (planned Person search category)

## 2026-07-30 - Person dossier verified against real data (headless Edge)

**What:** Drove the running dev host (localhost:5000, Postgres `visa2026`) with Selenium + msedgedriver
from `Visa2026.E2E.Tests/.webdrivers` to open **Open dossier** on an employee and a family member, then
the dossier's **Document copies** button.

**Confirmed:** dossier renders in the main area while the copies catalog occupies `#visa-preview-slot`
side by side - the `OwnerViewId` = `PersonDossierHost_DetailView` keeps the slot from self-closing.

**Two runtime-only bugs the compiler could not catch** (details in `docs/PERSON_DOSSIER.md`):

- `ObjectViewController<DetailView, PersonDossierHost>` does **not** activate for a non-persistent host
  view; match on `View.Id` from a plain `ViewController` instead.
- Hiding Save / Delete only in `OnActivated` is undone when the view replaces the current one; reapply
  in `OnViewControlsCreated`.

**Tip:** when checking whether a rebuilt DLL contains a change, search string literals as **UTF-16**
(metadata `#US` heap) - an ASCII search finds type names but never literals, which reads as a false
"my code did not build".

### 2026-07-30 - Director hand-over export: two entry-naming bugs a green build hides (phase 4)

- **Symptom**: The first end-to-end export succeeded and produced a valid ZIP - with only `Dossier.pdf` in it. The dev Postgres has **zero rows** in every `*Document` table, so `PersonExportPacker`'s per-record loop never ran. `(1 / 1)` in the toast was truthful and useless.
- **Try**: Seeded fixtures directly in the dev DB - four `FileData` rows loaded with `pg_read_binary_file`, wired to two passports (one with a PDF **and** a PNG, to force a real merge) and one visa (to force a second section). Fixture files must sit somewhere the Postgres **service account** can read; `C:\Users\<me>\AppData\Local\Temp` is not that place.
- **Test**: Selenium: search -> open dossier -> **Export for director** -> wait for the toast -> download -> list ZIP entries.
- **Root cause**: Two assumptions in `BuildEntryName`, both invisible until a record actually had files.
  1. Folder came from `section.SectionLabel`, but the catalog nests a visa **under its passport** (`RecordKey` = `Passport:x/Visa:y`, section `Passports`) - so the visa scan landed in `Passports/`.
  2. Leaf came from `PersonDocumentCopyPdfMerger`'s file name, which returns the **uploaded** name for a single-file record - so the package read `visa-scan.pdf` instead of `Visa A1742149.pdf`.
- **Fix**: Folder now resolves from the record's own document class (`PersonExportPacker.FolderKeyByRecordType`, currently only `Visa` -> new `PersonDocumentCopies.Section.Visas` key), falling back to the section label; leaf now prefers `record.RecordLabel`. Result: `Passports/Passport U40412139.pdf`, `Visas/Visa A1742149.pdf`.
- **Prevent**: The merger's single-file naming is **right for the preview** (the officer recognizes the file they uploaded) and **wrong for a package** - do not "fix" it in the merger; adapt at the packer. And treat "batch completed with 1 record" as a red flag to check whether the fixture data exists at all, not as a pass.
- **Also**: `PersonExportBatches` needs idempotent DDL (`PersonExportBatchSchemaSql`) run from **both** the `ModuleUpdater` and host start - a database already at the current module version skips updaters, and the worker then fails with `relation "PersonExportBatches" does not exist`.
- **Cross-skill**: person-document-copies, report-dashboard (search category is the entry point)

### 2026-07-30 - Verifying an export: the toast can hand you a stale package (phase 4)

- **Symptom**: After adding `PersonDossier.<Enum>.<Member>` keys and rebuilding, the exported PDF *still* showed raw `PrivateHouse` / `Entry` / `External`. The catalog contained the keys, the deployed `Visa2026.Module.dll` contained the literals (UTF-16 `#US` search), and only one host process was running, started after the build.
- **Root cause**: The package was **not the one the run had just produced**. The toast host resolves the user's latest batch, and a previously completed batch surfaced immediately; the smoke followed its download link. The giveaway was inside the artifact itself - the PDF footer read `Сформировано: 12:26`, minutes *before* the rebuild.
- **Fix (to the verification, not the code)**: Pulled the newest batch's ZIP straight out of Postgres (`lo_from_bytea` + `\lo_export`) instead of trusting the toast link. The enum labels were correct all along in the new build.
- **Prevent**: Any artifact a batch produces should be checked against a **freshness marker it carries itself** (generated-on stamp, batch id) before concluding anything about the code. A green toast plus a downloaded file is not evidence that *this* run produced *that* file.
- **Also (real finding)**: `PersonDossierResolver.LOr` silently falls back to the raw enum name, so a missing key is invisible until someone reads the output in a non-English culture. The dossier duplicates the XAF enum captions into the message catalog deliberately - `CaptionHelper` has no XAF application context inside the export worker.
- **psql on Windows**: PowerShell strips the doubled quotes in `psql -c "... \"Table\" ..."`, which surfaces as `relation "people" does not exist` for a table that plainly exists. Put the SQL in a file and use `-f`.
- **Cross-skill**: person-document-copies, lookup-data (`ApplicationItem.LastApplicationState` still renders English)
