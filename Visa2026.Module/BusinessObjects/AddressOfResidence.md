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
| `ExpirationDate` | `DateTime?` | Registration / residence valid until (private house). | Required when `Type = PrivateHouse`. | Hidden for lodging/hotel. |
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
| `LodgingDocumentsGuidance` | `string` | Hint when type is Lodging: files are managed on the **Lodging** master (Lookup → Housing). | Not mapped; read-only | Shown instead of `LodgingDocuments` on detail view. |
| `LodgingDocuments` | `LodgingDocument` | Mirror of `Lodging.Documents` (packaging/API only). | Not mapped; hidden in UI | |
| `LodgingImages` | `LodgingImage` | Mirror of `Lodging.Images`. | Not mapped; hidden in UI | |

---
## 5. Business Rules & Logic
## 5. Business Rules & Logic

- **Single Active Item**: As a `SingleActiveBaseObject`, only one address of residence can be active for a `Person` at a time. Activating a new item automatically updates the `Person.CurrentAddressOfResidence` property,
- **Expiration Logic**: The object implements `IExpirationLogic`. The system tracks `EndDate` to determine if the registration is Active, Expiring Soon (<= 30 days), or Expired.
- **Default type**: New records default to **`Lodging`** (`OnCreated`).
- **Address Automation**: If `Type` is set to `Lodging` and a `Lodging` is selected, the `FullAddress` property is automatically populated from the `Lodging` object and becomes read-only.
- **Document Visibility**:
    - `Documents` collection is hidden if `Type` is `Lodging`.
    - `LodgingDocumentsGuidance` is shown when `Type` is `Lodging` (documents are edited on the **Lodging** record, not here).
    - `Documents` is hidden when `Type` is `Lodging`; editable for **Private house** and **Hotel**.
- **Image Visibility**:
    - `Images` collection is hidden if `Type` is `Lodging`.
    - `LodgingImages` collection is hidden if `Type` is *not* `Lodging`.

---
## 6. UI & Behavior Notes
## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under "Lookup/Person".
- **Default Property**: `FullAddress` is the default property used for display purposes.
- **Validation**: `ExpirationDate` is required for `PrivateHouse` (`Type = PrivateHouse`).
- **Appearance Rules**: Visibility is controlled by `Type` property.
