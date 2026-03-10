# Business Object: Education

## 1. Purpose
The `Education` business object represents the educational history of a `Person`. It stores information about the education level, institution, country, specialty, and graduation year. It inherits from `SingleActiveBaseObject`, ensuring only one education record can be active at a time for a given person. It also implements the `ISoftDelete` interface for soft deletion functionality.

---

## 2. Inheritance

This object inherits from `SingleActiveBaseObject<Person, Education>` and implements the `ISoftDelete` interface.

---

## 3. Properties

| Property Name         | Data Type             | Description                                                                 | Constraints / Validation Rules                                                                                             |
|-----------------------|-----------------------|-----------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| `EducationLevel`      | `EducationLevel`      | The level of education achieved (e.g., Bachelor's, Master's).             | Required. Lookup to `EducationLevel` business object.                                                                    |
| `EducationInstitution`  | `EducationInstitution` | The institution where the education was obtained.                           | Required. Lookup to `EducationInstitution` business object.                                                               |
| `EducationCountry`    | `Country`             | The country where the education institution is located.                       | Required. Lookup to `Country` business object.                                                                         |
| `Specialty`           | `Specialty`           | The field of study or specialization.                                         | Required. Lookup to `Specialty` business object.                                                                       |
| `GraduationYear`      | `int?`                | The year of graduation.                                                       | Required. Must be between 1950 and 10 years from the current year. Uses a `RuleCriteria` attribute for validation. |
| `Person`              | `Person`              | The person to whom this education record belongs.                             | Required.                                                                                                                |
| `Images`              | `IList<EducationImage>` | A collection of images related to the education (e.g., degree certificates). | Aggregated. Inverse property to `EducationImage.Education`.                                                               |
| `Documents`           | `IList<EducationDocument>`| A collection of documents related to the education (e.g., transcripts).      | Aggregated. Inverse property to `EducationDocument.Education`.                                                            |
| `EducationDescription`| `string`              | A calculated property providing a summary description of the education.        | Not Mapped. Read-only. Concatenates EducationLevel, EducationCountry, Specialty and GraduationYear.                     |
| `IsDeleted`           | `bool`                | Indicates whether the record has been soft deleted.                          | Browsable(false). Part of `ISoftDelete` interface.                                                                     |
| `DateDeleted`         | `DateTime?`           | The date the record was soft deleted.                                        | Browsable(false). Part of `ISoftDelete` interface.                                                                     |
| `DeletedBy`           | `ApplicationUser`     | The user who soft deleted the record.                                        | Browsable(false). Part of `ISoftDelete` interface.                                                                     |

---

## 4. Business Rules & Logic

-   **Graduation Year Range**: The `GraduationYear` property is validated to ensure it falls within a reasonable range (1950 to 10 years in the future).
-   **Single Active Education**: Inherits logic from `SingleActiveBaseObject` to ensure only one education record is active for a person at any given time.
-   **Soft Delete**: Implements the `ISoftDelete` interface, allowing records to be marked as deleted without being physically removed from the database.

---

## 5. UI & Behavior Notes

-   **Navigation**:  Located under "Lookup/Person" navigation item.
-   **Default Property**: `EducationDescription` is used as the default display property, providing a concise summary of the education record.
-   **Rule**:  `GraduationYearRange` is used to validate `GraduationYear` property.