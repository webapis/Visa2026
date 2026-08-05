---
title: Add an address of residence (temporary visitor)
slug: temporary-visitor/add-address
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
e2eScenarioId: person-officer-journey
verified: false
---

# Add an address of residence (temporary visitor)

This guide shows how to add an **address of residence** on an existing **temporary visitor**. Addresses are on the visitor detail form under the **Addresses Of Residence** tab.

!!! tip "Prerequisites"
    The temporary visitor must already exist ([Register a temporary visitor](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **temporary visitor** record | [Register a temporary visitor](register.md) first |
| Address type | **Lodging** is the default for new records |
| Lookup chain for **Lodging** | **Region** → **City** → **Lodging** |

## Step 1 — Open the temporary visitor

1. [Find and open the temporary visitor](../person/open-and-search.md).
2. Wait for the **temporary visitor** detail form to load.

![Temporary visitor detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-address-step-01-employee-detail.png)

## Step 2 — Open the Addresses tab

1. Select **Addresses Of Residence**.
2. Wait for the nested list to load.

## Step 3 — Start a new address

1. On the nested list toolbar, select **New Address Of Residence**.
2. Wait for the address **detail form** to open.

**Type** usually defaults to **Lodging**.

![New address detail form](../../../assets/screenshots/v2026.08/en/person-add-address-step-02-address-form-new.png)

## Step 4 — Fill required fields (Lodging)

| Field | What to enter |
|-------|----------------|
| **Type** | Confirm **Lodging** (or choose another type) |
| **Region** | Search and pick from the catalog |
| **City** | Filtered by region |
| **Lodging** | Filtered by city |
| **Expiration Date** | Required for lodging addresses |

![Address fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-address-step-03-address-fields-filled.png)

## Add a lodging, hotel, hospital, or other site (tenant catalog)

**Lodging**, **Hotel**, **Hospital**, and **Other site** are shared **tenant** catalogs. Create or pick them **from this address form** (same steps as on [employee addresses](../employee/add-address.md#add-a-lodging-hotel-hospital-or-other-site-tenant-catalog)).

!!! warning "Avoid duplicate site entries"
    Search the dropdown before **New**. Duplicate rows for the same physical site confuse every officer — **reuse the existing catalog row** when the site is already listed.

## Step 5 — Save the address

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

![Address saved](../../../assets/screenshots/v2026.08/en/person-add-address-step-04-address-saved.png)

!!! success "Address added"
    The row appears on the **Addresses Of Residence** tab for this temporary visitor.

## Step 6 — Confirm on the Addresses tab

1. Return to the visitor detail form if needed.
2. Open the **Addresses Of Residence** tab.
3. Confirm your address appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **City** or **Lodging** empty | Set **Region** first — child lookups filter from parent values |
| **Private house** path | Select **Private house**, then enter **Full Address** |

## What to read next

- [Add a travel history](add-travel.md)
- [Add a passport](add-passport.md)
- [Mark incomplete or complete](../person/mark-incomplete.md)