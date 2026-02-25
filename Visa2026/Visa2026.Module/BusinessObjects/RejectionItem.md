# Business Object: RejectionItem

## 1. Purpose

The `RejectionItem` business object acts as a line item within a `Rejection`. It links a specific person (either an `Employee` or a `FamilyMember`) to the rejection record and can store a reason specific to that individual.

---

## 2. Inheritance

This object inherits from `PersonLinkedItemBase<RejectionItem, Rejection>`, which provides the core logic for linking to a `Person` and handling `Employee`/`FamilyMember` polymorphism. It also inherits from `SingleActiveBaseObject<Person, RejectionItem>`, which manages the active rejection item for a person.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules | UI Notes |
|---------------|-----------|-------------|--------------------------------|----------|
| `Rejection` | `Rejection` | A required reference to the parent `Rejection`. | Required. | |
| `Person` | `Person` | The person (Employee or FamilyMember) this rejection is for. | Required. | The data source is filtered to `Rejection.AvailablePeople`. |
| `Reason` | `string` | A specific reason for this person's rejection, if applicable. | | |
| `Employee` | `Employee` | A wrapper to get/set the `Person` as an `Employee`. | | Inherited from `PersonLinkedItemBase`. Hidden if `Rejection.Application.IsForFamily` is true. |
| `FamilyMember` | `FamilyMember` | A wrapper to get/set the `Person` as a `FamilyMember`. | | Inherited from `PersonLinkedItemBase`. Hidden if `Rejection.Application.IsForFamily` is false. |
| `RejectionItemName` | `string` | A calculated, read-only field for display purposes. | Read-only. | Default display property. |
| `IsPersonValid` | `bool` | A validation property to ensure the selected person is part of the parent application. | | `RuleFromBoolProperty` with ID `RejectionItem_PersonIsValid`. |

---

## 4. Business Rules & Logic

- **`IsPersonValid`**: This boolean property provides the logic for the `RuleFromBoolProperty` validation rule, ensuring that the selected `Person` exists within the `ApplicationItem` list of the parent `Rejection`'s `Application`.
- **Active Item Management**: As a `SingleActiveBaseObject`, this class manages setting itself as the `CurrentRejectionItem` on the associated `Person` object.
- **`OnSaving` Logic**: When a `RejectionItem` is saved, it finds the corresponding `ApplicationItem` for the `Person` and sets its `RejectionIssued` flag to `true`.

---

## 5. Relationships to Other Objects

- **`Rejection`**: A required, many-to-one relationship to the parent `Rejection` object.
- **`Person`**: A many-to-one relationship to the `Person` object.

---

## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Application" group.
- **Default Property**: `RejectionItemName` is the default property used for display purposes.
- **Conditional UI**: The `Employee` and `FamilyMember` properties are conditionally displayed based on the `IsForFamily` property of the parent `Application`.
- **Data Source Filtering**: The `Person` property's lookup is filtered by the `AvailablePeople` list from the parent `Rejection`.