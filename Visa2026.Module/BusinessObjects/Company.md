# Business Object: Company

## 1. Purpose

The `Company` business object represents a company or organization within the system. It serves as a central container for company-specific information, including contact details, employees, authorized signatories, and representatives. It also plays a role in system-wide configurations, such as defining the default company and application numbering conventions.

---

## 2. Inheritance

This object inherits from `BaseObject`.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name` | `string` | The legal name of the company. | Required. |
| `Address` | `string` | The physical address of the company. | |
| `PhoneNumber` | `string` | The primary contact phone number. | |
| `Email` | `string` | The primary contact email address. | |
| `TaxInformation` | `string` | The company's tax identification information. | |
| `AppNumberPrefix` | `string` | A prefix used for generating unique application numbers. | |
| `ApplicationNumberPadding` | `int` | The number of digits to use for padding in auto-generated application numbers. | Default: 4. |
| `IsDefault` | `bool` | A flag to indicate if this is the default company for the system. | |
| `CurrentAuthorizedSignatory` | `CompanyHead` | A reference to the currently active authorized signatory for the company. | |
| `CurrentRepresentative` | `Representative` | A reference to the currently active representative for the company. | |

---

## 4. Collections (Relationships)

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `Heads` | `CompanyHead` | A collection of all authorized signatories associated with the company. | Aggregated | `CompanyHead.Company` |
| `Representatives` | `Representative` | A collection of all representatives associated with the company. | Aggregated | `Representative.Company` |
| `LocalEmployees` | `LocalEmployee` | A collection of all local employees belonging to the company. | | `LocalEmployee.Company` |
| `Employees` | `Employee` | A collection of all expat employees belonging to the company. | | `Employee.Company` |
| `FamilyMembers` | `FamilyMember` | A collection of all family members associated with the company's employees. | | |

---

## 5. Business Rules & Logic

- **Default Company Uniqueness (`OnSaving`)**: The system ensures that only one company can be marked as the default (`IsDefault = true`) at any given time. When a company is saved with `IsDefault` set to `true`, the `OnSaving` method automatically finds any other company that was previously marked as the default and sets its `IsDefault` flag to `false`.

---

## 6. UI & Behavior Notes

- When a new `Application` is created, it defaults to the company where `IsDefault` is `true`.
- The `CurrentAuthorizedSignatory` and `CurrentRepresentative` properties are managed by the `SingleActiveBaseObject` logic within the `CompanyHead` and `Representative` classes, respectively.