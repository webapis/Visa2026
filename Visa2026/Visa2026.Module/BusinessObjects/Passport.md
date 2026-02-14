# Business Object: Passport

## 1. Purpose

The `Passport` business object stores details about a person's passport, including its number, issuing country, and expiration date.

---

## 2. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `PassportNumber`| `string` | The unique number of the passport. | Required; Max 50 chars. |
| `PassportType` | `PassportType` (Lookup) | The type of passport (e.g., Regular, Diplomatic). | Optional. |
| `PersonalID` | `string` | The personal identification number on the passport. | Optional. |
| `IssueDate` | `DateTime`| The date the passport was issued. | Optional. |
| `ExpirationDate`| `DateTime`| The date the passport expires. | Required; Must be after `IssueDate`. |
| `IssuingCountry`| `Country` (Lookup) | The country that issued the passport. | Required. |
| `Passport Issued Place` | `string` | The city or authority that issued the passport. | Optional. |
| `Citizenship` | `Country` (Lookup) | The citizenship of the passport holder. | Optional. |
| `IsActive` | `bool` | Indicates if this is the currently active passport for the person. | - |
| `IsVisaRequired` | `bool` | Indicates if a visa is required for this passport holder. | Default: `true`. |
| `Documents` | `XPCollection<PassportDocument>` | A collection of scanned passport documents. | Aggregated. |

---

## 3. Relationships to Other Objects

- **`Person` (Person)**: A many-to-one relationship to the `Person` object who owns the passport. This is a required, aggregated relationship.
- **`Visas` (Visa)**: A one-to-many relationship to a collection of `Visa` objects. This collection is aggregated.
- **`Documents` (PassportDocument)**: A one-to-many, aggregated relationship to a collection of `PassportDocument` objects.

---

## 4. UI & Behavior Notes

- Passports should be managed within the `Person`'s Detail View via a nested List View.
- Active passports are highlighted in **Green/Bold** in List Views.