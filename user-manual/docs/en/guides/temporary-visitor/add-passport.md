---
title: Add a passport to a temporary visitor
slug: temporary-visitor/add-passport
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
videoFile: person-add-passport.mp4
videoSource: recordings/passport-create-with-shots.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Add a passport to a temporary visitor

This guide shows how to add a **passport** on an existing **temporary visitor** person record. Passports are on the visitor detail form under **Passports** in **Person record data** — not from a top-level passport menu.

Temporary visitors use a typed detail view: you will **not** see employee-only tabs (Educations, Salaries, Work duties) or family-member fields (**Sponsoring Employee**, **Relationship**).

!!! tip "Prerequisites"
    The temporary visitor must already exist ([Register a temporary visitor](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-add-passport.mp4"
  title="Add a passport to a temporary visitor in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| An existing **temporary visitor** record | [Register a temporary visitor](register.md) first |
| **Passport Number** and dates | Use office rules for numbering |
| **Passport Type** and **Issued Country** | Choose from lookup lists |

## Step 1 — Open the temporary visitor

1. Open **Temporary visitor** in the left menu, or [find the person](../person/open-and-search.md).
2. Open the visitor who needs a passport.
3. Wait for the **temporary visitor** detail form to load.

![Temporary visitor detail](../../../assets/screenshots/v2026.08/en/person-add-passport-step-01-employee-detail.png)

## Step 2 — Open the Passports tab

1. In **Person record data**, select the **Passports** tab.
2. Wait for the nested **Passports** list to load.

## Step 3 — Start a new passport

1. On the **Passports** nested list toolbar, select **New Passport**.
2. Wait for the passport **detail form** to open.

![New passport detail form](../../../assets/screenshots/v2026.08/en/person-add-passport-step-02-passport-form-new.png)

## Step 4 — Fill required fields

| Field | What to enter |
|-------|----------------|
| **Passport Number** | Unique passport number |
| **Passport Type** | Choose from the list |
| **Issue Date** | Date picker |
| **Expiration Date** | Date picker |
| **Authority** | Issuing authority text |
| **Issued Country** | Choose from the list |

![Passport fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-passport-step-03-passport-fields-filled.png)

## Step 5 — Save the passport

1. Select **Save** on the passport detail toolbar.
2. Wait until the save completes.

After save, **Passport Number** should match what you entered.

![Passport saved](../../../assets/screenshots/v2026.08/en/person-add-passport-step-04-passport-saved.png)

!!! success "Passport added"
    The passport is linked to this temporary visitor.

## Step 6 — Confirm on the Passports tab

1. Return to the visitor detail form if needed.
2. Select the **Passports** tab again.
3. Confirm your passport appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **Passports** tab missing | Your role may not allow this collection — ask your supervisor |
| **New Passport** not found | Open the **Passports** tab first; wait for the nested list to load |
| Family member fields visible | Confirm you opened a **temporary visitor**, not a family member |

## What to read next

- [Add a visa on this passport](add-visa.md)
- [Add a medical record](add-medical-record.md)
- [Main navigation](../../getting-started/navigation.md)