# Person DetailView — nested collection tabs (editable vs issued)

**Status:** Shipped (2026-07-09) on typed Person detail views: `Person_DetailView_Employee`, `Person_DetailView_FamilyMember`, `Person_DetailView_TemporaryVisitor`.

## Problem

Officers opening **Employee / Family member / Temporary visitor** saw **one long tab strip** mixing:

| Kind | Examples | Officer expectation |
|------|----------|---------------------|
| **Person record data** (editable) | Educations, Passports, Position history, Medical records, Addresses, Documents | "I add master data here." |
| **Issued documents** (workflow output, browse-only) | Applications (linked), Work permits, Invitations, Rejections, Family members (linked) | "These come from applications." |

Same tab styling + visible **New** on nested grids caused officers to try adding rows in issued-document tabs.

Domain model already marks issued collections read-only in [`Person.md`](../Visa2026.Module/BusinessObjects/Person.md) (`AllowEdit=False` on `WorkPermitItems`, `ApplicationProfileInstances`, etc.) — but **layout and nested ListView chrome** did not communicate that.

## Solution (three layers)

Use **all three**; layout alone is not enough.

### 1. Layout — two tab groups (`Model.xafml`)

Under `PersonCollectionSections` (vertical `LayoutGroup`):

| Node | CSS class | Caption (localized at runtime) |
|------|-----------|--------------------------------|
| `PersonRecordTabs` | `visa-person-record-tabs` | `Person.DetailSection.PersonRecordData` |
| `PersonNewRecordIssuedHint` | `visa-person-new-record-issued-hint` | hint keys below |
| `IssuedDocumentsTabs` | `visa-person-issued-tabs` | `Person.DetailSection.IssuedDocuments` |

Legacy single `TabbedGroup Id="Tabs"` is **`Removed="True"`** on typed views.

**Per typed view — tab assignment**

| Tab | Employee | Family member | Temporary visitor |
|-----|----------|---------------|-------------------|
| Educations | Person record | — (hidden via appearance) | — |
| CV & personal files (`Documents`) | Person record (employee), after Educations/Passports | — (family uses `FamilyRelationDocuments`) | — |
| Passports, Medical records, Addresses | Person record | Person record | Person record |
| Family relation documents | — | Person record | — |
| Position history, Salaries, Work duties, Travel histories | Person record (employee) | Mixed / employee-only hidden | Travel only |
| Incomplete data (`IncompleteData`) | Person record (last tab; hidden when not incomplete) | Same | Same |
| Application profiles (linked) (`ApplicationProfileInstances`), Work permit items, Invitations, Rejections | Issued | Issued | Application profiles + Invitations + Rejections |
| Family members (linked) | Issued | Issued (tab hidden for non-employees) | — |

**Caption scope:** Parent-specific captions for layout id `Documents` / `Documents_Group` via `DocumentCollectionTabCaptionHelper` (e.g. Passport → **Passport copies**, Education → **Diploma copies**, Employee Person → **CV & personal files**). Property names stay `Documents`. Localized in `UiStrings.messages.json` + `UiStrings.document-copies.json`; English runtime via `VisaUiMessages` / `DocumentCollectionCaptionLayoutController`.

Issued tab **captions** use "(issued)" / "(linked)" suffix in English `Model.xafml`; runtime tab-group captions come from `VisaUiMessages`.

### 2. Model + controller — disable New/Delete/Link on nested ListViews

**Updater:** `Visa2026.Module/Model/PersonNestedListViewsUpdater.cs` — sets on nested list view IDs:

- `Person_ApplicationProfileInstances_ListView`
- `Person_WorkPermitItems_ListView`
- `Person_InvitationItems_ListView`
- `Person_RejectionItems_ListView`
- `Person_FamilyMembers_ListView`

`AllowNew`, `AllowDelete`, `AllowEdit`, `AllowLink`, `AllowUnlink` = **false**.

**Controller:** `PersonNestedReadOnlyListViewController` — disables `NewObjectViewController` / `DeleteObjectsViewController` / `LinkUnlinkController` on those nested views (belt-and-suspenders if model merge lags).

Constants: `PersonNestedCollectionLayout.cs`.

Registered in `Module.AddGeneratorUpdaters`.

### 3. New-person UX + visuals (Blazor)

**Controller:** `PersonDetailViewIssuedDocumentsLayoutController` (Blazor.Server)

- Hides `IssuedDocumentsTabs` when `ObjectSpace.IsNewObject(Person)`.
- Shows `PersonNewRecordIssuedHint` instead.
- Sets localized captions on `PersonRecordTabs`, `IssuedDocumentsTabs`, hint group via `VisaUiMessages`.

Hint keys (`tools/GenerateModelLocalization/UiStrings.messages.json`):

