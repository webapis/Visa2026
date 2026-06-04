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
*   **Subcontractors**: `Subcontractor` (**Company (Subcontractor)**) for external staff on employees.
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
    *   **Detail view**: `Person_DetailView_Employee` (via `DetailViewID` on the list view).
*   **`Person_ListView_FamilyMembers`**:
    *   **Filter**: `[IsEmployee] = false`
    *   **Columns**: Shows `SponsoringEmployee`, `Relationship`. Hides `Company`, `WorkPermit`.
    *   **Detail view**: `Person_DetailView_FamilyMember`.

### 4.3. Application Model (where views live)

| View | Role | Primary definition |
|------|------|-------------------|
| `Person_ListView_Employees` | Employee list + URL | `Visa2026.Blazor.Server/Model.xafml` (columns); Module `CustomViewClonerUpdater` if missing |
| `Person_ListView_FamilyMembers` | Family list + URL | Same |
| `Person_DetailView` | Generic / lookup detail | Generated for `Person` + layout in Blazor `Model.xafml` |
| `Person_DetailView_Employee` | Employee detail + URL | Layout in Blazor `Model.xafml`; Items cloned from `Person_DetailView` via `PersonTypedDetailViewFactory` |
| `Person_DetailView_FamilyMember` | Family detail + URL | Same |

Typed detail views **must** have layout deltas in Blazor `Model.xafml`. **Do not** add empty `<DetailView Id="Person_DetailView_Employee" />` stubs in Module `Model.DesignedDiffs.xafml` (breaks Items merge). List views wire `DetailViewID` in Module DesignedDiffs; `PersonTypedDetailViewModelUpdater` clones Items after the default detail view is generated.

Culture files (`Model.tr-TR.xafml`, etc.) only override captions; run `tools/GenerateModelLocalization` after changing `UiStrings.person-detail.json` / `UiStrings.blazor-layouts.json`.

**Model Editor:** open **Visa2026.Blazor.Server** → **Model.xafml** (Default). Under **Views** → **Person** you should see `Person_DetailView_Employee` and `Person_DetailView_FamilyMember` (each with **Items** + **Layout**). Typed views are cloned from `Person_DetailView` in `PersonTypedDetailViewFactory` **after** default Items are generated (not at Views generation — that was too early). Rebuild, close the Model Editor tab, reopen `Model.xafml`. Do **not** add empty `<DetailView Id="Person_DetailView_Employee" />` stubs in Module DesignedDiffs (they can strip Items on merge).

Expected Blazor URLs:
* `…/Person_ListView_Employees` → list
* `…/Person_DetailView_Employee/{id}` → employee detail
* `…/Person_ListView_FamilyMembers` → list
* `…/Person_DetailView_FamilyMember/{id}` → family detail

### 4.4. Automatic Type Assignment
Since the `IsEmployee` checkbox is hidden, the system must set it automatically. This is handled by the `PersonListViewController`:
*   If a user clicks "New" while in the **Employees** list, the controller intercepts the creation and sets `IsEmployee = true`.
*   If a user clicks "New" while in the **Family Members** list, the controller sets `IsEmployee = false`.

## 5. Summary
This architecture provides the best of both worlds: a unified backend for data integrity and document management, presented as two distinct, purpose-built modules to the end user.