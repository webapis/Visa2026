# Person intake wizard — implementation plan

**Status:** in progress (create wizard + dual open path).

Until the wizard is finished: **New** on Employees / Family members opens the intake wizard; **open / double-click** an existing row opens the **native** Person DetailView (`Person_DetailView_Employee` / `Person_DetailView_FamilyMember`). Temporary visitors stay native-only. When the wizard is complete, open-existing can switch to the wizard as well.

Officer create for **Employee** and **Family member** moves into one wizard. The person can be saved while required information is still missing, and the Employees and Family members lists split into **Complete** and **Incomplete**.

Prototypes:

| Source | What it locks |
|--------|----------------|
| [person-entry-wizard-README.md](prototypes/person-entry-wizard-README.md) | List tabs and the first wizard pass |
| Claude intake dialog (`Person Intake Wizard Prototype – 1`) | Step list, **Not received** switches, review gate |

Related docs: [PERSON_INCOMPLETE_DATA.md](PERSON_INCOMPLETE_DATA.md), [OPTIONAL_DETAIL_FIELDS.md](OPTIONAL_DETAIL_FIELDS.md), [PERSON_DETAIL_NESTED_COLLECTION_TABS.md](PERSON_DETAIL_NESTED_COLLECTION_TABS.md).

---

## 1. Locked behavior

### Where it lives

- **New** on `Person_ListView_Employees` and `Person_ListView_FamilyMembers` opens the wizard (`PersonIntakeWizardHost`).
- **Open / double-click** (transition period) opens the native typed DetailView. **Provide info** will open the wizard once that slice ships.
- The XAF shell, left navigation, and those two list views stay.
- Role comes from the list that created the person (`PersonListViewController.OnObjectCreated` already sets `PersonRole`). There is no Employee / Family member switch inside the wizard.
- **Issued documents** (applications, work permits, invitations, rejections, emailed documents) stay out of the steps. A saved person can still open the existing `IssuedDocumentsTabs` from the wizard. New persons keep that section hidden (`PersonDetailViewIssuedDocumentsLayoutController`).

### Steps

| Step | Employee | Family member |
|------|----------|----------------|
| Identity | First name, last name, middle name, date of birth, age (computed), gender, personal number, photo | Same |
| Origin and address | Birth place, country of birth, nationality, foreign address, foreign address country | Birth place, country of birth, nationality. Foreign address is not required |
| Employment / relation | Project contract, company (subcontractor), family members for visa (manual) | Sponsoring employee, relationship, project contract, company (subcontractor) |
| Passports | One passport record, then **Add another** | Same |
| Education | Education rows | Same. Adult family member with no pending education mark still gets the Orta / Orta mekdep / Orta bilim row from `FamilyMemberDefaultEducation` |
| Medical | Medical record | Same |
| Residence | Address of residence | Same |
| Other records | CV and files, position history, travel history, purpose of arrival, salaries | Family relation documents, travel history |
| Review | Unresolved list, pending list, officer note | Same |

Photo is the image editor, not a text field. Family members for visa (manual) is the existing `VisaFamilyMembersText` editor. New employees still receive the default `Ýok` from `VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty`, and that value counts as filled.

### Two waiting controls

| Control | Applies to | On |
|---------|------------|----|
| **Not received** on a field | A required person field that is allowed to wait | The input is disabled and amber. The property stays empty. A `PersonPendingItem` is stored. |
| **Not received** on a record | Passport, Education, Medical, Residence, and each Other-records entry | No child row is required. Optional waiting-on note. A `PersonPendingItem` is stored. |

A child row is saved only when its own required fields are filled. The wizard does not save a half-filled passport (or education, medical, residence). To leave that step, the officer finishes the row or switches the whole record to **Not received**. An in-progress row that is then marked **Not received** is discarded, and the waiting-on note is kept. Existing saved rows are left as they are: the record switch is for “no row yet”, and it is hidden once a row of that kind exists.

Other records are optional. Switch off and no row means the person can still be Complete. Switch on means the officer is waiting, so the person stays Incomplete.

These cannot be marked **Not received**:

- First name and last name (both roles), so the list row can be found
- Sponsoring employee and relationship (family member)

**Not received** on personal number stores a blank. It does not store the sentinel `0`. `0` still means “this passport has no personal number”.

