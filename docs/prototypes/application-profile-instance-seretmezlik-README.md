# Seretmezlik — exclude people from processing (2026-09-24)

New case workspace section on the Application Profile Instance. After the case has been submitted, the officer prepares a letter (and roster) asking the current holder — a ministry leg or the Migration Service — to stop processing one or more people. One instance can have several Seretmezlik letters on different dates, each with its own number.

**Status:** Prototype approved; **implemented** 2026-09-24 (Application Profile slice 14).

## Implementation

| Part | Where |
|------|-------|
| Letter + excluded people (BOs) | `Visa2026.Module/BusinessObjects/ApplicationProfileInstanceExclusion.cs`, `ApplicationProfileInstanceExclusionPerson.cs`; tables healed by `DatabaseUpdate/ApplicationProfileInstanceExclusionSchemaSql.cs` |
| Letter number | `ApplicationProfileInstanceExclusionNumbering` — same prefix / year / month counter as `ApplicationProfileInstance.ApplicationNumber` (neither reuses the other's numbers) |
| Rules (gate, current holder, prefills, save, close as Cancelled) | `Services/OfficerShell/ApplicationProfileInstanceExclusionService.cs` |
| Letter + Roster documents | `Services/OfficerShell/ApplicationProfileInstanceExclusionLetterBuilder.cs` — built-in Letter and Roster (Sanaw) layouts, Word merge (DocxTemplater) and Excel merge (ClosedXML), placeholder catalog, upload validation. Header tokens `{{ds.LetterNumber}}`, `LetterDate`, `AddresseeName`, `Salutation`, `ReferenceMinistryName`, `ReferenceMinistryGenitive`, `ReferenceLetterDate`, `ReferenceLetterNumber`, `Subject`, `OriginalRosterCount`, `ExcludedCount`, `ExcludedCountText`, `ApplicationNumber`, `CompanyName`, `SignatoryName`, `SignatoryPosition`; people row `{{#ds.People}}` … `{{/ds.People}}` with `{{.Index}}`, `FullName`, `LastName`, `FirstName`, `MiddleName`, `PassportNumber`, `DateOfBirth`, `BirthPlace`, `Nationality` |
| Company templates (slice 14b, Phase 1) | `BusinessObjects/ApplicationProfileInstanceExclusionTemplate.cs` (one row per Letter / Roster, not `UserReportTemplate`) + `Services/OfficerShell/ApplicationProfileInstanceExclusionTemplateStore.cs` (status, Replace with validation, Reset to built-in). **Templates** pane in the tab: Preview sample, Download current / built-in, Replace (Letter .docx; Roster .docx or .xlsx), Reset, placeholder manual. Documents are regenerated from the current template on each download / preview. Phase 2 (not started): Create from yellow marks / Add existing. |
| Tab | `Visa2026.Blazor.Server/Editors/OfficerShellCaseSeretmezlikTab.razor` + `wwwroot/css/officer-shell/seretmezlik.css`; PDF preview in `#visa-preview-slot` via sources `seretmezlik-letter`, `seretmezlik-roster`, `seretmezlik-template` (sample merge). Excluded people skipped by Resminamalar, Document copies, People-links completeness, and Application Result expected/coverage (+ ListView Netije chips). |

Differences from the prototype: the letter preview opens in the preview slot as PDF instead of an inline paper mock; there is an optional **Salutation** field (second sample letter); the ministry letter **number** must be typed because Progress stores only the date.

Source: [application-profile-instance-seretmezlik-prototype.html](./application-profile-instance-seretmezlik-prototype.html) (`?s=list|empty|new|detail|people|close`). Uses the real `officer-shell/tokens.css` + `case-workspace.css`.

## Locked for this set

| Decision | Choice |
|----------|--------|
| Addressee | Suggested from Progress (current ministry leg or Migration); officer can change |
| When allowed | Only after the case left office preparation |
| Effect on person | Stays on the roster, marked **Excluded** (letter № + date); left out of later Resminamalar, Document copies, People & links completeness, and **Application Result** expected / coverage (Invitation, Visa, Work permit, Border zone — not required). Numbers only (no Seretmezlik note on Result overview). ListView Netije chips use the same active-roster rule. All excluded → expected 0 → Result complete. |
| Last person excluded | Warning dialog; offer to close the case as **Cancelled** or keep it open |
| Letter № | Company numbering (`ApplicationNumberingProfile`), assigned on save |
| Reference letter + original roster count | Prefilled from Progress (ministry letter date / №) and roster; editable |
| Documents per record | Generated Word letter from a Resminamalar-style template (no signed-scan upload, no reply record in v1) |
| Status | None — saving excludes the people |
| Undo | Not possible; letter details stay editable, the people list is locked after save |

## Screens

| File | State |
|------|--------|
| [application-profile-instance-seretmezlik-01-list-prototype.png](./application-profile-instance-seretmezlik-01-list-prototype.png) | Section with two letters, counts, nav badge, rail action |
| [application-profile-instance-seretmezlik-02-empty-prototype.png](./application-profile-instance-seretmezlik-02-empty-prototype.png) | No exclusions yet |
| [application-profile-instance-seretmezlik-03-new-prototype.png](./application-profile-instance-seretmezlik-03-new-prototype.png) | New letter: prefilled fields (blue), roster picker, irreversible-save warning |
| [application-profile-instance-seretmezlik-04-detail-prototype.png](./application-profile-instance-seretmezlik-04-detail-prototype.png) | Saved letter: details, excluded people, generated letter preview |
| [application-profile-instance-seretmezlik-05-people-links-prototype.png](./application-profile-instance-seretmezlik-05-people-links-prototype.png) | People & links with excluded people greyed out |
| [application-profile-instance-seretmezlik-06-last-person-close-prototype.png](./application-profile-instance-seretmezlik-06-last-person-close-prototype.png) | Excluding the last person — close case as Cancelled? |
