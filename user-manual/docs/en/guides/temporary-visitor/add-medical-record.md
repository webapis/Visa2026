---
title: Add a medical record (temporary visitor)
slug: temporary-visitor/add-medical-record
locale: en
tier: 2
guideStatus: draft
bo: Person
personRole: TemporaryVisitor
navPath: TemporaryVisitor
roles: [Visa Officer]
prerequisiteSlugs:
  - temporary-visitor/register
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-medical-record.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Add a medical record (temporary visitor)

This guide shows how to add a **medical record** on an existing **temporary visitor**. Medical records are on the visitor detail form under the **Medical Records** tab.

!!! tip "Prerequisites"
    The temporary visitor must already exist ([Register a temporary visitor](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-add-medical-record.mp4"
  title="Add a medical record for a temporary visitor in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| An existing **temporary visitor** record | [Register a temporary visitor](register.md) first |
| **Document Number** | Certificate or registry number |
| **Issue Date** and **Validity Duration** | Defaults often apply on a new record |

**Expiration Date** is calculated from issue date and validity duration.

## Step 1 — Open the temporary visitor

1. [Find and open the temporary visitor](../person/open-and-search.md).
2. Wait for the **temporary visitor** detail form to load.

![Temporary visitor detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-medical-record-step-01-employee-detail.png)

## Step 2 — Open the Medical Records tab

1. Select the **Medical Records** tab.
2. Wait for the nested list to load.

## Step 3 — Start a new medical record

1. On the nested list toolbar, select **New Medical Record**.
2. Wait for the medical record **detail form** to open.

![New medical record detail form](../../../assets/screenshots/v2026.08/en/person-add-medical-record-step-02-medical-form-new.png)

## Step 4 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **Document Number** | Certificate or registry number |
| **Issue Date** | Confirm or set the issue date |
| **Validity Duration** | Choose duration; updates **Expiration Date** |

![Medical record fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-medical-record-step-03-medical-fields-filled.png)

## Step 5 — Save the medical record

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

![Medical record saved](../../../assets/screenshots/v2026.08/en/person-add-medical-record-step-04-medical-saved.png)

!!! success "Medical record added"
    The row appears on the **Medical Records** tab for this temporary visitor.

## Step 6 — Confirm on the Medical Records tab

1. Return to the visitor detail form if needed.
2. Open the **Medical Records** tab.
3. Confirm your record appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **Expiration Date** validation | **Expiration Date** must be later than **Issue Date** |
| Tab caption differs | Look for **Medical Records** on the visitor detail form |

## What to read next

- [Add an address of residence](add-address.md)
- [Add a passport](add-passport.md)
- [Mark incomplete or complete](../person/mark-incomplete.md)