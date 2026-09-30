# Date editing (officer UI)

Officers type and read calendar dates as **day.month.year** (`dd.MM.yyyy`), with the caret moving to the next part as soon as that part is filled. The same behavior is used on standard XAF detail forms and on custom Blazor date controls.

Example: typing `15032026` produces **15.03.2026**.

## Standard XAF date properties

Each editable `DateTime` / `DateTime?` (and `DateOnly` where used) sets the display and the edit mask on the property:

```csharp
[ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
[ModelDefault("EditMask", "dd.MM.yyyy")]
public virtual DateTime? IssueDate { get; set; }
```

- **`DisplayFormat`** — how the value is shown (detail form and list).
- **`EditMask`** — how the officer types it. The mask is `dd.MM.yyyy` (no `{0:…}` wrapper).

`Passport.IssueDate` and `Passport.ExpirationDate` in `Visa2026.Module/BusinessObjects/Passport.cs` are the usual example. Organization popups use the same pair so they show `dd.MM.yyyy` instead of the culture default (`M/d/yyyy`): `CompanyProfile.RegistrationDate`, `AuthorizedSignatory.PassportIssueDate` and `PassportExpirationDate`, and `AuthorizedRepresentative.PassportIssueDate`. The same pair is repeated on other date properties (visa, work permit, person, invitation, dashboard rows, and so on). There is no shared attribute; each property carries both `ModelDefault` lines.

## Keyboard behavior on detail forms

`Visa2026.Blazor.Server/Controllers/DateTimeMaskAdvancingController.cs` runs on every `DetailView`. For each `DateTimePropertyEditor` it sets:

| Setting | Value | Effect |
|---------|--------|--------|
| `CaretMode` | `MaskCaretMode.Advancing` | After the day (2 digits), month (2 digits), or year (4 digits) is complete, the caret moves to the next section. The officer does not tab between day, month, and year. |
| `UpdateNextSectionOnCycleChange` | `true` | Rolling past the end of a section (day 31 back to 01) also advances the next section (month, then year). |

It applies to both `DxDateEditMaskProperties.DateTime` and `DxDateEditMaskProperties.DateOnly`. XAF discovers the controller in the Blazor host assembly; business objects do not reference it.

List views use `DisplayFormat` only. The advancing caret applies on detail editors.

## Custom Blazor date fields

Screens that do not use `DateTimePropertyEditor` set the same mask themselves.

**`OfficerDateEdit`** (`Visa2026.Blazor.Server/Components/OfficerDateEdit.razor`) is the shared control:

- `Mask` and `DisplayFormat`: `dd.MM.yyyy`
- `DxDateTimeMaskProperties`: `CaretMode="MaskCaretMode.Advancing"` and `UpdateNextSectionOnCycleChange="true"`
- Calendar picker (`PickerDisplayMode.Calendar`) and an automatic clear button

Used by:

- `IssueIssuedHeaderSlotPanel.razor` — invitation / work permit / rejection / border-zone header dates, visa start and end, work-permit item start and expiration
- `IssueIssuedVisaSlotPanel.razor` — issued visa issue and expiration dates

**Family-members birth date** in `Visa2026.Blazor.Server/Editors/VisaFamilyMembersTextComponent.razor` uses the same `DxDateEdit` settings directly. That popup was the original pattern; the controller and `OfficerDateEdit` copy it. Domain notes for that editor: [`VISA_FAMILY_MEMBERS_TEXT_EDITOR.md`](VISA_FAMILY_MEMBERS_TEXT_EDITOR.md).

## Date and time

Timestamps that include a clock time use a different display format and are not typed with the date-only mask. Example: `UserFeedback.SubmittedAt` and `FixedAt` use `{0:dd.MM.yyyy HH:mm}` and are not officer-edited dates.

Do not put `EditMask` `dd.MM.yyyy` on a value that must keep a time of day.

## Adding a new date

**On a business object** (XAF detail / list):

1. Add both attributes: `DisplayFormat` `{0:dd.MM.yyyy}` and `EditMask` `dd.MM.yyyy`.
2. Leave caret behavior to `DateTimeMaskAdvancingController`. Do not set `CaretMode` on the business object.

**On a custom Razor screen:**

1. Prefer `OfficerDateEdit` so mask, advancing caret, calendar, and clear button stay in one place.
2. If the screen already uses `DxDateEdit` directly, copy the mask block from `OfficerDateEdit.razor` (same settings as the family-members birth date).
