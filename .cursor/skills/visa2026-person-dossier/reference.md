# Person dossier — reference

Canonical narrative: [`docs/PERSON_DOSSIER.md`](../../../docs/PERSON_DOSSIER.md).

## File map (Phase 1 page)

| File | Role |
|------|------|
| `Visa2026.Module/Services/PersonDossier/PersonDossierModels.cs` | Snapshot DTOs |
| `Visa2026.Module/Services/PersonDossier/PersonDossierResolver.cs` | `Resolve` → snapshot |
| `Visa2026.Module/Services/PersonDossier/PersonDossierOpenHelper.cs` | Create DetailView |
| `Visa2026.Module/BusinessObjects/PersonDossier/PersonDossierHost.cs` | Non-persistent host |
| `Visa2026.Module/BusinessObjects/PersonDossier/PersonDossierViewIds.cs` | DetailView id constant |
| `Visa2026.Module/Editors/PersonDossierEditorAliases.cs` | Editor alias |
| `Visa2026.Module/Controllers/PersonDossierController.cs` | Open dossier action |
| `Visa2026.Module/Controllers/PersonDossierChromeController.cs` | Hide Save/Delete/Refresh |
| `Visa2026.Module/DatabaseUpdate/PersonDossierDetailViewUpdater.cs` | Layout caption suppress |
| `Visa2026.Blazor.Server/Editors/PersonDossierModel.cs` | Component model (+ load progress) |
| `Visa2026.Blazor.Server/Editors/PersonDossierPropertyEditor.cs` | Load stages + resolve |
| `Visa2026.Blazor.Server/Editors/PersonDossierComponent.razor` | Screen / Paper / loading UI |
| `Visa2026.Blazor.Server/wwwroot/css/person-dossier.css` | `.person-dossier*` |

## File map (Phase 4 export)

| File | Role |
|------|------|
| `Visa2026.Module/BusinessObjects/PersonExportBatch.cs` | Batch BO |
| `Visa2026.Module/DatabaseUpdate/PersonExportBatchSchemaSql.cs` | Idempotent DDL |
| `Visa2026.Module/Services/PersonDossier/PersonDossierDocumentHtmlBuilder.cs` | Snapshot → HTML (`Build` / `BuildFragment`) |
| `Visa2026.Module/Services/PersonDossier/PersonDossierPdfBuilder.cs` | HTML → PDF |
| `Visa2026.Module/Services/PersonDossier/PersonExportPacker.cs` | ZIP layout / folder keys |
| `Visa2026.Module/Services/PersonDossier/PersonExportBatchEnqueueService.cs` | Queue |
| `Visa2026.Blazor.Server/Services/PersonExportBatchWorkerService.cs` | Background worker |
| `Visa2026.Blazor.Server/Controllers/PersonExportBatchesController.cs` | Download API |
| `Visa2026.Blazor.Server/Components/PersonExportBatchToastHost.razor` | Toast |

## Localization

- Keys: `PersonDossier.*` in `tools/GenerateModelLocalization/UiStrings.messages.json`
- Regenerate: `dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj`
- Loading stage keys: `PersonDossier.Chrome.LoadingPreparing` … `LoadingFinishing`, `LoadingProgress`

## Phase 2 (open)

Toolbar **Document copies** already opens person copies with dossier `OwnerViewId`.
Still open: per-section / per-row deep-link into the matching copies catalog record (align section ids with `PersonLinkedDocumentsResolver`).