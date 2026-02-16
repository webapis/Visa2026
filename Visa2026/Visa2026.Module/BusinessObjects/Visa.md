# Business Object: Visa

## 1. Purpose

The `Visa` business object stores information about a travel visa issued for a specific passport.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `VisaNumber`    | `string`  | The unique number identifying the visa. | Required; Max 50 chars. |
| `VisaType`      | `VisaType` (Lookup) | The category of the visa (e.g., Work, Business). | Required. |
| `VisaCategory`  | `VisaCategory` (Lookup) | A reference to the category of the visa (e.g., Single-entry, Multiple-entry). | Required. |
| `VisaIssuedPlace` | `VisaIssuedPlace` (Lookup) | The location where the visa was issued. | Required. |
| `IssueDate`     | `DateTime`| The date the visa was issued. | Required. |
| `StartDate`     | `DateTime`| The date from which the visa is valid. | Required. |
| `ExpirationDate`| `DateTime`| The date the visa expires. | Required; Must be after `StartDate`. |
| `IsActive`      | `bool`    | Indicates if this is the currently active visa for the person. | Default: `true`. |
| `HasBorderZonePermit` | `bool` | Indicates if a border zone permit is included. | - |
| `BorderZone`    | `BorderZone` (Lookup) | The specific border zones the holder is permitted to enter. | Conditionally Required. |
| `PersonInApplication` | `PersonInApplication` (Lookup) | A reference to the application process that resulted in this visa. | Optional. |
| `ASNumber`      | `string`  | An alternative identification or application number. | Optional. |
| `Notes`         | `string`  | Additional comments or notes about the visa. | Optional; Unlimited size. |

---

## 3. UI & Behavior Notes

- This object appears in the navigation menu under the "Lookup/Visa" group.
- The `ExpirationDate` must always be later than the `StartDate`.
- **Dynamic Visibility**: The `BorderZone` field is visible only when `HasBorderZonePermit` is checked. When visible, it is required.
- Active visas are highlighted in **Green/Bold** in List Views.