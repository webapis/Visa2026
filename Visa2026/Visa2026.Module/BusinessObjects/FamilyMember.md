# Business Object: FamilyMember

## 1. Purpose

The `FamilyMember` business object extends the `Person` object to store information about an employee's family members (e.g., spouse, child). It links a person's record to a specific employee.

---

## 2. Inheritance

This object inherits all properties from the `Person` business object, including `FirstName`, `LastName`, `DateOfBirth`, etc.

---

## 3. Properties

This section details the data fields specific to the `FamilyMember` object.

| Property Name | Data Type | Description | Constraints / Validation Rules |
|---------------|-----------|-------------|--------------------------------|
| `Employee` | `Employee` | The employee to whom this family member is related. | Required. |
| `Relationship`| `Relationship` (Lookup) | The family relationship to the employee (e.g., Spouse, Child). | Required. |
| `Person` | `Person` | A required reference to the parent `Person`. | Required. | |

---

## 4. Collections (Relationships)

| Collection Name | Item Type | Description | Aggregation | Inverse Property |
|-----------------|-----------|-------------|-------------|------------------|
| `Images` | `FamilyMemberImage` | A collection of scanned images for the family member. | Aggregated | `FamilyMemberImage.FamilyMember` |

---

## 5. Relationships to Other Objects

- **`Employee`**: A required, many-to-one relationship back to the `Employee` object. This association links the family member to their related employee.
- **`Relationship`**: A many-to-one lookup relationship to the `Relationship` object.

---

## 6. UI & Behavior Notes

- This object appears in the navigation menu under the "FamilyMember" group.
- It is primarily intended to be created and managed from within the `Employee`'s Detail View, via the `FamilyMembers` collection.