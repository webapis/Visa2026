# Business Object: CompanyHead

## 1. Purpose

The `CompanyHead` business object represents an **Authorized Signatory** for a `Company`. This individual is legally authorized to sign documents and applications on behalf of the organization.

---

## 2. Inheritance

This object inherits from `SingleActiveBaseObject<Company, CompanyHead>`. This implementation ensures that a `Company` can have only one active `CurrentAuthorizedSignatory` at any given time.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules | UI Notes |
|---------------|-----------|-------------|--------------------------------|----------|
| `Company` | `Company` | The parent company this signatory belongs to. | | |
| `IsLocalEmployee` | `bool` | A flag to determine if the signatory is a local or expat employee. | | `ImmediatePostData` enabled. Toggles visibility of `LocalEmployee` and `Employee`. |
| `LocalEmployee` | `LocalEmployee` | A reference to the local employee acting as the signatory. | Data source filtered by `Company`. | Hidden if `IsLocalEmployee` is `false`. |
| `Employee` | `Employee` | A reference to the expat employee acting as the signatory. | | Hidden if `IsLocalEmployee` is `true`. |
| `FullName` | `string` | A calculated, read-only field displaying the full name of the selected employee. | Read-only; Not Mapped. | Default display property. |
| `Position` | `Position` | The specific position held by the signatory (e.g., Director). | | |

---

## 4. Relationships to Other Objects

- **`Company`**: A many-to-one relationship. Each `CompanyHead` belongs to one `Company`.
- **`LocalEmployee`**: An optional, one-to-one relationship.
- **`Employee`**: An optional, one-to-one relationship.
- **`Position`**: A many-to-one relationship to the `Position` lookup.

---

## 5. Business Rules & Logic

- **Single Active Item**: As a `SingleActiveBaseObject`, activating a new `CompanyHead` record (by setting `IsActive` to `true`) automatically:
    1. Deactivates any previously active signatory for the same `Company`.
    2. Updates the `Company.CurrentAuthorizedSignatory` property.
- **Source Validation**: A `RuleCriteria` validation rule ensures that a user must select either a `LocalEmployee` or an `Employee`.
- **Polymorphic Selection**: The `IsLocalEmployee` property controls which lookup field is available. Changing it clears the unavailable field to ensure data consistency.

---

## 6. UI & Behavior Notes

- **Display Name**: The object is displayed as "Authorized Signatory" in the UI.
- **Default Property**: `FullName` is used for display in lookups.
- **Conditional Visibility**: `LocalEmployee` and `Employee` fields are mutually exclusive in the UI based on `IsLocalEmployee`.
- **Data Source Filtering**: The `LocalEmployee` lookup is filtered to only show employees belonging to the parent `Company`.