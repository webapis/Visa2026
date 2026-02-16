# Business Object: Passport

## 1. Purpose

The `Passport` business object stores details about an individual's passport. It serves as a parent object for `Visa` records and is linked to a `Person`.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `PassportNumber` | `string` | The unique alphanumeric identifier of the passport. | Required; Unique; Max 20 chars. |
| `PassportType` | `PassportType` (Lookup) | The type of passport (e.g., Regular, Diplomatic). | Optional. |
| `IssueDate` | `DateTime` | The date the passport was issued. | - |
| `ExpirationDate` | `DateTime` | The date the passport expires. | - |
| `Authority` | `string` | The authority that issued the passport. | Optional; Max 100 chars. |
| `Person` | `Person` (Lookup) | A reference to the passport holder. | Required; Aggregated. |
| `Visas` | `List<Visa>` | A collection of visas associated with this passport. | Aggregated. |

---

## 3. UI & Behavior Notes

- **Single Active Item**: This object inherits from `SingleActiveBaseObject`. Only one passport can be active for a `Person` at a time. Activating a new passport automatically archives the previous one.
- **Expiration Logic**: This object implements `IExpirationLogic`. The system tracks `ExpirationDate` to determine if the passport is Active, Expiring Soon, or Expired.
- **Navigation**: This object appears in the navigation menu under the "Lookup/Person" group.