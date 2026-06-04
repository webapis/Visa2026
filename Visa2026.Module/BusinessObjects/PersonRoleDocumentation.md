# Person Class: Unified Employee, Family Member & Temporary Visitor Model

## 1. Architectural Decision: Single Table with Role Discriminator

The application uses one `Person` table with a **`PersonRole`** enum (`PersonRecordRole` in code) instead of separate tables or a lone `IsEmployee` flag.

| `PersonRole` | Legacy `IsEmployee` | Meaning |
|--------------|---------------------|---------|
| `Employee` | `true` | Staff on work permit / long-term HR data |
| `FamilyMember` | `false` (legacy) | Relative of an employee (`SponsoringEmployee`, `Relationship`) |
| `TemporaryVisitor` | `false` | Short-stay guest (invitation, typically ≤ ~1 month); **not** a family member |

`IsEmployee` remains on the table and is **kept in sync** on save for reports, PDF gates, and older criteria during migration.

## 2. Data Model Breakdown

### 2.1. Common Properties

Identity, passports, medical, addresses, travel history, invitation/rejection/application item collections — shared where relevant.

### 2.2. Employee (`PersonRole = Employee`)

Employment fields, work permits, positions, salaries, employee documents, family roster on employee record.

### 2.3. Family Member (`PersonRole = FamilyMember`)

`SponsoringEmployee`, `Relationship`, `FamilyRelationDocuments`. No work permit on the person.

### 2.4. Temporary Visitor (`PersonRole = TemporaryVisitor`)

- **Not** linked as family: `Relationship` and `SponsoringEmployee` are hidden and cleared on save; validation blocks family links.
- **No** work permit, position history, employee contracts, salaries, or employee-only document tabs.
- **Invitation items** tab is prominent on `Person_DetailView_TemporaryVisitor`.
- Use invitation / short-stay application types on `ApplicationItem` (no `CurrentWorkPermitItem` for this role).

## 3. User Interface

### 3.1. Conditional Appearance

Rules use `PersonRole` (and `PersonRoleHelper` criteria strings). Family-only fields require `PersonRole = FamilyMember`. Employee-only fields require `PersonRole = Employee`.

### 3.2. Read-Only Role

`PersonRole` is set from **navigation** (New on the correct list), not by the user on the detail form.

## 4. Custom Navigation & Views

Under **People**:

1. **Employees** → `Person_ListView_Employees` → `Person_DetailView_Employee`
2. **Family Members** → `Person_ListView_FamilyMembers` → `Person_DetailView_FamilyMember`
3. **Temporary visitors** → `Person_ListView_TemporaryVisitors` → `Person_DetailView_TemporaryVisitor`

List criteria (must stay disjoint):

- Employees: `PersonRole = Employee`
- Family: `PersonRole = FamilyMember` (not `IsEmployee = false`)
- Visitors: `PersonRole = TemporaryVisitor`

Typed detail views: Items cloned from `Person_DetailView` via `PersonTypedDetailViewFactory`; layouts in `Visa2026.Blazor.Server/Model.xafml`.

Expected Blazor URLs:

- `…/Person_ListView_TemporaryVisitors`
- `…/Person_DetailView_TemporaryVisitor/{id}`

## 5. Automatic Role Assignment

`PersonListViewController` (and Blazor list navigation) sets role on **New**:

| List | `PersonRole` |
|------|----------------|
| `Person_ListView_Employees` | `Employee` |
| `Person_ListView_FamilyMembers` | `FamilyMember` |
| `Person_ListView_TemporaryVisitors` | `TemporaryVisitor` |

## 6. Database

`PersonRoleMigrationUpdater` adds column `PersonRole` on **`dbo.People`** (with `SET QUOTED_IDENTIFIER ON` — required because of the filtered `PersonalNumber` index) and backfills: `IsEmployee = 1` → `Employee`, `IsEmployee = 0` → `FamilyMember`. Runs before other person migrations that touch family document rows.

## 7. Summary

One person master data model, three officer-facing modules, and strict separation so temporary visitors never appear under family members or carry family / work-permit fields.
