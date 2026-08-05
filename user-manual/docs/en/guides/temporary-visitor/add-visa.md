---
title: Add a visa on a passport (temporary visitor)
slug: temporary-visitor/add-visa
locale: en
tier: 2
guideStatus: draft
bo: Person
personRole: TemporaryVisitor
navPath: TemporaryVisitor
roles: [Visa Officer]
prerequisiteSlugs:
  - temporary-visitor/register
  - temporary-visitor/add-passport
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-visa.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Add a visa on a passport (temporary visitor)

This guide shows how to add a **visa** on an existing temporary visitor **passport**. Visas are created on the passport detail form under the **Visas** tab — not from a top-level menu.

!!! tip "Prerequisites"
    The temporary visitor must exist with at least one **passport** ([Register a temporary visitor](register.md), [Add a passport](add-passport.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-add-visa.mp4"
  title="Add a visa on a temporary visitor passport in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| A **temporary visitor** with a saved **passport** | [Add a passport](add-passport.md) first |
| **Visa Number** and validity dates | Office numbering rules |
| Lookup values | **Visa Type**, **Visa Category**, **Visa Issued Place** |

Do **not** set **Sponsoring Employee** or **Relationship** on a temporary visitor — those fields belong to family members only.

## Step 1 — Open the passport

1. [Find and open the temporary visitor](../person/open-and-search.md).
2. Select the **Passports** tab on the visitor detail form.
3. Open the passport that should receive the visa (click the row).

Wait for the **passport detail form** to load.

![Passport detail form](../../../assets/screenshots/v2026.08/en/person-add-visa-step-01-passport-detail.png)

## Step 2 — Open the Visas tab

1. On the passport detail form, select the **Visas** tab.
2. Wait for the nested **Visas** list to load.

## Step 3 — Start a new visa

1. On the **Visas** nested list toolbar, select **New Visa**.
2. Wait for the visa **detail form** to open.

![New visa detail form](../../../assets/screenshots/v2026.08/en/person-add-visa-step-02-visa-form-new.png)

## Step 4 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **Visa Number** | Unique visa number |
| **Visa Type** | Choose from the list |
| **Visa Category** | Choose from the list |
| **Visa Issued Place** | Choose from the list |
| **Issue Date** | Date picker |
| **Start Date** | Often suggested from issue date |
| **Expiration Date** | Must be later than **Start Date** |
| **Border Zone Location** | Multi-select catalog; **None** is allowed |

![Visa fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-visa-step-03-visa-fields-filled.png)

## Step 5 — Save the visa

1. Select **Save** on the visa detail toolbar.
2. Wait until the save completes.

After save, **Visa Number** should match what you entered.

![Visa saved on detail form](../../../assets/screenshots/v2026.08/en/person-add-visa-step-04-visa-saved.png)

!!! success "Visa added"
    The visa is linked to this passport.

## Step 6 — Confirm on the Visas tab

1. Return to the passport detail form if needed.
2. Select the **Visas** tab again.
3. Confirm your visa appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **Visas** tab missing | Open a **passport** row first |
| **Expiration Date** validation | Ensure expiration is after **Start Date** |
| Family member fields visible | Confirm you opened a **temporary visitor**, not a family member |

## What to read next

- [Add a passport](add-passport.md)
- [Add a medical record](add-medical-record.md)
- [Add an address of residence](add-address.md)