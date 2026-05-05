# Map: Zähmet şertnamasy (Labor Contract) — ApplicationItem

**Status:** ✅ Implemented — `AppLaborContractItemReportV2` matches `App_Labor_Contract_item.png` using positioned labels.

---

## Report Identity

| Field | Value |
| --- | --- |
| **Class name** | `AppLaborContractItemReportV2` |
| **Registered name** | `"App Labor Contract Item Report V2"` |
| **Display name (Tm)** | `Zähmet şertnamasy — Şahsy (V2)` |
| **Reference image** | `App_Labor_Contract_item.png` |

---

## Data

| Field | Value |
| --- | --- |
| **Data type (BO)** | `ApplicationItem` |
| **Base class** | `AppItemBaseReport` |
| **Visibility criteria** | *(empty — show for every `ApplicationItem`)* |
| **Shared vs per-type** | Cross-cutting (same form for any application workflow) |
| **Background** | None (plain white inherited from base class) |

---

## Page Setup

| Property | Value |
| --- | --- |
| **Paper** | A4 Portrait *(inherited)* |
| **Margins** | `DXMargins(40F, 40F, 30F, 40F)` — matches printable width `626.7717F` similar to scan |
| **Fonts** | Times New Roman — 13pt bold for title, 12pt bold for section headers, 11pt regular for body text |

---

## Band Map

| Band | Height | Notes |
| --- | --- | --- |
| `TopMargin` | 30F | Inherited content untouched |
| `PageHeader` | 40F | Inherited — application number/date aligned right |
| `Detail` | 915F | Entire contract body, sections, and signature blocks |
| `ReportFooter` | `Visible = false` | Section 7 contains signature lines |
| `BottomMargin` | 40F | Inherited |

---

## Control Map — Detail band layout

Printable width: **626.7717F**. `lblTitle`/`lblCity` remain positioned labels; the rest of the body is rendered via a single-column `XRTable` (`tableBody`). Each row auto-expands, eliminating coordinate juggling and overlaps.

