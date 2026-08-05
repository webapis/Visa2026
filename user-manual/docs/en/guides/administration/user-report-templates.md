---
title: User report templates
slug: administration/user-report-templates
locale: en
tier: 7
guideStatus: draft
bo: UserReportTemplate
navPath: Reports / User Report Template
roles: [Administrator]
prerequisiteSlugs:
  - applications/resminamalar
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: user-report-templates.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/APPLICATION_REPORT_PACKAGE.md
  - docs/TEMPLATE_STAGING_EDIT.md
---

# User report templates

This guide is for **administrators** who maintain **Word** and **Excel** report layouts that officers use from **Templates** (Resminamalar) on applications.

Each layout is stored as a **User Report Template** record under **Reports** in the left menu. Officers only see templates that are **active** and match the application type, scope, and visibility rules you configure.

!!! tip "Prerequisites"
    Officers use templates from [Templates report package (Resminamalar)](../applications/resminamalar.md). Read that guide first so you understand **application** vs **application item** scope and readiness chips.

!!! warning "Administrator task"
    Most visa officers do not need this screen. Changing templates affects every future ZIP and preview for that report.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/user-report-templates.mp4"
  title="User report templates in Visa2026"></video>

<p class="visa-manual-video-caption">Recording placeholder — steps below match the template maintenance form.</p>

## Before you start

| Concept | Meaning |
|---------|---------|
| **User Report Template** | One Word (`.docx`) or Excel (`.xlsx`) file plus visibility settings |
| **Placeholder** | Token in the file (for example `{{Application.Number}}`) filled from Visa2026 data |
| **Root Business Object** | Whether the report merges **Application**, **Application item**, or **Person** data |
| **Templates** (officer UI) | Catalog on an application — lists active templates that pass your rules |

| Root Business Object | Typical reports |
|----------------------|-----------------|
| **Application** | Cover letters, header-only ministry letters |
| **Application item** | Per-person Word forms, single-line Excel exports |
| **Person** | Person-centric letters (rare; most reports use application scope) |

## Step 1 — Open User Report Templates

1. Sign in with an account that can edit templates.
2. In the left menu, expand **Reports**.
3. Select **User Report Template**.

The list shows **Template Name**, **Is Active**, **Output Format**, and **Validation Status**.

![User Report Templates list](../../../assets/screenshots/v2026.08/en/user-report-templates-step-01-list.png)

## Step 2 — Create or open a template record

**New template:**

1. Select **New** on the list toolbar.
2. Enter **Template Name** (officers see this in the Templates catalog).
3. Optional: **Description** for administrators.

**Existing template:**

1. Double-click a row to open the detail form.

You can also **Clone** an existing template when your office needs a variant (name gets a copy suffix).

## Step 3 — Attach the template file

1. On **Template File**, select **Choose file** (or the paperclip control).
2. Upload a `.docx` or `.xlsx` from your workstation.
3. Set **Output Format** to **Word** or **Excel** to match the file.

For **Excel** templates, set **Excel Merge Mode**:

| Mode | Use when |
|------|----------|
| **ItemList** | One workbook with a table row per selected application item |
| **SingleItem** | One workbook per application item line |

4. Select **Save**.

## Step 4 — Set scope and visibility

1. **Root Business Object** — must match how officers open **Templates** (application detail vs application items list).
2. **Applicable Application Types** — optional rows; leave empty with empty **Applicable Application Type Groups** to allow **all** application types.
3. **Applicable Application Type Groups** — optional (for example Registration); combined with types as a union.
4. **Applicable Project Contracts** — optional filter by contract (hidden when root is **Person**).
5. **Visibility Criteria** — optional extra filter (advanced; leave empty unless IT documented a rule).
6. **Sort Order** — lower numbers appear earlier in the officer catalog.
7. **Is Active** — clear to hide from **Templates** without deleting the record.

Select **Save** after changes.

![Template detail form](../../../assets/screenshots/v2026.08/en/user-report-templates-step-02-detail.png)

## Step 5 — Extract placeholders

After the file is attached:

1. On the detail toolbar, select **Extract Placeholders**.
2. Wait for the success message with the placeholder count.

Visa2026 reads tokens from the Word or Excel file and stores them on the template. Run **Extract Placeholders** again whenever you upload a new file version.

## Step 6 — Validate placeholders

1. Select **Validate Placeholders** on the toolbar.
2. Read the result message — all valid, or a count of invalid tokens.
3. Check **Validation Status** on the form (for example `All N placeholders valid`).

Invalid placeholders usually mean a typo in the file or a token that does not exist for the chosen **Root Business Object**. Fix the desktop file, re-upload, then **Extract** and **Validate** again.

## Step 7 — Placeholder manual (reference)

1. Select **Placeholder manual** on the toolbar.
2. Browse allowed tokens in the preview panel — filtered by scope when possible.
3. Copy the exact token text into Word or Excel when authoring layouts.

Use the manual instead of guessing field names.

## Step 8 — Verify in Templates (officer path)

Before officers rely on a new template:

1. Open a test **Application** (or **Application items** list) that should show the template.
2. Select **Templates** on the toolbar.
3. Confirm the template appears with the correct **Ready** / **Check** chip.
4. **Preview** one row, then **Download package** on a test ZIP if needed.

See [Templates report package (Resminamalar)](../applications/resminamalar.md).

To edit the `.docx` / `.xlsx` from the catalog without re-uploading manually, use [Edit and sync templates (desktop)](template-staging.md).

## Common problems

| Problem | What to do |
|---------|------------|
| Template missing in officer catalog | **Is Active**; check application type / group / contract links; **Visibility Criteria** |
| Wrong scope (header vs lines) | Fix **Root Business Object**; officers must open **Templates** from the matching screen |
| All placeholders invalid | **Extract Placeholders**; open **Placeholder manual**; fix tokens in the file |
| Excel export wrong shape | Check **Excel Merge Mode** (**ItemList** vs **SingleItem**) |
| Officers see **Check** on every preview | Usually missing application or person data — not always a template defect |
| No **Extract** / **Validate** | Your role may lack template edit permission — ask IT |
| Seeded templates from install | Do not delete without backup; clone and adjust instead |

## What to read next

- [Edit and sync templates (desktop)](template-staging.md) — Word/Excel on the PC, **Sync to database**
- [Templates report package (Resminamalar)](../applications/resminamalar.md) — officer ZIP workflow
- [What Visa2026 does](../../about/capabilities.md) — feature #7 Templates