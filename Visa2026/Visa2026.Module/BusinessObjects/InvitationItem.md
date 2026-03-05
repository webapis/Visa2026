# Business Object: InvitationItem

## 1. Purpose

The `InvitationItem` business object acts as a line item within an `Invitation`. It links a specific person (either an `Employee` or a `FamilyMember`) to the invitation and specifies the passport to be used.

---

## 2. Inheritance

This object inherits from `PersonLinkedItemBase<InvitationItem, Invitation>`, which provides the core logic for linking to a `Person` and handling `Employee`/`FamilyMember` polymorphism. It also inherits from `SingleActiveBaseObject<Person, InvitationItem>`, which manages the active invitation item for a person.

---

## 3. Properties

This section details the data fields of the `InvitationItem` object as defined in `InvitationItem.cs`.

| Property Name | Data Type | Description | Constraints / Validation Rules | UI Notes |
|---------------|-----------|-------------|--------------------------------|----------|
| `Invitation` | `Invitation` | A required reference to the parent `Invitation`. | Required. | |
| `Person` | `Person` | The person (Employee or FamilyMember) this invitation is for. | Required. | The data source is filtered to `Invitation.AvailablePeople`. Setting this property also attempts to set the `Passport`. |
| `Passport` | `Passport` | The passport being used for this invitation. | Required. | |
| `Employee` | `Employee` | A wrapper to get/set the `Person` as an `Employee`. | | Inherited from `PersonLinkedItemBase`. Hidden if `Invitation.Application.IsForFamily` is true. |
| `IsUsed` | `bool` | A flag indicating if this invitation item has been used. | | |
| `FamilyMember` | `FamilyMember` | A wrapper to get/set the `Person` as a `FamilyMember`. | | Inherited from `PersonLinkedItemBase`. Hidden if `Invitation.Application.IsForFamily` is false. |
| `InvitationItemName` | `string` | A calculated, read-only field for display purposes. | Read-only. | Default display property. |
| `IsPersonValid` | `bool` | A validation property to ensure the selected person is part of the parent application. | | `RuleFromBoolProperty` with ID `InvitationItem_PersonIsValid`. |
| `IsCancelled` | `bool` |Indicates the InvitationItem is cancelled or not.||
| `IsChanged` | `bool` | Indicates the InvitationItem is changed or not.||
| `IsActive` | `bool` | Indicates the InvitationItem is active or not.||
| `IsUsed` | `bool` | A flag indicating if this invitation item has been used. |||

---

## 4. Business Rules & Logic

- **`Person` Setter**: When the `Person` property is set, the system searches for the corresponding `ApplicationItem` in the parent `Invitation.Application`. If found, it automatically populates the `Passport` property from that `ApplicationItem`.
- **`IsPersonValid`**: This boolean property provides the logic for the `RuleFromBoolProperty` validation rule, ensuring that the selected `Person` exists within the `ApplicationItem` list of the parent `Invitation`'s `Application`.
- **Active Item Management**: As a `SingleActiveBaseObject`, this class manages setting itself as the `CurrentInvitationItem` on the associated `Person` object.
- **`OnSaving` Logic**: When an `InvitationItem` is saved, it finds the corresponding `ApplicationItem` for the `Person` and sets its `InvitationIssued` flag to `true`.

---

## 5. Relationships to Other Objects

- **`Invitation`**: A required, many-to-one relationship to the parent `Invitation` object.
- **`Person`**: A many-to-one relationship to the `Person` object.
- **`Passport`**: A many-to-one relationship to the `Passport` object.

---

## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Invitation" group.
- **Default Property**: `InvitationItemName` is the default property used for display purposes.
- **Conditional UI**: The `Employee` and `FamilyMember` properties are conditionally displayed based on the `IsForFamily` property of the parent `Application`.
- **Data Source Filtering**: The `Person` property's lookup is filtered by the `AvailablePeople` list from the parent `Invitation`.
