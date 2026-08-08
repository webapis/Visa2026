---
title: Applications — ministry and direct migration
slug: applications/overview
locale: en
tier: 4
guideStatus: draft
bo: Application
navPath: Applications
roles: [Visa Officer]
prerequisiteSlugs:
  - getting-started/login
  - getting-started/navigation
screenshotsVersion: "2026.08"
verified: false
---

# Applications — ministry and direct migration

Visa2026 splits application work into **two workflow routes**. Each route has its own **header list** and **application items list** under **Applications** in the left menu.

Use this guide to choose the correct list before you [create an application](create.md) or [add application items](add-items.md).

!!! tip "Prerequisites"
    [Sign in](../../getting-started/login.md) and know the shell ([Main navigation](../../getting-started/navigation.md)).

## Two routes at a glance

| Route | Header list | Application items list | Use when the procedure… |
|-------|-------------|------------------------|---------------------------|
| **Via ministry** | **Applications (via ministry)** | **Application items (ministry)** | Goes through **ministry approval** before the migration service |
| **Direct migration** | **Applications (direct migration)** | **Application items (migration)** | Goes **directly** to the migration service (no ministry leg) |

If you are unsure which route applies, ask your visa chief. The **application profile** you pick must match the list you open.

## Application profiles (new)

Visa2026 links each application to an **Application Profile** — shared configuration for route, field visibility, progress rules, and templates. Officers choose the profile in a **picker** when they select **New**; it is fixed after save.

| Topic | Guide |
|-------|--------|
| What profiles are and how create works | [Application profiles — how configuration works](application-profiles.md) |
| Short create checklist | [Create an application](create.md) |
| VisaOffice: define profiles | [Configure application profiles](../administration/configuration/application-profiles.md) |

Legacy **Application Type Code** on a blank form is being replaced by this picker during rollout.

## The Applications menu (four lists)

Expand **Applications** in the left menu. Officers typically use these four lists:

| # | Menu item | What it is for |
|---|-----------|----------------|
| 1 | **Applications (via ministry)** | Create, search, and open **ministry-route** application headers |
| 2 | **Applications (direct migration)** | Create, search, and open **direct-migration** application headers |
| 3 | **Application items (ministry)** | Search **person lines** on ministry-route applications (across many headers) |
| 4 | **Application items (migration)** | Search **person lines** on direct-migration applications |

![Applications group in the left menu](../../../assets/screenshots/v2026.08/en/navigation-step-02-left-menu.png)

!!! note "Headers and lines work together"
    - Create the **header** on list **1** or **2** → [Create an application](create.md)
    - Add **person lines** on the header **Application items** tab → [Add application items](add-items.md)
    - Or open a line from list **3** or **4** when you need to find items across many applications (for example document copies)

## Applications (via ministry)

**Use for:** invitation, visa extension, work permit, and other procedures that include a **ministry review** step before the migration service.

| Task | Where to go |
|------|-------------|
| Create a new ministry-route application | **Applications (via ministry)** → **New** |
| Find an existing ministry application | **Applications (via ministry)** — search by **Full Application Number** |
| Add people to the application | Open the header → **Application items** tab → **New** |
| Search person lines across ministry applications | **Application items (ministry)** |

Progress rules and available **application types** on this list match the ministry workflow.

## Applications (direct migration)

**Use for:** registration check-in/out, passport change, and other procedures that go **straight to the migration service** without a ministry leg.

| Task | Where to go |
|------|-------------|
| Create a new direct-migration application | **Applications (direct migration)** → **New** |
| Find an existing direct-migration application | **Applications (direct migration)** |
| Add people to the application | Open the header → **Application items** tab → **New** |
| Search person lines across direct-migration applications | **Application items (migration)** |

The **Application Type Code** picker on this list shows only types valid for the direct-migration route.

## How to choose the correct header list

| Your office procedure… | Open |
|------------------------|------|
| Requires ministry letter / ministry approval step | **Applications (via ministry)** |
| Migration service only (no ministry leg on the contract) | **Applications (direct migration)** |
| You opened a list but the type code is missing | Switch to the **other** header list — types are filtered per route |

## Typical workflow (both routes)

The steps are the **same** on both lists; only the menu entry differs.

1. Open the correct **header list** (via ministry **or** direct migration).
2. **New** → choose **Application Profile** → **Use profile (live link)** → fill header fields → **Save**.
3. On the saved header, open **Application items** → **New** → pick **Person** → **Save** for each person.
4. Track progress, document copies, and report packages — [Track application progress](progress.md) · [Document copies](document-copies.md) · [Templates (Resminamalar)](resminamalar.md)

## Common problems

| Problem | What to do |
|---------|------------|
| Wrong application types in the picker | You are on the wrong header list — use the other route |
| Application not visible in the other list | Ministry and direct-migration headers are **separate lists** — search the list where you created it |
| **Application items (ministry)** row not on migration list | Items follow the parent route — use the matching items list |
| Cannot find **Applications** menu | Your role may hide applications — ask your supervisor |

## What to read next

- [Track application progress](progress.md)
- [Create an application](create.md) · [Add application items](add-items.md)
- [Main navigation](../../getting-started/navigation.md) — full menu map
- [What Visa2026 does](../../about/capabilities.md) — applications in the feature overview