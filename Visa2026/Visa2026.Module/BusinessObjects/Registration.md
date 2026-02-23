# Business Object: Registration

## 1. Purpose

The `Registration` business object is designed to store the details of an individual's address registration for a specific period. It creates an auditable history of a person's registrations.

---

## 2. Properties
| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Person` | `Person` (Lookup) | A required, aggregated reference to the parent `Person`. | Required. |
| `RegistrationDate` | `DateTime` | The date the registration was made. | Required. |
| `ExpirationDate` | `DateTime` | The date the registration expires. | Optional. |
| `RegistrationNumber` | `string` | The official registration number. | Optional; Max 50 chars. |
| `AddressOfResidence` | `AddressOfResidence` (Lookup) | The address for this registration. | Required. Auto-populated from Person. |
| `CurrentPassport` | `Passport` | The person's active passport at the time of registration. | Auto-populated from Person. |
| `CurrentVisa` | `Visa` | The person's active visa at the time of registration. | Auto-populated from Person. |
| `CheckPoint` | `CheckPoint` (Lookup) | The border checkpoint for entry/exit related to this registration. | Optional. |
| `PurposeOfTravel` | `PurposeOfTravel` (Lookup) | The purpose of travel for this registration. | Optional. Auto-populated from CurrentTravelHistory. |
| `CurrentPositionHistory` | `EmployeePositionHistory` | The employee's active position at the time of registration. | Auto-populated from Person (if Employee). |
| `CurrentTravelHistory` | `TravelHistory` | The person's last known travel movement at the time of registration. | Auto-populated from Person. |
| `RegistrationType` | `RegistrationType` (Lookup) | The type of registration (e.g., Check in, Check out). | Required. |

---

## 3. UI & Behavior Notes

- **Default Property**: The `RegistrationName` (Person Name - Type on Date) is the default display property in lookups and references.
- **Single Active Item**: This object inherits from `SingleActiveBaseObject`. Only one registration can be active for a `Person` at a time. Activating a new registration automatically archives the previous one.
- **Auto-population**: When a `Person` is selected, the `CurrentPassport`, `CurrentVisa`, `CurrentTravelHistory`, `AddressOfResidence`, `PurposeOfTravel`, and `CurrentPositionHistory` (if applicable) properties are automatically populated from the selected person's current active records.
- **Navigation**: This object appears in the navigation menu under the "Lookup/Person" group.