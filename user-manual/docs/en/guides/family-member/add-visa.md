---
title: Add a visa on a passport
slug: family-member/add-visa
locale: en
tier: 2
guideStatus: draft
bo: Person
personRole: FamilyMember
navPath: FamilyMember
roles: [Visa Officer]
prerequisiteSlugs:
  - person/open-and-search
screenshotsVersion: "2026.08"
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeMasterDataCrud
e2eScenarioId: person-officer-journey
verified: false
---

# Add a visa on a passport

This guide shows how to add a **visa** on an existing family member **passport**. Visas are created on the passport detail form under the **Visas** tab — not from a top-level menu.

Application items resolve the family member's **current visa** from this nested list when you add people to applications later.

!!! tip "Prerequisites"
    The family member must exist with at least one **passport** ([Add a passport](add-passport.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| A family member with a saved **passport** | [Add a passport](add-passport.md) first |
| **Sponsoring Employee** and **Relationship** on the header | Required unless the sponsor uses manual visa family text |
| **Process number** | Stamp / processed document number from the visa image |
| **Visa Number** and validity dates | Use office rules for numbering |
| Lookup values | **Visa Type**, **Visa Category**, **Visa Issued Place** — defaults may apply |

## Step 1 — Open the passport

1. [Find and open the family member](../person/open-and-search.md).
2. Select the **Passports** tab on the family member detail form.
3. Open the passport that should receive the visa (click the row).

Wait for the **passport detail form** to load.

![Passport detail form](../../../assets/screenshots/v2026.08/en/person-add-visa-step-01-passport-detail.png)

## Step 2 — Open the Visas tab

1. On the passport detail form, select the **Visas** tab.
2. Wait for the nested **Visas** list to load.

The nested toolbar should offer **New Visa** (or **New** on the visas list).

## Step 3 — Start a new visa

1. On the **Visas** nested list toolbar, select **New Visa**.
2. Wait for the visa **detail form** to open.

The new visa is linked to the passport you opened in step 1.

![New visa detail form](../../../assets/screenshots/v2026.08/en/person-add-visa-step-02-visa-form-new.png)

## Step 4 — Fill required fields

Enter values using the on-screen labels. Typical **required** fields include:

| Field | What to enter |
|-------|----------------|
| **Process number** | Stamp / processed document number from the visa image (e.g. ministry reference) |
| **Visa Number** | Unique visa number |
| **Visa Type** | Choose from the list |
| **Visa Category** | Choose from the list |
| **Visa Issued Place** | Choose from the list |
| **Issue Date** | Date picker |
| **Start Date** | Often suggested from issue date |
| **Expiration Date** | Must be later than **Start Date** |
| **Border Zone Location** | Multi-select catalog; **None** is allowed |

Use the optional-fields gear (if shown) for extra stamp fields not listed above.

![Visa fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-visa-step-03-visa-fields-filled.png)

## Step 5 — Save the visa

1. Select **Save** on the visa detail toolbar.
2. Wait until the save completes.

If validation fails, read the message and complete any missing required field.

After save, **Visa Number** on the form should match what you entered.

![Visa saved on detail form](../../../assets/screenshots/v2026.08/en/person-add-visa-step-04-visa-saved.png)

!!! success "Visa added"
    When **Visa Number** shows the value you saved, the visa is on this passport.

## Step 6 — Confirm on the Visas tab

1. Return to the passport detail form if you navigated away.
2. Select the **Visas** tab again.
3. Confirm your visa appears in the nested list.

Select **Refresh** on the nested list if the row does not appear immediately.

## Common problems

| Problem | What to do |
|---------|------------|
| **Visas** tab missing | Open a **passport** row first — visas are not on the family member header |
| **New Visa** not found | Select the **Visas** tab; wait for the nested list to load |
| **Expiration Date** validation | Ensure expiration is after **Start Date** |
| Duplicate visa number | Another active visa may use the same number — change the number or retire the old visa |

## What to read next

- [Add a passport](add-passport.md) — if the family member has no passport yet
- [Add an address of residence](add-address.md)
- [Find and open a person](../person/open-and-search.md)
