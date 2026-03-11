# Business Object: BusinessTrip

## 1. Purpose

The `BusinessTrip` business object represents a business trip undertaken by an employee. It tracks essential details such as the destination, duration, purpose, and current status of the trip. It also links the trip to a specific `Application` if applicable.

---

## 2. Inheritance

This object inherits from `SingleActiveBaseObject<Employee, BusinessTrip>`. This inheritance structure ensures that an employee can have only one "active" business trip at a time (e.g., the one currently ongoing or most recently planned), automatically managing the `CurrentBusinessTrip` property on the `Employee` object.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Employee` | `Employee` | The employee going on the business trip. | Required. |
| `Purpose` | `string` | The reason or objective of the trip. | Required; Max 255 chars. |
| `DestinationCountry` | `Country` | The country where the business trip takes place. | Required. |
| `DestinationCity` | `string` | The specific city of destination. | Optional; Max 100 chars. |
| `StartDate` | `DateTime` | The date the trip begins. | Required. |
| `EndDate` | `DateTime` | The date the trip ends. | Required. |
| `Status` | `BusinessTripStatus` (Enum) | The current status of the trip (Planned, Ongoing, Completed, Cancelled). | |
| `Application` | `Application` | The application associated with this business trip, if any. | |
| `Address` | `BusinessTripAddress` | Detailed address information for the trip. | Aggregated. |
| `DefaultProperty` | `string` | A calculated field used for display purposes (`Employee - Purpose`). | Read-only; Not Mapped; Not Browsable. |
| `IsDeleted`        | `bool`        | Indicates whether the record has been soft deleted.                         | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DateDeleted`      | `DateTime?`   | The date the record was soft deleted.                                       | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DeletedBy`        | `ApplicationUser`| The user who soft deleted the record.                                      | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |

---

## 4. Relationships to Other Objects

- **`Employee`**: A required, many-to-one relationship. The `Employee` object maintains a collection of `BusinessTrips`.
- **`Application`**: An optional, many-to-one relationship linking the trip to a visa or migration application.
- **`BusinessTripAddress`**: A one-to-one aggregated relationship containing specific address details.

---

## 5. Business Rules & Logic

- **Single Active Item**: As a `SingleActiveBaseObject`, setting a `BusinessTrip` as active (via the `IsActive` property inherited from the base class) will automatically set it as the `CurrentBusinessTrip` on the associated `Employee` and deactivate any other active trips for that employee.

---

## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Application" group.
- **Default Property**: The calculated `DefaultProperty` (Employee Name - Purpose) is used as the display name in lookups and list views.