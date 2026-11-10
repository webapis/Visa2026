# Document copies — profile-required filter + missing preview upload

Filter People **Preview document copies** and left-nav **Document copies** to Application Profile **Required person-related data** (`RequirePerson*`). Required sections with no scan stay clickable (quiet **No file** chip). Preview shows an empty state with **Upload file** — no loud submenu warning banner.

**Status:** implemented.

## Locked decisions

| Decision | Choice |
|----------|--------|
| Scope | People submenu **and** left-nav Document copies catalog |
| Menu rows | Required ∩ profile; ready rows normal; empty required rows clickable + **No file** chip |
| Missing remediation | Preview empty state + Upload (open linked-record editor) |
| Submenu warning banner | **No** |
| Unmapped | Border zone / Salary / Travel / Person documents / Family relation hidden here |
| Visa | Person submenu under Passports; AppItem catalog keeps Passport + Visa families separate |

## Screen

| File | Notes |
|------|--------|
| [case-workspace-doc-copies-missing-preview-upload-prototype.jpg](./case-workspace-doc-copies-missing-preview-upload-prototype.jpg) | Submenu with No file chips; Medical preview empty state + Upload |

## Implementation notes

- Allow-list: `PersonDocumentCopiesProfileSectionFilter`
- People submenu: `OfficerShellCaseWorkspaceComponent.BuildDocumentSectionMenuItems`
- Left-nav: `ApplicationItemDocumentCopiesTypeCatalog.Build(..., AllowedFamilyKeys)` via `OfficerShellCaseDocumentsTab`
- Missing preview: `PersonDocumentCopiesSlotPanel` / `DocumentCopiesSlotPanel` empty state + `LinkedRecordDetailOpenHelper.ShowNewEditor`