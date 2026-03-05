# Business Object: MedicalRecord

## 1. Purpose
The `MedicalRecord` business object represents a person's medical record, storing information about their medical history, issue and expiration dates, and related documents. It inherits from `SingleActiveBaseObject`, ensuring only one medical record can be active at a time for a given person. It also implements the `IExpirationLogic` and `ISoftDelete` interfaces.

---

## 2. Inheritance

This object inherits from `SingleActiveBaseObject<Person, MedicalRecord>` and implements the `IExpirationLogic` and `ISoftDelete` interfaces.

---

## 3. Properties

| Property Name         | Data Type             | Description                                                                 | Constraints / Validation Rules                                                                                                                                                                                            |
|-----------------------|-----------------------|-----------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `DocumentNumber`      | `string`              | The unique number or identifier of the medical record document.            | Required.  Maximum length of 50 characters.                                                                                                                                                                            |
| `IssueDate`           | `DateTime`            | The date the medical record was issued.                                       | Required. `ImmediatePostData` is used to trigger the `UpdateExpirationDate` method.                                                                                                                               |
| `ValidityDuration`    | `ValidityDuration`    | The duration for which the medical record is valid.                           | Required. `ImmediatePostData` is used to trigger the `UpdateExpirationDate` method.                                                                                                                               |
| `ExpirationDate`      | `DateTime?`           | The date the medical record expires. Calculated automatically.              | Read-only. Calculated based on `IssueDate` and `ValidityDuration`.                                                                                                                                                     |
| `Person`              | `Person`              | The person to whom this medical record belongs.                             | Required.                                                                                                                                                                                                             |
| `Documents`           | `IList<MedicalRecordDocument>` | A collection of documents related to the medical record.                       | Aggregated. Inverse property to `MedicalRecordDocument.MedicalRecord`.                                                                                                                                      |
| `Images`              | `IList<MedicalRecordImage>`| A collection of images related to the medical record.                          | Aggregated. Inverse property to `MedicalRecordImage.MedicalRecord`.                                                                                                                                         |
| `DaysRemaining`       | `int`                 | A calculated property showing the number of days until the medical record expires. | Read-only. Part of `IExpirationLogic` interface.                                                                                                                                                             |
| `ExpirationState`     | `ExpirationState`     | A calculated property indicating the status (e.g., Active, ExpiringSoon).    | Read-only. Part of `IExpirationLogic` interface.                                                                                                                                                             |
| `ChronologicalSortDate`| `DateTime?`          | Used for chronological sorting.                                              | Not Mapped. Read-only. Returns the `IssueDate`.                                                                                                                                                                        |
| `IsDeleted`           | `bool`                | Indicates whether the record has been soft deleted.                          | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                  |
| `DateDeleted`         | `DateTime?`           | The date the record was soft deleted.                                        | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                  |
| `DeletedBy`           | `ApplicationUser`     | The user who soft deleted the record.                                        | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                  |

---

## 4. Business Rules & Logic

-   **Expiration Date Calculation**: The `ExpirationDate` is automatically calculated based on the `IssueDate` and `ValidityDuration`. The `UpdateExpirationDate` method handles this calculation.
-   **Single Active Medical Record**: Inherits logic from `SingleActiveBaseObject` to ensure only one medical record is active for a person at any given time.
-   **Expiration Logic**: Implements the `IExpirationLogic` interface for tracking expiration status.
-   **Soft Delete**: Implements the `ISoftDelete` interface, allowing records to be marked as deleted without being physically removed from the database.

---

## 5. UI & Behavior Notes

-   **Navigation**: Located under "Lookup/Person" navigation item.
-   **Default Property**: `DocumentNumber` is used as the default display property.
-   **Immediate Post Data**:  `IssueDate` and `ValidityDuration` properties use `ImmediatePostData` to update `ExpirationDate` automatically.
-   **Rule**: `MedicalRecord_DateRange` is used to validate `ExpirationDate` property.