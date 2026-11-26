# Find employees

Canonical doc for the officer **Find employees** search: one screen on the left navigation that filters `Person` records with `PersonRole = Employee`.

Related:

| Doc | Role |
|-----|------|
| [`PERSON_DOSSIER.md`](PERSON_DOSSIER.md) | **Open dossier** from a result row |
| [`location-multiselect/location-multiselect-spec.md`](location-multiselect/location-multiselect-spec.md) | Chip search control reused by the lookup fields |
| [`REPORT_DASHBOARD.md`](REPORT_DASHBOARD.md) | Separate free-text person haystack on the dashboard. This search is not that haystack |
| [`ROLE_PERMISSIONS_GUIDE.md`](ROLE_PERMISSIONS_GUIDE.md) | Role grants. Find employees follows the Employees nav item |

---

## 1. Who is searched

The subject is **employees only**. Family members are not result rows. **Has family members** is an extra fact on the employee.

An empty form returns every non-archived employee, ordered by last name then first name, capped at **200** rows (`EmployeeSearchService.ResultLimit`). When more than 200 match, the total is still shown and the list is truncated.

**Include archived** is off by default.

---

## 2. How filters combine

| Rule | Behavior |
|------|----------|
| Across fields | **AND**. Citizenship and department both have to match |
| Inside one lookup | **OR**. Citizenship Turkey or United States returns either |
| Empty lookup | That filter is off |
| Text | First name and last name are case- and diacritic-insensitive (`PersonSearchTextNormalizer.Fold`; `u` matches `ü`) and match a substring |
| Passport, invitation, work permit | Case-insensitive substring (`ToLower`). They do not use the name fold |
| Reversed ranges | Age, visa year, and graduation year swap when from is greater than to |
| Ignored numbers | Age outside 0–120 is dropped. Years outside 1900–2100 are dropped. Birth dates before 1901 are ignored for age |

Lookup chips store **display captions**, then the page maps each caption back to lookup ids by the same fold used in the location control. Two lookups with the same caption both match.

---

## 3. Criteria

The form has thirteen groups. Filters still combine with AND across fields and OR inside one list. A yes/no control left on **Any** is off.

| Group | What it filters |
|-------|-----------------|
| 1. Name and identity | First, middle, last, and full name in any order; personal number; email; photo on file; active, archived, or both; incomplete file and a specific missing area |
| 2. Age, sex, family | Gender; age today or as of 31 December of a year; birth dates and birth years; marital status; family present, relationship, and count; a family member's visa in a year; the visa-family text, including empty or Ýok |
| 3. Citizenship and home | Citizenship, country and place of birth, citizenship different from birth country, a passport issued by another country, foreign home country and address |
| 4. Position and department | Department, visa-report position, and actual position on one history row. **Current** is an open row, **Any time** is every row, **Left** is a closed row, optionally in a year. Also position-year overlap, hire dates, years with the company, a department or position change in a year, more than one position, and previous workplaces |
| 5. Project and company | Contracts, a year in which the person also has position history, whether a linked contract is active, contract description, more than one contract, and subcontractor |
| 6. Pay | Salary amount on a period, a salary period overlapping a year, and a salary change |
| 7. Education | Level, specialty, country, institution, and graduation year on one education row. Also any education, educated outside citizenship, specialty different from the open position, and a diploma copy |
| 8. Visas | Overlap of a year, a date range, or a single day; valid today; expiring within N days; expired, optionally in a year; never had a visa; first and last visa year; a span covered with a gap of at most N days; a gap longer than N days; visa number, type, category, and issued place; extended; extension still required; registration state; border-zone text on the visa; days of stay in a year |
| 9. Passport | Number, current only, type, issued country, issued and expiry ranges, expired, expiring within N days, cancelled, and more than one passport |
| 10. Invitation | Ever, unused, or already used; number; issued year; visa category and period; valid on a date; expired with no visa; cancelled by a completed cancellation application |
| 11. Work permit | Ever or current; number; AS number; overlap of a year; work-permitted places; the position on the permit line; an extension-tracking row |
| 12. Where they lived | Residence type, region, city, lodging, hotel, hospital, or other site on one address, address text, and an address expiration that falls inside a visa |
| 13. Border zone, rejection, medical | A border-zone permit ever or current, in a year, by number or place; a rejection ever or in a year; medical on file, document number, issued year, valid on a date, and a medical expiration that falls inside a visa |

Two catalog rows have no stored history, so they are not filters: an address has an expiration and no registration start, and subcontractor is one field on the person, not a change log.

Salary amounts are stored as text. A row counts when the text parses as a number and falls in the range.

