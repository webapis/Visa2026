# Person Class: Unified Employee & Family Member Model

## 1. Architectural Decision: Single Table Inheritance
Instead of creating separate database tables for `Employee` and `FamilyMember`, the application uses a **Single Table Inheritance** strategy (conceptually) implemented within a single class: `Person`.

*   **Discriminator**: The boolean property `IsEmployee` determines the role of the record.
*   **Benefits**:
    *   Shared logic for common documents (Passports, Visas, Medical Records).
    *   Simplified database schema.
    *   Easier reporting on all "People" in the system.

## 2. Data Model Breakdown

### 2.1. Common Properties
These fields apply to every person, regardless of role:
*   **Identity**: `FirstName`, `LastName`, `MiddleName`, `DateOfBirth`, `Gender`, `Nationality`, `Photo`.
*   **Documents**: Collections for `Passports`, `Visas`, `Educations`, `MedicalRecords`, `AddressesOfResidence`.
*   **System**: `IsArchived`.

### 2.2. Employee-Specific Logic (`IsEmployee = true`)
When `IsEmployee` is checked, the following fields are relevant:
*   **Employment Details**: `Company`, `HireDate`, `Email`.
*   **Subcontractors**: `IsSubcontractorEmployee`, `Subcontractor` (for external staff).
*   **Active Records**: `CurrentWorkPermitItem`, `CurrentPositionHistory`, `CurrentEmployeeContract`, `CurrentBusinessTrip`.
*   **History**: Collections for `WorkPermitItems`, `PositionHistory`, `EmployeeContracts`, `BusinessTrips`.

### 2.3. Family Member-Specific Logic (`IsEmployee = false`)
When `IsEmployee` is unchecked, the system tracks:
*   **Sponsorship**: `SponsoringEmployee` (Links back to an Employee `Person`).
*   **Relation**: `Relationship` (e.g., Spouse, Child).

## 3. User Interface Experience

### 3.1. Conditional Appearance
The `Person` class uses the DevExpress `[Appearance]` attribute to dynamically modify the Detail View based on the `IsEmployee` flag.

*   **Rule "EmployeeOnly"**: Hides family-specific fields (`SponsoringEmployee`, `Relationship`) when the person is an Employee.
*   **Rule "FamilyMemberOnly"**: Hides employee-specific fields (`Company`, `Email`, `WorkPermitItems`, etc.) when the person is a Family Member.

This ensures that although the underlying object is the same, the user sees a form tailored exactly to the context.

### 3.2. Read-Only Discriminator
The `IsEmployee` property is marked with `[Browsable(false)]` and `[ModelDefault("AllowEdit", "False")]`. Users do not manually check a box to define the type; instead, the type is inferred from *where* they created the record (see Navigation).

## 4. Custom Navigation & Views

The application splits the single `Person` class into two distinct logical views for the user. This is handled by the `CustomNavigationUpdater` and `CustomViewClonerUpdater`.

### 4.1. Navigation Structure
A custom "People" group is created at the root level with two items:
1.  **Employees**: Opens a list of people where `IsEmployee = true`.
2.  **Family Members**: Opens a list of people where `IsEmployee = false`.

### 4.2. View Cloning & Customization
To support distinct column layouts, the system programmatically clones the default `Person_ListView`:
*   **`Person_ListView_Employees`**:
    *   **Filter**: `[IsEmployee] = true`
    *   **Columns**: Shows `Company`, `Email`, `Position`. Hides `SponsoringEmployee`.
*   **`Person_ListView_FamilyMembers`**:
    *   **Filter**: `[IsEmployee] = false`
    *   **Columns**: Shows `SponsoringEmployee`, `Relationship`. Hides `Company`, `WorkPermit`.

### 4.3. Automatic Type Assignment
Since the `IsEmployee` checkbox is hidden, the system must set it automatically. This is handled by the `PersonListViewController`:
*   If a user clicks "New" while in the **Employees** list, the controller intercepts the creation and sets `IsEmployee = true`.
*   If a user clicks "New" while in the **Family Members** list, the controller sets `IsEmployee = false`.

## 5. Summary
This architecture provides the best of both worlds: a unified backend for data integrity and document management, presented as two distinct, purpose-built modules to the end user.