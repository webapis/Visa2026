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
| `Person` | `Person` | The person this invitation is for. | Required; always on detail view with `Passport`. | Data source: `Invitation.AvailablePeople` (application lines when linked, else all active people). Auto-fills `Passport` from `ApplicationItem` when an application is linked. |
| `Passport` | `Passport` | The passport used for this invitation. | Required; always on detail view with `Person`. | |
| `Employee` | `Employee` | A wrapper to get/set the `Person` as an `Employee`. | | Inherited from `PersonLinkedItemBase`. Hidden if `Invitation.Application.IsForFamily` is true. |
| `FamilyMember` | `FamilyMember` | A wrapper to get/set the `Person` as a `FamilyMember`. | | Inherited from `PersonLinkedItemBase`. Hidden if `Invitation.Application.IsForFamily` is false. |
| `InvitationItemName` | `string` | Display name (set on save). | | Default display property; optional (gear). |
| `IsPersonValid` | `bool` | A validation property to ensure the selected person is part of the parent application. | | `RuleFromBoolProperty` with ID `InvitationItem_PersonIsValid`. |
| `IsCancelled` | `bool` | This line cancelled. | | Optional (gear); mutually exclusive with `IsChanged` and `IsUsed`; setting on one item applies to all siblings on the same invitation. |
| `IsChanged` | `bool` | This line superseded by a change workflow. | | Optional (gear); mutually exclusive with `IsCancelled` and `IsUsed`; setting on one item applies to all siblings. |
| `IsUsed` | `bool` | Linked to a visa / consumed in workflow. | | Optional (gear); mutually exclusive with `IsCancelled` and `IsChanged`; per item only. |
| `IsActive` | `bool` | Indicates the InvitationItem is active or not. | | |

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

- **Optional detail fields**: `IsCancelled`, `IsChanged`, and `IsUsed` are behind the gear toggle; any flag `true` auto-expands on open.
- **Status flags (single source of truth)**: At most one of `IsCancelled`, `IsChanged`, or `IsUsed` per item. Setting `IsCancelled` or `IsChanged` on one item updates every non-deleted sibling on the same invitation. The `Invitation` header has no status columns.
- **Navigation**: This object appears in the navigation menu under the "Invitation" group.
- **Default Property**: `InvitationItemName` is the default property used for display purposes.
- **Conditional UI**: The `Employee` and `FamilyMember` properties are conditionally displayed based on the `IsForFamily` property of the parent `Application`.
- **Data Source Filtering**: The `Person` property's lookup is filtered by the `AvailablePeople` list from the parent `Invitation`.