Age has no switch. Under-18 marital status `Çaga` is still assigned by the `DateOfBirth` setter.

### Review and save

Every required item is either filled or marked **Not received**.

- Anything else is **unresolved**. **Create as incomplete** / **Save** stays disabled, and Review lists those items in red.
- Pending items and no unresolved items: save as **Incomplete**.
- No pending items: save as **Complete**.

The same wizard updates an existing person. **Provide info** opens it on the first pending step. Saving the last pending item clears the incomplete flag, the nine area checkboxes, `IncompleteNotes`, and `IncompleteMarkedOn` / `IncompleteMarkedBy`. The person then appears on the Complete tab. There is no history table.

### Lists

Each of the two lists has tabs **Complete (n)** and **Incomplete (n)**. The list opens on Complete. The Incomplete tab shows the missing-area chips (the existing nine labels), the officer note, and **Provide info**. A missing-item filter uses the nine area flags.

People already in the database are not reclassified. A person moves to Incomplete only when the wizard saves at least one pending item, or when `IsDataIncomplete` was already set by **Mark incomplete**.

### What stays

- `IsDataIncomplete` and the nine `IncompleteMissing*` flags remain the summary. Report Dashboard **Incomplete persons** keeps reading them.
- The nine flags are written by a rollup from `PersonPendingItem`, not ticked by hand in the wizard.
- **Mark incomplete** / **Mark complete** remain on the Temporary visitor detail view. They are hidden on Employee and Family member once the wizard is the editor.
- The soft flag still does not block applications.
- `PersonIncompleteMarkOptions` stays as it is (it already carries `[DomainComponent]`). New types in this work do not add `[DomainComponent]`.

---

## 2. Completeness rules

`PersonIntakeCompleteness` (Module, no UI) is the only place that decides unresolved, pending, and the nine areas.

**Required and may wait**

| Key | Who | Area flag |
|-----|-----|-----------|
| `Person.DateOfBirth` | Both | Personal data |
| `Person.Gender` | Both | Personal data |
| `Person.PersonalNumber` | Both | Personal data |
| `Person.BirthPlace` | Both | Personal data |
| `Person.CountryOfBirth` | Both | Personal data |
| `Person.Nationality` | Both | Personal data |
| `Person.MaritalStatus` | Employee | Personal data |
| `Person.ForeignAddress` | Employee | Personal data |
| `Person.ForeignAddressCountry` | Employee | Personal data |
| `Person.ProjectContract` | Both | Personal data |
| `Person.Subcontractor` | Both | Personal data |
| `Person.VisaApplicationFamilyMembersText` | Employee | Personal data |
| `Person.Photo` | Both, only when the officer switches it on | Photo |
| `Passport` | Both | Passport |
| `Education` | Both | Education |
| `MedicalRecord` | Both | Medical |
| `AddressOfResidence` | Both | Address |
| `PersonDocument` (CV and files) | Employee, only when switched on | CV |
| `EmployeePositionHistory` | Employee, only when switched on | Other |
| `TravelHistory` | Both, only when switched on | Other |
| `PurposeOfArrival` | Employee, only when switched on | Other |
| `EmployeeSalary` | Employee, only when switched on | Other |
| `PersonFamilyRelationDocument` | Family member, only when switched on | Family docs |

Photo and the Other-records keys do not block save when they are empty and the switch is off. Passport, Education, Medical, and Residence do: an empty collection is unresolved until a valid row exists or the record switch is on.

`FamilyMemberDefaultEducation.ShouldEnsure` returns false while an Education pending item exists, including the `DateOfBirth` setter path. When the officer later clears that pending item and saves an adult family member with no education row, `Ensure` runs as it does today.

**Legacy rows.** If `IsDataIncomplete` is true and the person has no `PersonPendingItem` rows, opening the wizard shows the nine checked areas and the old notes. Nothing is written until Save. Save replaces those checkboxes with pending items (one `Legacy.<Area>` key per checked area the officer did not re-map). Clearing the last of them follows the normal complete path.

---

## 3. Domain

### `PersonPendingItem` : `BaseObject`

Persistent child of `Person`. EF `DbSet` next to `Person` in `Visa2026DbContext`. No `[DomainComponent]`.

