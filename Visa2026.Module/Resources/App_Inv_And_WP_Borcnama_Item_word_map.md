# Word Map: App_Inv_And_WP_Borcnama_Item — Word Report Design Contract

## Identity

| | |
|---|---|
| **ApplicationType** | All (cross-cutting) |
| **Level** | `ApplicationItem` (cross-cutting) |
| **Template** | `App_Inv_And_WP_Borcnama_Item.docx` |
| **ReportDef** | `AppInvAndWPBorcnamaItemReportDef.cs` |
| **Display (Tm)** | Borçnama (Word) — cross-cutting |
| **Reference image** | `App_Inv_And_WP_item_borcnama.png` |
| **Status** | ✅ Implemented |

## Page

- **Format**: A4 portrait (11906×16838 twips).
- **Margins**: 1020 twips L/R (~0.7"), 720 twips T/B (~0.5").
- **Font**: Times New Roman throughout.
- **Content width**: ~9866 twips (content column for underlined fields).

## Layout Family

- **F1**: Single-item commitment form with underlined value fields and justified legal body.
- One A4 page per `ApplicationItem` (compact spacing for typical data).

## Cross-Cutting Applicability

- `ApplicableApplicationTypeNames => Array.Empty<string>()` (empty = all types).
- `IsApplicable(Application)` returns `true` unconditionally.
- Reports menu shows this report for every `Application` with at least one `ApplicationItem`.

## Data Anchors

- **Header**: Empty (no root-level fields).
- **Rows**: Per-item data via `FillListForm` (`{{#ds.rows}}` … `{{/ds.rows}}`).
- **Page break**: `{{:s:}}{{:PageBreak}}` between persons (DocxTemplater separator).

## Band / Control Map (Word → OpenXML)

| Section | OpenXML Structure | Content |
|---|---|---|
| **Header** | Centered paragraphs (sz=20/10pt) | Static 4-line header + "BORÇNAMA" title (sz=28/14pt, bold) |
| **Company name** | Full-width single-cell table, bottom border | `{{ds.rows.Application_SponsorName}}` underlined |
| **Company caption** | Italic centered paragraph (sz=14/7pt) | "(kärhananyň ady, hukuk guramasyçylyk görnüşi)" |
| **Registry line** | Full-width single-cell table, bottom border | `{{ds.rows.Application_CompanyRegistryAddressLine}}` underlined |
| **Registry caption** | Italic centered paragraph (sz=14/7pt) | "(hasaba alynan belgisi, ýuridiki salgysy)" |
| **Worker preamble** | Left-aligned paragraph (sz=20/10pt) | "Ýokarda ady görkezilen kärhana tarapyndan..." |
| **Worker row** | 3-column borderless table | Name (w=334/746) + DOB (w=168/746) + "jogapkärçiligini alýarys:" |
| **Worker name** | Underlined value + caption below (sz=14/7pt) | `{{ds.rows.Person_FullName}}` + "(ady, familliýasy...)" |
| **Worker DOB** | Underlined value + caption below | `{{ds.rows.Person_DateOfBirthText}}` + "(doglan senesi)" |
| **Head preamble** | Left paragraph (sz=20/10pt) | "Kärhananyň ýolbaşçysy" |
| **Head name** | Full-width table, bottom border | `{{ds.rows.CompanyHead_FullName}}` underlined |
| **Head caption** | Italic centered (sz=14/7pt) | "(familliýasy, ady, atasynyň ady)" |
| **Head passport** | Full-width table with "pasporty " prefix | `{{ds.rows.CompanyHead_PassportLine}}` underlined |
| **Head passport caption** | Italic centered (sz=14/7pt) | "(pasportyň seriýasy, belgisi...)" |
| **Representative preamble** | Left paragraph | "we Kärhananyň wiza işleri boýunça ygtyýarly wekili:" |
| **Rep name** | Full-width table, bottom border | `{{ds.rows.Representative_FullName}}` underlined |
| **Rep caption** | Italic centered (sz=14/7pt) | "(familliýasy, ady, atasynyň ady)" |
| **Rep passport** | Full-width table with "pasporty " prefix | `{{ds.rows.Representative_PassportLine}}` underlined |
| **Rep passport caption** | Italic centered (sz=14/7pt) | "(pasportyň seriýasy, belgisi..., telefon belgisi)" |
| **Legal body** | 6 justified paragraphs (sz=20/10pt) | Body P1a-e + P2 (tight line spacing 220 twips) |
| **Confirm preamble** | Left paragraph (sz=20/10pt, spaceBefore=72) | "Borçnamany tassyklaýarys:" |
| **Head signature** | Full-width table with "Kärhananyň ýolbaşçysy:" prefix | `{{ds.rows.CompanyHead_FullName}}` underlined |
| **Head sig caption** | Italic centered | "(familliýasy, ady, atasynyň ady)" |
| **Head "(gol)"** | Italic right-aligned (sz=14/7pt) | "(gol)" |
| **Rep signature** | Full-width table with "Kärhananyň wiza işleri..." prefix | `{{ds.rows.Representative_FullName}}` underlined |
| **Rep sig caption** | Italic centered | "(familliýasy, ady, atasynyň ady)" |
| **Rep "(gol)"** | Italic right-aligned (sz=14/7pt) | "(gol)" |

## Data Fields (`ApplicationItem`)

| Field | Notes |
|---|---|
| `Application_SponsorName` | Company legal name (italic underlined) |
| `Application_CompanyRegistryAddressLine` | Tax + Address + Phone (single line, italic underlined) |
| `Person_FullName` | Worker name (italic underlined) |
| `Person_DateOfBirthText` | Worker DOB (italic underlined) |
| `CompanyHead_FullName` | Head signatory name |
| `CompanyHead_PassportLine` | Head passport info (italic) |
| `Representative_FullName` | Representative name |
| `Representative_PassportLine` | Rep passport info (italic) |
| `Representative_Phone` | Reserved (empty caption placeholder) |

## Template Patterns

- **Underlined fields**: Single-cell tables with bottom border only (`BorderValues.Single` at 12 half-points).
- **Captions**: Small italic (sz=14) centered below underlined values.
- **Legal body**: Justified paragraphs with first-line indent (454 twips), compact line spacing (220 twips).
- **Signatures**: Full-width tables with left-aligned label prefix + underlined value.

## Ignored on Reference Scan

- Handwritten signatures (generated as "(gol)" placeholders).
- Round stamp (not generated).

## Differences from XtraReport

| Aspect | XtraReport | Word |
|---|---|---|
| Header | `xrRichHeader` (RTF) | 4 centered paragraphs |
| Underlines | `XRLabel` + `XRLine` | Single-cell table bottom border |
| Captions | Small labels under fields | Italic paragraphs below tables |
| Worker row | Labels + underlines | 3-column borderless table |
| Body | Single `xrRichBody` | 6 justified paragraphs |
| Spacing | XtraReport units | OpenXML twips (converted) |

## Verification

- **Preview preset**: `borcnama` in `PreviewWordReports/Program.cs`
- **Check**: Single page per person with typical data; all underlines visible; captions not clipped.
