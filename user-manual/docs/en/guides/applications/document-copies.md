---
title: Ministry document copies (PDF package)
slug: applications/document-copies
locale: en
tier: 5
guideStatus: draft
bo: ApplicationItem
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
  - docs/APPLICATION_ITEM_DOCUMENT_COPIES.md
---

# Ministry document copies (PDF package)

This guide shows how to use **Document copies** on **application item** lines — the officer workflow for checking attached scans, previewing documents, and downloading a **ministry PDF package** (filled application forms plus supporting files in one ZIP).

Document copies replaces the older **Generate PDF** / **My PDF Jobs** buttons. The same background job builds the ZIP; the dialog adds **readiness**, **preview**, and **gap confirmation** before you queue the package.

!!! tip "Prerequisites"
    Saved application with person lines ([Create an application](create.md), [Add application items](add-items.md)). Linked passports, visas, and other scans should be on the person records before packaging.

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| **Application item** line(s) | One or more rows on a saved application |
| Linked documents on each person | Passport, visa, medical, education, etc. — depends on application type |
| **Visa Officer** role with document copies access | Ask your supervisor if the action is missing |

| Where to open the list | Menu |
|------------------------|------|
| Ministry-route items | **Application items (ministry)** |
| Direct-migration items | **Application items (migration)** |
| From the application header | **Application items** tab on the detail form |

See [Applications — ministry and direct migration](overview.md) if you are unsure which list to use.

## Step 1 — Open an application items list

1. Sign in to Visa2026.
2. Open **Application items (ministry)** or **Application items (migration)** — or open an **Application** and select the **Application items** tab.
3. Find the line(s) for your case (filter by application number, person name, or date).

![Application items list](../../../assets/screenshots/v2026.08/en/application-document-copies-step-01-items-list.png)

## Step 2 — Select application item line(s)

1. Select **one or more rows** in the list (checkboxes or multi-select).
2. Confirm each row is the correct person on the correct application.

You can package **multiple lines** in one job when they share the same export scope (for example several people on one application).

## Step 3 — Open Document copies

1. On the list toolbar, select **Document copies**.
2. Wait for the panel to open in the **preview area** on the right (same shell as **Templates** / Resminamalar).

The action is enabled only when at least one line is selected.

![Document copies panel](../../../assets/screenshots/v2026.08/en/application-document-copies-step-02-panel.png)

## Step 4 — Review document readiness

The catalog has two sections:

| Section | What it shows |
|---------|----------------|
| **Linked documents** | Scan slots (passport, visa, work permit, medical record, education, …) with **Files**, **Status**, and **Preview** per slot |
| **Application form** | Filled ministry application form — always listed **last** |

| Status (typical) | Meaning |
|------------------|---------|
| Ready | File attached for all selected lines |
| Partial | Some lines missing this scan |
| Missing | No file for this slot |

1. Scan the **Status** column for gaps before you export.
2. Optional: select the **gear** (show details) to see file names, sizes, and per-line breakdown.

!!! tip "Fix gaps first"
    If a slot is **Missing**, return to the person record or application item and attach the scan ([Add a passport](../employee/add-passport.md), etc.), then select **Refresh** in the document copies footer.

## Step 5 — Preview a scan slot (optional)

1. On a **Linked documents** row, select **Preview**.
2. Wait while Visa2026 builds the merged PDF (a short progress indicator may appear on the row).
3. Read the document in the preview panel.

From the preview header you can **Download** the merged PDF or open a **Batch summary** when multiple lines are selected.

!!! note "Application form row"
    **Preview** on **Application form** does **not** open the preview panel — it **downloads** the filled form directly (PDF or a small ZIP when several lines are selected).

## Step 6 — Set package options (optional)

1. In the footer, select **Package options** to expand the panel.
2. Adjust include flags (which document types go into the ZIP, diploma scope, merged diploma mode, etc.).
3. Defaults match a full ministry supporting-document package — change only when your office procedure requires it.

Most officers use defaults and skip this step.

## Step 7 — Download package

1. Select **Download package** in the footer.
2. If some **included** slots are partial or missing, Visa2026 shows a **gap confirmation** — read it carefully.
   - **Cancel** and fix attachments, or
   - **Continue** if your office allows exporting with known gaps.
3. Wait until the job is **queued**.

A **PDF generation** toast appears (bottom of the screen). It tracks the background job — the dialog does **not** show a progress bar in the footer.

![PDF generation toast](../../../assets/screenshots/v2026.08/en/application-document-copies-step-03-toast.png)

## Step 8 — Download the ZIP from the toast

1. Watch the **PDF generation** toast until status shows complete.
2. Select **Download ZIP** (or equivalent) on the toast.
3. Save the file to your workstation and unzip if needed.

The ZIP typically contains:

- Filled **application forms** per line (`PDF_Form/` folder when multiple)
- Supporting **scans** merged or grouped per package options
- **PACKAGING_NOTES.txt** when the worker records warnings or skipped slots

!!! success "Package ready"
    Use the ZIP for ministry submission or internal archive. Re-open **Document copies** and **Refresh** if you attached new scans after queuing.

## Step 9 — Refresh after changes

If you add or replace scans on a person or application item:

1. Return to **Document copies** on the same selection.
2. Select **Refresh** in the footer.
3. Confirm **Status** turns **Ready** before you queue again.

## Common problems

| Problem | What to do |
|---------|------------|
| **Document copies** disabled | Select at least one application item row |
| Action missing | Role may not allow packaging — ask supervisor |
| Slot always **Missing** | Attach scan on person or item; **Refresh** |
| Preview fails | File may be corrupt or wrong type — re-upload scan (PDF/image) |
| Application form empty/wrong fields | Person data or PDF mapping issue — ask administrator; scans may still package |
| Toast never completes | Wait; check network; ask IT if job stays queued — job history remains in the database |
| Wrong list (ministry vs migration) | Open items from the route that matches the parent application |

Document copies is for **application item** ministry PDF packages. To browse scans on the **person master record**, see [Person document copies](../person/document-copies.md).

## What to read next

- [Add application items](add-items.md) · [Track application progress](progress.md)
- [Applications — ministry and direct migration](overview.md)
- **Resminamalar report package** — [Templates report package (Resminamalar)](resminamalar.md) — Word/Excel templates on applications
- [What Visa2026 does](../../about/capabilities.md) — document copies in the feature overview