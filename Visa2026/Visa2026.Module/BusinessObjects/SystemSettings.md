# Business Object: SystemSettings

## 1. Purpose

The `SystemSettings` business object is a singleton entity designed to hold global configuration values for the application. This provides a centralized, UI-editable location for parameters that affect system-wide behavior.

---

## 2. Inheritance

This object inherits from `BaseObject`.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `ExpirationWarningThreshold` | `decimal` | The threshold at which an item is considered 'Expiring Soon'. Enter as a decimal (e.g., 0.90 for 90%). | Default: 0.90 |

---

## 4. Business Rules & Logic

- **Singleton Pattern**: The `GetInstance(IObjectSpace objectSpace)` static method ensures that only one instance of `SystemSettings` exists in the database. If no instance is found, it creates one with default values.
- **`OnCreated`**: When a new `SystemSettings` object is created for the first time, `ExpirationWarningThreshold` is initialized to `0.9m`.

---

## 5. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "System" group.
- **Editing**: Administrators can modify these settings through the standard Detail View to dynamically adjust application behavior without requiring code changes or a new deployment.