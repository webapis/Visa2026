# SingleActiveBaseObject Documentation

## 1. Overview & Purpose
`SingleActiveBaseObject<TParent, TItem>` is an abstract base class derived from `BaseObject`. It implements the **"Single Active Item"** design pattern.

### What Problem Does It Solve?
In business applications like Visa2026, entities often have historical records where only one record is valid at a time. For example:
*   A Person can have many Passports over their lifetime, but only one is **Current**.
*   An Employee can have many Work Permits, but only one is **Active**.

Without this base class, developers would need to repeat complex logic for every business object to:
1.  **Enforce Uniqueness**: Ensure no two siblings are active simultaneously.
2.  **Manage History**: Automatically archive (deactivate) the old record when a new one is activated.
3.  **Synchronize Parent**: Update the parent object (e.g., `Person.CurrentPassport`) to point to the new active item.

**`SingleActiveBaseObject` automates this entire lifecycle**, ensuring data integrity and reducing code duplication.

## 2. Generic Type Parameters
*   **TParent**: The type of the parent business object (must inherit from `BaseObject`).
*   **TItem**: The type of the concrete class inheriting from `SingleActiveBaseObject`.

## 3. Key Properties
*   **IsActive**: A boolean property.
    *   **Behavior**: When set to `true` (and saved), it triggers the logic to deactivate all other sibling objects belonging to the same parent and updates the parent's reference to the current item.

## 4. Abstract Methods (Implementation Required)
Concrete classes must implement the following methods to define the relationship between the item and its parent:

1.  **`TParent GetParent()`**
    *   Should return the instance of the parent object associated with this item.
2.  **`IList<TItem> GetSiblings(TParent parent)`**
    *   Should return the list or collection of sibling objects from the parent to iterate over for deactivation.
3.  **`void SetParentActiveItem(TParent parent, TItem item)`**
    *   Defines how the parent tracks the active item (e.g., setting a `CurrentItem` property on the parent or a related object).
4.  **`bool IsParentActiveItem(TParent parent, TItem item)`**
    *   Used to verify if the parent currently references this specific item as the active one.

## 5. Current Inheritors
Based on the current project context, the following business objects inherit from this base class:

### **Visa**
*   **Inheritance**: `SingleActiveBaseObject<Person, Visa>`
*   **Parent**: `Passport`
*   **Behavior**: Ensures only one `Visa` is active for a `Person`.
*   **Side Effect**: Updates the `CurrentVisa` property on the `Person`.

### **Passport**
*   **Inheritance**: `SingleActiveBaseObject<Person, Passport>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one `Passport` is active for a `Person`. 
*   **Side Effect**: Updates the `CurrentPassport` property on the `Person`.

### **WorkPermitItem**
*   **Inheritance**: `SingleActiveBaseObject<Employee, WorkPermitItem>`
*   **Parent**: `Employee`
*   **Behavior**: Ensures only one `WorkPermitItem` is active for an `Employee`. 
*   **Side Effect**: Updates the `CurrentWorkPermitItem` property on the `Employee`.

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

### **InvitationItem**
*   **Inheritance**: `SingleActiveBaseObject<Person, InvitationItem>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one invitation item is active for a `Person`. 
*   **Side Effect**: Updates the `CurrentInvitationItem` property on the `Person`. **Note**: This object's "single active" logic is implemented via the `SyncRule` engine rather than in C# code, due to the need to handle `Employee` and `FamilyMember` polymorphism.

### **RejectionItem**
*   **Inheritance**: `SingleActiveBaseObject<Person, RejectionItem>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one rejection item is active for a `Person`. 
*   **Side Effect**: Updates the `CurrentRejectionItem` property on the `Person`.

### **Registration**
*   **Inheritance**: `SingleActiveBaseObject<Person, Registration>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one registration is active for a `Person`. 
*   **Side Effect**: Updates the `CurrentRegistration` property on the `Person`.

### **TravelHistory**
*   **Inheritance**: `SingleActiveBaseObject<Person, TravelHistory>`
*   **Parent**: `Person`
*   **Behavior**: Ensures only one travel history record is active for a `Person`. 
*   **Side Effect**: Updates the `CurrentTravelHistory` property on the `Person`.

### **CompanyHead**
*   **Inheritance**: `SingleActiveBaseObject<Company, CompanyHead>`
*   **Parent**: `Company`
*   **Behavior**: Ensures only one `CompanyHead` (Authorized Signatory) is active for a `Company`.
*   **Side Effect**: Updates the `CurrentAuthorizedSignatory` property on the `Company`.

### **Representative**
*   **Inheritance**: `SingleActiveBaseObject<Company, Representative>`
*   **Parent**: `Company`
*   **Behavior**: Ensures only one `Representative` is active for a `Company`.
*   **Side Effect**: Updates the `CurrentRepresentative` property on the `Company`.

### **BusinessTrip**
*   **Inheritance**: `SingleActiveBaseObject<Employee, BusinessTrip>`
*   **Parent**: `Employee`
*   **Behavior**: Ensures only one `BusinessTrip` is active for an `Employee`.
*   **Side Effect**: Updates the `CurrentBusinessTrip` property on the `Employee`.

### **EmployeeContract**
*   **Inheritance**: `SingleActiveBaseObject<Employee, EmployeeContract>`
*   **Parent**: `Employee`
*   **Behavior**: Ensures only one `EmployeeContract` is active for an `Employee`.
*   **Side Effect**: Updates the `CurrentEmployeeContract` property on the `Employee`.