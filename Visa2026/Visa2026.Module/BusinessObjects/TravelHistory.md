# Business Object: TravelHistory

## 1. Purpose

The `TravelHistory` business object logs the travel movements of a `Person`, both within the country and internationally. It serves as an audit trail for entries and exits.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Person` | `Person` (Lookup) | A required, aggregated reference to the parent `Person`. | Required. |
| `TravelDate` | `DateTime` | The date and time of the travel event. | Required. |
| `TravelType` | `TravelType` (Enum) | The type of travel: `Internal` or `External`. | Required. |
| `MovementType` | `MovementType` (Enum) | The direction of movement: `Entry` or `Exit`. | Required. |
| `CheckPoint` | `CheckPoint` (Lookup) | The border checkpoint used for external travel. | Required if `TravelType` is `External`. |
| `FromLocation` | `string` | The point of origin for the travel. | Optional; Max 100 chars. |
| `ToLocation` | `string` | The destination of the travel. | Optional; Max 100 chars. |
| `PurposeOfTravel` | `PurposeOfTravel` (Lookup) | The purpose of the travel. | Optional. |
| `Notes` | `string` | Additional notes or comments about the travel event. | Optional. |

---

## 3. UI & Behavior Notes

- **Single Active Item**: This object inherits from `SingleActiveBaseObject`. Only one travel history record can be active for a `Person` at a time. Activating a new record automatically archives the previous one.
- **Default Property**: The `Title` (Person Name - Movement on Date) is the default display property in lookups and references.
- **Navigation**: This object appears in the navigation menu under the "Lookup/Person" group.