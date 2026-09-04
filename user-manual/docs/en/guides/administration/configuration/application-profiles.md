---
title: Configure application profiles
slug: administration/configuration/application-profiles
locale: en
tier: 8
guideStatus: draft
bo: ApplicationProfile
navPath: Configuration
roles: [VisaOffice, Administrator]
prerequisiteSlugs:
  - getting-started/navigation
  - administration/configuration/overview
  - applications/application-profiles
screenshotsVersion: "2026.08-preview"
verified: false
sourceDocs:
  - docs/APPLICATION_PROFILE_PLAN.md
---

# Configure application profiles

**Application Profiles** replace the old per-type flags officers used to infer from **Application Type (Deprecated)**. Each profile is a **reusable configuration package**: route, related-to family, field visibility, approval legs, person-data requirements, nested templates, and **defaults** for per-application fields.

Officers **pick** a profile when they create an application ([Application profiles — how configuration works](../../applications/application-profiles.md)). This guide is for **VisaOffice** and **administrator** accounts that maintain profiles.

!!! info "Screenshots (preview)"
    The **Configure profile** wizard is new. Steps below follow the live wizard labels. Officer screenshots will be added in a later manual release — use the wizard on a test profile while learning.

!!! warning "Office-wide impact"
    Changes to a profile affect **all applications** that link to it (live configuration). Per-application values officers already entered are **not** overwritten.

## Before you start

| You need | Notes |
|----------|--------|
| **VisaOffice** or **Administrator** role | **Configuration** menu visible |
| Agreement on procedure families | Issuance, cancellation, registration, business trip — one **Related to** per profile |
| Route | **Via ministries** vs **Direct migration** — must match the Applications list officers use |

## Open Application Profiles

1. Sign in with a **VisaOffice** or administrator account.
2. In the left menu, expand **Configuration**.
3. Select **Application Profiles**.
4. Wait for the list to load.

Use **New** for a blank profile row, or select an existing row and **Configure profile** for the wizard.

## Configure profile wizard (five steps)

Select a **saved** profile, then **Configure profile** on the toolbar. The wizard opens in place of the standard detail form.

| Step | Purpose |
|------|---------|
| **1 — Identity** | Application name, description, code, audience (employee / family / visitor), **Related to**, **Directed to** |
| **2 — Results & fields** | Produce/cancel flags; which per-application fields are required |
| **3 — Process & SLA** | Progress route behaviour, ministry/migration SLA days, **approval legs** (add/remove ministries) |
| **4 — Templates & person** | Nested templates hint; person-data toggles (passport, education, position, address, …) |
| **5 — Review & save** | Summary → **Save profile** |

!!! tip "Live FK model"
    Officers do **not** get a copy of this wizard on each application. They link to this profile once at create; configuration changes here apply live until **config lock** (below).

### Step 1 — Identity & purpose

- **Application name** — label officers see in the picker (for example *Invitation + work permit (employee)*).
- **Code** — short stable key (often matches legacy type code).
- **Selection / quick code** — optional three-digit quick pick (legacy compatibility).
- **Related to** — exclusive: **Issuance**, **Cancellation**, **Registration**, or **Business trip**.
- **Directed to** — **Via ministries** or **Direct migration** (must match the Applications list filter).
- **Audience** — **For employee**, **For family member**, **For temporary visitor** (who may use this profile).

### Step 2 — Results & fields

- Turn on **produce** / **cancel** capabilities that match the procedure (invitation, work permit, visa, border zone, …).
- Set **Require** flags for per-application fields (visa type, contract, dates, entry check point, …).
- Set **defaults** where the office always uses the same lookup (for example default **Visa Type**).

Defaults copy to new applications **once** at create; officers may change them on the application afterward.

### Step 3 — Process & SLA

- Confirm **Directed to** matches how progress will run.
- Set **Ministry SLA (days)** and **Migration SLA (days)** when your office uses working-day warnings.
- Add **approval legs** in order (ministry sequence for via-ministry profiles). Use **Add leg** / remove controls in the wizard.

### Step 4 — Templates & person

- Review **nested templates** — attach Word/Excel/PDF template files on the standard profile detail nested list when needed (wizard may point you there).
- Enable **person-data** toggles that match templates and readiness (passport, education, position, local address, …).

### Step 5 — Review & save

1. Review the summary.
2. Select **Save profile**.
3. Wait for confirmation.

Officers can now pick this profile from **Applications → New**.

## Config lock (read-only profile)

When **any** application using this profile has left **office preparation** (submitted / past draft office work), the profile becomes **config locked**:

- **Configure profile** and standard profile edit are **read-only**.
- **New** applications may **still** select this profile.
- Per-application fields on existing files remain editable.

To publish a **variant** configuration, select the profile on the list and use **Clone** to duplicate, then edit the copy.

## Applicability criteria (advanced)

On the standard profile detail form, **Applicability criteria** (optional) limits when a profile appears in the picker. Leave empty to show the profile for all applications on the matching route (subject to **Active**). Ask your technical lead before editing criteria expressions.

## Relationship to deprecated Application Type

During migration, each legacy **Application Type** may have a matching **Application Profile** (same code). Officers should use the **profile picker**; **Application Type (Deprecated)** on the application detail is filled for compatibility and is not the long-term configuration surface.

## Common problems

| Problem | What to do |
|---------|------------|
| **Configure profile** disabled | Save the profile row first (**New** → fill identity → **Save**) |
| Wizard read-only | Profile is **config locked** — **Clone** or wait until no linked application has left office prep |
| Officers do not see profile on ministry list | **Directed to** must be **Via ministries** |
| Defaults not on new application | Set defaults on step 2; officer must pick profile via **Use profile (live link)** |
| Wrong fields on application header | Adjust **Require** / produce flags on the profile, not on each application |

## What to read next

- [Application profiles — officer overview](../../applications/application-profiles.md)
- [Create an application](../../applications/create.md)
- [Configuration overview](overview.md)
- [Contracts and approvals](contracts-and-approvals.md) — project contracts and approval legs
- [SLA settings](sla.md) — migration SLA profiles (legacy complement)
