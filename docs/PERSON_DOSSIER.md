# Person dossier ("dosye")

**Status:** Phases **1, 3, and 4 verified** against real data on the local PostgreSQL dev database
(headless Edge). Phase 2 is **partial** - the dossier opens document copies from its toolbar, but
per-section deep-linking from a row to its own copies record is still open.

**Localization:** all `PersonDossier.*` keys carry **en / tr-TR / tk-TM / ru-RU**, verified on screen
and in the exported PDF. One value still renders English - the Applications status, which comes from
the denormalized `ApplicationItem.LastApplicationState` column rather than a UI string.

Single-person 360 page: identity, current status, and every visa/permit/travel record for one
`Person`, in one read-only screen, with the existing document-copies catalog beside it.

**Views:** toolbar **Screen** | **Paper**. Screen is the officer dashboard layout. Paper renders the
same HTML fragment the export PDF uses (`PersonDossierDocumentHtmlBuilder.BuildFragment`) inside an
A4 sheet chrome — preview of the director hand-over without opening the preview slot (so Document
copies can stay open on the right).

**Primary audience:** company directors asking about a foreign employee. Officers produce it;
directors consume it as an export. That split drives most of the design decisions below.

**Agent skills:** [`visa2026-report-dashboard`](../.cursor/skills/visa2026-report-dashboard/SKILL.md)
(search entry) - [`visa2026-person-document-copies`](../.cursor/skills/visa2026-person-document-copies/SKILL.md)
(files) - [`visa2026-preview-slot`](../.cursor/skills/visa2026-preview-slot/SKILL.md) (slot shell).

---

## Why not a Report Dashboard panel

Every Report Dashboard category is "many rows -> status buckets -> chart -> drill to ListView".
A dossier is **one record with no aggregate**: no `Status` grouping, no `TotalCount`, no Excel
population parity. Forcing it through `IReportDashboardQueryService.LoadPanel` would produce a
category that violates the Preview `<->` SQL view `<->` ListView contract the dashboard skill exists
to protect.

**Decision:** the dossier is its own XAF view over a non-persistent host, mirroring the
`ReportDashboardHost` + `[EditorAlias]` + `BlazorPropertyEditorBase` pattern. Only the **search**
entry point becomes a dashboard category (Phase 3).

## Why the dossier is not a preview-slot occupant

The preview slot ([`PREVIEW_SLOT.md`](PREVIEW_SLOT.md)) is a right-side dock in the
`#visa-app-shell` CSS grid (main app `1fr` + slot width) and enforces **one occupant at a time**
- last `Open*` wins.

If the dossier were an occupant, opening passport copies would **evict the dossier**. Keeping the
dossier in the main content area means data on the left and scans on the right, simultaneously.

**Consequence (Phase 2):** `VisaPreviewSlotCloseController` auto-closes the slot when the owning
XAF `View` deactivates. Entry points must pass the **dossier** view id as `ownerViewId`
(`VisaPreviewSlotViewHelper.ResolveOwnerViewId`), otherwise navigating search -> dossier closes the
slot the officer just opened.

---

## Phases

| Phase | Deliverable | Status |
|-------|-------------|--------|
| **1** | Read model + dossier page + `Open dossier` action on Person DetailView / ListView | **Done, verified against real data** |
| **2** | Copies affordance -> `OpenPersonDocumentCopiesAsync` with dossier `OwnerViewId` | **Toolbar button built**; per-section deep-link open |
| **3** | `Person` search category in Report Dashboard (`vw_rd_person_search` + result rows) | **Done, verified against real data** |
| **4** | Director hand-over export: dossier document + person-scoped ZIP of copies | **Done, verified against real data** |

