using System.Drawing;
using DevExpress.Drawing;
using DevExpress.XtraReports.UI;
using DevExpress.XtraPrinting;

namespace Visa2026.Module.Reports
{
    partial class RegistrationForm16Report
    {
        private void InitializeComponent()
        {
            // ── Controls ────────────────────────────────────────────────────
            this.xrLabelTitle           = new XRLabel();
            this.xrTablePersonal        = new XRTable();
            this.xrRowName              = new XRTableRow();
            this.xrRowNationality       = new XRTableRow();
            this.xrRowPassportNo        = new XRTableRow();
            this.xrRowPassportDate      = new XRTableRow();
            this.xrRowBirth             = new XRTableRow();
            this.xrRowVisaType          = new XRTableRow();
            this.xrRowAddress           = new XRTableRow();
            this.xrRowVisaPlace         = new XRTableRow();
            this.xrRowVisaDates         = new XRTableRow();
            this.xrRowDuration          = new XRTableRow();
            this.xrRowCompany           = new XRTableRow();
            this.xrPicturePhoto         = new XRPictureBox();
            this.xrTableReg             = new XRTable();
            this.xrRowRegHeader         = new XRTableRow();
            this.xrRowRegData           = new XRTableRow();
            this.xrTableLoc             = new XRTable();
            this.xrRowLocHeader         = new XRTableRow();
            this.xrRowLocData           = new XRTableRow();
            this.xrLabelDeregLabel      = new XRLabel();
            this.xrLabelDeregLine       = new XRLabel();
            this.xrLabelNoteLabel       = new XRLabel();
            this.xrLabelNoteValue       = new XRLabel();
            this.xrLabelMilliLabel      = new XRLabel();
            this.xrLabelMilliValue      = new XRLabel();
            this.xrLabelPassportMLabel  = new XRLabel();
            this.xrLabelPassportMValue  = new XRLabel();
            this.xrLabelEsasLabel       = new XRLabel();
            this.xrLabelEsasValue       = new XRLabel();

            ((System.ComponentModel.ISupportInitialize)(this.xrTablePersonal)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableReg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableLoc)).BeginInit();

            // ── Page — A4 Portrait (override base Landscape) ────────────────
            this.Landscape    = false;
            this.PageWidthF   = 826.7717F;
            this.PageHeightF  = 1169.291F;
            this.Margins      = new DXMargins(20F, 20F, 50F, 60F);
            // Content width: 786.7717F

            // ── Hide inherited PageHeader (app number embedded in reg table) ─
            this.PageHeader.HeightF = 0F;
            this.PageHeader.Visible = false;

            // ── Reposition inherited signatory for portrait width ────────────
            this.ReportFooter.HeightF = 60F;
            this.xrLabelSignatoryPosition.LocationFloat = new DevExpress.Utils.PointFloat(0F, 20F);
            this.xrLabelSignatoryPosition.SizeF         = new SizeF(393F, 20F);
            this.xrLabelSignatoryPosition.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelSignatoryFullName.LocationFloat = new DevExpress.Utils.PointFloat(393F, 20F);
            this.xrLabelSignatoryFullName.SizeF         = new SizeF(393.7717F, 20F);
            this.xrLabelSignatoryFullName.TextAlignment = TextAlignment.MiddleRight;

            // ================================================================
            // TITLE
            // ================================================================
            this.xrLabelTitle.Text          = "DA\u015EARY \u00DDURT RA\u00DDATLARYNY BELLIGE ALY\u015E NAMASY";
            this.xrLabelTitle.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabelTitle.SizeF         = new SizeF(786.7717F, 26F);
            this.xrLabelTitle.TextAlignment = TextAlignment.MiddleCenter;
            this.xrLabelTitle.Name          = "xrLabelTitle";
            this.xrLabelTitle.Borders       = BorderSide.All;
            this.xrLabelTitle.BorderWidth   = 0.5F;
            this.xrLabelTitle.BorderColor   = Color.Black;

