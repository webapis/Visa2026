---
title: Update family member details
slug: family-member/edit-family-member
locale: en
tier: 3
guideStatus: draft
bo: Person
personRole: FamilyMember
navPath: FamilyMember
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
  - person/open-and-search
  - family-member/register
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/OPTIONAL_DETAIL_FIELDS.md
---

# Update family member details

This guide shows how to change fields on an existing **family member** person record and save your updates. When you finish, the new values appear on the family member **detail form** and in the **Family Members** list.

!!! tip "Prerequisites"
    You can [find and open a person](../person/open-and-search.md) and the family member already exists ([register guide](register.md)).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Before you start

| You need | Notes |
|----------|--------|
| **Visa Officer** role with edit access on **Family Members** | Ask your supervisor if fields are read-only |
| The correct family member open on the detail form | Use [Find and open a person](../person/open-and-search.md) |
| Approval for sensitive changes | Your office may restrict **Personal Number** changes |

This guide updates **scalar fields** on the family member header (name, sponsor, relationship, lookups). For passports, medical records, or addresses, use the dedicated guides.

## Step 1 — Open the family member detail form

1. Sign in to Visa2026.
2. In the left menu, select **Family Members**.
3. Search for the family member if needed.
4. Click the row to open the **detail form**.

Confirm **Personal Number** and **Full Name** match the person you intend to edit.

![Family member detail form open for editing](../../../assets/screenshots/v2026.08/en/person-edit-family-member-step-01-detail-form.png)

## Step 2 — Change the fields you need

Edit values directly on the form. Common updates include:

| Field | When officers change it |
|-------|-------------------------|
| **Sponsoring Employee** | Family member moved to another employee (office policy) |
| **Relationship** | Relationship type corrected |
| **Project Contract** | Contract sync after sponsor change |
| **Company (Subcontractor)** | Subcontractor assignment changed |
| **Nationality** or **Gender** | Data correction |

!!! warning "Personal Number"
    Change **Personal Number** only when your supervisor confirms the old number was wrong. Duplicate numbers are blocked on save.

## Step 3 — Show optional fields (gear)

1. At the top of the form, select **Show optional fields** (gear control).
2. Optional members appear — for example **Middle Name**, **Photo**, **Is Archived**.
3. Edit the optional values you need.
4. Select **Hide optional fields** when you want a shorter form again.

## Step 4 — Save your changes

1. Review every field you changed.
2. Select **Save** on the toolbar.
3. Wait until the save completes.

To return to the list after a successful save, use **Save and Close**.

![Family member detail after save](../../../assets/screenshots/v2026.08/en/person-edit-family-member-step-02-after-save.png)

## Step 5 — Confirm in the list

1. Open **Family Members** again.
2. Find the family member by **Personal Number**.
3. Open the row and check that your changes appear.

!!! success "Family member updated"
    When the detail form and list show your new values, the update succeeded.

## What this guide does not cover

| Topic | Where to read |
|-------|----------------|
| New family member | [Register a family member](register.md) |
| Passport tab | [Add a passport](add-passport.md) |
| Family relation files | [Add family relation documents](add-family-relation-documents.md) |
| Incomplete flag | [Mark incomplete / complete](../person/mark-incomplete.md) |

## Common problems

| Problem | What to do |
|---------|------------|
| Fields are read-only | Your role may not allow edit — ask your supervisor |
| **Relationship** required on save | Pick a relationship or check sponsor manual visa family rules |
| Duplicate **Personal Number** | Revert the number or confirm with a senior officer |

## What to read next

- [Add a passport](add-passport.md)
- [Add family relation documents](add-family-relation-documents.md)
- [Mark incomplete or complete](../person/mark-incomplete.md)