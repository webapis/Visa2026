---
title: Edit and sync templates (desktop)
slug: administration/template-staging
locale: en
tier: 7
guideStatus: draft
bo: UserReportTemplate
navPath: Templates (Resminamalar)
roles: [Administrator]
prerequisiteSlugs:
  - administration/user-report-templates
  - applications/resminamalar
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/TEMPLATE_STAGING_EDIT.md
  - docs/APPLICATION_REPORT_PACKAGE.md
---

# Edit and sync templates (desktop)

This guide shows how administrators edit **Word** or **Excel** template files on the **local PC** from the officer **Templates** panel, then **Sync to database** so all users get the updated layout.

You do **not** need to open **User Report Template** in the left menu for every text change — use this flow when desktop Word/Excel is easier than upload + **Extract Placeholders** on the detail form.

!!! tip "Prerequisites"
    [User report templates](user-report-templates.md) (record setup) and [Templates report package (Resminamalar)](../applications/resminamalar.md) (how officers open the catalog).

!!! warning "Requirements"
    - **Microsoft Edge** or **Google Chrome** on Windows
    - **HTTPS** in production (or `localhost` for development)
    - Desktop **Word** and/or **Excel**
    - Template edit permission (same gate as **User Report Template** maintenance)

## Before you start

| Footer control | Purpose |
|----------------|---------|
| **Change template folder** | One-time pick of a writable folder on your PC (browser remembers it) |
| **Edit template** (per catalog row, after **gear**) | Download that template to the folder; open Word/Excel |
| **Sync to database** | Upload changed files from the folder back to Visa2026 |
| **Refresh** | Reload readiness chips only — **not** an import |

Default folder path (paste in the picker address bar):

`%LOCALAPPDATA%\Visa2026\TemplateEdit`

Example: `C:\Users\<you>\AppData\Local\Visa2026\TemplateEdit`

## Step 1 — Open Templates on an application

1. Open any **Application** detail (or **Application items** list with rows selected) where the template you need appears.
2. Select **Templates** on the toolbar.
3. Wait for the catalog in the preview panel.

![Templates catalog with gear](../../../assets/screenshots/v2026.08/en/template-staging-step-01-catalog-gear.png)

## Step 2 — Set up the template folder (once per browser)

1. In the catalog footer, select **Change template folder**.
2. In the Windows folder picker **address bar**, paste: `%LOCALAPPDATA%\Visa2026\TemplateEdit`
3. Press **Enter** — create `Visa2026` and `TemplateEdit` if Windows asks.
4. With **TemplateEdit** selected, click **Select Folder**.
5. Allow **write** access when the browser prompts.

!!! tip "Protected folders"
    Do **not** pick Desktop, Documents, or Downloads — the browser blocks them. Use the AppData path above.

If your office already configured a folder, the button may show the current path; use **Change template folder** again only when moving to a new PC or browser profile.

## Step 3 — Show Edit template (gear)

1. In the catalog footer, select the **gear** icon to show extra row actions.
2. Confirm **Edit template** appears on rows you are allowed to change.

If **Edit template** is missing, template staging may be disabled for your server, or you lack edit permission — ask IT.

## Step 4 — Edit template on a row

1. Select **Edit template** on the catalog row.
2. Wait until Visa2026 downloads the file into your template folder (status message in the panel).
3. Word or Excel opens automatically when Windows allows it.

If Office blocks opening from the browser, open the file manually from `%LOCALAPPDATA%\Visa2026\TemplateEdit`.

4. Edit the document — change wording or placeholders only as your office standards allow.
5. **Save** and **Close** Word or Excel on your PC.

The file remains in the local folder until you sync.

## Step 5 — Sync to database

1. Make sure Word/Excel is **fully closed** (not locking the file).
2. In the Templates footer, select **Sync to database**.
3. If prompted, confirm the changed file(s).
4. Wait for the success message.

Visa2026 replaces the stored template file and runs **Extract Placeholders** and **Validate Placeholders** when the file hash changed.

5. Select **Refresh** in the catalog and check **Ready** / **Check** chips.
6. **Preview** the row to confirm the merge looks correct.

![Edit and sync footer](../../../assets/screenshots/v2026.08/en/template-staging-step-02-edit-sync.png)

## Step 6 — Verify for all officers

1. Ask another user (or a test account) to open **Templates** on the same application type.
2. Confirm **Preview** and a test **Download package** use the updated layout.

Changes apply to the **database copy** — not to other officers' local folders until they **Edit template** again.

## Common problems

| Problem | What to do |
|---------|------------|
| "Choose a template folder first" | Run **Change template folder** (Step 2) |
| Browser blocks the folder | Use `%LOCALAPPDATA%\Visa2026\TemplateEdit` in the address bar |
| Word "unsafe content" / won't open | IT may run Office trust script for your Visa2026 HTTPS URL; or open file from AppData manually |
| Sync says file still open | Close Word/Excel completely; retry **Sync to database** |
| No changes imported | You edited a different copy — use **Edit template** from the row, save, then sync that file |
| **Refresh** did not import | Expected — only **Sync to database** uploads |
| Invalid placeholders after sync | Fix tokens using [Placeholder manual](user-report-templates.md#step-7-placeholder-manual-reference) on the **User Report Template** form |
| Buttons hidden | Staging disabled on server or no HTTPS — contact IT |

## What to read next

- [User report templates](user-report-templates.md) — create records, visibility, **Extract** / **Validate** on the detail form
- [Templates report package (Resminamalar)](../applications/resminamalar.md) — officer ZIP workflow
- [What Visa2026 does](../../about/capabilities.md)