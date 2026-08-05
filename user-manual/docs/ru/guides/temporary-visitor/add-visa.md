---
title: Add a visa on a passport (temporary visitor)
slug: temporary-visitor/add-visa
locale: ru
tier: 2
guideStatus: draft
bo: Person
personRole: TemporaryVisitor
navPath: TemporaryVisitor
roles: [Visa Officer]
prerequisiteSlugs:
  - temporary-visitor/add-passport
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-visa.mp4
videoSource: recordings/person-master-data-journey.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Add a visa on a passport (temporary visitor)

Add a **visa** on a temporary visitor's **passport** from the passport detail form → **Visas** tab.

!!! tip "Prerequisites"
    The visitor must have a saved passport ([Add a passport](add-passport.md)).

## Before you start

| You need | Notes |
|----------|--------|
| A temporary visitor with a **passport** | [Add a passport](add-passport.md) first |
| **Visa Number** and validity dates | Office numbering rules |
| Lookup values | **Visa Type**, **Visa Category**, **Visa Issued Place** |

Do **not** set **Sponsoring Employee** or **Relationship** on a temporary visitor — those fields belong to family members only.

## Steps

1. [Open the temporary visitor](../person/open-and-search.md) → **Passports** → open the passport row.
2. Select the **Visas** tab → **New Visa**.
3. Fill **Visa Number**, types, dates, **Border Zone Location** → **Save**.
4. Confirm the row on the **Visas** nested list.

![Visa fields filled](../../../assets/screenshots/v2026.08/ru/person-add-visa-step-03-visa-fields-filled.png)

## What to read next

- [Add a passport](add-passport.md)
- [Add an address of residence](add-address.md)
