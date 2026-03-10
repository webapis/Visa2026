# Business Object: Representative

## 1. Purpose

The `Representative` business object designates a specific person, either a `LocalEmployee` or an expat `Employee`, as an authorized representative for a `Company`. This object is crucial for identifying the individual responsible for liaising with authorities on behalf of the company in official applications.

---

## 2. Inheritance

This object inherits from `SingleActiveBaseObject<Company, Representative>`. This implementation of the "Single Active Item" pattern ensures that a `Company` can have only one `CurrentRepresentative` at any given time.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules | UI Notes |
|---------------|-----------|-------------|--------------------------------|----------|
| `Company` | `Company` | The parent company this representative is associated with. | | |
| `IsLocalEmployee` | `bool` | A flag to determine if the representative is a local or expat employee. | | `ImmediatePostData` enabled. Toggles the visibility of the `LocalEmployee` and `Employee` fields. |
| `LocalEmployee` | `LocalEmployee` | A reference to the local employee acting as the representative. | Data source filtered by `Company`. | Hidden if `IsLocalEmployee` is `false`. |
| `Employee` | `Employee` | A reference to the expat employee acting as the representative. | | Hidden if `IsLocalEmployee` is `true`. |
| `FullName` | `string` | A calculated, read-only field displaying the full name of the selected employee. | Read-only; Not Mapped. | Default display property. |

---

## 4. Relationships to Other Objects

- **`Company`**: A many-to-one relationship. Each `Representative` belongs to one `Company`.
- **`LocalEmployee`**: An optional, one-to-one relationship.
- **`Employee`**: An optional, one-to-one relationship.

---

## 5. Business Rules & Logic

- **Single Active Item**: By inheriting from `SingleActiveBaseObject`, activating a new `Representative` record (by setting its `IsActive` flag to `true` and saving) will automatically:
    1. Deactivate any previously active `Representative` for the same `Company`.
    2. Update the `Company.CurrentRepresentative` property to point to this new active record.
- **Source Validation**: A `RuleCriteria` validation rule ensures that a user must select either a `LocalEmployee` or an `Employee` before saving. The record cannot be saved if both are null.
- **Polymorphic Selection**: The `IsLocalEmployee` boolean property acts as a switch. When checked, it clears the `Employee` field and shows the `LocalEmployee` field. When unchecked, it clears the `LocalEmployee` field and shows the `Employee` field.

---

## 6. UI & Behavior Notes

- **Display Name**: The object is displayed as "Authorized Representative" in the UI.
- **Default Property**: The calculated `FullName` property is used for display in lookups and references.
- **Conditional Visibility**: The `LocalEmployee` and `Employee` properties are conditionally shown or hidden based on the state of the `IsLocalEmployee` checkbox, providing a clean user interface.
- **Data Source Filtering**: The `LocalEmployee` lookup is filtered to only show employees belonging to the parent `Company`.