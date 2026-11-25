# Person intake wizard — implementation plan

**Status:** in progress (create + edit wizard; Temporary visitors native-only).

**New** and **open / double-click** on Employees / Family members open the intake wizard (`PersonIntakeWizardHost`). Child 1:N records (passport, education, medical, residence) use stacked cards inside the wizard — officers do not use the native Person DetailView for those roles. Temporary visitors stay native-only.

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

- **New** and **Open / double-click** on `Person_ListView_Employees` and `Person_ListView_FamilyMembers` open the wizard (`PersonIntakeWizardHost`).
- **Provide info** (list Incomplete tab) opens the same wizard on the first pending step once that slice ships.
- The XAF shell, left navigation, and those two list views stay.
- Role comes from the list that created the person (`PersonListViewController.OnObjectCreated` already sets `PersonRole`). There is no Employee / Family member switch inside the wizard.
- **Issued documents** other than work permits, invitations, visas, and application profile instances (rejections, emailed documents) stay out of the steps. Work permits are a read-only employee step when the person already has at least one. Application profile instances are a read-only step on both wizards when the person is already on one. A saved person can still open the existing `IssuedDocumentsTabs` from the wizard. New persons keep that section hidden (`PersonDetailViewIssuedDocumentsLayoutController`).

### Steps

Editable steps use a green nav item. Read-only steps (Family members, Visas, Invitations, Medical records, Work permits, Application profile instances) use a blue nav item and sit after every editable step. Review stays last.

| Step | Employee | Family member |
|------|----------|----------------|
| Identity | First name, last name, date of birth, age, birth place, country of birth, gender, marital status, nationality, personal number, photo | Same without marital status |
| Work & address | Family members for visa (manual), foreign address, foreign address country, project contract, company (subcontractor) | Sponsoring employee, relationship, project contract, company (subcontractor) |
| Passports | Native-style list (columns from `Person_Passports_ListView`) with **New** / **Delete**; a row opens the detail popup (**OK** / **Cancel**) | Same |
| Education | Native-style list (`Person_Educations_ListView`) + detail popup | Same. Adult family member with no pending education mark still gets the Orta / Orta mekdep / Orta bilim row from `FamilyMemberDefaultEducation` |
| Medical records | Read-only. Shown on both wizards when this person has a medical record, after Invitations when that step is present and before Work permits. Grid: preview, state, document number, issue date, validity, expiration, days. Preview opens that record's scans. Hidden when the list is empty. Does not make a medical record required to save. Nav item is blue. | Same |
| Residence | Stacked address-of-residence cards; defaults to **Not received** on create until a card is added | Same |
| CV & files | Stacked personal-file cards (employee) | — |
| Travel | Not in the wizard. Added later from Travel navigation. | Same |
| Salaries | Native-style list (`Person_Salaries_ListView`) + detail popup (employee). Optional: the tab is labeled **Salaries (optional)**. There is no Not received switch. An empty salary does not keep the person incomplete. | — |
| Position history | Native-style list (`Person_PositionHistory_ListView`) + detail popup (employee) | — |
| Purpose of arrival | Native-style list of `WorkDuty` (employee), after Position history. Optional: the tab is labeled **Purpose of arrival (optional)** / **Gelmeginiň maksady (hökman däl)**. The grid column is the description. New / Delete and a detail popup. There is no Not received switch. An empty list does not keep the person incomplete. Nav item is green. | — |
| Family members | Read-only. Shown only when this employee has family members, and only after the editable steps. Grid: full name, relationship, date of birth, nationality. A row opens that family member's wizard. Hidden when the list is empty. Nav item is blue. | — |
| Visas | Read-only. Shown on both wizards when any passport has a visa, after Family members when that step is present and before Invitations. Grid: preview, state, number, type, category, issued place, issue date, start, expiration, days, passport, process number, issuing application. Preview opens that visa's scans. Hidden when there is no visa. Nav item is blue. | Same |
| Invitations | Read-only. Shown on both wizards when this person has an invitation line, after Visas when that step is present and before Work permits. Grid: preview, state (Ready, Used, Cancelled, Changed, Expired), invitation number, issued date, expiration, passport, application profile instance, issued visa, process number. Preview opens that line's invitation scans. Hidden when the list is empty. Nav item is blue. | Same |
| Work permits | Read-only. Shown only when this employee has work permit lines, after Invitations when that step is present. Grid: preview, state, number, start, expiration, days, locations, AS number, position, passport, parent work permit, application profile instance. Preview opens that line's work-permit scans. Hidden when the list is empty. Nav item is blue. | — |
| Application profile instances | Read-only. Shown on both wizards when this person is already on an instance, after Work permits when that step is present and before Review. Grid: state, number, profile, date, process number. Clicking the number opens that instance's case workspace in a new tab. Hidden when the list is empty. Does not make an instance required to save. Nav item is blue. | Same |
| Other records | — | Family relation documents (optional) |
| Review | Unresolved list, pending list, officer note | Same |

