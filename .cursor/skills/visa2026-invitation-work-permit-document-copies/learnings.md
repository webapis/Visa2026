# learnings.md — invitation & work permit document copies

Append-only notes from verified implementation runs. Read before implementing.

### 2026-08-26 — Case-workspace issued visa Preview reuses header-copies occupant

- **Need**: **Visas issued by this case** Preview of `VisaDocument` in `#visa-preview-slot`, same as invitation issued-row Preview.
- **Fix**: Added `HeaderDocumentCopiesFamily.Visa`. Resolver/object-space/occupant key `visa-document-copies:visa:{id}`. Workspace Preview only — no Visa ListView Copies column, no Delete.
- **Test**: Occupant-key + HasCopy unit tests. Blazor Debug build. Officer: Preview on a0002 with copy.
- **Prevent**: Do not invent a File occupant for this; keep `OpenPreviewOnly` on `HeaderDocumentCopies`.
- **Cross-skill**: invitation-work-permit-document-copies | preview-slot | application-profile

## 2026-06-06 — Phases 0–2 initial implementation

- **`SupportingDocumentsPdfSharpHelper.TryWriteSinglePagePdfFromRasterBytes`** uses positional parameter `landscape`, not `landscapePage:` — mirror `PersonDocumentCopyPdfMerger` call style.
- **Blazor list-link controller** needs `using Visa2026.Module.BusinessObjects;` — do not use `typeof(BusinessObjects.WorkPermit)` without that alias.
- **`Startup.cs`** must `using Visa2026.Module.Services.HeaderLinkedDocuments;` for `HeaderDocumentCopyPdfMerger` DI.
- **`BorderZone_DetailView`** Documents tab lives in `Visa2026.Blazor.Server/Model.xafml` (tabbed with `BorderZoneItems`); localization captions via `UiStrings.document-copies.json` + `GenerateModelLocalization`.
- **EF schema:** `BorderZoneDocument` table is created on next XAF DB update / deploy (`FORCE_XAF_DB_UPDATE` if needed on an already-current DB).
- **Not in v1:** `PersonLinkedDocumentsResolver` border-zone section; Phases 3–4 (cross-links, images, ZIP).
- **ListView NRE in `DxGridListEditorBase.AddColumnCore`:** Do **not** use `[Browsable(false)]` on `DocumentCopiesListLink` — XAF cannot resolve the member for the grid column. Match `Person.DocumentCopiesListLink`: `[VisibleInDetailView(false)]`, `[VisibleInLookupListView(false)]`, `[ModelDefault("AllowEdit", "False")]`.
- **Parent ListView empty columns:** `HeaderDocumentCopiesListViewColumnUpdater` must seed **data columns + link** for parent ListViews. Adding only `DocumentCopiesListLink` switches the grid to an explicit one-column model. Mirror `Person_ListView_Employees` in `Model.xafml`.
- **Person vs header ListView (why Person works):** Person navigation uses **typed** `Person_ListView_Employees` etc. — `CustomViewClonerUpdater` **CopyColumns** from `Person_ListView` first; `PersonDocumentCopiesListViewColumnUpdater` adds **only** the link column. Header BOs use default `Invitation_ListView` / `WorkPermit_ListView` / `Rejection_ListView` — early doc-copies work poisoned these ids (link-only column set + `ModelDifference` per user). Fix: generator adds link only (Person pattern); `HeaderParentListViewConfigurator.Wire` reapplies full parent columns on **`SetupComplete` and `LoggedOn`** (after user model merge).
- **Item ListViews (`*Item_ListView`):** Same collapse as parent views when only `DocumentCopiesListLink` gets `Index=1` from `HeaderDocumentCopiesListViewColumnUpdater` while localization columns lack `Index`/`Width`. Extend `HeaderParentListViewColumns` with item layouts; add explicit columns in `Visa2026.Blazor.Server/Model.xafml` for `RejectionItem_ListView`, `InvitationItem_ListView`, `BorderZoneItem_ListView`; shift `WorkPermitItem_ListView` indices after link at index 1. Update `UiStrings.documents-views.json` (not only `person-detail.json`) and re-run `GenerateModelLocalization`.

### 2026-07-31 — Prototype A nav across all document-copies catalogs

- **Ask**: Apply Foxit-style vertical nav layout across all document copies in the project.
- **Fix**: Shared `DocumentCopiesCatalogNavIcons`; Person/Header/ApplicationItem section heads use `__section-head--nav` + Open/Close; exclusive expand (AppItem/Person multi-section); Header single Documents card collapsed by default.
- **Prevent**: Do not keep flat always-open section heads on Header/AppItem while Person has nav cards.
- **Cross-skill**: person-document-copies | document-copies | invitation-work-permit-document-copies | preview-slot

### 2026-07-31 — Remove Document copies ListView toolbar (Person + Header)

- **Ask**: Drop "Person document copies" from Employees toolbar; row icon is enough. Same for header BOs.
- **Fix**: `PersonDocumentCopiesController` and `HeaderDocumentCopiesController` are `ViewController<DetailView>` only. ApplicationItem keeps ListView toolbar (multi-select package; no row link).
- **Prevent**: Do not re-add ListView toolbar copies when a Copies column already opens the slot.
- **Cross-skill**: person-document-copies | invitation-work-permit-document-copies | document-copies
