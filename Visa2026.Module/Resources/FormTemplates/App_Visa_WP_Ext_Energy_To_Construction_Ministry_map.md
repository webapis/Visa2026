# Report Map: App_Visa_WP_Ext_Energy_To_Construction_Ministry

**Status:** Word — `AppVisaWPExtEnergyToConstructionMinistryLetterReportDef` / `App_Visa_WP_Ext_Energy_To_Construction_Ministry_Letter.docx`  
**Layout family:** L3 (portrait ministry letter; **not** company-headed — static Energy minister signatory per scan)

## Data type(s)

| Role | Type | Notes |
|------|------|--------|
| Root | `Application` | `FillForm` only; no rows |

## Reference scan

| File | Role |
|------|------|
| `FormTemplates/App_Visa_WP_Ext_Energy_To_Construction_Ministry_scan.png` | Official Energy ministry → Construction ministry letter (Çalyk / GT-15 context) |

## Applicability

- `ApplicationType.Name` = `App_Visa_and_WP_Ext`
- `IsApplicable`: `ProjectContract.NameTm` contains `GT-15` (substring, case-insensitive). Static body paragraph 1 is **frozen to the scan transcription** (not auto-synced from `ProjectContract.Description`).

## Dynamic fields (`{{ds.*}}`)

| Placeholder | Source on `Application` | Notes |
|-------------|-------------------------|--------|
| `CancelPersonCount` | `CancelPersonCount` (`ApplicationItems.Count`) | Numeric count in “**N (…)** sany daşary ýurt raýat…” |
| `CancelPersonCountText` | `CancelPersonCountText` | Turkmen cardinal in parentheses (e.g. `on sekiz`) |
| `VisaPeriod_NameTm` | `VisaPeriod_NameTm` | Lookup `VisaPeriod.NameTm` — should include full phrase when needed (e.g. `6 (alty) aý köp gezeklik`) |

All other letter text (addressee, salutation, decree/contract narrative, **paragraph 3** Netijenama / coordination wording with bold opening, responsibility, signatory) is **static** in the `.docx` per the reference scan and supplied paragraph-3 copy.

## Generator

`MakeAppVisaWPExtEnergyToConstructionMinistryLetterTemplate` in `tools/GenerateTemplates/Program.cs`.

## Preview preset

`tools/PreviewWordReports` preset `energy-to-construction-ministry-letter` — dump values transcribed from the scan (`18`, `on sekiz`, `6 (alty) aý köp gezeklik`).
