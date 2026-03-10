# Business Object: Person

## 1. Purpose

The `Person` business object is an **abstract** base class that serves as the foundation for representing individuals within the system, such as `Employee` and `FamilyMember`. It consolidates common personal information (name, birth date, citizenship) and acts as a central hub for an individual's entire document history, including passports, visas, registrations, and more.

---

## 2. Inheritance

- **Inherits from**: `BaseObject`
- **Is Inherited By**: `Employee`, `FamilyMember`

This is an `abstract` class and cannot be instantiated directly.

---

## 3. Properties

| Property Name | Data Type | Description | UI Notes |
|---------------|-----------|-------------|----------|
| `FirstName` | `string` | The person's first name. | Required. |
| `LastName` | `string` | The person's last name. | Required. |
| `MiddleName` | `string` | The person's middle name or patronymic. | |
| `FullName` | `string` | A calculated, read-only field combining the first, middle, and last names. | Read-only. Default display property. |
| `BirthDate` | `DateTime` | The person's date of birth. | |
| `Gender` | `Gender` (Enum) | The person's gender. | |
| `Citizenship` | `Country` | The country of the person's citizenship. | |
| `BirthCountry` | `Country` | The country where the person was born. | |
| `Image` | `MediaDataObject` | A photo of the person. | |
| `CurrentPassport` | `Passport` | The currently active passport. | Read-only. Managed by `Passport`'s `SingleActiveBaseObject` logic. |
| `CurrentVisa` | `Visa` | The currently active visa. | Read-only. Managed by `Visa`'s `SingleActiveBaseObject` logic. |
| `CurrentTravelHistory` | `TravelHistory` | The most recent travel history record. | Read-only. Managed by `TravelHistory`'s `SingleActiveBaseObject` logic. |
| `CurrentRegistration` | `Registration` | The currently active registration. | Read-only. Managed by `Registration`'s `SingleActiveBaseObject` logic. |
| `CurrentInvitationItem` | `InvitationItem` | The currently active invitation item. | Read-only. Managed by `InvitationItem`'s `SingleActiveBaseObject` logic. |
| `CurrentRejectionItem` | `RejectionItem` | The currently active rejection item. | Read-only. Managed by `RejectionItem`'s `SingleActiveBaseObject` logic. |
| `CurrentWorkPermitItem` | `WorkPermitItem` | The currently active work permit item. | Read-only. Managed by `WorkPermitItem`'s `SingleActiveBaseObject` logic. |
| `CurrentAddressOfResidence` | `AddressOfResidence` | The currently active address of residence. | Read-only. Managed by `AddressOfResidence`'s `SingleActiveBaseObject` logic. |
| `CurrentMedicalRecord` | `MedicalRecord` | The currently active medical record. | Read-only. Managed by `MedicalRecord`'s `SingleActiveBaseObject` logic. |

---

## 4. Collections (Relationships)

| Collection Name | Item Type | Description |
|-----------------|-----------|-------------|
| `Passports` | `Passport` | A history of all passports associated with the person. |
| `Visas` | `Visa` | A history of all visas issued to the person. |
| `TravelHistories` | `TravelHistory` | A log of all travel movements. |
| `Registrations` | `Registration` | A history of all registrations. |
| `InvitationItems` | `InvitationItem` | A history of all invitation items for the person. |
| `RejectionItems` | `RejectionItem` | A history of all rejection items for the person. |
| `WorkPermitItems` | `WorkPermitItem` | A history of all work permit items for the person. |
| `AddressesOfResidence` | `AddressOfResidence` | A history of all addresses of residence. |
| `MedicalRecords` | `MedicalRecord` | A history of all medical records. |

---

## 5. Business Rules & Logic

- **`FullName` Calculation**: The `FullName` property is automatically calculated and updated whenever the object is saved, based on the `FirstName`, `MiddleName`, and `LastName` properties.
- **Current Item Management**: The various "Current" properties (e.g., `CurrentPassport`, `CurrentVisa`) are not set directly. They are automatically updated by the `SingleActiveBaseObject` logic implemented in the corresponding child objects. When a new item (like a `Passport`) is marked as active, it sets itself as the `CurrentPassport` on its parent `Person` object.

---

## 6. UI & Behavior Notes

- **Hidden Navigation Item**: The `Person` object is not visible in the navigation menu because it is an abstract class. Users interact with its concrete implementations, `Employee` and `FamilyMember`.
- **Central Data Hub**: The Detail View for `Employee` or `FamilyMember` serves as the primary interface for viewing and managing all of a person's related information and document histories through the collections listed above.