Phase 4 is the answer to open question 6 in
[`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md) ("is person ZIP export needed at all?").
It must be a **separate service** - not `PdfGenerationBatch`, which carries ministry slot semantics.

---

## Phase 1 - read model and page

### Sections

Role-aware, mirroring the `[Appearance]` rules already on `Person` (`EmployeeOnly_*`). A family
member has no `Educations` / `Salaries` / `WorkDuties`; a temporary visitor has almost no
employment block.

| Section | Source | Notes |
|---------|--------|-------|
| Passports | `Person.Passports` | `Current` badge via `PersonCurrentItems.GetCurrentPassport` |
| Visas | `Passport.Visas` **flattened** | Not on `Person` - see Modelling traps |
| Work permits | `Person.WorkPermitItems` -> parent `WorkPermit` | |
| Education | `Person.Educations` | Employee only |
| Position history | `Person.PositionHistory` | Employee only |
| Addresses of residence | `Person.AddressesOfResidence` | Registration/lodging |
| Travel history | `Person.TravelHistories` | In/out movement |
| Medical records | `Person.MedicalRecords` | |
| Family members | `Person.FamilyMembers` | Employee only (self-ref via `SponsoringEmployee`) |
| Applications | `Person.ApplicationItems` | Read-only issued workflow output |
| Invitations | `Person.InvitationItems` | |
| Rejections | `Person.RejectionItems` | |

### Status tiles

Four derived "right now" tiles at the top - the part the typed DetailView tabs do **not** compute:

| Tile | Source | Bucket |
|------|--------|--------|
| Passport | `GetCurrentPassport` | valid / expiring / expired / missing |
| Visa | `GetCurrentVisa` | same |
| Work permit | `GetCurrentWorkPermitItem` | same |
| Registration | `GetCurrentAddressOfResidence` | present / absent |

Reuse the dashboard status vocabulary so colours stay consistent across the app:
`st-approved` (green) / `st-pending` (amber) / `st-expiring` (red) / no class (gray).

### File map

| File | Role |
|------|------|
| `Visa2026.Module/Services/PersonDossier/PersonDossierModels.cs` | DTOs: snapshot, tile, section, record, cell |
| `Visa2026.Module/Services/PersonDossier/PersonDossierResolver.cs` | `Resolve(IObjectSpace, Person)` -> snapshot |
| `Visa2026.Module/Services/PersonDossier/PersonDossierOpenHelper.cs` | Opens the dossier view for a person |
| `Visa2026.Module/BusinessObjects/PersonDossier/PersonDossierHost.cs` | Non-persistent host (`PersonId` + `DossierUi`) |
| `Visa2026.Module/Editors/PersonDossierEditorAliases.cs` | Editor alias constant |
| `Visa2026.Module/Controllers/PersonDossierController.cs` | `OpenPersonDossier` action (DetailView + ListView) |
| `Visa2026.Module/Controllers/PersonDossierChromeController.cs` | Hides Save / Delete / Refresh on the read-only view |
| `Visa2026.Module/DatabaseUpdate/PersonDossierDetailViewUpdater.cs` | Suppresses the generated `DossierUi` layout caption |
| `Visa2026.Blazor.Server/Editors/PersonDossierModel.cs` | `ComponentModelBase` state |
| `Visa2026.Blazor.Server/Editors/PersonDossierPropertyEditor.cs` | `[PropertyEditor]` + `IComplexViewItem` |
| `Visa2026.Blazor.Server/Editors/PersonDossierComponent.razor` | The page |
| `Visa2026.Blazor.Server/wwwroot/css/person-dossier.css` | `.person-dossier*` styles |

The resolver deliberately mirrors `PersonLinkedDocumentsResolver` (sections -> records) so Phase 2
can align a dossier section with its copies section by id.

### Deliberately not done in Phase 1

- No new SQL view. The dossier reads the EF object graph; a view would be `Person` with extra steps.
- No dashboard changes.
- No export. Phase 4.

---

## Phase 4 - director hand-over export

`Export for director` on the dossier toolbar queues a background batch that produces one ZIP:

```
Dossier_<person>_<yyyyMMdd_HHmm>.zip
  Dossier.pdf                       the dossier itself, laid out as on screen
  Passports/Passport U40412139.pdf  one merged PDF per record that has attachments
  Visas/Visa A1742149.pdf
  EXPORT_NOTES.txt                  which records had no readable copy
```

### Decisions

**PDF from HTML, not a Word template.** `PersonDossierDocumentHtmlBuilder` renders the snapshot to
print HTML and `PersonDossierPdfBuilder` runs it through `RichEditDocumentServer`. This keeps the
export layout tied to the on-screen dossier, with no template to maintain per language - unlike
Resminamalar, where the officer *wants* to edit the template.

**Background batch, not an inline download.** `PersonExportBatch` + `PersonExportBatchWorkerService`
mirror `PdfGenerationBatch` / `WordReportGenerationBatch`: a person with many scans can take longer
than a request should live, and the officer gets the same progress toast they already know.

**One merged PDF per record.** Reuses `PersonDocumentCopyPdfMerger`, so a passport with a bio-page
PDF and a stamps-page image becomes a single readable file rather than loose scans.

### Entry naming

Two rules exist only for this package and are worth keeping:

1. **Folder = the record's own document class, not the catalog section.** The copies catalog nests
   visas under their passport (`RecordKey` is `Passport:x/Visa:y`, section `Passports`), which is
   right for an officer browsing one passport. A director expects `Visas/` beside `Passports/`, so
   `PersonExportPacker.FolderKeyByRecordType` overrides the section for such records.
2. **Leaf = the record label, not the merged file name.** `PersonDocumentCopyPdfMerger` returns the
   *uploaded* file name when a record holds a single file - meaningful in the copies preview, opaque
   in a hand-over package, where `visa-scan.pdf` should read `Visa A1742149.pdf`.

### File map

| File | Role |
|------|------|
| `Visa2026.Module/BusinessObjects/PersonExportBatch.cs` | Batch BO + status enum |
| `Visa2026.Module/DatabaseUpdate/PersonExportBatchSchemaSql.cs` | Idempotent `PersonExportBatches` DDL (SQL Server + Postgres) |
| `Visa2026.Module/DatabaseUpdate/PersonExportBatchSchemaUpdater.cs` | Applies the DDL during XAF database update |
| `Visa2026.Module/Services/PersonDossier/PersonDossierDocumentHtmlBuilder.cs` | Snapshot -> print HTML |
| `Visa2026.Module/Services/PersonDossier/PersonDossierPdfBuilder.cs` | HTML -> PDF bytes |
| `Visa2026.Module/Services/PersonDossier/PersonExportPacker.cs` | Assembles the ZIP |
| `Visa2026.Module/Services/PersonDossier/PersonExportBatchEnqueueService.cs` | Validates and queues |
| `Visa2026.Blazor.Server/Services/PersonExportBatchWorkerService.cs` | `BackgroundService` worker |
| `Visa2026.Blazor.Server/Controllers/PersonExportBatchesController.cs` | `my-latest` / `status` / `zip` with an ownership gate |
| `Visa2026.Blazor.Server/Components/PersonExportBatchToastHost.razor` | Progress toast + download link |

### Localization

All 98 `PersonDossier.*` strings carry **en / tr-TR / tk-TM / ru-RU**. This matters more here than on
an officer screen: the exported PDF is the artifact handed to a director, and the worker renders it
in the requesting officer's culture (`PersonExportBatch.RequestedCulture`).

Enum-valued cells (`ResidenceType`, `MovementType`, `TravelType`) go through
`PersonDossierResolver.LOr`, which looks up `PersonDossier.<Enum>.<Member>` and **falls back to the
raw enum name** when the key is missing. The wording is duplicated from the XAF model localization
on purpose - `CaptionHelper` is not dependable inside the background export worker, which has no XAF
application context, and a silent fall back to English in a director's PDF is worse than a duplicated
string.

**Still English:** the Applications section status (`Issued`). It comes from the denormalized
`ApplicationItem.LastApplicationState` column, not a UI string, so localizing it means resolving the
`ApplicationState` lookup - see [`LOOKUP_SEEDING.md`](LOOKUP_SEEDING.md), not this feature.

### Schema note

`PersonExportBatches` is created by idempotent SQL, not an EF migration - the pattern the rest of
this repo uses. It runs from both `PersonExportBatchSchemaUpdater` and a `ApplyIfMissing` call at
host start, because a database already at the current module version skips the updater.

---

## Modelling traps

Three things that will bite an implementer:

1. **Visas hang off `Passport`, not `Person`.** There is no `Person.Visas`. The Visas section is a
   flatten across `Person.Passports` -> `Passport.Visas`, and "current visa" must survive a passport
   replacement (`PersonCurrentItems.GetCurrentVisa(Person)` already handles this).
2. **There is no `Registration` collection on `Person`.** Registration lives on `ApplicationItem`;
   physical movement is `TravelHistories`; lodging is `AddressesOfResidence`. The dossier merges
   these for the reader, but they are three sources.
3. **`Person.Photo` is `byte[]`**, already cropped to a 3:4 passport ratio (113x151 PNG) in
   `Person.OnSaving()`. Render as a base64 data URI; do not add a `FileData` round trip.

### XAF hosting traps (found while verifying Phase 1)

Both only show up at runtime; the code compiles either way.

1. **`ObjectViewController<DetailView, PersonDossierHost>` never activates** for this view, even though
   the view reports `ObjectTypeInfo.Type == PersonDossierHost`. `PersonDossierChromeController` is a
   plain `ViewController` that matches on `View.Id` instead. The same applies to any future controller
   targeting a non-persistent host object.
2. **Hiding CRUD chrome in `OnActivated` alone is not enough** when the dossier replaces the current
   view (`TargetWindow.Current` + `ShowViewParameters.CreatedView`): the standard controllers activate
   afterwards and reinstate Save / Delete. The chrome controller reapplies in `OnViewControlsCreated`.
3. **`[ModelDefault("ShowCaption", "False")]` on the editor property does not hide the layout caption.**
   The caption comes from the generated `IModelLayoutViewItem`, so `PersonDossierDetailViewUpdater`
   targets `ModelDetailViewLayoutNodesGenerator`. Setting the member item's `Caption` to an empty
   string is not a workaround - XAF then falls back to the item id (`DossierUi`).

The Report Dashboard host view still shows Save / Save and New / Delete for reason 2; it was never
given a chrome controller.

---

## Overlap with the typed Person DetailView

The typed DetailView already shows all of this data in tabs
([`PERSON_DETAIL_NESTED_COLLECTION_TABS.md`](PERSON_DETAIL_NESTED_COLLECTION_TABS.md)). The dossier
only earns its place by being:

- **one page** - no tab clicking to answer "is his visa valid?"
- **read-only** - safe to show to a director over the officer's shoulder
- **derived** - status tiles compute valid / expiring / expired, which the tabs do not
- **print-shaped** - the on-screen layout is the export layout

If it degrades into "the same tabs, stacked", it is duplicate UI. Officers keep using the typed
DetailView for **editing**; the dossier never edits.

---

## Permissions

Directors are **not** XAF users and should not become one. The director-facing artifact is the
Phase 4 export, handed over by an officer.

If a read-only in-app role is ever wanted, it is a role change via
[`visa2026-security-access`](../.cursor/skills/visa2026-security-access/SKILL.md) - `Read` on
`Person` and children plus navigation to the dossier view - not a bypass in the dossier code.

---

## Known gap

`FamilyMemberImage` (`Person.Images`) byte-array images are excluded from copies packaging today and
their preview was left TBD in [`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md). So "all file
copies" has a small hole around family member photos until product decides.