Opening a person from **Employees → Incomplete** or **Family Members → Incomplete** starts on **Overview**. First name and last name are shown in a highlighted banner so the officer can see who the record is before the other steps.

Every wizard step keeps the same person in the header: photo when one is stored, initials when it is not, and the initials plus first and last name beside the picture.

### Lookup fields

Every lookup in the wizard (Country of birth, Gender, Marital status, Nationality, Foreign address country, Project contract, Company (subcontractor), Relationship, Sponsoring employee, Passport type, Issued country, Education level / institution / country, Specialty, Position (visa reports), Position (actual / company), Department) uses one search-and-select control, `PersonIntakeSearchableLookup`. It follows the address autocomplete ([address-autocomplete/address-autocomplete-spec.md](address-autocomplete/address-autocomplete-spec.md)) at one line high. Prototype: [prototypes/person-intake-lookup-autocomplete.png](prototypes/person-intake-lookup-autocomplete.png).

- Lists of 15 entries or fewer open in full on click or focus. Longer lists open only while typing.
- Search ignores case and Turkmen/Turkish diacritics, and words match in any order. At most 6 suggestions show, ranked by "starts with", then by usage, then by name (`PersonIntakeLookupSearch`).
- "Used N×" counts rows that use the value in the same property (Nationality counts `Person.Nationality`), from `PersonIntakeLookupUsage`. "New" means no row uses it yet.
- + and the pen are rarely needed, so they are hidden by default. A gear button at the top right of each step that has list fields turns them on for the whole wizard (it stays on when moving between steps, until the wizard is closed). While the gear is off, a search with no match says to turn on the gear to add a value. View-only users get no gear.
- A selected value has a green border, and × clears it. The + button outside the box opens a small wizard dialog (`PersonIntakeLookupDialog`) with the typed text as the name. Clicking the selected name opens the same dialog to rename that record. Saving writes through `PersonIntakeLookupEditor` (Module) in its own object space, checks for an existing entry with the same folded name, and selects the new record. The dialog asks for the name (`NameTm`, or `Name` on `ActualPosition`). Relationship also asks for the reverse name. Project contract also asks for a description and Active; its approval legs and files stay on the Project contract screen.
- Shared catalogs (`GlobalLookupCatalogBase`: Country, Gender, Marital status, Passport type, Education level, Relationship, and the rest) can be added from the wizard but not renamed, so their name is not clickable. + and rename are shown only when the user's role grants Create / Write on that type.
- City (needs a Region) and Hotel, Hospital, Lodging, Other site (need address data) keep the XAF New / edit popup. City is a shared catalog, so it gets + only.
- Text that matches nothing creates nothing and is discarded when the field loses focus.
- Sponsoring employee has no + or edit: it only selects an existing employee.
- **Not received** makes the field read-only and amber.

List steps (Passports, Education, Salaries, Position history) look like the native XAF nested ListView: the grid shows the visible columns of the Person nested ListView model node (so model column changes carry over), a selection column, and **New** / **Delete** (with the XAF delete confirmation). Clicking a row, or **New**, opens the record detail popup. **OK** keeps the edits in the wizard draft; **Cancel** restores the row (or drops a new one). Nothing is written until the person is saved. CV & files keeps stacked cards.

Passports, Education, Position history, and CV & files open their scans in the right-hand document-copies preview. The list has a **Preview** column (CV uses **Preview** on the card). The detail popup previews from the copies list, including a file chosen in this session and not saved yet. The preview is the merged copies of that one record.

**New** (list steps) / **Add** (CV & files) on Passports, Education, CV & files, Salaries, and Position history stays disabled until every card on that step is complete and matches the last save. Only one new card can be added at a time. The officer fills the required fields, saves the person from Review, then **Add** enables for the next card.

Travel history is not a wizard step. Officers add it later from its own navigation. Medical records appear as a read-only step when the person already has one; the wizard does not create them. Empty CV & files do not block save and do not appear in unresolved warnings unless the officer marks **Not received**.

Photo is the image editor, not a text field. Family members for visa (manual) is the existing `VisaFamilyMembersText` editor. New employees still receive the default `Ýok` from `VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty`, and that value counts as filled. Officers may also mark that field **Not received** when the household list is still missing.

### Two waiting controls

