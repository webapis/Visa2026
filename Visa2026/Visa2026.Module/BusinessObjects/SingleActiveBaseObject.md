# SingleActiveBaseObject Documentation

## Overview
`SingleActiveBaseObject<TParent, TItem>` is an abstract base class derived from `BaseObject`. It is designed to implement a "Single Active Item" pattern, ensuring that within a specific collection of sibling objects, only one can be marked as `IsActive` at any given time.

When an instance of this class is set to `IsActive = true`, it automatically iterates through its sibling objects (belonging to the same parent) and deactivates them. It also provides hooks to update the parent object with the currently active item.

## Generic Type Parameters
*   **TParent**: The type of the parent business object (must inherit from `BaseObject`).
*   **TItem**: The type of the concrete class inheriting from `SingleActiveBaseObject`.

## Key Properties
*   **IsActive**: A boolean property.
    *   **Setter Logic**: When set to `true`, it retrieves the parent, iterates through siblings returned by `GetSiblings`, sets their `IsActive` property to `false`, and calls `SetParentActiveItem`. If set to `false`, it checks if it was the active item on the parent and clears it if necessary.

## Abstract Methods (Implementation Required)
Concrete classes must implement the following methods to define the relationship between the item and its parent:

1.  **`TParent GetParent()`**
    *   Should return the instance of the parent object associated with this item.
2.  **`IList<TItem> GetSiblings(TParent parent)`**
    *   Should return the list or collection of sibling objects from the parent to iterate over for deactivation.
3.  **`void SetParentActiveItem(TParent parent, TItem item)`**
    *   Defines how the parent tracks the active item (e.g., setting a `CurrentItem` property on the parent or a related object).
4.  **`bool IsParentActiveItem(TParent parent, TItem item)`**
    *   Used to verify if the parent currently references this specific item as the active one.

## Current Inheritors
Based on the current project context, the following business objects inherit from this base class:

### **Visa**
*   **Inheritance**: `SingleActiveBaseObject<Passport, Visa>`
*   **Parent**: `Passport`
*   **Behavior**: Ensures only one `Visa` is active for a `Passport`.
*   **Side Effect**: When a Visa is activated, it updates the `CurrentVisa` property on the `Person` associated with the Passport.

### **Passport**
*   **Inheritance**: `SingleActiveBaseObject<Person, Passport>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one `Passport` is active for a `Person`.
*   **Side Effect**: Updates the `ActivePassport` property on the `Person`.

### **WorkPermit**
*   **Inheritance**: `SingleActiveBaseObject<Employee, WorkPermit>`
*   **Parent**: `Employee`
*   **Behavior**: Ensures only one `WorkPermit` is active for an `Employee`.
*   **Side Effect**: Updates the `CurrentWorkPermit` property on the `Employee`.

### **EmployeePositionHistory**
*   **Inheritance**: `SingleActiveBaseObject<Employee, EmployeePositionHistory>`
*   **Parent**: `Employee`
*   **Behavior**: Ensures only one position history record is active for an `Employee`.
*   **Side Effect**: Updates the `CurrentPositionHistory` property on the `Employee`. Additionally, it updates the `Position` and `Department` properties on the `Employee` to match the active history item.

### **AddressOfResidence**
*   **Inheritance**: `SingleActiveBaseObject<Person, AddressOfResidence>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one address of residence is active for a `Person`.
*   **Side Effect**: Updates the `CurrentAddressOfResidence` property on the `Person`.

### **MedicalRecord**
*   **Inheritance**: `SingleActiveBaseObject<Person, MedicalRecord>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one medical record is active for a `Person`.
*   **Side Effect**: Updates the `CurrentMedicalRecord` property on the `Person`.

### **Education**
*   **Inheritance**: `SingleActiveBaseObject<Person, Education>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one education record is active for a `Person`.
*   **Side Effect**: Updates the `CurrentEducation` property on the `Person`.