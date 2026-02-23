# Refactoring Person-Linking Logic

## 1. Overview

This document details the refactoring of the `InvitationItem` and `RejectionItem` business objects to eliminate code duplication and create a single source of truth for common logic related to linking a `Person`.

## 2. The Problem

Initially, both `InvitationItem.cs` and `RejectionItem.cs` contained nearly identical code for:
-   A `Person` property to link to an applicant.
-   `Employee` and `FamilyMember` properties (both inheriting from `Person`) that acted as wrappers around the `Person` property.
-   DevExpress attributes (`[DataSourceProperty]`, `[Appearance]`) to control the UI behavior of these properties.
-   A validation property `IsPersonValid` to ensure the selected person was part of the parent application.

This duplication made the code harder to maintain. Any change to this logic had to be manually applied in multiple places, increasing the risk of errors.

## 3. The Solution

The solution was to introduce a new abstract base class that encapsulates all the shared functionality. This was achieved through the following steps:

1.  **Create a Unifying Interface (`IPersonLinkParent`)**: To allow the new base class to work with both `Invitation` and `Rejection` objects, a common interface was created.
2.  **Create an Abstract Base Class (`PersonLinkedItemBase`)**: This class contains all the shared properties, attributes, and validation logic.
3.  **Refactor `InvitationItem` and `RejectionItem`**: These classes were updated to inherit from the new base class, removing the duplicated code.

## 4. Implementation Details

### 4.1. `IPersonLinkParent.cs`

This interface defines the contract for any object that can be a parent to a person-linked item. It ensures that the parent object provides access to the root `Application` and the list of `AvailablePeople`.

```csharp
public interface IPersonLinkParent
{
    Application Application { get; }
    IList<Person> AvailablePeople { get; }
}
```
*Both `Invitation.cs` and `Rejection.cs` were updated to implement this interface.*

### 4.2. `PersonLinkedItemBase.cs`

This is the core of the refactoring. It is an abstract generic class that provides the common implementation for person-linked items.

```csharp
public abstract class PersonLinkedItemBase<TItem, TParent> : SingleActiveBaseObject<Person, TItem>
    where TItem : PersonLinkedItemBase<TItem, TParent>
    where TParent : class, IPersonLinkParent
```

**Key Features:**

-   **Generic Constraints**: It is generic on `TItem` (the type of the item itself, for `SingleActiveBaseObject`) and `TParent` (the type of the parent object, constrained to `IPersonLinkParent`).
-   **`ParentObject` Property**: An abstract property `ParentObject` must be implemented by derived classes to return the specific parent object (e.g., `Invitation` or `Rejection`).
-   **Shared Properties**: It contains the `Person`, `Employee`, and `FamilyMember` properties. The DevExpress attributes on these properties now use `ParentObject` to construct their paths, making them generic.
    ```csharp
    [DataSourceProperty("ParentObject.AvailablePeople")]
    [Appearance("EmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ParentObject.Application.IsForFamily", Context = "DetailView")]
    public virtual Employee Employee { ... }
    ```
-   **Shared Validation**: The `IsPersonValid` property is also implemented here, using `ParentObject` to access the application details.

### 4.3. Refactored `InvitationItem.cs` and `RejectionItem.cs`

These classes are now much simpler. They inherit from `PersonLinkedItemBase` and only contain the logic specific to them.

**Example: `RejectionItem.cs`**

```csharp
public class RejectionItem : PersonLinkedItemBase<RejectionItem, Rejection>
{
    // 1. Specific property for the parent
    [RuleRequiredField]
    public virtual Rejection Rejection { get; set; }

    // 2. Implementation of the abstract ParentObject
    public override Rejection ParentObject => Rejection;

    // 3. Properties unique to RejectionItem
    public virtual string Reason { get; set; }
    public string RejectionItemName => $"{Person?.FullName} - Rejected on {Rejection?.Date:d}";

    // ... overrides for SingleActiveBaseObject
}
```
The `InvitationItem.cs` class is similarly simplified, with the only addition being an override of the `Person` property to retain its custom setter logic for updating the `Passport`.

## 5. Benefits

This refactoring provides several key advantages:

-   **Single Source of Truth**: The logic for handling person-linking is now defined in one place (`PersonLinkedItemBase`).
-   **Improved Maintainability**: Changes to the UI or validation logic only need to be made in the base class.
-   **Reduced Code Duplication**: The `InvitationItem` and `RejectionItem` classes are smaller and easier to read.
-   **Increased Consistency**: Ensures that all person-linked items behave identically.
-   **Extensibility**: It is now much easier to create new business objects that link to a `Person` by simply inheriting from `PersonLinkedItemBase`.