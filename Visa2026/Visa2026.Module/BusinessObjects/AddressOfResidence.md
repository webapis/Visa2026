# Business Object: AddressOfResidence

## 1. Purpose

The `AddressOfResidence` business object is designed to store the details of an individual's registered address for a specific period. It creates an auditable history of where a person has lived, which is essential for registration-related applications.

---

## 2. Properties
| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Person` | `Person` (Lookup) | A required, aggregated reference to the parent `Person`. | Required. |
| `Type` | `ResidenceType` (Enum) | The type of accommodation. | Required. |
| `Lodging` | `Lodging` (Lookup) | The specific lodging facility. | Required if Type is Lodging. |
| `StartDate` | `DateTime` | The date the residence at this address begins. | Required. |
| `EndDate` | `DateTime` | The date the residence at this address ends. | Required; Must be after `StartDate`. |
| `FullAddress` | `string` | The complete street address, including building and apartment number. | Required; Max 255 chars. |
