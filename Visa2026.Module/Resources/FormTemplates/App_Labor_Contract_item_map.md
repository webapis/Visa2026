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

Printable width: **626.7717F**. All labels use transparent background, no borders, `WordWrap = true`, and zero padding unless noted. Paragraph bodies mimic scan line breaks.

| Control | Location (X, Y) | Size (W, H) | Source | Value / Expression | Notes |
| --- | --- | --- | --- | --- | --- |
| `lblTitle` | (0F, 0F) | (626.77F, 26F) | Static | `ZÄHMET ŞERTNAMASY` | 13pt Bold, center-aligned |
| `lblCity` | (0F, 30F) | (626.77F, 18F) | Static | `Aşgabat şäheri` | 11pt, center |
| `lblIntroParagraph` | (0F, 52F) | (626.77F, 70F) | Expression | `FormatString("Bu zähmet şertnamasynda bir tarapdan {0} (mundan beýläk \"IŞ BERIJI\"), onuň {1} arkaly wekillerçilik edilýän tarap bilen, beýleki tarapdan {2} (mundan beýläk \"IŞGÄR\") arasynda baglaşyldy. IŞGÄR {3} wezipesinde işe kabul edilýär.", [Application_SponsorName], [Application_SponsorSignatory], [Person_FullName], [Position_PositionTm])` | Left aligned paragraph |
| `lblSection1Header` | (0F, 128F) | (626.77F, 18F) | Static | `1. Iş berijiniň borçlary` | 12pt Bold |
| `lblSection1Body` | (0F, 148F) | (626.77F, 86F) | Static | `1.1. Işgäre hünärine laýyk iş bermeli.
1.2. Her aý aýlyk zähmet hakyny bellenilen günde tölemeli.
1.3. Türkmenistanyň zähmet kanunlaryna laýyklykda ýyllyk zähmet rugsadyny üpjün etmeli.
1.4. Şertnamanyň möhletinde zähmet şertlerini we sosial goraglary kanuna laýyk üpjün etmeli.` | Uses manual line breaks, 11pt |
| `lblSection2Header` | (0F, 236F) | (626.77F, 18F) | Static | `2. Işgäriň borçlary` | 12pt Bold |
| `lblSection2Body` | (0F, 256F) | (626.77F, 110F) | Static | `2.1. Tabşyrylan işleri dogry we doly ýerine ýetirmeli.
2.2. Kärhananyň içerki tertipnamalaryna, tehniki howpsuzlyk we zähmet gorag düzgünlerine eýermeli.
2.3. Iş ýerini arassa saklamaly we enjamlary ähtiyatly ulanmaly.
2.4. Kompaniýanyň hyzmat syrlaryny gizlin saklamaly.
2.5. Bölüm başlygynyň görkezmelerini ýerine ýetirmeli.` | |
| `lblSection3Header` | (0F, 370F) | (626.77F, 18F) | Static | `3. Iş we dynç alyş düzgüni` | 12pt Bold |
| `lblSection3Body` | (0F, 390F) | (626.77F, 74F) | Static | `3.1. Içerki iş tertibine we Türkmenistanyň zähmet kanunlaryna eýermeli.
3.2. Iş wagty — günde 8 sagat, hepdede 6 iş güni.
3.3. Zerur bolanda kanunçylykdaky tertipde goşmaça işe çagyrylyp bilinýär.
3.4. Aýlyk zähmet haky kadr bölüminiň sanawynyň esasynda tölenýär.` | |
| `lblSection4Header` | (0F, 466F) | (626.77F, 18F) | Static | `4. Zähmet şertnamasynyň ýatyrylmagy` | 12pt Bold |
| `lblSection4Body` | (0F, 486F) | (626.77F, 124F) | Static | `Şertnama aşakdaky ýagdaýlarda ýatyrylýar:
4.1. Şertnamanyň möhleti gutaranda.
4.2. Işi tamamlananda.
4.3. Işgärleriň sany azalanda.
4.4. Işe serhoş, täsirli serişde ýa-da neşe arkaly gelende.
4.5. Zähmet borçlaryny ýerine ýetirmeýände ýa-da içerki düzgünleri bozanda.
4.6. Kompaniýanyň emlägine zeper ýetirende ýa-da ogurlanda.
4.7. Galan dawalar Türkmenistanyň hereket edýän kanunlaryna laýyk çözülýär.` | |
| `lblSection5Header` | (0F, 614F) | (626.77F, 18F) | Static | `5. Zähmet şertnamasynyň hereket edýän möhleti` | 12pt Bold |
| `lblSection5Line1` | (0F, 634F) | (626.77F, 20F) | Expression | `FormatString("Şertnama {0} — {1} aralygynda hereket edýär.", [Contract_StartDateText], [Contract_ExpirationDateText])` | |
| `lblSection5Line2` | (0F, 656F) | (626.77F, 36F) | Static | `Taraplar gol çeken pursatdan başlap güýje girýär. Möhleti gutaran ýagdaýynda taraplaryň biri garşy bolmasa şertnama şol bir möhlet üçin awtomatiki uzaldylan hasaplanylýar.` | |
| `lblSection6Header` | (0F, 698F) | (626.77F, 18F) | Static | `6. Türkmenistan döwletinde alýan aýlyk zähmet haky` | 12pt Bold |
| `lblSection6Line1` | (0F, 718F) | (626.77F, 20F) | Expression | `FormatString("Aýlyk zähmet haky: {0} {1}.", [Contract_SalaryText], [Salary_CurrencyCode])` | |
| `lblSection6Line2` | (0F, 740F) | (626.77F, 20F) | Static | `Töleg: zähmet haky Türkiýedäki bank hasabyna geçirilýär.` | |
| `lblSection7Header` | (0F, 766F) | (626.77F, 18F) | Static | `7. Taraplaryň gollary we salgylary` | 12pt Bold |
| `panelSignatures` | (0F, 788F) | (626.77F, 108F) | Container | — | Hosts two-column layout |
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