| Control | Applies to | On |
|---------|------------|----|
| **Not received** on a field | A required person field that is allowed to wait | The input is disabled and amber. The property stays empty. A `PersonPendingItem` is stored. |
| **Not received** on a record | Passport, Education, CV & files, Salaries, Position history, and each Other-records entry | No child row is required. Optional waiting-on note. A `PersonPendingItem` is stored. |

A child row is saved only when its own required fields are filled. The wizard does not save a half-filled passport (or education). To leave that step, the officer finishes the row or switches the whole record to **Not received**. An in-progress row that is then marked **Not received** is discarded, and the waiting-on note is kept. Existing saved rows are left as they are: the record switch is for “no row yet”, and it is hidden once a row of that kind exists.

CV & files, Salaries, Position history, Purpose of arrival, and Other records (family relation docs) are optional collections (`BlocksWhenEmpty=false`). Medical records are a read-only step when they already exist, and travel stays outside the wizard. Both are omitted from unresolved warnings when empty. Switch off and no row means the person can still be Complete. Switch on means the officer is waiting, so the person stays Incomplete.

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
| `MedicalRecord` | Shown read-only when records already exist. The wizard does not create them and an empty list does not block save. | Medical |
| `AddressOfResidence` | Both | Address |
| `PersonDocument` (CV and files) | Employee (Record step; optional unless Not received) | CV |
| `EmployeePositionHistory` | Employee (Record step; optional unless Not received) | Other |
| `TravelHistory` | Not a wizard step. Added later from Travel navigation. | Other |
| `PurposeOfArrival` | Employee — deferred (not shown in wizard for now) | Other |
| `EmployeeSalary` | Employee (Record step; optional unless Not received) | Other |
| `PersonFamilyRelationDocument` | Family member, only when switched on | Family docs |

Photo, CV & files, Salaries, Position history, and Other-records keys do not block save when they are empty and the switch is off. Medical records are displayed when they already exist and are not collected here. Travel is not collected here. Passport and Education do: an empty collection is unresolved until a valid row exists or the record switch is on.

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

**XAF native access (implemented).** The wizard asks XAF security through `PersonIntakeWizardAccess` (Module). An administrator regulates it in the Roles screen; no wizard-specific settings exist.

| Roles screen setting | Wizard behaviour |
|---|---|
| Read on **Person intake** (`PersonIntakeWizardHost`) | Turns the wizard on for the role. Without it, Employees / Family members **New** and row open use the classic `Person_DetailView_Employee` / `Person_DetailView_FamilyMember` form. |
| No Write on `Person` (object or type; Create for a new person) | View-only wizard: every step visible, inputs locked, no Save, blue "view only" banner. `UsersReadOnly` gets this. |
| Member Read denied on a `Person` property | That field is hidden; a form step with no readable field is hidden. Age follows `DateOfBirth`. |
| Member Write denied on a `Person` property | That field is read-only, and its **Not received** box is locked. |
| Read denied on a child type (`Passport`, `Education`, `PersonDocument`, `EmployeeSalary`, `EmployeePositionHistory`, `PersonFamilyRelationDocument`) | That step / Other-records option is hidden. |
| Create denied on a child type | **+ Add** is hidden and the step's **Not received** box is locked. |
| Write denied on a child type (or object criteria on a row) | That card's fields are locked (`inert`); defaults are not filled in. |
| Delete denied on a child type | **Remove** is hidden on saved cards, and **Delete** skips saved rows in list steps. Unsaved rows can still be discarded. |
| Create, Write, and Delete on `PersonPendingItem` | Needed to toggle **Not received**. Without them, the boxes show the current marks but are locked. |
| Write on `Person.IncompleteNotes` | Needed for the Review note. |

`PersonIntakeWizardPersistHelper.TrySave` repeats the checks before writing (`FindSaveViolation`): it refuses a view-only save, adding or removing a record the role cannot create or delete, and changing Not received marks without the `PersonPendingItem` rights. `SyncPendingItems` keeps unchanged rows, so saving without pending-item rights works when the marks did not change. Any other denied member write is still rejected by the secured object space at commit.

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

### Localization

- Field labels (Person members, record popup fields, step titles that name a collection) use the XAF model member caption through `PersonIntakeFieldLabels` (`CaptionHelper`), so they follow `Model.DesignedDiffs.Localization.*.xafml`. The English text in code is only a fallback.
- All other wizard text uses `PersonIntakeText.Get` / `Format` with `PersonIntake.*` keys in `tools/GenerateModelLocalization/UiStrings.person-intake.json` (en, tr-TR, tk-TM, ru-RU). After editing it, run the generator and commit `VisaUiMessageCatalog.g.cs`.

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