| Key | When |
|-----|------|
| `Person.DetailSection.NewRecordIssuedHint` | Employee / family member |
| `Person.DetailSection.NewRecordIssuedHint.Visitor` | Temporary visitor (shorter text) |
| `Person.DetailSection.PersonRecordData` | Editable tab group caption |
| `Person.DetailSection.IssuedDocuments` | Issued tab group caption |

**CSS:** `Visa2026.Blazor.Server/wwwroot/css/site.css` — `.visa-person-record-tabs`, `.visa-person-issued-tabs`, `.visa-person-new-record-issued-hint`.

### 4. Role-based layout visibility (`Person.cs`)

`[Appearance(..., AppearanceItemType = "LayoutItem", ...)]` on `Person`:

- `EmployeeOnly_PersonRecordTabsLayout` — hides `Educations`, `PositionHistory`, `Salaries`, `WorkDuties` for non-employees.
- `EmployeeOnly_IssuedFamilyMembersLayout` — hides `FamilyMembers` tab for non-employees.

Existing `EmployeeOnly` / `PersonDocumentsEmployeeOnly` view-item rules remain for field-level hide.

## What not to use

| Approach | Why insufficient |
|----------|------------------|
| `AllowEdit=False` on BO collection only | Does not always remove nested grid **New/Delete** |
| Permissions only | Tabs still look editable |
| `ShowOptionalFields` gear | [`OPTIONAL_DETAIL_FIELDS.md`](OPTIONAL_DETAIL_FIELDS.md) — gear is for **scalar** optional fields, not `IList` collections |
| Hiding issued data from Person entirely | Officers need person-scoped **view** of applications/permits |

## File map

| Area | Path |
|------|------|
| Layout IDs | `Visa2026.Module/PersonNestedCollectionLayout.cs` |
| ListView updater | `Visa2026.Module/Model/PersonNestedListViewsUpdater.cs` |
| Nested read-only controller | `Visa2026.Module/Controllers/PersonNestedReadOnlyListViewController.cs` |
| New-person + captions (Blazor) | `Visa2026.Blazor.Server/Controllers/PersonDetailViewIssuedDocumentsLayoutController.cs` |
| Typed layouts | `Visa2026.Blazor.Server/Model.xafml` → `Person_DetailView_Employee` / `_FamilyMember` / `_TemporaryVisitor` |
| Tab counts (unchanged) | `DetailViewTabCountController` + `NestedListViewTabCountController` — works on any `TabbedGroup` child |
| Localization source | `tools/GenerateModelLocalization/UiStrings.messages.json` → run generator → `VisaUiMessageCatalog.g.cs` |

## Verification

1. **New employee** — only "Person record data" tabs + dashed hint; no issued section.
2. **Saved employee** — two sections; issued nested grids have **no New/Delete**.
3. **Family member** — no Educations / position / salaries tabs; issued tabs without Family members.
4. **Temporary visitor** — minimal person-record tabs; issued = application items + invitations + rejections.

```powershell
dotnet build Visa2026.slnx -c Debug
```

## Agent implementation notes

### UTF-8 on Windows (critical)

Cursor **`Write`** / **`StrReplace`** on `.cs` under this repo can save **UTF-16** on Windows. Symptom: `CS1056 Unexpected character '\0'`. **Fix:** rewrite file with PowerShell:

```powershell
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($path, $content, $utf8)
# Verify: first bytes should be 117,115,105,110 (= "using")
```

See also [visa2014 import learnings](../.cursor/skills/visa2014-to-visa2026-import/learnings.md).

### Extending tab sets

- Add layout groups under `PersonRecordTabs` or `IssuedDocumentsTabs` in **all three** typed detail views (or only those where the collection applies).
- If browse-only: add list view ID to `PersonNestedCollectionLayout.ReadOnlyNestedListViewIds` and `PersonNestedListViewsUpdater`.
- If employee-only: extend `EmployeeOnly_*` layout `TargetItems` on `Person.cs`.
- New UI strings → `UiStrings.messages.json` → `dotnet run --project tools/GenerateModelLocalization/GenerateModelLocalization.csproj`.

### Related patterns

- Application header uses a similar split: `Application_DetailView` → header tabs vs `Tabs` (Application items, Invitations, …).
- Nested grid "show all rows": [visa2026-blazor-server rule](../.cursor/rules/visa2026-blazor-server.mdc) (`xaf-show-all-rows`).

## Related docs

- [`Person.md`](../Visa2026.Module/BusinessObjects/Person.md) — collection semantics (issued = read-only on person)
- [`OPTIONAL_DETAIL_FIELDS.md`](OPTIONAL_DETAIL_FIELDS.md) — gear toggle (not for collection tabs)
- [`PERSON_DOCUMENT_COPIES.md`](PERSON_DOCUMENT_COPIES.md) — preview slot entry on Person ListView/DetailView