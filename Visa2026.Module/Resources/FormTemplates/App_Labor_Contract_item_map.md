# Map: Zähmet şertnamasy (Labor Contract) — ApplicationItem

**Status:** ✅ Implemented — `AppLaborContractItemReport`; report visibility: **all** `ApplicationItem` rows (empty criteria; no `ApplicationType` filter).

---

## Report Identity

| Field | Value |
|---|---|
| **Class name** | `AppLaborContractItemReport` |
| **Registered name** | `"App Labor Contract Item Report"` |
| **Display name (Tm)** | `Zähmet şertnamasy — Şahsy` |
| **Reference image** | `App_Labor_Contract_item.png` *(also acceptable: export/duplicate as `.jpg` for naming consistency with other templates)* |

---

## Data

| Field | Value |
|---|---|
| **Data type (BO)** | `ApplicationItem` |
| **Base class** | `AppItemBaseReport` |
| **Visibility criteria** | *(empty — show for every `ApplicationItem`)* |
| **Shared vs per-type** | Cross-cutting (same form for any application workflow) |
| **Background** | None (inherits `AppItemBaseReport` white page) |

---

## Page Setup

| Property | Value |
|---|---|
| **Paper** | A4 Portrait *(inherited)* |
| **Margins** | **Note:** `AppItemBaseReport` defaults to **20F** left/right. For this formal contract, **override in derived `.cs` constructor** to **100F** left/right (REPORT_STANDARDS) unless you intentionally keep 20F; printable width then **626.7717F** at 100F margins. |
| **Body font** | Times New Roman **15pt** *(section titles may use Bold)* |

---

## Band Map

| Band | Height (initial) | Notes |
|---|---|---|
| `TopMargin` | 50F | Inherited |
| `PageHeader` | 40F | Inherited — app number + date (top right) |
| `Detail` | **TBD (large, ~900–1100F)** | Full contract: title, intro, §1–§7 static bodies in `XRRichText`, bound fields via mail-merge-style RTF or adjacent `XRLabel`s per REPORT_STANDARDS |
| `ReportFooter` | **0F or `Visible = false`** | Base footer is single signatory row; **not** used for this form (replaced by §7 two-party block in `Detail`) |
| `BottomMargin` | 60F | Inherited |

---

## Control Map — Detail band (logical layout)

Positions below are **relative** placeholders; final `LocationFloat` / `SizeF` set in `Designer.cs` after pixel-matching the scan. Printable width **626.7717F**.

### Header / title

| Control | Location (approx) | Size (approx) | Source | Value / expression | Notes |
|---|---|---|---|---|---|
| `xrRichTextTitle` | X=0, Y=0 | W=626.77, H≈35 | Static RTF | Centered **ZÄHMET ŞERTNAMASY** (bold) | |
| `xrRichTextCity` | Below title | W=626.77, H≈28 | Static + Bound | Line 1: static **Aşgabat şäheri.** Line 2: intro sentence binding employer + employee + position | Intro pattern: *IŞ BERIJI* `[Application_SponsorName]` … *wezipesine* `[Position_PositionTm]` *wezipesine işe kabul edilen* **[bound]** *IŞGÄR* `[Person_FullName]` … *(Turkmen legal wording — finalize against scan in pre-code review §14)* |

### Body — Sections 1–6 (mostly static)

| Control | Source | Notes |
|---|---|---|
| `xrRichBodySections1to4RTF` | Static RTF | **§1** Iş berijiniň borçlary (1.1–1.4); **§2** Işgäriň borçlary (2.1–2.5); **§3** Iş we dynç alyş düzgüni (3.1–3.4); **§4** Zähmet şertnamasynyň ýatyrylmagy (4.1–4.7). Use `\qj\fi720` for paragraphs. |
| `xrRichBodySection5RTF` | Static + Bound | **§5** Möhlet: dates `[Contract_StartDateText]` – `[Contract_ExpirationDateText]`, closing sentences static (ýöz öňünden çykarmazdan uzaldygy, etc.). |
| `xrRichBodySection6RTF` | Static + Bound | **§6** Aýlyk: `[Contract_SalaryText]` + `[Salary_CurrencyCode]` (or Tm label); payment sentence **static** (e.g. Türkiýedäki bank hasabyna geçirilýän — *verify Turkmen phrasing against official form*). |

### §7 Taraplaryň gollary we salgylary (two columns)

| Control | Source | Value / expression | Notes |
|---|---|---|---|
| `xrRichTextEmployerParty` | Mixed | Bold label *IŞ BERIJI.* Bind: `[Application_SponsorSignatory]`; company `[Application_SponsorName]`; address `[Application_CompanyAddress]` | Round stamp area: **ignored** |
| `xrRichTextEmployeeParty` | Mixed | Bold label *IŞGÄR.* Bind: `[Person_FullName]`; passport `[Passport_Number]` | Handwritten signature: **ignored** |

---

## Ignored Elements (do not render)

| Element | Reason |
|---|---|---|
| Employer’s round blue stamp | Official stamp — not reproduced |
| Handwritten signatures | Not reproduced |
| Logo / decorative borders | None on sample; if present on other scans, skip |

---

## Required BO properties (ApplicationItem unless noted)

| Property | Purpose | Status |
|---|---|---|
| `Person_FullName` | Employee name | ✅ exists |
| `Position_PositionTm` | Position (wezipe) | ✅ exists |
| `Passport_Number` | Passport belgisi | ✅ exists |
| `Application_SponsorName` | Employer company name | ✅ exists |
| `Application_SponsorSignatory` | Employer representative name | ✅ exists |
| `Application_FullNumber` | Header (inherited) | ✅ exists |
| `Application_DateText` | Header date (inherited) | ✅ exists |
| `Contract_Salary` / `Contract_SalaryText` | Numeric salary | ✅ exists |
| `Contract_StartDateText` | `dd.MM.yyyy` from `CurrentEmployeeContract.ContractStartDate` | ❌ **add** `[NotMapped]` |
| `Contract_ExpirationDateText` | from `CurrentEmployeeContract.ExpirationDate` | ❌ **add** `[NotMapped]` |
| `Salary_CurrencyCode` | `Person.CurrentSalary.Currency` → `"USD"` / `"TMT"` | ❌ **add** `[NotMapped]` |
| `Application_CompanyAddress` | `Application.Company.Address` | ❌ **add** `[NotMapped]` |

---

## Open points for your approval

1. **Exact `ApplicationType.Name`** for visibility (see Data section).
2. **§6 currency & payment text:** OK to bind currency from `Person.CurrentSalary.Currency` when contract salary is in the same currency, or should salary currency be a dedicated field on `EmployeeContract`?
3. **Intro paragraph (under city):** Final Turkmen wording must match the official template — compare character-by-character to your scan during implementation (REPORT_STANDARDS §14).
4. **ReportFooter:** Confirm **hiding** the base single signatory footer in favor of the two-party §7 block in `Detail`.

---

## Next step (after ✅ Agreed)

1. Add missing `[NotMapped]` properties on `ApplicationItem.cs`.
2. Turkmen pre-code review on all static RTF strings.
3. Generate `AppLaborContractItemReport.cs`, `.Designer.cs`, `.resx`; register in `ReportsUpdater.cs`.
4. Update catalog + dashboard in `REPORT_GENERATION_GUIDE.md`.
