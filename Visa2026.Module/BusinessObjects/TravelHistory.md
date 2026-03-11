# Business Object: TravelHistory

## 1. Purpose
The `TravelHistory` business object represents the travel history of a `Person`. It stores information about their trips, including dates, types of travel, locations, and purpose. It inherits from `SingleActiveBaseObject`, ensuring only one travel history record can be active at a time for a given person. It also implements the `ISoftDelete` interface for soft deletion functionality.

---

## 2. Inheritance

This object inherits from `SingleActiveBaseObject<Person, TravelHistory>` and implements the `ISoftDelete` interface.

---

## 3. Properties

| Property Name    | Data Type    | Description                                                              | Constraints / Validation Rules                                                                                                                                                                                                   |
|-------------------|-------------|--------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Person`           | `Person`      | The person to whom this travel history record belongs.                      | Required.                                                                                                                                                                                                                    |
| `TravelDate`       | `DateTime`    | The date of the travel.                                                    | Required.                                                                                                                                                                                                                    |
| `TravelType`       | `TravelType?` | The type of travel (e.g., Internal, External).                             | Required.                                                                                                                                                                                                                    |
| `MovementType`     | `MovementType?`| The type of movement (e.g., Arrival, Departure).                           | Required.                                                                                                                                                                                                                    |
| `CheckPoint`       | `CheckPoint`  | The border checkpoint used for external travel.                            | Required if `TravelType` is `External`. Hidden in the UI if `TravelType` is not `External`.                                                                                                                                  |
| `FromLocation`     | `string`      | The location from which the travel originated.                             | Maximum length of 100 characters.                                                                                                                                                                                            |
| `ToLocation`       | `string`      | The destination location of the travel.                                    | Maximum length of 100 characters.                                                                                                                                                                                            |
| `PurposeOfTravel`  | `PurposeOfTravel`| The purpose of the travel.                                               |                                                                                                                                                                                                                              |
| `Notes`            | `string`      | Any additional notes or details about the travel.                          |                                                                                                                                                                                                                              |
| `Title`            | `string`      | A calculated property providing a summary title of the travel history record. | Not Mapped. Read-only. Concatenates Person's FullName, MovementType, and TravelDate. Used as the default display property.                                                                                               |
| `ChronologicalSortDate`| `DateTime?` | Used for chronological sorting.                                           | Not Mapped. Read-only. Returns the `TravelDate`.                                                                                                                                                                             |
| `IsDeleted`        | `bool`        | Indicates whether the record has been soft deleted.                         | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DateDeleted`      | `DateTime?`   | The date the record was soft deleted.                                       | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DeletedBy`        | `ApplicationUser`| The user who soft deleted the record.                                      | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |

---

## 4. Business Rules & Logic

-   **Travel Type and Checkpoint**: The `CheckPoint` property is only required and visible when the `TravelType` is set to `External`.
-   **Single Active Travel History**: Inherits logic from `SingleActiveBaseObject` to ensure only one travel history record is active for a person at any given time.
-   **Soft Delete**: Implements the `ISoftDelete` interface, allowing records to be marked as deleted without being physically removed from the database.

---

## 5. UI & Behavior Notes

-   **Navigation**: Located under "Lookup/Person" navigation item.
-   **Default Property**: `Title` is used as the default display property, providing a concise summary of the travel history record.
-   **Immediate Post Data**: The `TravelType` property uses `ImmediatePostData` to dynamically show/hide the `CheckPoint` property.