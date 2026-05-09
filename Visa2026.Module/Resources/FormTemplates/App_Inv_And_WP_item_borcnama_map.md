# Map: App_Inv_And_WP — Borçnama (ApplicationItem)

## Identity

| | |
|---|---|
| **ApplicationType** | `App_Inv_And_WP` |
| **Level** | `ApplicationItem` |
| **Report class** | `AppInvAndWPBorcnamaItemReport` |
| **Registered name** | `App Inv And WP Borcnama Item Report` |
| **Display (Tm)** | Çakylyk we Iş Rugsatnamasy — Borçnama |
| **Reference image** | `App_Inv_And_WP_item_borcnama.png` |
| **Status** | ✅ Implemented |

## Page

- A4 portrait, white background (no letterhead watermark).
- Margins: 40F L/R, 30F T/B (matches other dense item forms).

## Band / control map

| Control | Type | Content |
|---|---|---|
| `xrRichHeader` | XRRichText | Centered static header (four lines) + title context |
| `xrLabelTitle` | XRLabel | `BORÇNAMA` |
| `xrLabelCompanyLine` + captions | XRLabel | Underlined company + small helper captions (scan-style) |
| `xrLabelCompanyRegistryLine` + captions | XRLabel | Underlined registry/address/phone line + helper caption |
| Worker name/DOB lines | XRLabel | Underlined fields + helper captions |
| Head/Representative lines | XRLabel | Underlined fields + passport lines + helper captions |
| `xrRichBody` | XRRichText | Verbatim scan paragraphs (justified) |
| Signature block | XRLabel + XRLine | Underlined signature lines + helper captions, names centered |

## Data fields (`ApplicationItem`)

| Field | Notes |
|---|---|
| `Application_SponsorName` | Company legal name |
| `Application_CompanyRegistryAddressLine` | `TaxInformation` + `Address` + `PhoneNumber` (single line; operators format tax/date block in `TaxInformation`) |
| `Person_FullName`, `Person_DateOfBirthText` | Invited worker |
| `CompanyHead_FullName`, `CompanyHead_PassportLine` | Signatory; passport only when head is expat `Person` |
| `Representative_FullName`, `Representative_PassportLine` | Rep; passport only when rep is expat `Person` |
| `Representative_Phone` | Reserved (empty until BO supports phone) |

## Ignored on reference scan

- Handwritten signatures and round stamp (not generated).
