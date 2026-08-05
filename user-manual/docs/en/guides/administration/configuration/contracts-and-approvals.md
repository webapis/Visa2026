---
title: Contracts and approvals
slug: administration/configuration/contracts-and-approvals
locale: en
tier: 8
guideStatus: draft
bo: ProjectContract
navPath: Configuration
roles: [Administrator, VisaOffice]
prerequisiteSlugs:
  - administration/configuration/overview
  - applications/create
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/APPLICATION_PROGRESS_APPROVAL_AND_CONTRACT_DEPTH.md
---

# Contracts and approvals

Configure how **ministry-route** applications choose a **project contract** and which **ministries** review them in order. Three **Configuration** lists work together:

| Menu item | Purpose |
|-----------|---------|
| **Approving ministries** | Short ministry names shown on progress steps |
| **Approval Leg Profile** | Ordered chain of ministries (review legs) |
| **Project contracts** | Contract/project names officers pick on applications |

!!! tip "Prerequisites"
    Officers use these values when [creating applications](../../applications/create.md) and [tracking progress](../../applications/progress.md). Read [Configuration overview](overview.md) first.

## Approving ministries

Government ministries that perform **review legs** (`1_REVIEW_*` … `N_REVIEW_*`) on an application — not the migration service step.

### Add or edit a ministry

1. **Configuration → Approving ministries**.
2. **New** (or open an existing row).
3. Fill **Short name** (required) — shown on **Application progress** ministry steps.
4. **Is Active** is set by administrators; inactive ministries should not appear on new profiles.
5. **Save**.

Keep short names concise (max 40 characters) — they appear in progress history and list labels.

## Approval Leg Profile

A reusable **approval route**: an ordered list of approving ministries. Many **project contracts** can share one profile.

### Create a profile

1. **Configuration → Approval Leg Profile**.
2. **New**.
3. Set **Is Active** (active profiles are selectable on applications).
4. In the **Ministry legs** nested list, add rows in review order:
   - **Sequence** — order of review (1, 2, 3, …)
   - **Approving Ministry** — pick from **Approving ministries**
5. **Save**.

The list view shows a **Ministries** column (short names joined with `-`, for example `Türkmenenergo-Energetika-Gurluşyk`).

### Link contracts to a profile

Each **Project contract** row points to one **Approval Leg Profile**. Officers first choose the profile (when the application type shows it), then pick a contract filtered to that profile.

## Project contracts

Tenant catalog of project/contract identities. Selected on **Application** and **Person** when the application type enables **Project contract**.

### Add or edit a contract

1. **Configuration → Project contracts**.
2. **New** (or open a row).
3. Enter display name fields (Turkmen name is the default list label).
4. Optional **Description** for administrators.
5. Set **Approval Leg Profile** — parent route for this contract.
6. **Is Active** — inactive contracts should not be offered on new applications.
7. **Save**.

When an officer creates an application, only contracts for the selected **Approval Leg Profile** (and active flag) appear in the contract lookup.

## How officers see this

| Application type flags | Officer experience |
|------------------------|-------------------|
| Shows **Approval Leg Profile** | Pick profile on application header → contract list filters |
| Shows **Project contract** | Pick contract after profile (or directly when type allows) |
| Ministry route | Progress steps use ministries from the profile's ordered legs |

Direct-migration application types may skip ministry legs — see [Applications overview](../../applications/overview.md).

## Common problems

| Problem | What to do |
|---------|------------|
| Contract missing on new application | Check **Is Active** and **Approval Leg Profile** matches the header |
| Wrong ministry order on progress | Reorder **Ministry legs** on the profile (affects **new** applications) |
| Cannot delete ministry | In use on a profile leg — deactivate instead |
| Progress stuck between ministries | Officer workflow issue — see [Track application progress](../../applications/progress.md), not catalog edits |
