# Business Object: Registration

## 1. Purpose

The `Registration` business object is designed to store the details of an individual's registration.

---

## 2. Properties
| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Person` | `Person` | A required, aggregated reference to the parent `Person`. | Required. Read-only in UI. |
| `RegistrationDate` | `DateTime` | The date the registration was made. | Required. |
| `ExpirationDate` | `DateTime` | The date the registration expires. | Optional. |
| `RegistrationNumber` | `string` | The official registration number. | Optional; Max 50 chars. |
| `Application` | `Application` | The application associated with this registration. | | |
| `IsDeleted`        | `bool`        | Indicates whether the record has been soft deleted.                         | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DateDeleted`      | `DateTime?`   | The date the record was soft deleted.                                       | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DeletedBy`        | `ApplicationUser`| The user who soft deleted the record.                                      | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `RegistrationName` | `string` | A calculated property used for display purposes. | Read-only; Not Mapped; Not Browsable. |
|`PersonNationality` | `string` | A calculated property showing the nationality of the registered person. | Read-only; Not Mapped; Not Browsable. |
|`PersonDateOfBirth` | `DateTime?` | A calculated property showing the date of birth of the registered person. | Read-only; Not Mapped; Not Browsable. |
|`PersonPassportNumber` | `string` | A calculated property showing passport number of the registered person. | Read-only; Not Mapped; Not Browsable. |
|`PersonVisaNumber`| `string`| A calculated property showing visa number of the registered person. | Read-only; Not Mapped; Not Browsable. |

---

## 3. UI & Behavior Notes

- **Default Property**: The `RegistrationName` (Person Name - Type on Date) is the default display property in lookups and references.

- **Single Active Item**: This object inherits from `SingleActiveBaseObject`. Only one registration can be active for a `Person` at a time. Activating a new registration automatically archives the previous one.
- **Auto-population**: When a `Person` is selected, the `CurrentPassport`, `CurrentVisa`, `CurrentTravelHistory`, `AddressOfResidence`, and `CurrentPositionHistory` (if applicable) properties are automatically populated from the selected person's current active records.
- **Navigation**: This object appears in the navigation menu under the "Lookup/Person" group.
- **Conditional UI**: The `Employee` and `FamilyMember` properties are conditionally displayed based on the `IsForFamily` property of the parent `Application`.