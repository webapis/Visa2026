---
title: Add family relation documents
slug: family-member/add-family-relation-documents
locale: en
tier: 2
guideStatus: draft
bo: Person
personRole: FamilyMember
navPath: FamilyMember
roles: [Visa Officer]
prerequisiteSlugs:
  - family-member/register
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-cv-documents.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Add family relation documents

This guide shows how to attach **family relation documents** on an existing **family member**. Files are stored on the family member detail form under the **Family relation documents** tab in **Person record data**.

Use this tab for marriage certificates, birth certificates, and other proofs of the family relationship. **Passport copies** and **medical scans** belong on their own record tabs — not here.

Employees use **CV & personal files** instead; temporary visitors do not have this tab.

!!! tip "Prerequisites"
    The family member must already exist ([Register a family member](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Tab labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-add-cv-documents.mp4"
  title="Add family relation documents in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The tab name differs; upload steps are the same.</p>

## Before you start

| You need | Notes |
|----------|--------|
| An existing **family member** record | [Register a family member](register.md) first |
| A file to upload | PDF or image scan |
| File size within office limit | Often **5 MB** per file |

**Allowed file types:** PDF, PNG, JPEG, TIFF, GIF, or BMP.

## Step 1 — Open the family member

1. [Find and open the family member](../person/open-and-search.md).
2. Wait for the **family member** detail form to load.

![Family member detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-family-relation-documents-step-01-detail.png)

## Step 2 — Open the Family relation documents tab

1. In **Person record data**, select the **Family relation documents** tab.
2. Wait for the nested list to load.

## Step 3 — Start a new document row

1. On the nested list toolbar, select **New** (or **New Family Relation Document**).
2. Wait for the document **detail form** to open.

![New family relation document form](../../../assets/screenshots/v2026.08/en/person-add-family-relation-documents-step-02-form-new.png)

## Step 4 — Attach the file

1. In the **File** field, choose **Browse**.
2. Select the certificate or scan from your computer.
3. Wait until the file name appears on the form.

![File attached before save](../../../assets/screenshots/v2026.08/en/person-add-family-relation-documents-step-03-file-attached.png)

## Step 5 — Save the document row

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

![Document saved](../../../assets/screenshots/v2026.08/en/person-add-family-relation-documents-step-04-saved.png)

!!! success "Document added"
    The row appears on the **Family relation documents** tab for this family member.

## Step 6 — Confirm on the tab

1. Return to the family member detail form if needed.
2. Open the **Family relation documents** tab again.
3. Confirm your file row appears in the nested list.

You can add **multiple** rows — one file per row.

## Common problems

| Problem | What to do |
|---------|------------|
| Tab missing | Confirm the person is a **family member**, not an employee or temporary visitor |
| **File type is not allowed** | Use PDF, PNG, JPEG, TIFF, GIF, or BMP |
| **File exceeds maximum size** | Compress the scan or ask an administrator |

## What to read next

- [Add a passport](add-passport.md)
- [Update family member details](edit-family-member.md)
- [Mark incomplete or complete](../person/mark-incomplete.md)