            // ================================================================
            // PERSONAL INFO TABLE — left section (X=0, W=560F, 11 rows × 27F)
            // ================================================================
            // Label column = weight 200, Value column = weight 360, total = 560
            float personalW = 560F;
            float rowH      = 27F;

            SetupPersonalRow(this.xrRowName,         1,  "1. Famili\u00FDasy, ady, atasyny\u0148 ady:",
                "[Person_LastName] + ' ' + [Person_FirstName] + ' ' + [Person_MiddleName]", rowH);
            SetupPersonalRow(this.xrRowNationality,  2,  "2. Ra\u00FDatalygy:",
                "[Person_NationalityCode] + ' ' + [Person_NationalityTm]", rowH);
            SetupPersonalRow(this.xrRowPassportNo,   3,  "3. Pasport belgisi:",
                "[Passport_Number]", rowH);
            SetupPersonalRow(this.xrRowPassportDate, 4,  "4. Pasport berleni:",
                "[Passport_IssueDateText]", rowH);
            SetupPersonalRow(this.xrRowBirth,        5,  "5. Doglan \u00FDeri, \u00FDurdy:",
                "[Person_CountryOfBirthCode] + ' ' + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace] + '  ' + [Person_DateOfBirthText]", rowH);
            SetupPersonalRow(this.xrRowVisaType,     6,  "6. Emek ugrundaky wizany\u0148 g\u00F6rn\u00FC\u015Fi:",
                "[Visa_TypeTm] + ' ' + [Visa_CategoryTm]", rowH);
            SetupPersonalRow(this.xrRowAddress,      7,  "7. T\u00FCrkmenistandaky bol\u00FDan \u00FDeri:",
                "[Address_FullAddress]", rowH);
            SetupPersonalRow(this.xrRowVisaPlace,    8,  "8. Wiza berlen \u00FDeri we belgisi:",
                "[Visa_IssuedPlaceTm] + ' \u2116' + [Visa_Number]", rowH);
            SetupPersonalRow(this.xrRowVisaDates,    9,  "9. Wizany\u0148 berleni we m\u00F6hleti:",
                "[Visa_StartDateText] + ' \u2014 ' + [Visa_ExpirationDateText]", rowH);
            SetupPersonalRow(this.xrRowDuration,     10, "10. T\u00FCrkmenistandaky galynmaly g\u00FCnleri:",
                "", rowH);
            SetupPersonalRow(this.xrRowCompany,      11, "11. Kabul edilen elara \u00FDa-da \u015Fa\u00FDh\u00FD\u00FDet:",
                "[Person_CompanyName]", rowH);

            this.xrTablePersonal.LocationFloat = new DevExpress.Utils.PointFloat(0F, 26F);
            this.xrTablePersonal.Name          = "xrTablePersonal";
            this.xrTablePersonal.SizeF         = new SizeF(personalW, rowH * 11);
            this.xrTablePersonal.Rows.AddRange(new XRTableRow[] {
                this.xrRowName, this.xrRowNationality, this.xrRowPassportNo, this.xrRowPassportDate,
                this.xrRowBirth, this.xrRowVisaType, this.xrRowAddress, this.xrRowVisaPlace,
                this.xrRowVisaDates, this.xrRowDuration, this.xrRowCompany
            });

            // ================================================================
            // PHOTO — right section (X=565F, W=221.7717F, same height as table)
            // ================================================================
            this.xrPicturePhoto.LocationFloat = new DevExpress.Utils.PointFloat(565F, 26F);
            this.xrPicturePhoto.Name          = "xrPicturePhoto";
            this.xrPicturePhoto.SizeF         = new SizeF(221.7717F, rowH * 11);
            this.xrPicturePhoto.Sizing        = ImageSizeMode.Squeeze;
            this.xrPicturePhoto.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Image", "[Person_Photo]"));
            this.xrPicturePhoto.Borders     = BorderSide.All;
            this.xrPicturePhoto.BorderWidth = 0.5F;
            this.xrPicturePhoto.BorderColor = Color.Black;

