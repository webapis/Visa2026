# Edit template — learnings

Append-only. Newest first. Add a row only after a verified fix. Promote a repeated cause into [SKILL.md](./SKILL.md) **Scenarios**.

## Entries

### 2026-11-28 — Install on this PC crashed in SecuritySystem

- **Need**: **Install on this PC** should download the setup zip.
- **Cause**: The GET called `UserReportTemplateEditAccess.CanEditTemplates()` → `SecuritySystem.IsGranted`. A plain HTTP request has no XAF `ValueManagerContext` (`Storage is null`).
- **Fix**: The action only checks that staging is enabled. The controller is `[Authorize]`. The button is still shown from the Blazor circuit, where the permission check works.
- **Officer**: Restart Visa2026 and click **Install on this PC** again.
- **Prevent**: Do not call `SecuritySystem.IsGranted` from staging HTTP actions. The upload path already skips it for the same reason.

### 2026-11-28 — Home Sync is one add-in, not a control in each file

- **Need**: Sync next to Save, for templates already on the PC and templates added later.
- **Cause**: Office will not put a third-party button on the built-in Save control. A `customUI` part inside each `.docx` would miss existing and future files.
- **Fix**: COM add-in `Visa2026.OfficeSync.Connect`, first group on the Home tab, Word and Excel. `GetCustomUI` always returns the Sync button. `ribbon-host.json` (written on Edit template) authorizes every template in the folder. The shipped DLL embeds `IDTExtensibility2` (`csc /link`). A hand-rolled interface left `Connect=false` (`Specified OLE variant is invalid`). ProgId `CLSID` must be a subkey.
- **Officer**: Close Word and Excel, run **Install.cmd** once, click **Edit template** once, then **Home → Sync**.
- **Prevent**: Do not compile the add-in on officer PCs. Copy `office-sync\Visa2026.OfficeSync.dll`.

### 2026-11-28 — Preview ignored bold saved by Sync

- **Need**: After Sync, Preview of yüztutma-energetika still showed the opening line in regular weight. The Word file had **Türkmenistanyň** bold.
- **Cause**: Sync wrote `UserReportTemplate` FileData. Preview and ZIP read the same-named `ApplicationProfileTemplate.TemplateFile`.
- **Fix**: Sync also writes those bytes onto every same-name Word/Excel profile copy.
- **Officer**: Click Preview again after Sync.
- **Prevent**: Do not treat a matching master hash as “nothing to publish” while a profile copy still differs.

### 2026-11-28 — Sync SQL used SQL Server brackets on PostgreSQL

- **Need**: Catalog Sync should save the edited file.
- **Cause**: The FileData update used `[FileData]` / `[Content]`. Npgsql rejected that.
- **Fix**: Quote identifiers: `"FileData"`, `"Content"`, `"Size"`, `"FileName"`, `"ID"`.
- **Prevent**: New staging SQL is PostgreSQL only.