| Control | Location (X, Y) | Size (W, H) | Source | Value / Expression | Notes |
| --- | --- | --- | --- | --- | --- |
| `lblTitle` | (0F, 0F) | (626.77F, 26F) | Static | `ZÄHMET ŞERTNAMASY` | 13pt bold, centered |
| `lblCity` | (0F, 30F) | (626.77F, 18F) | Static | `Aşgabat şäheri` | 11pt bold, centered |
| `tableBody` | (0F, 52F) | (626.77F, auto) | Static + Expressions | XRTable with 1 column, multiple rows | All cells use Times NR 11pt; paragraph rows have left padding (20F) and `TextAlignment=TopJustify`. |
| ├─ `tableCellIntro` | — | — | Expression | `FormatString("Bu zähmet şertnamasynda bir tarapdan {0} (mundan beýläk \"IŞ BERIJI\"), onuň {1} arkaly wekillerçilik edilýän tarap bilen, beýleki tarapdan {2} (mundan beýläk \"IŞGÄR\") arasynda baglaşyldy. IŞGÄR {3} wezipesinde işe kabul edilýär.", [Application_SponsorName], [Application_SponsorSignatory], [Person_FullName], [Position_PositionTm])` | Left aligned paragraph |
| ├─ `tableCellSection1` | — | — | Static | `1. Iş berijiniň borçlary
1.1. Işgäre hünärine laýyk iş bermeli.
1.2. Her aý aýlyk zähmet hakyny bellenilen günde tölemeli.
1.3. Türkmenistanyň zähmet kanunlaryna laýyklykda ýyllyk zähmet rugsadyny üpjün etmeli.
1.4. Şertnamanyň möhletinde zähmet şertlerini we sosial goraglary kanuna laýyk üpjün etmeli.` | Uses manual line breaks, 11pt |
| ├─ `tableCellSection2` | — | — | Static | `2. Işgäriň borçlary
2.1. Tabşyrylan işleri dogry we doly ýerine ýetirmeli.
2.2. Kärhananyň içerki tertipnamalaryna, tehniki howpsuzlyk we zähmet gorag düzgünlerine eýermeli.
2.3. Iş ýerini arassa saklamaly we enjamlary ähtiyatly ulanmaly.
2.4. Kompaniýanyň hyzmat syrlaryny gizlin saklamaly.
2.5. Bölüm başlygynyň görkezmelerini ýerine ýetirmeli.` | |
| ├─ `tableCellSection3` | — | — | Static | `3. Iş we dynç alyş düzgüni
3.1. Içerki iş tertibine we Türkmenistanyň zähmet kanunlaryna eýermeli.
3.2. Iş wagty — günde 8 sagat, hepdede 6 iş güni.
3.3. Zerur bolanda kanunçylykdaky tertipde goşmaça işe çagyrylyp bilinýär.
3.4. Aýlyk zähmet haky kadr bölüminiň sanawynyň esasynda tölenýär.` | |
| ├─ `tableCellSection4` | — | — | Static | `4. Zähmet şertnamasynyň ýatyrylmagy
Zähmet şertnamasy "IŞ BERJI" tarapyndan aşakdaky ýagdaýlarda ýatyrylýar:
4.1. Zähmet şertnamasynyň möhletiniň gutarmagy;
4.2. Işleriň gutarmagy;
4.3. Işgäriň azalmagy;
4.4. Işe serhoş bolup, narkotiki ýa-da zäherli maddalaryň täsiri astynda gelmegi;
4.5. Zähmet şertnamasyna ýa-da kärhananyň içerki tertip düzgünlerine laýyklykda özüne tabşyrylan borçlary işgäriň berjaý etmän ýeriňe ýetir-
megi;
4.6. Kärhana degişli emlägi ogurlamagy;
4.7. Kärhananyň bähbitlerine garşy gönükdirilen hereketleri amala aşyrmagy;
4.8. Şu şertnamada görkezilen jähtlerdäki meseleler Türkmenistanyň hereket edýän kanunlary esasynda çözülýär.` | |
| ├─ `tableCellSection5` | — | — | Expression | `FormatString("Şertnama {0} — {1} aralygynda hereket edýär.", [Contract_StartDateText], [Contract_ExpirationDateText])` | |
| ├─ `tableCellSection5Note` | — | — | Static | `Taraplar gol çeken pursatdan başlap güýje girýär. Möhleti gutaran ýagdaýynda taraplaryň biri garşy bolmasa şertnama şol bir möhlet üçin awtomatiki uzaldylan hasaplanylýar.` | |
| ├─ `tableCellSection6` | — | — | Expression | `FormatString("Aýlyk zähmet haky: {0} {1}.", [Contract_SalaryText], [Salary_CurrencyCode])` | |
| ├─ `tableCellSection6Note` | — | — | Static | `Töleg: zähmet haky Türkiýedäki bank hasabyna geçirilýär.` | |
| ├─ `tableCellSection7` | — | — | Static | `7. Taraplaryň gollary we salgylary` | 12pt Bold |
| ├─ `tableCellSignatures` | — | — | Container | — | Hosts `panelSignatures`; fills auto height |
| ├─ `tableCellEmployerHeader` | — | — | Static | `IŞ BERIJI:` | Bold |
| ├─ `tableCellEmployerSignatory` | — | — | Expression | `[Application_SponsorSignatory]` | |
| ├─ `tableCellEmployerCompany` | — | — | Expression | `[Application_SponsorName]` | |
| ├─ `tableCellEmployerAddress` | — | — | Expression | `[Application_CompanyAddress]` | Multi-line |
| ├─ `tableCellEmployeeHeader` | — | — | Static | `IŞGÄR:` | Bold |
| ├─ `tableCellEmployeeName` | — | — | Expression | `[Person_FullName]` | |
| └─ `tableCellEmployeePassport` | — | — | Expression | `FormatString("Pasport belgisi: {0}", [Passport_Number])` | |
| `panelSignatures` | (0F, 0F) | (626.77F, 100F) | Container | — | Nested inside `tableCellSignatures`; two-column layout |
| ├─ `lblEmployerHeader` | (0F, 0F) | (304F, 18F) | Static | `IŞ BERIJI:` | Bold |
| ├─ `lblEmployerSignatory` | (0F, 20F) | (304F, 18F) | Expression | `[Application_SponsorSignatory]` | |
| ├─ `lblEmployerCompany` | (0F, 40F) | (304F, 18F) | Expression | `[Application_SponsorName]` | |
| ├─ `lblEmployerAddress` | (0F, 60F) | (304F, 38F) | Expression | `[Application_CompanyAddress]` | Multi-line |
| ├─ `lblEmployeeHeader` | (322.77F, 0F) | (304F, 18F) | Static | `IŞGÄR:` | Bold |
| ├─ `lblEmployeeName` | (322.77F, 20F) | (304F, 18F) | Expression | `[Person_FullName]` | |
| └─ `lblEmployeePassport` | (322.77F, 40F) | (304F, 18F) | Expression | `FormatString("Pasport belgisi: {0}", [Passport_Number])` | |

---

## Ignored Elements

| Element | Reason |
| --- | --- |
| Blue round stamp | Physical stamp, not reproduced |
| Handwritten signatures | Left blank for manual signing |
| Any background shading | Report remains white per standards |

---

## Required BO properties

| Property | Purpose | Status |
| --- | --- | --- |
| `Application_SponsorName` | Intro and employer block | ✅ exists |
| `Application_SponsorSignatory` | Intro and employer block | ✅ exists |
| `Application_CompanyAddress` | Employer block | ✅ exists |
| `Person_FullName` | Intro and employee block | ✅ exists |
| `Position_PositionTm` | Intro paragraph | ✅ exists |
| `Passport_Number` | Employee block | ✅ exists |
| `Contract_StartDateText` | Section 5 | ✅ exists |
| `Contract_ExpirationDateText` | Section 5 | ✅ exists |
| `Contract_SalaryText` | Section 6 | ✅ exists |
| `Salary_CurrencyCode` | Section 6 | ✅ exists |

---

## Open points

1. Confirm paragraph wording matches the approved Turkmen language for this contract.
2. Confirm 40F side margins and 11pt body font provide acceptable visual fidelity to the scanned form.
3. Validate two-column signature panel spacing against stamping/signature requirements.

---

## Next steps after approval

Once confirmed, generate `AppLaborContractItemReportV2.cs`, `.Designer.cs`, and `.resx` following this layout, register the new report in `ReportsUpdater.cs`, and set map status to `✅ Implemented`.
