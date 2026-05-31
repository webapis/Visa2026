# Business Object: Invitation

## 1. Purpose

The `Invitation` business object represents an official invitation letter. This document is a key outcome of an `Application` and is required for a foreign employee or family member to obtain a visa. It tracks the validity period of the invitation and the people included in it.

**Cancelled / changed / used** workflow state is stored only on **`InvitationItem`** (one row per person), not on the invitation header.

Invitations can be created **standalone** (Invitation navigation) or from an **Application** (nested `Invitations` list). Linking an **`Application`** is optional and appears behind the detail-view **gear**; when linked, invitation-item people are limited to that application's lines.

---

## 2. Inheritance

This object inherits from `BaseObject` and implements the `IExpirationLogic` and `IPersonLinkParent` interfaces.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `InvitationNumber` | `string` | The official reference number of the invitation letter. | Required; Max 50 chars. |
| `StartDate` | `DateTime` | The date the invitation becomes valid. | Required. |
| `ExpirationDate` | `DateTime?` | Expiry (from `StartDate` + `ValidityDuration`). | Required; always visible on detail view. |
| `Application` | `Application` | Optional link to a visa application. | Optional (gear); when set, limits `InvitationItem.Person` to application lines. |
| `ValidityDuration` | `ValidityDuration` | Validity length (e.g. 3 months). | Required. |
| `DaysRemaining` | `int` | Days until `ExpirationDate`. | Read-only; always visible on detail view. |
| `ExpirationState` | `ExpirationState` | Active / expiring / expired. | Read-only. |
| `AvailablePeople` | `IList<Person>` | Person lookup for new items: application lines when `Application` is set; otherwise all active people. | Not mapped; not browsable. |

---

## 4. Collections (Relationships)

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `InvitationItems` | `InvitationItem` | People on this invitation (status flags per row). | Aggregated | `InvitationItem.Invitation` |

---

## 5. Business Rules & Logic

- **`UpdateExpirationDate`**: `ExpirationDate = StartDate.AddDays(ValidityDuration.NumberOfDays)` when both are set.
- **`IExpirationLogic`**: `ExpirationDate`, `DaysRemaining`, `ExpirationState`.
- **`IPersonLinkParent`**: `Application` and `AvailablePeople` for `InvitationItem` data sources and validation.

---

## 6. UI & Behavior Notes

- **Navigation**: "Invitation" group.
- **Default Property**: `InvitationNumber`.
- **Optional application link**: Use the **gear** to set or change `Application` when you have one; existing invitations with a linked application open with the field visible.
- **Standalone use**: Create from **Invitation** menu without an application; add **Invitation Items** with any active person, then link an application later if needed.
- **Status**: Edit **Cancelled**, **Changed**, and **Used** on each **Invitation Item** (nested list or item detail), not on this header.
