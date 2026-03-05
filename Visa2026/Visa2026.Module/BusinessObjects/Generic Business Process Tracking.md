# Design Document: Generic Business Process Tracking

## 1. Overview

This document outlines a design for a generic business process tracking system applicable to various business objects within the Visa2026 application.  The goal is to provide a configurable, extensible, and reusable mechanism for logging and tracking the progress of business processes (e.g., Visa extensions) as they move through different stages.

## 2. Requirements

*   **Generic**: The solution should be applicable to multiple business objects (Visa, WorkPermit, etc.) without requiring significant code changes for each new object.
*   **Configurable**:  Administrators should be able to define which business objects are tracked and the events that trigger logging.
*   **Extensible**:  The system should be easily extended to support new tracking stages, trigger events, and data points.
*   **Reusable**:  Core components should be reusable across different business processes.
*   **Auditable**:  All tracked events should be logged with relevant details (timestamp, user, state, description).
*   **Visible**:  Tracking information should be readily accessible within the context of the tracked business object (e.g., on the Visa detail view).
*   **Filterable/Groupable**:  Users should be able to filter and group tracked events based on various criteria.
*    **Performance**: Solution must consider performance implications.

## 3. Proposed Solution

We will leverage a combination of `SyncRule`, a dedicated `ProcessLog` business object, and configuration options to achieve these goals.

### 3.1. Core Components

*   **ProcessLog (Business Object)**:
    *   `TrackedObject (BaseObject)`: A reference to the business object being tracked (e.g., a Visa).
    *   `ProcessStage (ProcessStage)`: A lookup to a predefined stage in the process (e.g., "Application Submitted", "Sent to Ministry", "Approved").
    *   `Date (DateTime)`: Timestamp of the event.
    *   `User (ApplicationUser)`: The user who triggered the event (if applicable).
    *   `Description (String)`:  Optional details or comments related to the event.
    *   `AdditionalData (String)`:  Optional field to store serialized data relevant to the process stage.
*   **ProcessStage (Business Object)**:
    *   `Name (String)`:  The name of the stage (e.g., "Application Submitted").
    *   `Code (String)`:  A unique code for the stage (e.g., "APPLICATION_SUBMITTED").
    *   `Description (String)`: A description of the stage.
    *   `IsInitial (bool)`: Indicates if the stage is the initial stage of the process.
    *   `IsFinal (bool)`: Indicates if the stage is the final stage of the process.
*   **SyncRule**:
    *   Used to trigger the creation of `ProcessLog` entries based on CRUD operations on relevant business objects.

### 3.2. Configuration

*   **Tracked Objects**: A configuration setting (perhaps in `SystemSettings`) to specify which business object types should be tracked (e.g., Visa, WorkPermit).
*   **Trigger Events**:  `SyncRule` configurations will define the specific events that trigger logging (e.g., `Save`, `Update`, `PropertyChanged`).
*   **Process Stages**: A predefined list of `ProcessStage` objects, managed through the application UI.

### 3.3. Workflow

1.  **User Action**: A user creates, updates, or deletes a business object (e.g., a Visa).
2.  **SyncRule Trigger**:  A `SyncRule` is triggered by the action.
3.  **ProcessLog Creation**: The `SyncRule` creates a new `ProcessLog` entry, linking it to the tracked business object, setting the `ProcessStage`, timestamp, and user.  The `Description` and `AdditionalData` fields can be populated based on the context of the event.
4.  **Display**: The `ProcessLog` entries are displayed in a List View embedded within the Detail View of the tracked business object (e.g., a Visa).

### 3.4. Example: Tracking Visa Extension

1.  **Tracked Object**: `Visa`
2.  **Process Stages**:
    *   `Application Submitted` (Code: `VISA_EXT_APP_SUBMITTED`)
    *   `Sent to Ministry` (Code: `VISA_EXT_SENT_TO_MINISTRY`)
    *   `Returned from Ministry` (Code: `VISA_EXT_RETURNED_FROM_MINISTRY`)
    *   `Sent to Migration Service` (Code: `VISA_EXT_SENT_TO_MIGRATION`)
    *   `Process Started` (Code: `VISA_EXT_PROCESS_STARTED`)
    *   `Process Complete` (Code: `VISA_EXT_PROCESS_COMPLETE`)
3.  **SyncRule Examples**:
    *   **Rule 1**:  When an `Application` is created with `ApplicationType.Code = 'visa_extension'`, and linked to a Visa, create a `ProcessLog` entry for the `Visa` with `ProcessStage = 'Application Submitted'`.
        *   Source Type: `Application`
        *   Trigger Type: `Save`
        *    Source Criteria: `[ApplicationType.Code] = 'visa_extension'`
        *   Target Path: `Visas`
        *   Target Match Criteria: `` // empty
        *   Target Type: `Visa`
        *   Target Property: `` // N/A
        *   Target Value:  `` // N/A
    *   **Rule 2**:  When an `ApplicationProgress` is created for an `Application` linked to a Visa, and `ApplicationProgress.State.Code = 'SENT_TO_MINISTRY'`, create a `ProcessLog` entry for the `Visa` with `ProcessStage = 'Sent to Ministry'`.
        *   Source Type: `ApplicationProgress`
        *   Trigger Type: `Save`
        *    Source Criteria: `[State.Code] = 'SENT_TO_MINISTRY'`
        *   Target Path: `Application.Visas`
        *   Target Match Criteria: `` // empty
        *   Target Type: `Visa`
        *   Target Property: `` // N/A
        *   Target Value:  `` // N/A

### 3.5. Technical Considerations

*   **Performance**:  Care should be taken to optimize the `SyncRule` criteria to avoid unnecessary logging.  Consider batching `ProcessLog` creation to reduce database load.
*   **User Interface**:  The embedded List View for `ProcessLog` entries should be sortable and filterable.
*   **Security**:  Ensure that users only have access to `ProcessLog` entries for objects they are authorized to view.
*   **Data Volume**: Over time, the number of `ProcessLog` records may grow significantly. Implement an archiving strategy to move older logs to a separate storage location.

## 4. Alternatives Considered

*   **Direct Logging in Business Object Code**:  This approach was rejected due to the lack of reusability and configurability.
*   **Workflow Engine**:  A full-fledged workflow engine (e.g., Windows Workflow Foundation) would provide more advanced features but would be overkill for this scenario.

## 5. Implementation Plan

1.  **Create `ProcessLog` and `ProcessStage` Business Objects**.
2.  **Implement Configuration Settings** to specify tracked objects.
3.  **Implement `SyncRule` configurations** for key trigger events.
4.  **Create UI components** to display `ProcessLog` entries within the tracked business object Detail Views.
5.  **Implement Filtering and Grouping** capabilities.
6.  **Implement Security Considerations**.

## 6. Open Questions

*   What specific data points should be captured in the `AdditionalData` field for each `ProcessStage`?
*   What is the expected volume of `ProcessLog` entries, and what archiving strategy should be implemented?
*   What security considerations are relevant to accessing and managing `ProcessLog` data?

