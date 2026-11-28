# Edit template — reference

Canonical narrative: [`docs/TEMPLATE_STAGING_EDIT.md`](../../../docs/TEMPLATE_STAGING_EDIT.md).

## File map

| Area | Path |
|------|------|
| Staging service, profile-copy publish, PostgreSQL FileData SQL | `Visa2026.Module/Services/UserReports/UserReportTemplateStagingService.cs` |
| Ribbon token | `Visa2026.Module/Services/UserReports/TemplateEditRibbonToken.cs` |
| HTTP: export, desktop open/sync, ribbon-sync, PC setup zip | `Visa2026.Blazor.Server/Controllers/UserReportTemplateStagingController.cs` |
| Same-PC write and Office start | `Visa2026.Blazor.Server/Services/TemplateEditDesktopBroker.cs` |
| Zip builder | `Visa2026.Blazor.Server/Services/TemplateEditPcSetupPackage.cs` |
| Catalog button and cards | `Visa2026.Blazor.Server/Editors/ApplicationReportPackageComponent.razor` |
| Browser folder, protocol launch, download of the zip | `Visa2026.Blazor.Server/wwwroot/js/template-staging-local.js` |
| Officer opener | `scripts/windows-iis/Invoke-Visa2026TemplateEdit.ps1` |
| Trust, opener registration, sandbox folder, calls the add-in installer | `scripts/windows-iis/Set-Visa2026TemplateEditOfficeTrust.ps1` |
| Add-in compile/register | `scripts/windows-iis/Install-Visa2026OfficeSyncAddin.ps1` |
| Add-in source and shipped DLL | `scripts/windows-iis/office-sync/Connect.cs`, `Visa2026.OfficeSync.dll` |
| Double-click entry for the zip | `scripts/windows-iis/template-edit-setup/Install.cmd`, `Install-Visa2026TemplateEdit.ps1` |

The Blazor project copies those scripts and the DLL to `template-edit-setup\` on build. The download reads that output folder, not the git path, at runtime.

## Officer PC layout

| Path | Role |
|------|------|
| `%LOCALAPPDATA%\Visa2026\TemplateEdit` | Temporary `.docx` / `.xlsx`, `{file}.meta.json`, `ribbon-host.json` |
| `%LOCALAPPDATA%\Visa2026\TemplateEditHost` | Installed opener script and `Visa2026.OfficeSync.dll` |
| `HKCU\...\Office\Word\Addins\Visa2026.OfficeSync.Connect` | LoadBehavior 3. Same key under Excel |

`ribbon-host.json` holds `serverBase` and a host token (`T="*"`) for every template this officer edits. Per-file meta still has `templateId`.

## Add-in build notes

- Target is .NET Framework `net48`, `csc` from `Framework64\v4.0.30319`.
- Extra reference: `System.Net.Http.dll` only. Do not also pass `System.dll` / `System.Windows.Forms` (CS1703).
- Embed Office interop with `/link:extensibility.dll` so officer PCs do not need Visual Studio.
- `ClassInterface.None`. Default dual interface exposes `OnSync`. Implement real `Extensibility.IDTExtensibility2`.
- ProgId `CLSID` is a **subkey**, not a value on the ProgId key.
- Word and Excel must be closed before replacing the DLL.

## Isolated build

```powershell
.\scripts\local\Build-Isolated.ps1 -Project Visa2026.Blazor.Server\Visa2026.Blazor.Server.csproj -SessionId <session>
```

F5 runs `Visa2026.Blazor.Server\bin\Debug\net8.0`, not the isolated artifacts folder.
