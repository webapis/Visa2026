---
title: Alerts and upload limits
slug: administration/configuration/alerts-and-upload-limits
locale: en
tier: 8
guideStatus: draft
bo: ExpirationAlertRule
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

# Alerts and upload limits

Two **Configuration** areas control **document expiry warnings** and **file upload size**:

| Menu item | Record type | Purpose |
|-----------|-------------|---------|
| **Document expiration alerts** | Catalog (one row per document type) | Calendar days before expiry for **expiring soon** styling |
| **Upload limits** | Singleton | Maximum image and attachment size (MB) |

## Document expiration alerts

Each row defines thresholds for a document family (visa, passport, work permit item, registration, and similar). Officers see **expiring soon** states on lists, detail forms, and [Report Dashboard](../../tracking/report-dashboard.md) when a record enters the window.

### Edit alert rules

1. **Configuration → Document expiration alerts**.
2. Open the row for the document type (display name identifies the rule).
3. Set:
   - **Duýduryş (gün)** / **Expiring soon days** — calendar days **before** `Expiration date` when the record becomes expiring-soon (required, > 0)
   - **Uzaltma arzasy (gün)** / **Extension application days** — optional; only for **Visa** and **Work permit item** rules — days before expiry when an extension application should start
4. **Save**.

!!! note "Seeded document types"
    The list is seeded per deployment. You edit thresholds, not the underlying document type key. Adding new document families is an administrator/developer task.

### Extension application days

| Document type | Extension days field |
|---------------|---------------------|
| Visa | Shown — optional |
| Work permit item | Shown — optional |
| Other types | Hidden — do not set |

## Upload limits

Singleton caps for scans and file attachments across person and application forms.

### Edit limits

1. **Configuration → Upload limits**.
2. Open the single row.
3. Set:
   - **Max image size (MB)** — photos and image uploads (product default 2 MB; hard cap 15 MB)
   - **Max document size (MB)** — PDF and other attachments (product default 5 MB; hard cap 5 MB)
4. **Save**.

Values above the hard cap are rejected on save. If officers need larger files, compress scans or split documents — the cap protects server stability.

!!! info "Legacy fields hidden"
    Older expiry fields on this form are unused. Maintain per-document rules under **Document expiration alerts** instead.

## Common problems

| Problem | What to do |
|---------|------------|
| Upload fails with size error | Lower file size or raise limits within the MB cap on **Upload limits** |
| Visa not flagged expiring soon | Open the **Visa** alert row — increase **Expiring soon days** or check expiration date on the record |
| Extension reminder wrong | Edit **Extension application days** on Visa / Work permit item rules only |
| Dashboard and person disagree | Both use the same rules — refresh lists; confirm expiration date is set |
