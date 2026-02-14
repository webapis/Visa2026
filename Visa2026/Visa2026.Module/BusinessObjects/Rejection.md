# Business Object: Rejection

## 1. Purpose
The `Rejection` business object represents a negative outcome for an `Application`. It records the official refusal from the State Migration Service, including the date and the reason for rejection.

## 2. Properties

| Property Name | Data Type | Description | Validation / Rules |
| :--- | :--- | :--- | :--- |
| **`RejectionDate`** | `DateTime` | The date the rejection decision was issued. | **Required**. |
| **`Reason`** | `string` | The official reason provided for the rejection. | **Required**. Unlimited size. |
| **`Application`** | `Application` | The parent application that was rejected. | **Required**. Association to `Application`. |
| **`File`** | `FileData` | A scanned copy of the rejection letter (if applicable). | Optional. |

## 3. Relationships
*   **`Application`**: Associated with the `Application` object.

## 4. UI Behavior
*   **Display Property**: `RejectionDate` or a calculated string (e.g., "Rejection on [Date]").
*   **Icon**: `BO_Validation` or `State_Validation_Invalid`.