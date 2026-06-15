# Business Object: SystemSettings

## 1. Purpose

The `SystemSettings` business object is a singleton entity for **global technical limits** (upload sizes, lookup catalog manifest version). Document expiration thresholds are configured under **Configuration → Document expiration alerts** (`ExpirationAlertRule`), not here.

---

## 2. Inheritance

This object inherits from `BaseObject`.

---

## 3. Properties (officer-visible)

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `MaxImageSizeInMB` | `int` | Maximum uploaded image size (MB). | Default: 2; cap 15 |
| `MaxDocumentSizeInMB` | `int` | Maximum file attachment size (MB). | Default: 5; cap 5 |

### Legacy (hidden from UI, retained in schema)

| Property | Notes |
|----------|--------|
| `ExpirationWarningThreshold` | Never wired to runtime evaluators. |
| `DefaultExpiringSoonDays` | Superseded by `ExpirationAlertRule.ExpiringSoonDays`; code fallback uses `ExpirationAlertRule.DefaultExpiringSoonDays` (30). |

---

## 4. Business Rules & Logic

- **Singleton Pattern**: `GetOrCreateInstance` / `TryGetInstance` ensure one row per tenant.
- **`OnCreated`**: Initializes upload limits and legacy expiration defaults.

---

## 5. UI & Behavior Notes

- **Navigation**: **Configuration → Upload limits**
- **Editing**: VisaOffice officers adjust max image and document upload sizes; expiration days per document family are on **Document expiration alerts**.
