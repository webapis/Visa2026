# Business Object: RegistrationReason

## 1. Purpose

The `RegistrationReason` business object serves as a lookup for the reason behind a `Registration` event. The available reasons are dependent on the `RegistrationType` (e.g., "Check in" or "Check out").

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name` | `string` | The descriptive name of the registration reason (e.g., "Visa Extended"). | Required; Max 100 chars. |
| `RegistrationType` | `RegistrationType` (Lookup) | The parent registration type this reason belongs to. | Required. |

---

## 3. UI & Behavior Notes

- **Default Property**: The `Name` property is the default display member for lookup editors.
- **Navigation**: This object appears in the navigation menu under the "Lookup/Registration" group.
- **Filtering**: In the `Registration` Detail View, the list of available reasons is filtered based on the selected `RegistrationType`.