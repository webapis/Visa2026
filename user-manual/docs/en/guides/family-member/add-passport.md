---
title: Add a passport to a family member
slug: family-member/add-passport
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
e2eTestFilter: PersonOfficerJourney_LoginCreateEmployeeAddPassport
e2eScenarioId: person-officer-journey
verified: false
---

# Add a passport to a family member

This guide shows how to add a **passport** record on an existing **family member**. Passports are created on the family member detail form under the **Passports** tab — not from a separate top-level menu.

!!! tip "Prerequisites"
    The family member must already exist ([Register a family member](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| An existing **family member** record | Create one first if needed |
| **Passport Number** and dates | Use office rules for numbering |
| **Passport Type** and **Issued Country** | Choose from lookup lists |

## Step 1 — Open the family member

1. [Find and open the family member](../person/open-and-search.md).
2. Open the family member who needs a passport.
3. Wait for the **family member** detail form to load.

You should see **Person record data** tabs such as **Passports**, **Medical Records**, and **Addresses Of Residence** (not employee-only tabs like **Educations**).

![Employee detail with tabs](../../../assets/screenshots/v2026.08/en/person-add-passport-step-01-employee-detail.png)

## Step 2 — Open the Passports tab

1. On the family member detail form, select the **Passports** tab.
2. Wait for the nested **Passports** list to load.

The nested toolbar should offer **New Passport** (or **New** on the passports list).

## Step 3 — Start a new passport

1. On the **Passports** nested list toolbar, select **New Passport**.
2. Wait for the passport **detail form** to open.

The new passport is linked to the family member you opened in step 1.

!!! tip "Default passport type"
    **Passport Type** is preset to **P — National passport** (the type officers use most often). Change it only when the document is a different type.

![New passport detail form](../../../assets/screenshots/v2026.08/en/person-add-passport-step-02-passport-form-new.png)

## Step 4 — Fill required fields

Enter values using the on-screen labels. Typical **required** fields include:

| Field | What to enter |
|-------|----------------|
| **Passport Number** | Unique passport number |
| **Passport Type** | Usually already **P — National passport**; change from the list if needed |
| **Issue Date** | Date picker |
| **Expiration Date** | Date picker |
| **Authority** | Issuing authority text |
| **Issued Country** | Choose from the list |

![Passport fields filled before save](../../../assets/screenshots/v2026.08/en/person-add-passport-step-03-passport-fields-filled.png)

## Step 5 — Save the passport

1. Select **Save** on the passport detail toolbar.
2. Wait until the save completes.

If validation fails, read the message and complete any missing required field.

After save, the **Passport Number** on the form should match what you entered.

![Passport saved on detail form](../../../assets/screenshots/v2026.08/en/person-add-passport-step-04-passport-saved.png)

!!! success "Passport added"
    When **Passport Number** shows the value you saved, the passport is on this family member.

## Step 6 — Confirm on the Passports tab

1. Return to the family member detail form if you navigated away.
2. Select the **Passports** tab again.
3. Confirm your passport appears in the nested list.

Select **Refresh** on the nested list if the row does not appear immediately.

## Common problems

| Problem | What to do |
|---------|------------|
| **Passports** tab missing | Your role may not allow this collection — ask your supervisor |
| **New Passport** not found | Open the **Passports** tab first; wait for the nested list to load |
| Detail form does not open | Select **New Passport** again; widen the window |
| **Save** validation error | Fill every required field; check date format |

## What to read next

- [Add a visa on this passport](add-visa.md)
- [Find and open a person](../person/open-and-search.md)
- [Main navigation](../../getting-started/navigation.md)
