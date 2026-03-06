# Business Object: AddressOfResidence

## 1. Purpose

The `AddressOfResidence` business object is designed to store the details of an individual's address of residence for a specific period. It creates an auditable history of where a person has lived, which is essential for registration-related applications.

---

## 3. Properties

This object inherits from `SingleActiveBaseObject<Person, AddressOfResidence>` and implements the `IExpirationLogic` interface.

---

## 3. Properties/Data Fields

This section details the data fields of the `AddressOfResidence` object as defined in `AddressOfResidence.cs`.

| Property Name | Data Type | Description | Constraints / Validation Rules | UI Notes |
|---------------|-----------|-------------|--------------------------------|----------|
| `Person` | `Person` | A required reference to the parent `Person`. | Required. | |
| `Type` | `ResidenceType?` | The type of accommodation (e.g., Lodging, Private Address). | Required. | `ImmediatePostData` enabled. Clears `Lodging` if changed to a non-Lodging type. |
| `Lodging` | `Lodging` | The specific lodging facility. | Required if `Type` is `Lodging`. | `ImmediatePostData` enabled. Hidden if `Type` is not `Lodging`. Updates `FullAddress` when selected. The list is filtered to show public lodgings and those owned by the person's company. |
| `FullAddress` | `string` | The complete street address. | Required; Max 255 chars. | Read-only when `Type` is `Lodging`. |
| `City` | `City` | The city or district of the residence. | Required. | |
| `StartDate` | `DateTime?` | The date the residence at this address begins. | Required. | |
| `ExpirationDate` | `DateTime?` | The date the residence at this address ends. | Required. | Validated to be later than `StartDate`. |
| `IsDeleted`        | `bool`        | Indicates whether the record has been soft deleted.                         | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DateDeleted`      | `DateTime?`   | The date the record was soft deleted.                                       | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DeletedBy`        | `ApplicationUser`| The user who soft deleted the record.                                      | Browsable(false). Part of `ISoftDelete` interface.                                                                                                                                                                         |
| `DaysRemaining` | `int` | A calculated property showing the number of days until the address of residence record expires. | Read-only. | |
| `ExpirationState` | `ExpirationState` | A calculated property indicating the status (e.g., Active, Expired, ExpiringSoon). | Read-only. | |

---
## 2. Inheritance
## 4. Collections (Relationships)

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `Documents` | `AddressOfResidenceDocument` | A collection of documents related to this specific address of residence record. | Aggregated | `AddressOfResidenceDocument.AddressOfResidence` |
| `Images` | `AddressOfResidenceImage` | A collection of images related to this specific residence record. | Aggregated | `AddressOfResidenceImage.AddressOfResidence` |
| `LodgingDocuments` | `LodgingDocument` | A read-only collection of documents associated with the selected `Lodging`. | Not Mapped | |
| `LodgingImages` | `LodgingImage` | A read-only collection of images associated with the selected `Lodging`. | Not Mapped | |

---
## 5. Business Rules & Logic
## 5. Business Rules & Logic

- **Single Active Item**: As a `SingleActiveBaseObject`, only one address of residence can be active for a `Person` at a time. Activating a new item automatically updates the `Person.CurrentAddressOfResidence` property,
- **Expiration Logic**: The object implements `IExpirationLogic`. The system tracks `EndDate` to determine if the registration is Active, Expiring Soon (<= 30 days), or Expired.
- **Address Automation**: If `Type` is set to `Lodging` and a `Lodging` is selected, the `FullAddress` property is automatically populated from the `Lodging` object and becomes read-only.
- **Document Visibility**:
    - `Documents` collection is hidden if `Type` is `Lodging`.
    - `LodgingDocuments` collection is hidden if `Type` is *not* `Lodging`.
- **Image Visibility**:
    - `Images` collection is hidden if `Type` is `Lodging`.
    - `LodgingImages` collection is hidden if `Type` is *not* `Lodging`.

---
## 6. UI & Behavior Notes
## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under "Lookup/Person".
- **Default Property**: `FullAddress` is the default property used for display purposes.
- **Validation**: A rule ensures that `EndDate` is greater than `StartDate`.
- **Appearance Rules**: Visibility is controlled by `Type` property.
