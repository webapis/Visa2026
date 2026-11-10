# People & Links — document Upload in table + empty preview (no No file chip)

Rework of People **Preview document copies** file UX. Submenu never shows a **No file** chip. Missing scans use preview empty state; upload starts from the people table row (or empty preview) into the right `#visa-preview-slot` multi-file uploader for that person's existing linked BO.

**Status:** implemented.

## Locked decisions

| Decision | Choice |
|----------|--------|
| Submenu **No file** chip | **Remove always** |
| Upload target | The **row** whose **Upload** was clicked (not merge of checked rows, not person chip) |
| Upload control location | People table beside Passport / Education / Position / Address (wherever that section's files are stored today) |
| Empty preview copy | Exact: **File is not uploaded yet** (+ Upload on that page) |
| BO prerequisite | Upload **only if** the linked Education / Position / Address / Passport row already exists |
| Multi-file | Several PDF and/or image files attached to that **one** existing document BO |
| Scope | **People & Links** submenu / table only — not left-nav Document copies |

## Screen

| File | Notes |
|------|--------|
| [case-workspace-people-doc-upload-prototype.jpg](./case-workspace-people-doc-upload-prototype.jpg) | Submenu without No file; table Upload links; empty preview + multi-file uploader in right slot |

## Out of scope (this prototype)

- Left-nav Document copies catalog chips / empty states
- Creating a missing BO from the upload flow
- Changing multi-person Passports merge preview behavior beyond removing the No file chip from the People submenu