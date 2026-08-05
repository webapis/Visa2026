---
title: Add a travel history
slug: employee/add-travel
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
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Add a travel history

This guide shows how to add a **travel history** row on an existing **employee**. Travel rows are on the employee detail form under the **Travel Histories** tab in **Person record data**.

Officers maintain travel history manually on the person record. Registration application lines do **not** create or update these rows automatically.

This guide uses **External Arrival** (entry at a border checkpoint). The same tab also supports **External Departure**, **Internal Arrival**, and **Internal Departure**.

This guide applies to **employees** and **temporary visitors** only — family members do not have this tab.

!!! tip "Prerequisites"
    The employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Tab and field labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| Movement type | This walkthrough: **External Arrival** (entry from abroad) |
| **Check Point** and **Country** | Required for external travel; defaults may be pre-filled |

The tab caption may appear as **Travel Histories** or **Travel histories** depending on your UI language.

## Step 1 — Open the employee

1. [Find and open the employee](../person/open-and-search.md).
2. Wait for the employee **detail form** to load.

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-travel-step-01-employee-detail.png)

## Step 2 — Open the Travel Histories tab

1. In **Person record data**, select the **Travel Histories** tab.
2. Wait for the nested list to load.

## Step 3 — Start External Arrival

The **New** control on this tab is a **split button** (arrow beside **New**):

1. Select the **arrow** on **New** (or open the split menu).
2. Choose **New External Arrival**.
3. Wait for the travel **detail form** to open.

![New External Arrival detail form](../../../assets/screenshots/v2026.08/en/person-add-travel-step-02-travel-form-new.png)

**External Arrival** sets **Travel Type** to external and **Movement Type** to entry. **Travel Date** usually defaults to today.

## Step 4 — Confirm required fields

| Field | What to enter |
|-------|----------------|
| **Travel Date** | Confirm or set the date of entry |
| **Travel Type** | **External** (pre-filled for this action) |
| **Movement Type** | **Entry** (pre-filled for External Arrival) |
| **Check Point** | Border checkpoint — pick from the list |
| **Country** | Country of arrival — pick from the list |
| **Travel Notes** | Optional free text |

For **Internal Arrival** or **Internal Departure**, the form shows **Region** and **City** instead of **Check Point** and **Country**.

![Travel fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-travel-step-03-travel-fields-filled.png)

## Step 5 — Save the travel record

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

After save, **Travel Date** and checkpoint/country should match what you entered.

![Travel record saved](../../../assets/screenshots/v2026.08/en/person-add-travel-step-04-travel-saved.png)

!!! success "Travel history added"
    The row appears on the **Travel Histories** tab for this employee.

## Step 6 — Confirm on the Travel Histories tab

1. Return to the employee detail form if needed.
2. Open the **Travel Histories** tab again.
3. Confirm your row appears in the nested list.

## Other movement types

Use the same **New** split menu when you need:

| Action | When to use |
|--------|-------------|
| **New External Departure** | Person leaves the country at a border checkpoint |
| **New Internal Arrival** | Movement within the country (region / city) |
| **New Internal Departure** | Internal exit movement |

## Common problems

| Problem | What to do |
|---------|------------|
| **Travel Histories** tab missing | Confirm the person is an **employee** or **temporary visitor**, not a family member |
| **New External Arrival** not in menu | Select the **Travel Histories** tab first; use the **arrow** on **New** |
| **Check Point** or **Country** empty | Pick values from the dropdown — both are required for external travel |
| **Save** validation error | Fill all visible required fields for the chosen movement type |

## What to read next

- [Add CV and personal files](add-cv-documents.md)
- [Update employee details](edit-employee.md)
- [Add a travel history (temporary visitor)](../temporary-visitor/add-travel.md)
