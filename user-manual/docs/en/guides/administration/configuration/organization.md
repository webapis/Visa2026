---
title: Organization settings
slug: administration/configuration/organization
locale: en
tier: 8
guideStatus: draft
bo: CompanyProfile
navPath: Configuration
roles: [Administrator, VisaOffice]
prerequisiteSlugs:
  - administration/configuration/overview
screenshotsVersion: "2026.08"
e2eScenarioId: person-officer-journey
verified: false
sourceDocs:
  - docs/LOOKUP_ORGANIZATION_SINGLETONS.md
---

# Organization settings

Maintain four **singleton** records under **Configuration** that identify your company on applications and in generated reports:

| Menu item | Purpose |
|-----------|---------|
| **Company** | Legal identity — name, address, phone, tax info, letterhead code |
| **Application Numbering** | How new application numbers are formatted and incremented |
| **Authorized Signatory** | Person who signs letters (name, position, passport) |
| **Authorized Representative** | Contact person for letters (name, position, phone, passport) |

Each type has **one row** for the tenant. Open the list, edit the existing record, and **Save** — do not create duplicate rows.

!!! tip "Related guides"
    [Configuration overview](overview.md) · Report output: [User report templates](../user-report-templates.md)

## Company

### Step 1 — Open Company

1. **Configuration → Company**.
2. Open the single row (double-click if the list shows one line).

### Step 2 — Edit company fields

| Field | Use |
|-------|-----|
| **Name** | Company name on reports and forms (required) |
| **Code** | Letterhead / asset key (for example `background_CLK.jpg`) |
| **Address**, **Phone Number**, **Email** | Contact block on merged documents |
| **Tax Information** | Tax ID or registration line when templates include it |

3. Select **Save**.

Officers do not edit this screen daily; wrong values show on every future [Resminamalar](../../applications/resminamalar.md) merge.

## Application Numbering

### Step 1 — Open Application Numbering

1. **Configuration → Application Numbering**.
2. Open the default profile row.

### Step 2 — Set numbering rules

| Field | Use |
|-------|-----|
| **App Number Prefix** | Fixed prefix (for example `TRM`) |
| **App Number Format** | Template with tokens — `{PREFIX}`, `{YEAR}`, `{YEAR2}`, `{MONTH}`, `{MONTH2}`, `{NUMBER}` |
| **Application Number Padding** | Zero-padding width for `{NUMBER}` (for example `4` → `0001`) |
| **Application Number Seed** | Starting counter when resetting numbering policy |

**Example:** Format `{PREFIX}{YEAR}-{NUMBER}` with prefix `TRM`, padding `4`, seed `0` → `TRM2026-0001`, `TRM2026-0002`, …

3. **Save** before creating applications that rely on the new format.

!!! warning "Changing format mid-year"
    Existing application numbers are not rewritten. Coordinate with the office before changing prefix or format on a live system.

## Authorized Signatory

### Step 1 — Open Authorized Signatory

1. **Configuration → Authorized Signatory**.
2. Open the single row.

### Step 2 — Enter signatory details

| Field | Use |
|-------|-----|
| **Full Name** | Signatory printed name (required) |
| **Position (Tm)** | Job title in Turkmen for letter templates |
| **Passport Number**, **Passport Authority**, **Passport Issue Date** | Passport line merged as **Passport (one line)** |

3. **Save**.

Word templates often include placeholders such as `{{AuthorizedSignatory.FullName}}` — update this record when the signatory changes.

## Authorized Representative

### Step 1 — Open Authorized Representative

1. **Configuration → Authorized Representative**.
2. Open the single row.

### Step 2 — Enter representative details

| Field | Use |
|-------|-----|
| **Full Name** | Representative name (required) |
| **Position (Tm)** | Title for templates |
| **Phone** | Contact phone on letters |
| **Passport Number**, **Passport Authority**, **Passport Issue Date** | Passport line for merges |

3. **Save**.

## Common problems

| Problem | What to do |
|---------|------------|
| Report shows old company name | **Save** Company; regenerate the report package |
| Duplicate singleton rows | Keep one row; remove extras with administrator help |
| Application number gaps | Normal after delete/cancel — only change **Seed** with office approval |
| Missing signatory on PDF/Word | Confirm template placeholders and that **Authorized Signatory** is filled |
