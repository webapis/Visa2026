# Business Object: WorkPermit

## 1. Purpose

The `WorkPermit` business object represents an official letter of approval from a ministry that authorizes the issuance of one or more work permits. It acts as a parent container, grouping all `WorkPermitItem` records that were granted under a single official document.

---

## 2. Inheritance

This object inherits from `BaseObject`.

---

## 3. Properties

This section details the data fields of the `WorkPermit` object as defined in `WorkPermit.cs`.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `WorkPermitNumber` | `string` | The official number of the approval letter. | Required. |
| `StartDate` | `DateTime` | The date the work permit letter was issued or becomes valid. | |
| `Application` | `Application` | A required reference to the parent `Application`. | Required. |
| `AvailableEmployees` | `IList<Employee>` | A calculated, non-persistent list of employees available to be added to this work permit, sourced from the parent `Application`. | Read-only; Not Mapped; Not Browsable. |

---

## 4. Collections (Relationships)

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `WorkPermitItems` | `WorkPermitItem` | A collection of individual work permits authorized by this letter. | Aggregated | |
| `Documents` | `WorkPermitDocument` | A collection of scanned copies of the work permit letter. | Aggregated | |

---

## 5. Business Rules & Logic

- **`AvailableEmployees`**: This property dynamically generates a list of employees from the `ApplicationItems` of the associated `Application`. This list is used to filter the `Employee` lookup in the nested `WorkPermitItems` list view, ensuring that only relevant employees can be added.

---

## 6. UI & Behavior Notes

- **Navigation**: This object appears in the navigation menu under the "Organization" group.
- **Nested Collections**: The `WorkPermitItems` and `Documents` collections are typically displayed as nested list views within the `WorkPermit`'s detail view. This allows for the creation and management of individual work permits and their documents directly from the context of their approval letter.