---

## Build / verify

```powershell
dotnet build Visa2026.slnx -c Debug
```

Manual (Phase 1): employee Person -> **Open dossier** -> identity header, four status tiles, sections
with counts -> family member Person shows no education/salary sections -> temporary visitor shows the
minimal set.

Verified on the local dev database (employee with 4 passports / 29 visas / 24 work permits / 94
applications, and a family member): identity header, status tiles, all sections populated, no stray
layout caption, toolbar limited to **Close all tabs** plus the dossier's own **Document copies**, and
the copies catalog opening in the preview slot beside the dossier rather than replacing it.

Open UX question from that run: the employee page is ~7500px tall because every section renders every
row (94 application rows). Capping each section with a "show all" affordance is a product decision,
not a bug.

---

## Related docs

- [`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md) - the files catalog this page links to
- [`PREVIEW_SLOT.md`](PREVIEW_SLOT.md) - slot shell, occupant rules, owner-aware close
- [`REPORT_DASHBOARD.md`](REPORT_DASHBOARD.md) - where the Phase 3 search entry lands
- [`PERSON_DETAIL_NESTED_COLLECTION_TABS.md`](PERSON_DETAIL_NESTED_COLLECTION_TABS.md) - the editable counterpart
- [`Visa2026.Module/BusinessObjects/Person.md`](../Visa2026.Module/BusinessObjects/Person.md) - collection semantics
