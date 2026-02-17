# Business Object: WorkPermitItem

## 1. Purpose

The `WorkPermitItem` business object is designed to manage the lifecycle of an employee's work permit. It stores all essential details, including the permit's validity dates, the associated employee and position, and references to the official documents and application processes that authorized it.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Employee` | `Employee` (Lookup) | The employee to whom the work permit is issued. | Required. |
| `Passport` | `Passport` (Lookup) | The passport associated with this work permit. | Required. |
| `Position` | `Position` (Lookup) | The employee's current position, populated automatically when an employee is selected. | Required. |
| `StartDate` | `DateTime` | The date the work permit becomes valid. | Required. |
| `ExpirationDate` | `DateTime` | The date the work permit expires. | Required; Must be after `StartDate`. |
| `WorkPermitNumber` | `string` | The official approval number of the work permit. | Required. |
| `ASNumber` | `string` | An alternative identification or application number. | Optional. |
| `WorkPermit` | `WorkPermit` (Lookup) | A reference to the official letter that granted the permit. | Optional. |
| `Location` | `WorkPermitLocation` (Lookup) | The geographical location where the work is permitted. | Optional. |
| `ProcessNumber` | `PersonInApplication` (Lookup) | A reference to the application process that resulted in this permit. | Optional. |
| `IsActive` | `bool` | Indicates if this is the currently active work permit. | - |

---

## 3. Business Rules & Logic

- The `ExpirationDate` must always be later than the `StartDate`.
- The `Passport` and `Position` properties are automatically populated based on the selected `Employee`.

---

## 4. Relationships to Other Objects

- **`Employee` (Employee)**: A many-to-one relationship to the `Employee` object.
- **`Passport` (Passport)**: A many-to-one relationship to the `Passport` object.
- **`PersonInApplication` (PersonInApplication)**: A lookup relationship to trace the permit back to its originating application line.
- **`WorkPermit` (WorkPermit)**: A lookup relationship to the official approval letter.
- **`Location` (WorkPermitLocation)**: A lookup relationship to the location where work is permitted.

---

## 5. UI & Behavior Notes

- Active work permits are highlighted in **Green/Bold** in List Views.