# Business Object: ApplicationType

## 1. Purpose

The `ApplicationType` business object is a lookup entity that defines the different types of applications that can be processed in the system. It plays a crucial role in controlling the UI by determining which fields are visible and required for a given application process.

---

## 2. Inheritance

This object inherits from the `LookupBase` class, which provides the standard `Name` and `Code` properties.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Category` | `ApplicationTypeCategory` (Enum) | Specifies if the application type is for an `Employee`, `FamilyMember`, or `Both`. | Required. |
| `OrganizationType` | `OrganizationType` (Lookup) | A required reference to the parent organization type (e.g., "Iş Buýrujy", "Migrasiýa"). | Required. |
| `ShowProjectContract` | `bool` | Controls visibility of the `ProjectContract` field in the `Application` Detail View. | - |
| `ShowVisaPeriod` | `bool` | Controls visibility of the `VisaPeriod` field in the `Application` Detail View. | - |
| `ShowVisaCategory` | `bool` | Controls visibility of the `VisaCategory` field in the `Application` Detail View. | - |
| `ShowMinistry` | `bool` | Controls visibility of the `Ministry` field in the `Application` Detail View. | - |
| `CanRequireWorkPermit` | `bool` | Controls visibility of the `IsWorkPermitRequired` checkbox in the `Application` Detail View. | - |
| `ShowPreviousPassport` | `bool` | Controls visibility of the `PreviousPassport` field in the `ApplicationItem` Detail View. | - |
| `ShowCurrentVisa` | `bool` | Controls visibility of the `CurrentVisa` field in the `ApplicationItem` Detail View. | - |
| `ShowCurrentWorkPermit` | `bool` | Controls visibility of the `CurrentWorkPermit` field in the `ApplicationItem` Detail View. | - |
| `ShowCurrentInvitation` | `bool` | Controls visibility of the `CurrentInvitation` field in the `ApplicationItem` Detail View. | - |
| `ShowCurrentAddressOfResidence` | `bool` | Controls visibility of the `CurrentAddressOfResidence` field in the `ApplicationItem` Detail View. | - |
| `ShowCurrentRegistration` | `bool` | Controls visibility of the `CurrentRegistration` field in the `ApplicationItem` Detail View. | - |
| `ShowCurrentEmployeeContract` | `bool` | Controls visibility of the `CurrentEmployeeContract` field in the `ApplicationItem` Detail View. | - |
| `ShowCurrentMedicalRecord` | `bool` | Controls visibility of the `CurrentMedicalRecord` field in the `ApplicationItem` Detail View. | - |

---

## 4. Relationships to Other Objects

- **`OrganizationType` (OrganizationType)**: A many-to-one relationship to the `OrganizationType` object. This creates a hierarchical structure where each `ApplicationType` belongs to a specific `OrganizationType`.

---

## 5. UI & Behavior Notes

- This object is managed under the `Lookup/Application` navigation item.
- The boolean "Show" properties are used in `Appearance` rules on the `Application` and `ApplicationItem` business objects to dynamically control the UI based on the selected `ApplicationType`.

---

## 6. Data Seeding

The data for this object is seeded from JSON files (`applicationtypes_migrasiya.json`, `applicationtypes_isbuyrujy.json`) located in the `Visa2026.Module/DatabaseUpdate` folder. The seeding logic is handled by the `Updater.cs` class.