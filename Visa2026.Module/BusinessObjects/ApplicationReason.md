# Business Object: ApplicationReason

## 1. Purpose

The `ApplicationReason` business object is a lookup entity that defines the specific, valid reasons for submitting an `Application`. It is used to populate a filtered dropdown list, ensuring that users can only select reasons that are relevant to the chosen `ApplicationType`.

---

## 2. Inheritance

This object inherits from `BaseObject`.

---

## 3. Properties

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Name` | `string` | The descriptive name of the reason. | Required; Max 100 chars. |
| `ApplicationType` | `ApplicationType` | The parent application type this reason is valid for. | Required. |

---

## 4. Relationships to Other Objects

- **`ApplicationType`**: A required, many-to-one relationship. Each reason belongs to a single `ApplicationType`. The `ApplicationType` object, in turn, has a collection of its valid `ApplicationReasons`.
- **Referenced By `Application`**: The `Application` business object has a required lookup property to `ApplicationReason`.

---

## 5. UI & Behavior Notes

- **Navigation**: This object is managed under the `Lookup/Application` navigation item.
- **Default Property**: `Name` is the default property used for display purposes in lookups.
- **Data Filtering**: In the `Application` Detail View, the data source for the `ApplicationReason` property is filtered based on the currently selected `ApplicationType`. This is configured with the `[DataSourceCriteria("ApplicationType.ID = '@This.ApplicationType.ID'")]` attribute on the `Application.ApplicationReason` property.

---

## 6. Data Seeding

The data for this object is seeded from the `applicationreasons.json` file located in the `Visa2026.Module/DatabaseUpdate` folder. The seeding logic is handled by the `Updater.cs` class.