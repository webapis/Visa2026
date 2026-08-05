---
title: Add a travel history (temporary visitor)
slug: temporary-visitor/add-travel
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
videoFile: person-add-travel.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Add a travel history (temporary visitor)

This guide shows how to add a **travel history** row on an existing **temporary visitor**. Travel rows are on the visitor detail form under the **Travel Histories** tab in **Person record data**.

Family members do **not** have this tab. The UI matches the employee **Travel Histories** tab.

This walkthrough uses **External Arrival** (entry at a border checkpoint).

!!! tip "Prerequisites"
    The temporary visitor must already exist ([Register a temporary visitor](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Tab labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/en/person-add-travel.mp4"
  title="Add a travel history for a temporary visitor in Visa2026"></video>

<p class="visa-manual-video-caption">Recording from the training environment (test data). The steps below match the video.</p>

## Before you start

| You need | Notes |
|----------|--------|
| An existing **temporary visitor** record | [Register a temporary visitor](register.md) first |
| Movement type | This walkthrough: **External Arrival** |
| **Check Point** and **Country** | Required for external travel |

## Step 1 — Open the temporary visitor

1. [Find and open the temporary visitor](../person/open-and-search.md).
2. Wait for the **temporary visitor** detail form to load.

![Temporary visitor detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-travel-step-01-employee-detail.png)

## Step 2 — Open the Travel Histories tab

1. In **Person record data**, select the **Travel Histories** tab.
2. Wait for the nested list to load.

## Step 3 — Start External Arrival

The **New** control on this tab is a **split button**:

1. Select the **arrow** on **New** (or open the split menu).
2. Choose **New External Arrival**.
3. Wait for the travel **detail form** to open.

![New External Arrival detail form](../../../assets/screenshots/v2026.08/en/person-add-travel-step-02-travel-form-new.png)

## Step 4 — Confirm required fields

| Field | What to enter |
|-------|----------------|
| **Travel Date** | Confirm or set the date of entry |
| **Travel Type** | **External** (pre-filled) |
| **Movement Type** | **Entry** (pre-filled for External Arrival) |
| **Check Point** | Border checkpoint — pick from the list |
| **Country** | Country of arrival — pick from the list |
| **Travel Notes** | Optional free text |

![Travel fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-travel-step-03-travel-fields-filled.png)

## Step 5 — Save the travel record

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

![Travel record saved](../../../assets/screenshots/v2026.08/en/person-add-travel-step-04-travel-saved.png)

!!! success "Travel history added"
    The row appears on the **Travel Histories** tab for this temporary visitor.

## Step 6 — Confirm on the Travel Histories tab

1. Return to the visitor detail form if needed.
2. Open the **Travel Histories** tab.
3. Confirm your row appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **Travel Histories** tab missing | Confirm the person is a **temporary visitor**, not a family member |
| Wrong movement type | Use the **New** split menu — do not use generic **New** if your office requires a specific type |

## What to read next

- [Add an address of residence](add-address.md)
- [Add a passport](add-passport.md)
- [Add a travel history (employee)](../employee/add-travel.md) — other movement types