| Member | Role |
|--------|------|
| `Person` | Owner |
| `TargetKey` | Stable key from the table in §2 |
| `AreaKey` | One of `PersonIncompleteDataLabels` |
| `Note` | Optional waiting-on note for a record. Empty for a field |

`Person.IncompleteNotes` remains the review note (“who is expected to send the missing information”). It is cleared when the person becomes complete, together with the pending rows.

### Rollup

`PersonPendingRollup.Apply(Person)` after the wizard builds the pending set and before commit:

- `IsDataIncomplete` = any pending row, or a legacy flag that this save did not replace
- Each `IncompleteMissing*` flag = true when any pending row has that `AreaKey`
- `IncompleteMarkedOn` / `IncompleteMarkedBy` set when the save leaves the person incomplete, cleared when it does not

The dashboard view `vw_rd_incomplete_persons_by_missing_area` stays. No new view.

### Validation

Today `[RuleRequiredField]` rejects an empty required member. A pending member must be allowed to save empty.

Change only the `TargetCriteria` of the members in the table above so the rule does not run when `PendingItems[TargetKey = '<key>'].Count() > 0`, and keep the existing role criteria (`Employee`, family, and so on). First name, last name, sponsoring employee, and relationship are unchanged.

Child `[RuleRequiredField]` rules on `Passport`, `Education`, `MedicalRecord`, and `AddressOfResidence` stay as they are, because a pending record creates no row.

### Schema

Add an idempotent `CREATE TABLE` heal beside `PersonIncompleteDataSchemaSql`, invoked from the same startup path **before** the dashboard view heal. PostgreSQL is the application database. Mirror the existing heal file’s shape. Do not point the Blazor host at SQL Server.

### Security

Extend `PersonIncompleteDataOfficerPermissions.Ensure` so the Users role can create, write, and delete `PersonPendingItem`. The nine person members stay writable as they are today.

---

## 4. UI

### Wizard host

A Blazor property editor, `PersonIntakeWizardPropertyEditor`, is the only master-data layout item on `Person_DetailView_Employee` and `Person_DetailView_FamilyMember` (`Visa2026.Blazor.Server/Model.xafml`).

- It reads and writes the current `Person` through that detail view’s `ObjectSpace`.
- `PersonRecordTabs` comes off those two layouts. `IssuedDocumentsTabs` stays, still hidden for a new person, and is opened from a button on the wizard for a saved person.
- The editor lives under `Visa2026.Blazor.Server`. Step definitions, required keys, and rollup live in `Visa2026.Module` so the Blazor project does not own the rules.
- Visual target: the dark dialog in the Claude prototype (left rail with status, amber **Not received**, red unresolved list, amber pending list). Labels go through `UiStrings.messages.json` and the catalog generator.

Footer:

| State | Button |
|-------|--------|
| Not on Review | **Next**. **Next** does not skip a required step that is still unresolved. |
| Review, unresolved | **Save** disabled |
| Review, pending | **Create as incomplete** or **Save** |
| Review, nothing pending | **Create employee** / **Create family member** or **Save** |

### List header

A controller on the two person list views (same id list as `PersonListViewController`, excluding temporary visitors):

- Tabs set `CollectionSource.Criteria` to `IsDataIncomplete = False` or `True`
- Tab captions include the count
- Incomplete tab shows `IncompleteMissingAreasDisplay` and `IncompleteNotes`
- Missing-item filter adds one area flag to the criteria
- **Provide info** is a row action, same column style as `PersonListViewActionColumnsUpdater`, and opens the typed detail view

---

## 5. Slices

Each slice is shippable on its own. Do not start the next slice by rewriting the previous one.