            // ================================================================
            // REGISTRATION TABLE — full width (786.7717F)
            // Columns: TDMG|Hasaba alyş|Hasaba alnan wagty|Möhleti|Esas|Jogapkär
            // Weights: 140|130|120|100|145|151.7717 = 786.7717
            // ================================================================
            float regY = 26F + (rowH * 11) + 8F;   // below personal info + gap

            XRTableCell[] regHdr = BuildRow(this.xrRowRegHeader, 55F, true,
                ("Hasapa alan, m\u00F6hletini uzaldan TDMG-ny\u0148 belgisi we m\u00F6hleti", 140),
                ("Hasaba aly\u015F belgisi we m\u00F6hleti", 130),
                ("Hasaba alnan wagty", 120),
                ("M\u00F6hleti", 100),
                ("Esas belgisi we wagty", 145),
                ("Jogapk\u00E4r ugurlar famili\u00FDasy we ady", 151.7717));

            XRTableCell[] regData = BuildBoundRow(this.xrRowRegData, 35F,
                ("TDMGAS", null, 140),
                ("[Visa_StartDateText]", null, 130),
                ("[Visa_ExpirationDateText]", null, 120),
                ("[Application_RegistrationDateText] + ' ' + [Application_FullNumber]", null, 100),
                ("", null, 145),
                ("[CompanyHead_FullName]", null, 151.7717));

            this.xrTableReg.LocationFloat = new DevExpress.Utils.PointFloat(0F, regY);
            this.xrTableReg.Name          = "xrTableReg";
            this.xrTableReg.SizeF         = new SizeF(786.7717F, 55F + 35F);
            this.xrTableReg.Rows.AddRange(new XRTableRow[] { this.xrRowRegHeader, this.xrRowRegData });

            // ================================================================
            // LOCATION TABLE — full width (786.7717F)
            // Columns: TmSalgysy|Gelen-giden|Kabul edilen|Jogapkär
            // Weights: 220|130|220|216.7717 = 786.7717
            // ================================================================
            float locY = regY + 55F + 35F + 8F;

            XRTableCell[] locHdr = BuildRow(this.xrRowLocHeader, 55F, true,
                ("T\u00FCrkmenistandaky \u00E7\u00E4gende bol\u00FDan \u00FDeri", 220),
                ("Gelen, giden \u00FDeri", 130),
                ("Kabul edilen elara \u00FDa-da \u015Fa\u00FDlary\u0148 ady we T\u00FCrk k\u00E4rhanasyny\u0148 T\u00FCrkmenistandaky \u015Faham\u00E7asy\u0148 ady", 220),
                ("Jogapk\u00E4r ugurlar famili\u00FDasy we ady", 216.7717));

            XRTableCell[] locData = BuildBoundRow(this.xrRowLocData, 35F,
                ("[Address_FullAddress]", null, 220),
                ("[Travel_CheckPointTm]", null, 130),
                ("[Person_CompanyName]", null, 220),
                ("[CompanyHead_FullName]", null, 216.7717));

            this.xrTableLoc.LocationFloat = new DevExpress.Utils.PointFloat(0F, locY);
            this.xrTableLoc.Name          = "xrTableLoc";
            this.xrTableLoc.SizeF         = new SizeF(786.7717F, 55F + 35F);
            this.xrTableLoc.Rows.AddRange(new XRTableRow[] { this.xrRowLocHeader, this.xrRowLocData });

            // ================================================================
            // BOTTOM SECTION — deregistration, notes, milli, passport validity
            // ================================================================
            float botY = locY + 55F + 35F + 12F;
            float lblH = 22F;
            float halfW = 393.38585F; // 786.7717 / 2

