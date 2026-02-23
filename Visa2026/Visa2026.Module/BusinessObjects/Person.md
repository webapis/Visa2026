# Business Object: Person

## 1. Purpose

The `Person` business object is designed to store fundamental information about an individual. This can be used for contacts, employees, customers, or any other role within the application that requires personal details.

---

## 2. Properties

This section details the data fields of the `Person` object.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `FirstName`   | `string`  | The person's given name. | Required; Max 100 chars. |
| `MiddleName`  | `string`  | The person's middle name. | Optional; Max 100 chars. |
| `LastName`    | `string`  | The person's family name. | Required; Max 100 chars. |
| `FullName`    | `string`  | A calculated, read-only field combining `FirstName` and `LastName`. | Read-only. Generated automatically. |
| `Email`       | `string`  | The primary email address for the person. | Must be a valid email format; Max 255 chars. |
| `DateOfBirth` | `DateTime`| The person's date of birth. | Required. |
| `Age`         | `int`     | The person's current age, calculated from `DateOfBirth`. | Read-only. Calculated automatically. |
| `Photo`       | `byte[]`  | A profile picture for the person, stored as an image. | - |
| `BirthPlace`  | `string`  | The city or town where the person was born. | Optional; Max 100 chars. |
| `BirthCountry`| `Country` | A reference to the country where the person was born. | Optional. |
| `Gender`      | `Gender`  | A reference to the person's gender. | Optional. |
| `IsArchived`  | `bool`    | Indicates if the person record is archived. | - |
| `MaritalStatus`| `MaritalStatus`| A reference to the person's marital status. | Optional. |
| `ForeignAddress` | `string` | The person's address in their home country. | Optional; Max 255 chars. |
| `ProjectContract` | `ProjectContract` | The project contract the person is associated with. | Optional. |
| `ForeignAddressCountry` | `Country` | A reference to the person's home country. | Optional. |
| `CurrentPassport` | `Passport` | The currently active passport. | Read-only. Managed automatically. |
| `CurrentVisa`    | `Visa`     | The currently active visa. | Read-only. Managed automatically. |
| `CurrentAddressOfResidence` | `AddressOfResidence` | The currently active address of residence. | Read-only. Managed automatically. |
| `CurrentInvitationItem` | `InvitationItem` | The currently active invitation item. | Read-only. Managed automatically. |
| `CurrentRejectionItem` | `RejectionItem` | The currently active rejection item. | Read-only. Managed automatically. |
| `CurrentRegistration` | `Registration` | The currently active registration. | Read-only. Managed automatically. |
| `CurrentTravelHistory` | `TravelHistory` | The currently active travel history record. | Read-only. Managed automatically. |
| `RejectionItems` | `List<RejectionItem>` | A list of all rejection line items this person is included in. | Association. |
| `Registrations` | `List<Registration>` | A list of all registration records for this person. | Aggregated. |
| `TravelHistories` | `List<TravelHistory>` | A log of all travel movements for this person. | Aggregated. |

---

## 3. Business Rules & Logic

- The `FullName` property is a persistent alias, automatically concatenating `FirstName` and `LastName`. It is not editable directly in the UI.
- The system validates that the `Email` property contains a correctly formatted email address upon saving.
- When a `DateOfBirth` is selected that results in an `Age` less than 18, the `MaritalStatus` is automatically set to "Çaga" (Child).
- If the `DateOfBirth` is changed and the `Age` becomes 18 or older, the `MaritalStatus` is automatically cleared if it was previously "Çaga".

---

## 4. UI & Behavior Notes
- In List Views, the `FullName` property should be the default display column for identifying a person.
- The `Photo` property should be rendered as an image editor in Detail Views.

---

# Business Object: Education

## 1. Purpose

The `Education` business object is designed to store information about a person's educational background, including degrees, institutions, and specialties.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---|---|---|---|
| `EducationLevel` | `EducationLevel` | The level of education (e.g., High School, Bachelor's, Master's). | Required. |
| `EducationInstitution` | `EducationInstitution` | The name of the educational institution. | Required. |
| `EducationCountry` | `Country` | A reference to the country where the institution is located. | Required. |
| `Specialty` | `Specialty` | The field of study or specialty. | Optional. |
| `HasEducationPeriod` | `bool` | Indicates if the education has a defined start and end date. | Default: False. |
| `EducationStartDate` | `DateTime`| The start date of the education period. | Visible only if `HasEducationPeriod` is true. |
| `EducationEndDate` | `DateTime` | The end date of the education period. | Visible only if `HasEducationPeriod` is true. |
| `Person` | `Person` | A reference back to the person this education belongs to. | Required. |

---

## 3. Business Rules & Logic

- The `EducationStartDate` and `EducationEndDate` properties are only visible and editable when `HasEducationPeriod` is set to `true`.
- `EducationEndDate` must be greater than `EducationStartDate` if both are provided.

# Business Object: MedicalRecord

## 1. Purpose

The `MedicalRecord` business object is designed to store information about a person's medical health checks, which are often required for Invitation and Visa procedures. These checks are typically required every 6 months.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---|---|---|---|
| `DocumentNumber` | `string` | The unique number of the medical certificate/document. | Required; Max 50 chars. |
| `IssueDate` | `DateTime` | The date the medical check was performed. | Required. |
| `Person` | `Person` | A reference back to the person. | Required. |