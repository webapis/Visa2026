# Business Object: Invitation

## 1. Purpose
The `Invitation` business object represents the official invitation letter issued by the State Migration Service of Turkmenistan. This document is the successful outcome of an `Application` (specifically for invitations) and is required for a foreign employee or family member to obtain a visa.

## 2. Properties

| Property Name | Data Type | Description | Validation / Rules |
| :--- | :--- | :--- | :--- |
| **`InvitationNumber`** | `string` | The official reference number of the invitation letter. | **Required**, **Unique**. |
| **`InvitationDate`** | `DateTime` | The date the invitation was issued. | **Required**. |
| **`ExpirationDate`** | `DateTime` | The date the invitation expires. Typically, invitations are valid for 3 months from the issue date. | **Required**. |
| **`Application`** | `Application` | The parent application that resulted in this invitation. | **Required**. Association to `Application`. |
| **`File`** | `FileData` | A scanned copy of the official invitation letter. | Optional. |

## 3. Relationships
*   **`Application`**: Associated with the `Application` object. An application can result in multiple Invitations.
*   **`InvitationItems`**: A one-to-many, aggregated collection of `InvitationItem` objects. Each item represents a person included in the invitation.
*   **Referenced By**:
    *   `Application` (as `InvitationToBeChanged`): Used when an application is submitted to correct or change an existing invitation.

## 4. UI & Behavior Notes
*   **Default Property**: The `InvitationNumber` is the default display property in lookups and references.
*   **Icon**: `BO_Report` or similar document icon.