| Slice | Build | Done when |
|-------|--------|-----------|
| **W1 Domain** | `PersonPendingItem`, DbSet, schema heal, `PersonIntakeCompleteness`, `PersonPendingRollup`, `RuleRequiredField` criteria, `FamilyMemberDefaultEducation.ShouldEnsure` skips a pending education, Users-role grant, unit tests | A person with a pending foreign address commits with that field empty. A person with no pending row still fails that rule. Pending education does not insert Orta. Rollup sets `IncompleteMissingPassport` and `IsDataIncomplete`. Personal number pending stays null, not `0`. Legacy flag with no pending rows is left as it is. |
| **W2 Employee wizard** | Editor on `Person_DetailView_Employee`, nine steps, both switches, review gate, create and save | New employee from the Employees list is the wizard. Unresolved blocks save. One pending passport creates the person on Incomplete and does not insert a `Passport`. `Ýok` counts as filled. Issued tabs still hidden until the person is saved. |
| **W3 Family wizard** | Same editor on `Person_DetailView_FamilyMember` | Sponsoring employee and relationship have no switch and block save while empty. Foreign address and marital status are not required. Adult default education appears only when education is not pending. |
| **W4 Update** | Provide-info focus, clear-on-complete | Opening an incomplete employee lands on the first pending step. Filling that item and saving removes the pending row. The last one clears the flag, the nine checkboxes, and the notes. |
| **W5 Lists** | Tabs, counts, chips, area filter, **Provide info** | Employees and Family members each show Complete and Incomplete. Counts match the grid. A passport chip filters to `IncompleteMissingPassport`. Temporary visitors are unchanged. |
| **W6 Legacy and dashboard** | Hide **Mark incomplete** on the two wizard views. Legacy open banner. Dashboard check | An old incomplete person opens with the previous areas and notes. Save rewrites them as pending items. Report Dashboard **Incomplete persons** still shows one row per incomplete person and the same buckets. Temporary visitor **Mark incomplete** still works. |
| **W7 Issued and strings** | Issued-documents button, `UiStrings.messages.json`, catalog generator | A saved employee opens issued documents from the wizard. New UI strings exist in the catalog. |

---

## 6. Tests

`Visa2026.Module.Tests`, not a new project:

- Rollup maps target keys onto the nine flags and clears them
- Unresolved versus pending for an employee and a family member
- Required-field bypass only when that target key is pending
- Personal number pending is not `0` and does not trip the uniqueness rule
- `FamilyMemberDefaultEducation.ShouldEnsure` is false while education is pending, and true for an adult family member after the pending item is removed
- Legacy synthesis does not write until save, and save then matches the dashboard flags

No Playwright slice in W1–W6. Add a Playwright path only after W5, on the Employees list: create with a pending passport, see the Incomplete tab, fill the passport, see the Complete tab.

---

## 7. Out of scope

- Temporary visitor wizard
- Automatic `DataCompleteness` state notifications
- Blocking application create on the incomplete flag
- Per-process incomplete scope (invitation versus extension)
- Pending history / episode table
- A half-saved passport, education, medical, or residence row
- Replacing the XAF left menu or drawing a new shell
- New `[DomainComponent]` types
- Bulk reclassification of existing complete people
- ListView row color for incomplete people

---

## 8. File map (when implementation starts)

```
Visa2026.Module/
  BusinessObjects/PersonPendingItem.cs
  Services/PersonIntake/PersonIntakeCompleteness.cs
  Services/PersonIntake/PersonPendingRollup.cs
  BusinessObjects/Person.cs                         # collection + RuleRequiredField criteria
  Services/ApplicationPersonRoster/FamilyMemberDefaultEducation.cs
  DatabaseUpdate/PersonPendingItemSchemaSql.cs
  DatabaseUpdate/PersonIncompleteDataOfficerPermissions.cs
  Controllers/PersonIncompleteDataController.cs     # hide actions on the two wizard views (W6)
Visa2026.Blazor.Server/
  Editors/PersonIntakeWizardPropertyEditor.cs
  Components/PersonIntake/PersonIntakeWizard.razor
  Controllers/PersonIntakeListController.cs
  Model.xafml                                       # two typed detail layouts + list column
  Startup.cs                                        # heal order unchanged: pending table, then dashboard views
tools/GenerateModelLocalization/UiStrings.messages.json
Visa2026.Module.Tests/PersonIntakeCompletenessTests.cs
```

Docs to touch in the slice that makes the behavior true: this plan (status line), [PERSON_INCOMPLETE_DATA.md](PERSON_INCOMPLETE_DATA.md) (wizard is the officer path for Employee and Family member; popup remains for Temporary visitor), and [OPTIONAL_DETAIL_FIELDS.md](OPTIONAL_DETAIL_FIELDS.md) (the wizard is the person entry surface; the gear stays on child rows opened from elsewhere).