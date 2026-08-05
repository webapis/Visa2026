---
title: Add an address of residence
slug: employee/add-address
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
e2eScenarioId: person-officer-journey
verified: false
---

# Add an address of residence

This guide shows how to add an **address of residence** on an existing employee. Addresses are on the Employee detail form under the **Addresses Of Residence** tab.

Application items use **Current Address Of Residence** from the person's nested list.

!!! tip "Prerequisites"
    the employee must already exist ([Register a new employee](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **employee** record | Create one first if needed |
| Address type | **Lodging** is the default for new records |
| Lookup chain for **Lodging** | **Region** → **City** → **Lodging** |

Other types (**Hotel**, **Hospital**, **Private house**, **Other**) show different required fields.

## Step 1 — Open the employee

1. [Find and open the employee](../person/open-and-search.md).
2. Wait for the employee **detail form** to load.

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-address-step-01-employee-detail.png)

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

**Lodging**, **Hotel**, **Hospital**, and **Other site** are **tenant** catalogs. Officers create and edit them **from this address form** — there is no separate Lookup menu in the left navigation.

### When the site is not in the list

1. Set **Type**, **Region**, and **City** first (the site list filters by city).
2. Open the field that matches **Type** (**Lodging**, **Hotel**, **Hospital**, or **Other site**).
3. **Search** the dropdown — type part of the name or address to filter.
4. If the site already exists, **select that row** and continue to Step 5.
5. Only if it is genuinely new, choose **New** in the lookup popup.
6. On the catalog mini-form:
   - **Lodging** or **Other site**: **Full address** (required) and confirm **City**
   - **Hotel** or **Hospital**: **Name** (required) and **City**
7. **Save** the catalog row, select it on the address, then continue.

For **Lodging**, supporting scans may attach to the **Lodging** record itself (not the address row). Open the lodging detail from the lookup if your office requires site documents.

!!! warning "Avoid duplicate site entries"
    Before you choose **New**, search for the same lodging, hotel, or hospital under a slightly different spelling. **Duplicate catalog rows** split address history and clutter dropdowns for every officer. Always **reuse the existing site** when it is already in the list.

## Step 5 — Save the address

1. Select **Save** on the detail toolbar.
2. Wait until the save completes.

After save, **Region** (or your primary identifier) should show on the form.

![Address saved on detail form](../../../assets/screenshots/v2026.08/en/person-add-address-step-04-address-saved.png)

!!! success "Address added"
    The row appears on the **Addresses Of Residence** tab for this employee.

## Step 6 — Confirm on the Addresses tab

1. Return to the Employee detail form if needed.
2. Open the **Addresses Of Residence** tab.
3. Confirm your address appears in the nested list.

## Common problems

| Problem | What to do |
|---------|------------|
| **City** or **Lodging** empty | Set **Region** first — child lookups filter from parent values |
| Duplicate lodging or hotel in the list | Search before **New** — pick the existing site; do not create a second row for the same place |
| Fields disappeared after changing **Type** | Each type has its own required set — re-fill after switching type |
| **Private house** path | Select **Private house**, then enter **Full Address** |

## What to read next

- [Add position history](../employee/add-position-history.md)
- [Add a medical record](add-medical-record.md)
- [Update Employee details](edit-employee.md)
