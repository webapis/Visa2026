# Document copies submenu — missing file as printable page

When **Preview document copies** opens a section for checked people, merge existing scans with a **printable placeholder page** for each person who is missing that document type. The placeholder is part of the PDF preview stack (not a web empty-state card). It must show **person** and **document type**. **Edit / upload is not offered** on the submenu path.

Editing remains only from People & Links **Nusga/Copies** or the **No file** badge.

**Status:** implemented.

## Locked decisions

| Decision | Choice |
|----------|--------|
| Missing presentation | Printable page inside the preview PDF |
| Page content | Title **File is not uploaded yet** + **Person** + **Document type** |
| Mixed people | Keep present scans; insert placeholder page(s) for missing person(s) of same type |
| Submenu Edit | **No** |
| Nusga / No file Edit | **Yes** (same-slot editor) |

## Screens

| File | Notes |
|------|--------|
| [case-workspace-doc-copies-submenu-missing-printable-page-prototype.jpg](./case-workspace-doc-copies-submenu-missing-printable-page-prototype.jpg) | **Canonical:** Passports for 3 people — ready scans + printable missing page for Umit (Person + Document type) |
| [case-workspace-doc-copies-submenu-missing-mixed-people-prototype.jpg](./case-workspace-doc-copies-submenu-missing-mixed-people-prototype.jpg) | Earlier mixed draft (UI blocks; superseded for missing presentation) |
| [case-workspace-doc-copies-submenu-missing-preview-only-prototype.jpg](./case-workspace-doc-copies-submenu-missing-preview-only-prototype.jpg) | Earlier all-missing draft |

## Defaults used at implement

| Question | Choice |
|----------|--------|
| Submenu CTAs | Close only (printable page; no Edit / Open person) |
| Left-nav Document copies | Unchanged (AppItem path) |
| All people missing | Placeholder page(s) only; still no Edit |

## Implementation notes

- PersonDocumentCopyPdfMerger.TryBuildMergedSectionPdf inserts PdfSharp placeholder pages per missing person
- SupportingDocumentsPdfSharpHelper.TryWriteMissingDocumentCopyPlaceholderPage
- PersonDocumentCopiesSlotRequest.AllowEditFiles — true only from Nusga / No file
- Section submenu always opens inline PDF preview (never web empty + Upload)