---
title: Add a passport to a temporary visitor
slug: temporary-visitor/add-passport
locale: ru
tier: 2
guideStatus: draft
bo: Person
personRole: TemporaryVisitor
navPath: TemporaryVisitor
roles: [Visa Officer]
prerequisiteSlugs:
  - person/open-and-search
screenshotsVersion: "2026.08"
videosVersion: "2026.08"
videoStorage: static
videoFile: person-add-passport.mp4
videoSource: recordings/passport-create-with-shots.mp4
e2eScenarioId: person-officer-journey
verified: false
---

# Add a passport to a temporary visitor

This guide shows how to add a **passport** on an existing **temporary visitor** person record. Passports are on the visitor detail form under **Passports** in the **Person record data** tab group — not from a top-level passport menu.

Temporary visitors use a typed detail view (`Person_DetailView_TemporaryVisitor`): you will **not** see employee-only tabs (Educations, Salaries, Work duties) or family-member fields (**Sponsoring Employee**, **Relationship**).

!!! tip "Prerequisites"
    The temporary visitor must already exist ([Find and open a person](../person/open-and-search.md) — **Temporary Visitors** list or Report Dashboard search).

!!! tip "Screenshots"
    Images are from the **English** UI (version **2026.08**). Labels differ by language; steps are the same.

## Video walkthrough

<video class="visa-manual-video" controls preload="metadata"
  src="../../../assets/videos/v2026.08/ru/person-add-passport.mp4"
  title="Add a passport to a temporary visitor in Visa2026"></video>

## Before you start

| You need | Notes |
|----------|--------|
| An existing **temporary visitor** record | Open from **Temporary Visitors** or person search |
| **Company (Subcontractor)** on the header | Often required when the record is active |
| **Passport Number** and dates | Use office rules for numbering |

## Step 1 — Open the temporary visitor

1. Open **Temporary Visitors** in the left menu, or [find the person](../person/open-and-search.md).
2. Open the visitor who needs a passport.
3. Wait for the **temporary visitor** detail form (shorter **Person record data** tab strip than employees).

![Temporary visitor detail](../../../assets/screenshots/v2026.08/ru/person-add-passport-step-01-employee-detail.png)

## Step 2 — Passports tab

1. In **Person record data**, select **Passports**.
2. On the nested list toolbar, select **New Passport**.

## Step 3 — Fill and save

Complete typical required fields (**Passport Number**, **Passport Type**, dates, **Authority**, **Issued Country**) → **Save**.

![Passport saved](../../../assets/screenshots/v2026.08/ru/person-add-passport-step-04-passport-saved.png)

## What to read next

- [Add a visa on this passport](add-visa.md)
- [Add a medical record](add-medical-record.md)
- [Main navigation](../../getting-started/navigation.md)