            // Hasapdan aýyrmak üçin esas:
            this.xrLabelDeregLabel.Text          = "Hasapdan a\u00FDyrmak \u00FC\u00E7in esas:";
            this.xrLabelDeregLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelDeregLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, botY);
            this.xrLabelDeregLabel.SizeF         = new SizeF(220F, lblH);
            this.xrLabelDeregLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelDeregLabel.Name          = "xrLabelDeregLabel";

            this.xrLabelDeregLine.Text           = "";
            this.xrLabelDeregLine.LocationFloat  = new DevExpress.Utils.PointFloat(220F, botY);
            this.xrLabelDeregLine.SizeF          = new SizeF(566.7717F, lblH);
            this.xrLabelDeregLine.Borders        = BorderSide.Bottom;
            this.xrLabelDeregLine.BorderWidth    = 0.5F;
            this.xrLabelDeregLine.BorderColor    = Color.Black;
            this.xrLabelDeregLine.Name           = "xrLabelDeregLine";

            // Başa bellik:
            float noteY = botY + lblH + 5F;
            this.xrLabelNoteLabel.Text          = "Ba\u015Fa bellik:";
            this.xrLabelNoteLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelNoteLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, noteY);
            this.xrLabelNoteLabel.SizeF         = new SizeF(120F, lblH * 2);
            this.xrLabelNoteLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelNoteLabel.Name          = "xrLabelNoteLabel";

            this.xrLabelNoteValue.Font          = new DXFont("Times New Roman", 8F);
            this.xrLabelNoteValue.LocationFloat = new DevExpress.Utils.PointFloat(120F, noteY);
            this.xrLabelNoteValue.SizeF         = new SizeF(666.7717F, lblH * 2);
            this.xrLabelNoteValue.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelNoteValue.WordWrap      = true;
            this.xrLabelNoteValue.Borders       = BorderSide.Bottom;
            this.xrLabelNoteValue.BorderWidth   = 0.5F;
            this.xrLabelNoteValue.BorderColor   = Color.Black;
            this.xrLabelNoteValue.Name          = "xrLabelNoteValue";
            this.xrLabelNoteValue.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Address_FullAddress]"));

            // Milli: (left) | Pasportynyň möhleti: (right)
            float milliY = noteY + lblH * 2 + 5F;

            this.xrLabelMilliLabel.Text          = "Milli:";
            this.xrLabelMilliLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelMilliLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, milliY);
            this.xrLabelMilliLabel.SizeF         = new SizeF(60F, lblH);
            this.xrLabelMilliLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelMilliLabel.Name          = "xrLabelMilliLabel";

            this.xrLabelMilliValue.Font          = new DXFont("Times New Roman", 8F);
            this.xrLabelMilliValue.LocationFloat = new DevExpress.Utils.PointFloat(60F, milliY);
            this.xrLabelMilliValue.SizeF         = new SizeF(halfW - 60F, lblH);
            this.xrLabelMilliValue.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelMilliValue.Borders       = BorderSide.Bottom;
            this.xrLabelMilliValue.BorderWidth   = 0.5F;
            this.xrLabelMilliValue.BorderColor   = Color.Black;
            this.xrLabelMilliValue.Name          = "xrLabelMilliValue";
            this.xrLabelMilliValue.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Person_NationalityCode]"));

            this.xrLabelPassportMLabel.Text          = "Pasportyny\u0148 m\u00F6hleti:";
            this.xrLabelPassportMLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelPassportMLabel.LocationFloat = new DevExpress.Utils.PointFloat(halfW, milliY);
            this.xrLabelPassportMLabel.SizeF         = new SizeF(160F, lblH);
            this.xrLabelPassportMLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelPassportMLabel.Name          = "xrLabelPassportMLabel";

            this.xrLabelPassportMValue.Font          = new DXFont("Times New Roman", 8F);
            this.xrLabelPassportMValue.LocationFloat = new DevExpress.Utils.PointFloat(halfW + 160F, milliY);
            this.xrLabelPassportMValue.SizeF         = new SizeF(halfW - 160F + 0.38585F, lblH);
            this.xrLabelPassportMValue.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelPassportMValue.Borders       = BorderSide.Bottom;
            this.xrLabelPassportMValue.BorderWidth   = 0.5F;
            this.xrLabelPassportMValue.BorderColor   = Color.Black;
            this.xrLabelPassportMValue.Name          = "xrLabelPassportMValue";
            this.xrLabelPassportMValue.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text",
                    "[Passport_IssueDateText] + ' \u2014 ' + [Passport_ExpirationDateText]"));

            // Esas we ýazylyşy wagty:
            float esasY = milliY + lblH + 5F;

            this.xrLabelEsasLabel.Text          = "Esas we \u00FDazylý\u015Fy wagty:";
            this.xrLabelEsasLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelEsasLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, esasY);
            this.xrLabelEsasLabel.SizeF         = new SizeF(180F, lblH);
            this.xrLabelEsasLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelEsasLabel.Name          = "xrLabelEsasLabel";

            this.xrLabelEsasValue.Font          = new DXFont("Times New Roman", 8F);
            this.xrLabelEsasValue.LocationFloat = new DevExpress.Utils.PointFloat(180F, esasY);
            this.xrLabelEsasValue.SizeF         = new SizeF(606.7717F, lblH);
            this.xrLabelEsasValue.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelEsasValue.Borders       = BorderSide.Bottom;
            this.xrLabelEsasValue.BorderWidth   = 0.5F;
            this.xrLabelEsasValue.BorderColor   = Color.Black;
            this.xrLabelEsasValue.Name          = "xrLabelEsasValue";
            this.xrLabelEsasValue.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Application_DateText]"));

            // ── Detail band ─────────────────────────────────────────────────
            float detailH = esasY + lblH + 20F;
            this.Detail.HeightF = detailH;
            this.Detail.Controls.AddRange(new XRControl[] {
                this.xrLabelTitle,
                this.xrTablePersonal,
                this.xrPicturePhoto,
                this.xrTableReg,
                this.xrTableLoc,
                this.xrLabelDeregLabel, this.xrLabelDeregLine,
                this.xrLabelNoteLabel,  this.xrLabelNoteValue,
                this.xrLabelMilliLabel, this.xrLabelMilliValue,
                this.xrLabelPassportMLabel, this.xrLabelPassportMValue,
                this.xrLabelEsasLabel,  this.xrLabelEsasValue
            });

            ((System.ComponentModel.ISupportInitialize)(this.xrTablePersonal)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableReg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableLoc)).EndInit();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>Sets up a 2-column personal info row: label | bound value.</summary>
        private void SetupPersonalRow(XRTableRow row, int num, string label, string expression, float height)
        {
            var labelCell = new XRTableCell();
            var valueCell = new XRTableCell();

            labelCell.Text          = label;
            labelCell.Weight        = 200;
            labelCell.Font          = new DXFont("Times New Roman", 7F, DXFontStyle.Bold);
            labelCell.TextAlignment = TextAlignment.MiddleLeft;
            labelCell.WordWrap      = true;
            labelCell.Padding       = new PaddingInfo(3, 2, 1, 1);
            labelCell.Borders       = BorderSide.Left | BorderSide.Right | BorderSide.Bottom;
            labelCell.BorderWidth   = 0.5F;
            labelCell.BorderColor   = Color.Black;

            valueCell.Weight        = 360;
            valueCell.Font          = new DXFont("Times New Roman", 8F);
            valueCell.TextAlignment = TextAlignment.MiddleLeft;
            valueCell.WordWrap      = true;
            valueCell.Padding       = new PaddingInfo(3, 2, 1, 1);
            valueCell.Borders       = BorderSide.Right | BorderSide.Bottom;
            valueCell.BorderWidth   = 0.5F;
            valueCell.BorderColor   = Color.Black;
            if (!string.IsNullOrEmpty(expression))
                valueCell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expression));

            row.HeightF = height;
            row.Cells.AddRange(new XRTableCell[] { labelCell, valueCell });

            // Add top border on first row only
            if (num == 1)
            {
                labelCell.Borders |= BorderSide.Top;
                valueCell.Borders |= BorderSide.Top;
            }
        }

        /// <summary>Builds a header row with bold centered cells across the full 786.7717F table.</summary>
        private XRTableCell[] BuildRow(XRTableRow row, float height, bool isHeader,
            params (string text, double weight)[] columns)
        {
            var cells = new XRTableCell[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                var c = new XRTableCell();
                c.Text          = columns[i].text;
                c.Weight        = columns[i].weight;
                c.Font          = isHeader
                    ? new DXFont("Times New Roman", 6.5F, DXFontStyle.Bold)
                    : new DXFont("Times New Roman", 8F);
                c.TextAlignment = TextAlignment.MiddleCenter;
                c.WordWrap      = true;
                c.Padding       = new PaddingInfo(2, 2, 1, 1);
                c.Borders       = BorderSide.All;
                c.BorderWidth   = 0.5F;
                c.BorderColor   = Color.Black;
                cells[i]        = c;
            }
            row.HeightF = height;
            row.CanGrow = false;
            row.Cells.AddRange(cells);
            return cells;
        }

        /// <summary>Builds a data row with expression-bound cells.</summary>
        private XRTableCell[] BuildBoundRow(XRTableRow row, float height,
            params (string expressionOrStatic, string staticOverride, double weight)[] columns)
        {
            var cells = new XRTableCell[columns.Length];
            for (int i = 0; i < columns.Length; i++)
            {
                var c = new XRTableCell();
                c.Weight        = columns[i].weight;
                c.Font          = new DXFont("Times New Roman", 8F);
                c.TextAlignment = TextAlignment.MiddleCenter;
                c.WordWrap      = true;
                c.Padding       = new PaddingInfo(3, 2, 1, 1);
                c.Borders       = BorderSide.Left | BorderSide.Right | BorderSide.Bottom;
                c.BorderWidth   = 0.5F;
                c.BorderColor   = Color.Black;

                var expr = columns[i].expressionOrStatic;
                if (!string.IsNullOrEmpty(expr))
                {
                    // If it's a plain string literal (no brackets) treat as static text
                    if (!expr.Contains("[") && !expr.Contains("+"))
                        c.Text = expr;
                    else
                        c.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expr));
                }
                cells[i] = c;
            }
            row.HeightF = height;
            row.CanGrow = false;
            row.Cells.AddRange(cells);
            return cells;
        }

        // ── Field declarations ───────────────────────────────────────────────
        private XRLabel     xrLabelTitle;
        private XRTable     xrTablePersonal;
        private XRTableRow  xrRowName;
        private XRTableRow  xrRowNationality;
        private XRTableRow  xrRowPassportNo;
        private XRTableRow  xrRowPassportDate;
        private XRTableRow  xrRowBirth;
        private XRTableRow  xrRowVisaType;
        private XRTableRow  xrRowAddress;
        private XRTableRow  xrRowVisaPlace;
        private XRTableRow  xrRowVisaDates;
        private XRTableRow  xrRowDuration;
        private XRTableRow  xrRowCompany;
        private XRPictureBox xrPicturePhoto;
        private XRTable     xrTableReg;
        private XRTableRow  xrRowRegHeader;
        private XRTableRow  xrRowRegData;
        private XRTable     xrTableLoc;
        private XRTableRow  xrRowLocHeader;
        private XRTableRow  xrRowLocData;
        private XRLabel     xrLabelDeregLabel;
        private XRLabel     xrLabelDeregLine;
        private XRLabel     xrLabelNoteLabel;
        private XRLabel     xrLabelNoteValue;
        private XRLabel     xrLabelMilliLabel;
        private XRLabel     xrLabelMilliValue;
        private XRLabel     xrLabelPassportMLabel;
        private XRLabel     xrLabelPassportMValue;
        private XRLabel     xrLabelEsasLabel;
        private XRLabel     xrLabelEsasValue;
    }
}
