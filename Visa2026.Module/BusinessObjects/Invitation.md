# Business Object: Invitation

## 1. Purpose

The `Invitation` business object represents an official invitation letter (legacy `ApplicationResult` with Result = Invitation). It tracks formalization date, intended visa category/period, invitation expiry, optional visa start/end window, and the people included.

**No Netije/Result property** — Rejection is a separate BO.

**Cancelled / changed / used** workflow state is stored only on **`InvitationItem`** (one row per person), not on the invitation header.

Invitations can be created **standalone** (Invitation navigation) or from an **Application** (nested `Invitations` list). Linking an **`Application`** is optional and appears behind the detail-view **gear**; when linked, invitation-item people are limited to that application's lines.

---

## 2. Inheritance

This object inherits from `BaseObject` and implements the `IExpirationLogic` and `IPersonLinkParent` interfaces.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `InvitationNumber` | `string` | Official reference number (Belgi). | Required; Max 50 chars. |
| `IssuedDate` | `DateTime` | Formalization date (Resmileşdirilen sene). DB column `StartDate`. | Required. |
| `VisaCategory` | `VisaCategory` | Visa category on the invitation (Wiza kategoriýasy). | Required. |
| `VisaPeriod` | `VisaPeriod` | Intended visa period (Wiza möhleti). Not used to compute invitation expiry. | Required. |
| `IsVisaStartAndEndDateDefined` | `bool` | When true, visa start/end dates apply. | Optional (gear). |
| `VisaStartDate` / `VisaEndDate` | `DateTime?` | Visa window when defined. | Required when flag is true; hidden otherwise. |
| `ExpirationDate` | `DateTime?` | Invitation letter expiry (Möhleti tamamlanýan sene). | Required; must be after `IssuedDate`. |
| `Application` | `Application` | Optional link; lookup limited to `CanIssueInvitation`. | Optional (gear). |
| `DaysRemaining` | `int` | Days until `ExpirationDate`. | Read-only; always visible. |
| `ExpirationState` | `ExpirationState` | Active / expiring / expired. | Read-only. |

---

## 4. Collections (Relationships)

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `InvitationItems` | `InvitationItem` | People on this invitation (Person In Result Letter). | Aggregated | `InvitationItem.Invitation` |
| `Documents` | `InvitationDocument` | Header documents. | Aggregated | `InvitationDocument.Invitation` |
| `Images` | `InvitationImage` | Header images. | Aggregated | `InvitationImage.Invitation` |

---

## 5. Business Rules & Logic

- **`IExpirationLogic`**: `ExpirationDate`, `DaysRemaining`, `ExpirationState`.
- **`IPersonLinkParent`**: `Application` and `AvailablePeople` for `InvitationItem` data sources and validation.
- **`IsApplicationTypeAllowed`**: when `Application` is set, requires `ApplicationType.CanIssueInvitation`.
- Visa window criteria: when `IsVisaStartAndEndDateDefined`, `VisaEndDate > VisaStartDate`.

---

## 6. UI & Behavior Notes

- **Navigation**: "Invitation" group.
- **Default Property**: `InvitationNumber`.
- **Always visible**: Number, Issued Date, Visa Category, Visa Period, Expiration Date, Days Remaining.
- **Gear**: ApplicationProfileInstance, visa start/end flag and dates.
- **Status**: Edit **Cancelled**, **Changed**, and **Used** on each **Invitation Item**, not on this header.