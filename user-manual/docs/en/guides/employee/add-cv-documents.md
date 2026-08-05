---
title: Add CV and personal files
slug: employee/add-cv-documents
locale: en
tier: 2
guideStatus: draft
bo: Person
personRole: Employee
navPath: Employee
roles: [Visa Officer]
prerequisiteSlugs:
  - employee/register
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-cv-documents.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Add CV and personal files

This guide shows how to attach **CV and personal files** on an existing **employee**. Files are stored on the employee detail form under the **CV & personal files** tab in **Person record data**.

Use this tab for general employee documents (for example a CV or personal certificate). **Passport copies**, **diploma copies**, and other scans belong on their own record tabs (passport, education, medical record, and so on) — not here.

Family members use **Family relation documents** instead; they do not have this tab.

!!! tip "Prerequisites"
    The employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Tab labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-add-cv-documents.mp4"
  title="Add CV and personal files in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| A file to upload | PDF or image scan (see allowed types below) |
| File size within office limit | Maximum size comes from system settings (often **5 MB** per file) |

**Allowed file types:** PDF, PNG, JPEG, TIFF, GIF, or BMP. The file content must match the extension.

## Step 1 — Open the employee

1. [Find and open the employee](../person/open-and-search.md).
2. Wait for the employee **detail form** to load.

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-cv-documents-step-01-employee-detail.png)

## Step 2 — Open the CV & personal files tab

1. In **Person record data**, select the **CV & personal files** tab.
2. Wait for the nested list to load.

The tab may appear after **Educations** and **Passports** on the employee view.

## Step 3 — Start a new file row

1. On the nested list toolbar, select **New** (or **New Person Document**).
2. Wait for the document **detail form** to open.

![New document detail form](../../../assets/screenshots/v2026.08/en/person-add-cv-documents-step-02-document-form-new.png)

## Step 4 — Attach the file

1. In the **File** field, choose **Browse** (or drag and drop if your browser supports it).
2. Select the CV or personal file from your computer.
3. Wait until the upload finishes and the file name appears on the form.

![File attached before save](../../../assets/screenshots/v2026.08/en/person-add-cv-documents-step-03-document-file-attached.png)

## Step 5 — Save the document row

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

After save, the **File** field should show the uploaded file name.

![Document saved](../../../assets/screenshots/v2026.08/en/person-add-cv-documents-step-04-document-saved.png)

!!! success "File added"
    The row appears on the **CV & personal files** tab for this employee.

## Step 6 — Confirm on the CV & personal files tab

1. Return to the employee detail form if needed.
2. Open the **CV & personal files** tab again.
3. Confirm your file row appears in the nested list.

You can add **multiple** rows — one file per row (for example separate CV and certificate files).

## Preview all person scans (optional)

To preview attachments from **all** person tabs in one place (passports, education, this tab, and more), use **Document copies** on the employee detail toolbar. That view is for browsing — ministry ZIP packaging still uses **Application item** document copies.

## Common problems

| Problem | What to do |
|---------|------------|
| **CV & personal files** tab missing | Confirm the person is an **employee**, not a family member or temporary visitor |
| **File type is not allowed** | Use PDF, PNG, JPEG, TIFF, GIF, or BMP; rename if the extension does not match the content |
| **File exceeds maximum size** | Compress or split the scan; ask an administrator if the office limit must change |
| **Empty file error** | Re-select a non-zero-byte file |

## What to read next

- [Add a travel history](add-travel.md)
- [Update employee details](edit-employee.md)
- [Add an education record](add-education.md) — use **Diploma copies** on the education record for diploma scans