---

## 4. Officer UX

Left navigation item **Find employees** (`FindEmployees`), index 2, icon `Action_Search`, immediately after Employees and Family Members.

The page is a full DetailView. Save, New, and Delete stay hidden.

**Search** and **Clear** stay in the header. Thirteen tabs sit under the title, and only the selected group's fields are open. Values already chosen appear in a strip above the tabs; choosing one opens that group's fields. Search still applies every group together.

Labels, tabs, buttons, and result headings follow the officer UI language (English, Turkish, Turkmen, Russian) through `EmployeeSearch.*` in the UI string catalog. Lookup names use `LocalizedDisplayName`. Missing-area and residence-type chips keep a stable English key so the filter still matches after the caption is translated.

Date filters use `OfficerDateEdit`: officers type `dd.MM.yyyy`, the caret advances after each part, and the calendar can clear the value. See `docs/DATE_EDITING.md`.

Lookup fields use `LocationMultiSelectField` with catalog add and rename **hidden** (`AllowCatalogEdit = false`). The officer picks existing values only. Typing a name that is not in the list adds nothing. Enter adds a chip only when the typed text is an exact saved caption. Backspace removes the last chip. The suggestion list is in the document flow so the page scroll does not clip it.

| Action | Behavior |
|--------|----------|
| Search | Runs the query. Result chips show one chip per field. Lookup chips read `Turkey or United States` |
| Clear | Empties the form and does not search |
| Remove a result chip | Clears that field and searches again |
| Edit criteria | Returns to the form with the values kept |
| Columns | Shared list for every officer. Drag sets the order. An unchecked column stays in the list and is left out of the table and Excel. The choice is stored on `EmployeeSearchColumnSettings` |
| Open in Excel | Downloads the rows on screen, using the same columns and order as the table, as `.xlsx`. When the list is truncated, the sheet repeats that note |
| Click a row | Opens the **why matched** panel for that employee |
| Open dossier | Existing person dossier (`PersonDossierOpenHelper`), same window |

Why matched prefers the history, visa, education, and contract row that satisfied the filters. The position line otherwise uses the open history row, then the latest start. A visa pill is **Valid** when there is no expiration or it is at least 30 days out, **Expiring** when it expires sooner but is still today or later, **Expired** otherwise, and **No visa** when the person has none.

---

## 5. Access

| Role | Navigation |
|------|------------|
| Users, UsersReadOnly | Allow `Application/NavigationItems/Items/FindEmployees` (`EnsureUsersOfficerNavigationPermissions`) |
| VisaOffice | Deny the same path |
| Administrators | See the item through the usual admin bypass |

The grant is applied when `ModuleUpdater` runs (assembly version increased).

---

## 6. Code map

| Piece | Path |
|-------|------|
| Query and row hydration | `Visa2026.Module/Services/EmployeeSearch/EmployeeSearchService.cs` |
| Criteria and row models | `Visa2026.Module/Services/EmployeeSearch/EmployeeSearchModels.cs` |
| Nav ids | `Visa2026.Module/BusinessObjects/EmployeeSearch/EmployeeSearchViewIds.cs` |
| Non-persistent host | `Visa2026.Module/BusinessObjects/EmployeeSearch/EmployeeSearchHost.cs` |
| Nav intercept and chrome | `Visa2026.Module/Controllers/EmployeeSearchControllers.cs` |
| Model updaters | `Visa2026.Module/DatabaseUpdate/EmployeeSearchModelUpdaters.cs` |
| Page | `Visa2026.Blazor.Server/Editors/EmployeeSearchComponent.razor` |
| Lookup wrapper | `Visa2026.Blazor.Server/Editors/EmployeeSearchMultiSelect.razor` |
| Property editor | `Visa2026.Blazor.Server/Editors/EmployeeSearchPropertyEditor.cs` |
| Theme | `Visa2026.Blazor.Server/wwwroot/css/employee-search.css` |

The host is a UI shell (`NonPersistentBaseObject`). It carries `[DomainComponent]` for the same reason as the medical-records workspace host: XAF EF Core `CreateObjectSpace` does not register the type without it. Do not add new Domain Components for persistent data. See `.cursor/rules/visa2026-no-domain-components.mdc`.

Lookup captions on the form use `LookupLocalization.GetDisplayName`. Result lines use `NameTm` when it is set, otherwise `Name`, because the row projection does not have the lookup entity. Extra SQL filters live in `EmployeeSearchFilters.cs`. Amounts, year-of-change, stay gaps, and days of stay are applied in memory after the SQL candidate list, before the 200-row cap.