# Business Object: Rejection

## 1. Purpose
The `Rejection` business object represents a negative outcome for an `Application`. It records the official refusal from the State Migration Service, including the date and the reason for rejection.

## 2. Properties

| Property Name | Data Type | Description | Validation / Rules |
| :--- | :--- | :--- | :--- |
| **`Application`** | `Application` | The parent application that was rejected. | **Required**. Association to `Application`. |
| **`RejectedDocNumber`** | `string` | The official document number of the rejection letter. | Optional; Max 50 chars. |
| **`Date`** | `DateTime` | The date the rejection decision was issued. | Optional. |
| **`Reason`** | `string` | The official reason provided for the rejection. | Optional. |
| **`RejectionItems`** | `List<RejectionItem>` | A list of individuals included in this rejection. | Aggregated. |
| **`File`** | `FileData` | A scanned copy of the official rejection letter. | Optional. |

## 3. Relationships
*   **`Application`**: Associated with the `Application` object.

## 4. UI Behavior
*   **Default Property**: The `RejectionTitle` ("Rejection [Doc Number] on [Date]") is the default display property in lookups and references.
*   **Icon**: `BO_Validation` or `State_Validation_Invalid`.