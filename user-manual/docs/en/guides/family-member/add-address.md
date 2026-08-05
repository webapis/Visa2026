---
title: Add an address of residence
slug: family-member/add-address
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
e2eScenarioId: person-officer-journey
verified: false
---

# Add an address of residence

This guide shows how to add an **address of residence** on an existing family member. Addresses are on the family member detail form under the **Addresses Of Residence** tab.

Application items use **Current Address Of Residence** from the person's nested list.

!!! tip "Prerequisites"
    The family member must already exist ([Register a family member](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **family member** record | Create one first if needed |
| Address type | **Lodging** is the default for new records |
| Lookup chain for **Lodging** | **Region** → **City** → **Lodging** |

Other types (**Hotel**, **Hospital**, **Private house**, **Other**) show different required fields.

## Step 1 — Open the family member

1. [Find and open the family member](../person/open-and-search.md).
2. Wait for the family member **detail form** to load.

![family member detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-address-step-01-employee-detail.png)

## Step 2 — Open the Addresses tab

1. Select **Addresses Of Residence** (caption may vary slightly).
2. Wait for the nested list to load.

## Step 3 — Start a new address

1. On the nested list toolbar, select **New Address Of Residence**.
2. Wait for the address **detail form** to open.

**Type** usually defaults to **Lodging**.

![New address detail form](../../../assets/screenshots/v2026.08/en/person-add-address-step-02-address-form-new.png)

## Step 4 — Fill required fields (Lodging)

For the default **Lodging** type, complete:

| Field | What to enter |
|-------|----------------|
| **Type** | Confirm **Lodging** (or choose another type) |
| **Region** | Search and pick from the catalog |
| **City** | Filtered by region — pick a city |
| **Lodging** | Filtered by city — pick the lodging site |
| **Expiration Date** | Required for lodging addresses |

If you choose **Private house**, fill **Full Address** and related fields instead.

![Address fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-address-step-03-address-fields-filled.png)

## Add a lodging, hotel, hospital, or other site (tenant catalog)

**Lodging**, **Hotel**, **Hospital**, and **Other site** are shared **tenant** catalogs. Create or pick them **from this address form** (same steps as on [employee addresses](../employee/add-address.md#add-a-lodging-hotel-hospital-or-other-site-tenant-catalog)).

!!! warning "Avoid duplicate site entries"
    Search the dropdown before **New**. Duplicate rows for the same physical site confuse every officer — **reuse the existing catalog row** when the site is already listed.

## Step 5 — Save the address

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

After save, **Region** (or your primary identifier) should show on the form.

![Address saved on detail form](../../../assets/screenshots/v2026.08/en/person-add-address-step-04-address-saved.png)

!!! success "Address added"
    The row appears on the **Addresses Of Residence** tab for this family member.

## Step 6 — Confirm on the Addresses tab

1. Return to the family member detail form if needed.
2. Open the **Addresses Of Residence** tab.
3. Confirm your address appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **City** or **Lodging** empty | Set **Region** first — child lookups filter from parent values |
| Fields disappeared after changing **Type** | Each type has its own required set — re-fill after switching type |
| **Private house** path | Select **Private house**, then enter **Full Address** |

## What to read next

- [Add a medical record](add-medical-record.md)
- [Add family relation documents](add-family-relation-documents.md)
- [Update family member details](edit-family-member.md)
