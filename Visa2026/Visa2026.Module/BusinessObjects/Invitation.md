# Business Object: Invitation

## 1. Purpose

The `Invitation` business object represents an official invitation letter. This document is a key outcome of an `Application` and is required for a foreign employee or family member to obtain a visa. It tracks the validity period of the invitation and the people included in it.

---

## 2. Inheritance

This object inherits from `BaseObject` and implements the `IExpirationLogic` and `IPersonLinkParent` interfaces.

---

## 3. Properties

This section details the data fields of the `Invitation` object as defined in `Invitation.cs`.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `InvitationNumber` | `string` | The official reference number of the invitation letter. | Required; Max 50 chars. |
| `StartDate` | `DateTime` | The date the invitation becomes valid. | |
| `ExpirationDate` | `DateTime` | The date the invitation expires. This is calculated automatically. | |
| `Application` | `Application` | A required reference to the parent `Application`. | Required. |
| `ValidityDuration` | `ValidityDuration` | The duration for which the invitation is valid (e.g., 3 months). | |
| `IsActive` | `bool` | Indicates if the invitation is currently active. | Default: `true`. |
| `DaysRemaining` | `int` | A calculated property showing the number of days until the invitation expires. | Read-only. |
| `ExpirationState` | `ExpirationState` | A calculated property indicating the status (e.g., Active, Expired, ExpiringSoon). | Read-only. |
| `AvailablePeople` | `IList<Person>` | A calculated, non-persistent list of people available to be added to this invitation, sourced from the parent `Application`. | Read-only; Not Mapped; Not Browsable. |

---

## 4. Collections (Relationships)

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `InvitationItems` | `InvitationItem` | A list of people included in this invitation. | Aggregated | `InvitationItem.Invitation` |

---

## 5. Business Rules & Logic

- **`UpdateExpirationDate`**: The `ExpirationDate` is automatically calculated whenever the `StartDate` or `ValidityDuration` properties are changed. The logic is `ExpirationDate = StartDate.AddDays(ValidityDuration.NumberOfDays)`.
- **`IExpirationLogic`**: The class implements the `IExpirationLogic` interface, providing the `ExpirationDate`, `DaysRemaining`, and `ExpirationState` properties to create a consistent expiration-handling pattern across the application.
- **`IPersonLinkParent`**: The class implements the `IPersonLinkParent` interface, providing the `Application` and `AvailablePeople` properties. This allows it to be a parent to `PersonLinkedItemBase` objects (`InvitationItem`), which use this information for data source filtering and validation.

---

## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Invitation" group.
- **Default Property**: `InvitationNumber` is the default property used for display purposes.
- **Calculated Fields**: `DaysRemaining` and `ExpirationState` are calculated in real-time and are read-only. `ExpirationDate` is calculated automatically based on `StartDate` and `ValidityDuration`.
