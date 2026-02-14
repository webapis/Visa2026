# Business Object: FamilyMember

## 1. Purpose

The `FamilyMember` business object extends the `Person` object to store information about an employee's family members (e.g., spouse, child). It links a person's record to a specific employee.

---

## 2. Inheritance

This object inherits all properties from the `Person` business object, including `FirstName`, `LastName`, `Email`, `Birthday`, etc.

---

## 3. Properties

This section details the data fields specific to the `FamilyMember` object.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Relationship`| `RelationshipType` | The family relationship to the employee (e.g., Spouse, Child, Parent). | - |

---

## 4. Relationships to Other Objects

- **`Employee` (Employee)**: A many-to-one relationship back to the `Employee` object. This association links the family member to their related employee.

---

## 5. UI & Behavior Notes

- This object appears in the navigation menu under the "HR" group.
- It is primarily intended to be created and managed from within the `Employee`'s Detail View, via the `FamilyMembers` collection.