# Incomplete localization scan - 2026-07-09

Automated scan of Layer A (UiStrings*.json), hard-coded UI strings, and Layer B lookup string tables.

## Summary

| Area | Result |
|------|--------|
| Layer A UiStrings | Mostly complete. 8 intentional blank column captions (space). No missing culture keys. |
| Hard-coded officer UI | ~15 actionable English literals (actions / ShowMessage / tooltips). |
| Layer B lookups | Real gaps: migration-service (no string table); application-state (9 keys); application-location (3 keys). |
| Out of scope | Tenant catalogs; Layer C documents; XafDisplayName English defaults when model captions exist. |

## 1. Layer A - blank captions

File: tools/GenerateModelLocalization/UiStrings.documents-views.json

Blank space for DocumentCopiesListLink on Invitation/Rejection/WorkPermit/BorderZone list views (parent + item where present).

File: tools/GenerateModelLocalization/UiStrings.messages.json

- PersonDocumentCopies.List.ColumnCaption - all cultures are space (intentional icon/link column)

same-as-en (33): mostly intentional - Resminamalar, {0} placeholders, sample notification keys, em dash.

CSV: docs/localization/incomplete-uistrings-scan.csv

## 2. Hard-coded UI (actionable)

| Kind | Location | Notes |
|------|----------|--------|
| ShowMessage literal | ApplicationItemPersonLinkedDefaults.cs | Please select Person... |
| ShowMessage literal | ApplicationItemVisaDefaults.cs | Please create or select Current Passport... |
| Action Caption + Confirmation | ApplicationRuntimeLogResolutionController.cs | Mark in progress / fixed / ignored (dev inbox) |
| Action Caption | UserFeedbackViewController.cs | Mark in progress / fixed |
| ToolTip | StateChangeLogNavigationController.cs | Open source/target |
| ToolTip | VisaExtensionStatusController.cs, VisaTransferStatusController.cs | Open full Application |
| Nav Caption | BoStateNotificationInboxModelUpdater.cs | State notifications |
| Blazor default | CommaSeparatedMultiSelectComponent.razor | No catalog items. |
| Blazor title | (removed 2026-07-11) | Legacy sync dashboard deleted |

Also spot-check ShowMessage helpers that pass a message variable (ApplicationTypeSelectionController, SyncRulesController, notification inbox editors).

CSV: docs/localization/incomplete-hardcoded-focused.csv

Not counted: ~100 XafDisplayName attribute defaults (plan prefers model captions; many already in UiStrings).

## 3. Layer B - lookup string tables

| Catalog | Status |
|---------|--------|
| migration-service | No string table (12 seed keys) - UI falls back to Name/NameTm |
| application-state | 9 keys missing from table (e.g. 3_REVIEW_*, 4_REVIEW_*) |
| application-location | 3 keys missing (AT_THE_MINISTERY_3 .. _5) |
| purpose-of-travel, visa-issued-place | Seed has no LocalizationKey/Code rows picked up (check seed shape) |
| Most others (gender, visa-type, country, ...) | Keys present; cultures use en-US + tr/tk/ru |

CSV: docs/localization/incomplete-layerB-by-catalog.csv, incomplete-layerB-missing-detail.csv

## 4. Suggested fix order

1. Wire hard-coded officer messages (PersonLinkedDefaults, VisaDefaults, multi-select empty text) into UiStrings.messages.json + VisaUiMessages.
2. Fill Layer B: migration-service, then application-state / application-location missing keys.
3. Leave blank DocumentCopiesListLink captions unless product wants a visible column title.
4. Optionally localize developer-only runtime-log / legacy-sync chrome later.