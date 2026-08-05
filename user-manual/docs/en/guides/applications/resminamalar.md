---
title: Templates report package (Resminamalar)
slug: applications/resminamalar
locale: en
tier: 5
guideStatus: draft
bo: Application
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - applications/overview
  - applications/create
  - applications/add-items
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/APPLICATION_REPORT_PACKAGE.md
---

# Templates report package (Resminamalar)

This guide shows how to use **Templates** (report package / **Resminamalar**) — the officer workflow for generating **Word and Excel reports** from user templates, previewing them, and downloading a **ZIP package**.

Templates replaces the older one-click **Resminamalar** queue. The same background job builds the ZIP; the panel adds a **catalog**, **readiness checks**, **selection**, and **preview** before you download.

!!! tip "Prerequisites"
    Saved application ([Create an application](create.md)) with person lines when templates need item data ([Add application items](add-items.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). The toolbar button is **Templates**; internal name Resminamalar.

## Before you start

| Concept | Officer meaning |
|---------|-----------------|
| **Templates** | Toolbar action that opens the report package in the **preview panel** (right side) |
| **User report template** | Office Word (`.docx`) or Excel (`.xlsx`) layout maintained by administrators |
| **Application scope** | Templates that merge data from the **application header** only |
| **Application item scope** | Templates that merge data from **selected person lines** on the application |

| Entry point | Where | Templates shown |
|-------------|-------|-----------------|
| **Application** detail | Toolbar **Templates** | Application-scoped reports |
| **Application items** list | Select line(s) from the **same** application → **Templates** | Item- or person-scoped reports |

For ministry **PDF** scans and forms, use [Ministry document copies (PDF package)](document-copies.md) instead — different ZIP engine.

## Step 1 — Open an application (application scope)

1. Open **Applications (via ministry)** or **Applications (direct migration)**.
2. Open the application **detail form**.
3. Confirm header fields and **Application items** are complete for the reports you need.

![Application detail — Templates action](../../../assets/screenshots/v2026.08/en/application-resminamalar-step-01-app-detail.png)

## Step 2 — Open Templates on the application

1. On the application detail toolbar, select **Templates**.
2. Wait for the **preview panel** to open on the right (same shell as **Document copies**).

If the catalog is **empty**, no application-scoped template applies to this application type — try **Application item** scope (step 8) or ask your administrator.

![Templates catalog](../../../assets/screenshots/v2026.08/en/application-resminamalar-step-02-catalog.png)

## Step 3 — Review the template catalog

Each card shows:

| Element | Meaning |
|---------|---------|
| **Checkbox** | Include this report in the ZIP (checked rows only) |
| **Ready** / **Check** chip | **Ready** = no warnings; **Check** = review hint (missing data, empty rows, etc.) |
| **Preview** | Generate and view PDF in the panel |
| Format badge | Word or Excel |

1. Read **Check** warnings before you export.
2. Use **Select all** or **Clear selection** in the footer when needed.
3. Optional: turn on the **gear** to show **Edit template** (only when your office enables desktop template editing and you have permission). See [Edit and sync templates (desktop)](../administration/template-staging.md).

## Step 4 — Preview a template (optional)

1. Select **Preview** on a catalog row.
2. Wait while Visa2026 generates the report and converts it to PDF for viewing.
3. In the preview header, you can **Download Word/Excel** or **Download PDF**.

Preview uses the **same merge** as the ZIP — if preview looks wrong, the ZIP will match.

To return to the catalog, select **Close** in the preview area.

## Step 5 — Select reports for the package

1. Check the templates you want in the ZIP.
2. At least **one** row must be checked before **Download package** is enabled.

Uncheck reports you do not need — the ZIP contains **checked rows only** (unlike the old one-click Resminamalar).

## Step 6 — Download package

1. Select **Download package** in the footer.
2. If checked rows show **Check** warnings, Visa2026 may ask you to **confirm** — read the message.
   - **Cancel** and fix data on the application or person records, or
   - **Continue** if your office allows exporting with known gaps.
3. Wait until the job is **queued**.

A **report generation** toast appears (bottom of screen) and tracks the background job.

![Report generation toast](../../../assets/screenshots/v2026.08/en/application-resminamalar-step-03-toast.png)

## Step 7 — Download the ZIP from the toast

1. Watch the toast until the job completes.
2. Select **Download ZIP** on the toast.
3. Unzip on your workstation — files are generated Word/Excel reports from the selected templates.

!!! success "Package ready"
    File names usually include application context. Re-open **Templates** and **Refresh** after you change application or person data.

## Step 8 — Templates on application items (item scope)

Use this when templates list people on the application (cover letters per line, Excel lists, etc.):

1. Open **Application items (ministry)** or **Application items (migration)** — or the **Application items** tab on the application.
2. Select **one or more rows** from the **same** application.
3. Select **Templates** on the list toolbar.
4. Follow steps 3–7 for the **item-scoped** catalog.

| Scope | ZIP behaviour (typical) |
|-------|-------------------------|
| Word per person | One file per selected application item line |
| Excel list | One spreadsheet with a row per selected line |

Preview for per-item Word templates uses the **first selected line**; Excel list preview uses all selected lines.

## Step 9 — Refresh after data changes

1. Update application header or person lines as needed.
2. In the Templates panel, select **Refresh**.
3. Confirm **Ready** / **Check** chips update before you queue again.

**Sync to database** (footer) is for administrators who edit template files on the desktop — not for refreshing application data. See [Edit and sync templates (desktop)](../administration/template-staging.md).

## Common problems

| Problem | What to do |
|---------|------------|
| **Templates** disabled or missing | No applicable templates for this type/scope — ask administrator |
| Empty catalog | Wrong scope (try Application vs Application items); type may have no templates |
| **Check** on every row | Fill missing application or person fields; see warning text |
| Preview fails | Same as ZIP merge — fix data or template; ask administrator if placeholder errors persist |
| Item scope error | All selected lines must belong to the **same** application |
| Toast never completes | Wait; check network; ask IT if job stays queued |
| Confused with PDF package | **Document copies** = ministry PDF/scans; **Templates** = Word/Excel reports |

## What to read next

- [User report templates](../administration/user-report-templates.md) · [Edit and sync templates (desktop)](../administration/template-staging.md) — administrators
- [Ministry document copies (PDF package)](document-copies.md) — PDF scan ZIP
- [Track application progress](progress.md) · [Add application items](add-items.md)
- [Applications — ministry and direct migration](overview.md)
- [What Visa2026 does](../../about/capabilities.md) — Templates in the feature overview