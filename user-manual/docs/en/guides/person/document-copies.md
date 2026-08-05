---
title: Person document copies
slug: person/document-copies
locale: en
tier: 6
guideStatus: draft
bo: Person
navPath: Person
roles: [Visa Officer, Visa Chief]
prerequisiteSlugs:
  - person/open-and-search
  - getting-started/navigation
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-document-copies.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/PERSON_DOCUMENT_COPIES.md
---

# Person document copies

This guide shows how to browse and **preview person scans** from one catalog — passports, visas, education, medical records, addresses, and more — without opening each tab on the person detail form.

**Person document copies** is for **master person records** (live data on **Employees**, **Family Members**, **Temporary visitor**). It is **not** the ministry **PDF ZIP** on application items — use [Ministry document copies (PDF package)](../applications/document-copies.md) for that.

!!! tip "Prerequisites"
    A saved person record ([Find and open a person](open-and-search.md)). Attach scans on nested tabs first ([Add a passport](../employee/add-passport.md), etc.).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). The detail toolbar says **Person document copies**; the dossier button says **Document copies**.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-document-copies.mp4"
  title="Person document copies in Visa2026"></video>

<p class="visa-manual-video-caption">Recording placeholder — steps below match the person document copies flow.</p>

## Before you start

| Concept | Officer meaning |
|---------|-----------------|
| **Preview panel** | Right side of the screen (same shell as **Templates** and application **Document copies**) |
| **Sectioned catalog** | Groups by document family — passports (with nested visas), education, medical, … |
| **Current** badge | Marks the passport or visa that matches today's **current** rules |
| **Ready** / **Missing** | Row has attached scan files, or not |

| Entry point | Control | Label in UI |
|-------------|---------|-------------|
| Person **detail form** | Toolbar | **Person document copies** |
| Person **list** | Second column (after **Dossier**) | **•** link (paperclip column) |
| **Person dossier** | Toolbar | **Document copies** |

There is **no** list-toolbar action for person copies — use the **•** column per row.

## Step 1 — Open person document copies

Choose one path:

### From the person detail form

1. Open **Employees**, **Family Members**, or **Temporary visitor**.
2. Open a **saved** person (detail form).
3. On the toolbar, select **Person document copies**.
4. Wait for the **preview panel** on the right.

![Person document copies toolbar](../../../assets/screenshots/v2026.08/en/person-document-copies-step-01-detail-toolbar.png)

### From a person list

1. Open the person list.
2. In the column after **Dossier**, select the **•** link on the row.
3. The preview panel opens for that person only.

![Copies link on person list](../../../assets/screenshots/v2026.08/en/person-document-copies-step-03-list-column.png)

### From the person dossier

1. Open the [Person dossier](dossier.md).
2. On the dossier toolbar, select **Document copies**.
3. The dossier stays on the left; the catalog opens on the right.

See [Person dossier](dossier.md) step 4 for the combined review workflow.

## Step 2 — Read the catalog header

The panel header shows the **person name** and optional **Personal Number**.

Sections appear only when the person has records in that family (empty groups are hidden).

| Section (typical) | Employee | Family member |
|-------------------|----------|---------------|
| Passports (visas nested) | Yes | Yes |
| Education, medical, addresses | Yes | Yes |
| Work permits, invitations, **Person files** | Yes | No |
| **Family relation documents** | No | Yes |
| Rejections | When records exist | When records exist |

## Step 3 — Browse sections

1. Scroll the section list on the left of the catalog card.
2. Select a section to expand or collapse it.
3. Each row is one child record (one passport, one education entry, one visa under a passport, …).
4. Read **Files** (count), **Status** (**Ready** / **Missing**), and the **Current** badge where shown.

By default, passport and visa sections may show **current** records first. Use **Show all documents** in the footer to include historical passports and visas; **Show current only** returns to the shorter list.

![Sectioned catalog](../../../assets/screenshots/v2026.08/en/person-document-copies-step-02-catalog.png)

## Step 4 — Show file details (optional)

1. In the footer, select the **gear** icon.
2. File names and sizes appear under rows that have attachments.
3. Select the gear again to hide details.

## Step 5 — Preview a record

When a row is **Ready**:

1. Select **Preview** on the row, **or** click the row itself.
2. Wait while Visa2026 builds the merged PDF (progress on the row).
3. Read the document in the preview area (full panel width).

From the preview header:

- **Download** — save the merged PDF to your PC
- **Close** — return to the catalog

![Preview in panel](../../../assets/screenshots/v2026.08/en/person-document-copies-step-04-preview.png)

Rows with **Missing** have no **Preview** — attach scans on the person detail form first.

## Step 6 — Refresh after changes

1. Attach or replace scans on the person **detail form** (passport tab, education tab, …).
2. In the person document copies footer, select **Refresh**.
3. Confirm **Ready** / **Missing** and file counts update.

**Refresh** reloads from the database — it does **not** upload files.

## Step 7 — Close the panel

1. Select **Close** on the preview panel header (when not in inline preview), or close the occupant the same way as **Templates**.
2. Continue editing the person on the detail form if needed.

There is **no Download package** button on person copies in the current release — ministry ZIP export remains on [application items](../applications/document-copies.md).

## Common problems

| Problem | What to do |
|---------|------------|
| **Person document copies** disabled | Open a **saved** person — new unsaved records may not qualify |
| Empty catalog | No child records with document rows yet — add passport, education, etc. |
| Section missing | Role rules (family member vs employee) or no records in that family |
| **Missing** on a row | Upload scan on the matching person tab; **Refresh** |
| Preview fails | File may be corrupt or wrong type — re-upload scan |
| Confused with application **Document copies** | Application items = ministry PDF ZIP; this guide = person master-data browse |
| List has no toolbar action | Use the **•** column per row (by design) |
| Need ZIP for ministry | [Ministry document copies](../applications/document-copies.md) on application items |

## What to read next

- [Person dossier](dossier.md) — read-only summary + **Document copies** beside dossier
- [Ministry document copies (PDF package)](../applications/document-copies.md) — application-item ZIP
- [Find and open a person](open-and-search.md) — lists and **Dossier** column
- [What Visa2026 does](../../about/capabilities.md) — feature #10