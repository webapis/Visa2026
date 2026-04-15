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

            // Personal info — top block (4 rows, mixed 2/4-column, 560F wide)
            this.xrTablePersonalTop     = new XRTable();
            this.xrRowName              = new XRTableRow();   // row 1: full-width name
            this.xrRowNatDob            = new XRTableRow();   // row 2: nationality | birth date  (4-col)
            this.xrRowPassIssue         = new XRTableRow();   // row 3: passport no | issue date  (4-col)
            this.xrRowBirthPlace        = new XRTableRow();   // row 4: birth country/place (full-width)

            // Personal info — bottom block (6 rows, full page width 786.7717F)
            this.xrTablePersonalBot     = new XRTable();
            this.xrRowAddress           = new XRTableRow();   // row 5
            this.xrRowVisaType          = new XRTableRow();   // row 6
            this.xrRowVisaPlace         = new XRTableRow();   // row 7
            this.xrRowVisaDates         = new XRTableRow();   // row 8
            this.xrRowDuration          = new XRTableRow();   // row 9
            this.xrRowCompany           = new XRTableRow();   // row 10

            this.xrPicturePhoto         = new XRPictureBox();

            // Registration table
            this.xrTableReg             = new XRTable();
            this.xrRowRegHeader         = new XRTableRow();
            this.xrRowRegData           = new XRTableRow();

            // Location table
            this.xrTableLoc             = new XRTable();
            this.xrRowLocHeader         = new XRTableRow();
            this.xrRowLocData           = new XRTableRow();

            // Bottom section labels
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

            ((System.ComponentModel.ISupportInitialize)(this.xrTablePersonalTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTablePersonalBot)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableReg)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableLoc)).BeginInit();

            // ── Page — A4 Portrait (override base Landscape) ─────────────────
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
            // LAYOUT CONSTANTS
            // ================================================================
            float pageW    = 786.7717F;  // full content width
            float leftW    = 560F;       // personal info top table width
            float photoX   = 565F;       // photo starts here (5F gap after left table)
            float photoW   = pageW - photoX;  // 221.7717F

            float rowH     = 27F;
            float nameRowH = 35F;        // row 1 (name) is taller

            // Photo spans exactly rows 1-4: nameRowH + 3 × rowH = 116F
            float topH = nameRowH + rowH * 3F;   // 116F
            float titleH = 26F;

            // Bottom table: 6 rows
            float addrRowH    = 35F;
            float companyRowH = 40F;
            float botH = addrRowH + rowH * 4F + companyRowH; // 35+108+40 = 183F

            float botY  = titleH + topH;          // 26+116 = 142F
            float regY  = botY + botH + 8F;       // 142+183+8 = 333F
            float locY  = regY + 55F + 35F + 8F;  // 333+90+8  = 431F
            float bsY   = locY + 55F + 35F + 12F; // 431+90+12 = 533F

            // Bottom-section label height and half-widths
            float lblH  = 22F;
            float halfW = pageW / 2F;   // 393.38585F

            // ================================================================
            // TITLE
            // ================================================================
            this.xrLabelTitle.Text          = "DA\u015EARY \u00DDURT RA\u00DDATLARYNY BELLIGE ALY\u015E NAMASY";
            this.xrLabelTitle.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabelTitle.SizeF         = new SizeF(pageW, titleH);
            this.xrLabelTitle.TextAlignment = TextAlignment.MiddleCenter;
            this.xrLabelTitle.Name          = "xrLabelTitle";
            this.xrLabelTitle.Borders       = BorderSide.All;
            this.xrLabelTitle.BorderWidth   = 0.5F;
            this.xrLabelTitle.BorderColor   = Color.Black;

            // ================================================================
            // PERSONAL INFO — TOP TABLE  (560F, 4 rows)
            //
            // Column weights (total = 560):
            //   2-cell rows  → label:190 | value:370
            //   4-cell rows  → label1:190 | value1:155 | label2:105 | value2:110
            //
            // Row 1 — Name (2-cell, tall)
            // Row 2 — Raýatalygy | Doglan senesi  (4-cell)
            // Row 3 — Pasportunyň belgisi | Berleni  (4-cell)
            // Row 4 — Doglan ýeri, ýurdy  (2-cell)
            // ================================================================

            // Row 1 — Name
            PersonalRow2(this.xrRowName, nameRowH, isFirst: true,
                label: "1. Famili\u00FDasy, ady, atasyny\u0148 ady:",
                expr:  "[Person_LastName] + ' ' + [Person_FirstName] + ' ' + [Person_MiddleName]",
                lw: 190, vw: 370);

            // Row 2 — Nationality + Birth date (4-column)
            PersonalRow4(this.xrRowNatDob, rowH,
                l1: "2. Ra\u00FDatalygy:",
                e1: "[Person_NationalityCode] + ' ' + [Person_NationalityTm]",
                l2: "3. Doglan senesi:",
                e2: "[Person_DateOfBirthText]",
                w1: 190, w2: 155, w3: 105, w4: 110);

            // Row 3 — Passport number + Issue date (4-column)
            PersonalRow4(this.xrRowPassIssue, rowH,
                l1: "4. Pasportuny\u0148 belgisi:",
                e1: "[Passport_Number]",
                l2: "5. Berleni:",
                e2: "[Passport_IssueDateText]",
                w1: 190, w2: 155, w3: 105, w4: 110);

            // Row 4 — Birth country/place (2-cell, full-width of left section)
            PersonalRow2(this.xrRowBirthPlace, rowH, isFirst: false,
                label: "6. Doglan \u00FDeri, \u00FDurdy:",
                expr:  "[Person_CountryOfBirthCode] + ' ' + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]",
                lw: 190, vw: 370);

            this.xrTablePersonalTop.LocationFloat = new DevExpress.Utils.PointFloat(0F, titleH);
            this.xrTablePersonalTop.Name          = "xrTablePersonalTop";
            this.xrTablePersonalTop.SizeF         = new SizeF(leftW, topH);
            this.xrTablePersonalTop.Rows.AddRange(new XRTableRow[] {
                this.xrRowName, this.xrRowNatDob, this.xrRowPassIssue, this.xrRowBirthPlace
            });

            // ================================================================
            // PHOTO — right of top table, spans rows 1-4 only
            // ================================================================
            this.xrPicturePhoto.LocationFloat = new DevExpress.Utils.PointFloat(photoX, titleH);
            this.xrPicturePhoto.Name          = "xrPicturePhoto";
            this.xrPicturePhoto.SizeF         = new SizeF(photoW, topH);
            this.xrPicturePhoto.Sizing        = ImageSizeMode.Squeeze;
            this.xrPicturePhoto.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Image", "[Person_Photo]"));
            this.xrPicturePhoto.Borders     = BorderSide.All;
            this.xrPicturePhoto.BorderWidth = 0.5F;
            this.xrPicturePhoto.BorderColor = Color.Black;

            // ================================================================
            // PERSONAL INFO — BOTTOM TABLE  (786.7717F, 6 rows, full width)
            //
            // Columns: label:235 | value:551.7717
            //
            // Row 5  — Türkmenistandaky bolýan ýeri  (tall, word-wrap)
            // Row 6  — Emek ugrundaky wizanyň görnüşi
            // Row 7  — Wiza berlen ýeri we belgisi
            // Row 8  — Wizanyň berleni we möhleti
            // Row 9  — Türkmenistandaky galynmaly günleri
            // Row 10 — Kabul edilen elara ýa-da şaýhýýet  (tall)
            // ================================================================
            float lw2 = 235F;
            float vw2 = pageW - lw2;   // 551.7717F

            BotRow(this.xrRowAddress,  rowNum: 7,  height: addrRowH,    isFirst: true,
                label: "7. T\u00FCrkmenistandaky bol\u00FDan \u00FDeri:",
                expr:  "[Address_FullAddress]",
                lw: lw2, vw: vw2);

            BotRow(this.xrRowVisaType, rowNum: 8,  height: rowH, isFirst: false,
                label: "8. Emek ugrundaky wizany\u0148 g\u00F6rn\u00FC\u015Fi:",
                expr:  "[Visa_TypeTm] + ' ' + [Visa_CategoryTm]",
                lw: lw2, vw: vw2);

            BotRow(this.xrRowVisaPlace, rowNum: 9, height: rowH, isFirst: false,
                label: "9. Wiza berlen \u00FDeri we belgisi:",
                expr:  "[Visa_IssuedPlaceTm] + ' \u2116' + [Visa_Number]",
                lw: lw2, vw: vw2);

            BotRow(this.xrRowVisaDates, rowNum: 10, height: rowH, isFirst: false,
                label: "10. Wizany\u0148 berleni we m\u00F6hleti:",
                expr:  "[Visa_StartDateText] + ' \u2014 ' + [Visa_ExpirationDateText]",
                lw: lw2, vw: vw2);

            BotRow(this.xrRowDuration, rowNum: 11, height: rowH, isFirst: false,
                label: "11. T\u00FCrkmenistandaky galynmaly g\u00FCnleri:",
                expr:  "",
                lw: lw2, vw: vw2);

            BotRow(this.xrRowCompany,  rowNum: 12, height: companyRowH, isFirst: false,
                label: "12. Kabul edilen elara \u00FDa-da \u015Fa\u00FDh\u00FD\u00FDet:",
                expr:  "[Person_CompanyName]",
                lw: lw2, vw: vw2);

            this.xrTablePersonalBot.LocationFloat = new DevExpress.Utils.PointFloat(0F, botY);
            this.xrTablePersonalBot.Name          = "xrTablePersonalBot";
            this.xrTablePersonalBot.SizeF         = new SizeF(pageW, botH);
            this.xrTablePersonalBot.Rows.AddRange(new XRTableRow[] {
                this.xrRowAddress, this.xrRowVisaType, this.xrRowVisaPlace,
                this.xrRowVisaDates, this.xrRowDuration, this.xrRowCompany
            });

            // ================================================================
            // REGISTRATION TABLE — full width
            // Columns: TDMG|Hasaba alyş|Hasaba alnan wagty|Möhleti|Esas|Jogapkär
            // Weights: 140|130|120|100|145|151.7717 = 786.7717
            // ================================================================
            BuildRow(this.xrRowRegHeader, 55F, isHeader: true,
                ("Hasapa alan, m\u00F6hletini uzaldan TDMG-ny\u0148 belgisi we m\u00F6hleti", 140),
                ("Hasaba aly\u015F belgisi we m\u00F6hleti", 130),
                ("Hasaba alnan wagty", 120),
                ("M\u00F6hleti", 100),
                ("Esas belgisi we wagty", 145),
                ("Jogapk\u00E4r ugurlar famili\u00FDasy we ady", 151.7717));

            BuildBoundRow(this.xrRowRegData, 35F,
                ("TDMGAS", null, 140),
                ("[Visa_StartDateText]", null, 130),
                ("[Visa_ExpirationDateText]", null, 120),
                ("[Application_RegistrationDateText] + ' ' + [Application_FullNumber]", null, 100),
                ("", null, 145),
                ("[CompanyHead_FullName]", null, 151.7717));

            this.xrTableReg.LocationFloat = new DevExpress.Utils.PointFloat(0F, regY);
            this.xrTableReg.Name          = "xrTableReg";
            this.xrTableReg.SizeF         = new SizeF(pageW, 55F + 35F);
            this.xrTableReg.Rows.AddRange(new XRTableRow[] { this.xrRowRegHeader, this.xrRowRegData });

            // ================================================================
            // LOCATION TABLE — full width
            // Columns: TmSalgysy|Gelen-giden|Kabul edilen|Jogapkär
            // Weights: 220|130|220|216.7717 = 786.7717
            // ================================================================
            BuildRow(this.xrRowLocHeader, 55F, isHeader: true,
                ("T\u00FCrkmenistandaky \u00E7\u00E4gende bol\u00FDan \u00FDeri", 220),
                ("Gelen, giden \u00FDeri", 130),
                ("Kabul edilen elara \u00FDa-da \u015Fa\u00FDlary\u0148 ady we T\u00FCrk k\u00E4rhanasyny\u0148 T\u00FCrkmenistandaky \u015Faham\u00E7asy\u0148 ady", 220),
                ("Jogapk\u00E4r ugurlar famili\u00FDasy we ady", 216.7717));

            BuildBoundRow(this.xrRowLocData, 35F,
                ("[Address_FullAddress]", null, 220),
                ("[Travel_CheckPointTm]", null, 130),
                ("[Person_CompanyName]", null, 220),
                ("[CompanyHead_FullName]", null, 216.7717));

            this.xrTableLoc.LocationFloat = new DevExpress.Utils.PointFloat(0F, locY);
            this.xrTableLoc.Name          = "xrTableLoc";
            this.xrTableLoc.SizeF         = new SizeF(pageW, 55F + 35F);
            this.xrTableLoc.Rows.AddRange(new XRTableRow[] { this.xrRowLocHeader, this.xrRowLocData });

            // ================================================================
            // BOTTOM SECTION — deregistration, notes, passport validity, basis
            // ================================================================

            // Hasapdan aýyrmak üçin esas:
            this.xrLabelDeregLabel.Text          = "Hasapdan a\u00FDyrmak \u00FC\u00E7in esas:";
            this.xrLabelDeregLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelDeregLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, bsY);
            this.xrLabelDeregLabel.SizeF         = new SizeF(220F, lblH);
            this.xrLabelDeregLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelDeregLabel.Name          = "xrLabelDeregLabel";

            this.xrLabelDeregLine.Text           = "";
            this.xrLabelDeregLine.LocationFloat  = new DevExpress.Utils.PointFloat(220F, bsY);
            this.xrLabelDeregLine.SizeF          = new SizeF(pageW - 220F, lblH);
            this.xrLabelDeregLine.Borders        = BorderSide.Bottom;
            this.xrLabelDeregLine.BorderWidth    = 0.5F;
            this.xrLabelDeregLine.BorderColor    = Color.Black;
            this.xrLabelDeregLine.Name           = "xrLabelDeregLine";

            // Başga bellikler:
            float noteY = bsY + lblH + 5F;
            this.xrLabelNoteLabel.Text          = "Ba\u015Fga bellikler:";
            this.xrLabelNoteLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelNoteLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, noteY);
            this.xrLabelNoteLabel.SizeF         = new SizeF(120F, lblH * 2);
            this.xrLabelNoteLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelNoteLabel.Name          = "xrLabelNoteLabel";

            this.xrLabelNoteValue.Font          = new DXFont("Times New Roman", 8F);
            this.xrLabelNoteValue.LocationFloat = new DevExpress.Utils.PointFloat(120F, noteY);
            this.xrLabelNoteValue.SizeF         = new SizeF(pageW - 120F, lblH * 2);
            this.xrLabelNoteValue.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelNoteValue.WordWrap      = true;
            this.xrLabelNoteValue.Borders       = BorderSide.Bottom;
            this.xrLabelNoteValue.BorderWidth   = 0.5F;
            this.xrLabelNoteValue.BorderColor   = Color.Black;
            this.xrLabelNoteValue.Name          = "xrLabelNoteValue";
            // Note field intentionally left empty — no data binding

            // Pasportynyň möhleti: (left half) | Milleti: (right half)
            float passMY = noteY + lblH * 2 + 5F;

            this.xrLabelPassportMLabel.Text          = "Pasportyny\u0148 m\u00F6hleti:";
            this.xrLabelPassportMLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelPassportMLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, passMY);
            this.xrLabelPassportMLabel.SizeF         = new SizeF(140F, lblH);
            this.xrLabelPassportMLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelPassportMLabel.Name          = "xrLabelPassportMLabel";

            this.xrLabelPassportMValue.Font          = new DXFont("Times New Roman", 8F);
            this.xrLabelPassportMValue.LocationFloat = new DevExpress.Utils.PointFloat(140F, passMY);
            this.xrLabelPassportMValue.SizeF         = new SizeF(halfW - 140F, lblH);
            this.xrLabelPassportMValue.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelPassportMValue.Borders       = BorderSide.Bottom;
            this.xrLabelPassportMValue.BorderWidth   = 0.5F;
            this.xrLabelPassportMValue.BorderColor   = Color.Black;
            this.xrLabelPassportMValue.Name          = "xrLabelPassportMValue";
            this.xrLabelPassportMValue.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text",
                    "[Passport_IssueDateText] + ' \u2014 ' + [Passport_ExpirationDateText]"));

            this.xrLabelMilliLabel.Text          = "Milleti:";
            this.xrLabelMilliLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelMilliLabel.LocationFloat = new DevExpress.Utils.PointFloat(halfW, passMY);
            this.xrLabelMilliLabel.SizeF         = new SizeF(60F, lblH);
            this.xrLabelMilliLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelMilliLabel.Name          = "xrLabelMilliLabel";

            this.xrLabelMilliValue.Font          = new DXFont("Times New Roman", 8F);
            this.xrLabelMilliValue.LocationFloat = new DevExpress.Utils.PointFloat(halfW + 60F, passMY);
            this.xrLabelMilliValue.SizeF         = new SizeF(halfW - 60F + 0.38585F, lblH);
            this.xrLabelMilliValue.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelMilliValue.Borders       = BorderSide.Bottom;
            this.xrLabelMilliValue.BorderWidth   = 0.5F;
            this.xrLabelMilliValue.BorderColor   = Color.Black;
            this.xrLabelMilliValue.Name          = "xrLabelMilliValue";
            this.xrLabelMilliValue.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Person_NationalityCode]"));

            // Esas we ýazylyşy wagty:
            float esasY = passMY + lblH + 5F;

            this.xrLabelEsasLabel.Text          = "Esas we \u00FDazylý\u015Fy wagty:";
            this.xrLabelEsasLabel.Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold);
            this.xrLabelEsasLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, esasY);
            this.xrLabelEsasLabel.SizeF         = new SizeF(180F, lblH);
            this.xrLabelEsasLabel.TextAlignment = TextAlignment.MiddleLeft;
            this.xrLabelEsasLabel.Name          = "xrLabelEsasLabel";

            this.xrLabelEsasValue.Font          = new DXFont("Times New Roman", 8F);
            this.xrLabelEsasValue.LocationFloat = new DevExpress.Utils.PointFloat(180F, esasY);
            this.xrLabelEsasValue.SizeF         = new SizeF(pageW - 180F, lblH);
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
                this.xrTablePersonalTop,
                this.xrPicturePhoto,
                this.xrTablePersonalBot,
                this.xrTableReg,
                this.xrTableLoc,
                this.xrLabelDeregLabel,    this.xrLabelDeregLine,
                this.xrLabelNoteLabel,     this.xrLabelNoteValue,
                this.xrLabelPassportMLabel, this.xrLabelPassportMValue,
                this.xrLabelMilliLabel,    this.xrLabelMilliValue,
                this.xrLabelEsasLabel,     this.xrLabelEsasValue
            });

            ((System.ComponentModel.ISupportInitialize)(this.xrTablePersonalTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTablePersonalBot)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableReg)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableLoc)).EndInit();
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// 2-cell personal info row: label | value (full-width of its table).
        /// Used for rows 1 and 4 of the top table, and all rows of the bottom table.
        /// </summary>
        private void PersonalRow2(XRTableRow row, float height, bool isFirst,
            string label, string expr, float lw, float vw)
        {
            var lc = MakeLabelCell(label, lw, addLeft: true, addTop: isFirst);
            var vc = MakeValueCell(expr,  vw, addTop: isFirst);
            row.HeightF = height;
            row.Cells.AddRange(new XRTableCell[] { lc, vc });
        }

        /// <summary>
        /// 4-cell row: label1 | value1 | label2 | value2.
        /// Used for rows 2-3 of the top table (paired fields).
        /// </summary>
        private void PersonalRow4(XRTableRow row, float height,
            string l1, string e1, string l2, string e2,
            float w1, float w2, float w3, float w4)
        {
            var c1 = MakeLabelCell(l1, w1, addLeft: true,  addTop: false);
            var c2 = MakeValueCell(e1, w2, addTop: false);
            var c3 = MakeLabelCell(l2, w3, addLeft: false, addTop: false);  // no Left — c2.Right covers it
            var c4 = MakeValueCell(e2, w4, addTop: false);
            row.HeightF = height;
            row.Cells.AddRange(new XRTableCell[] { c1, c2, c3, c4 });
        }

        /// <summary>
        /// 2-cell row for the bottom personal table (full page width).
        /// Identical structure to PersonalRow2 — kept separate for clarity.
        /// </summary>
        private void BotRow(XRTableRow row, int rowNum, float height, bool isFirst,
            string label, string expr, float lw, float vw)
        {
            var lc = MakeLabelCell(label, lw, addLeft: true, addTop: isFirst);
            var vc = MakeValueCell(expr,  vw, addTop: isFirst);
            row.HeightF = height;
            row.Cells.AddRange(new XRTableCell[] { lc, vc });
        }

        private XRTableCell MakeLabelCell(string text, float weight, bool addLeft, bool addTop)
        {
            var c = new XRTableCell();
            c.Text          = text;
            c.Weight        = weight;
            c.Font          = new DXFont("Times New Roman", 7F, DXFontStyle.Bold);
            c.TextAlignment = TextAlignment.MiddleLeft;
            c.WordWrap      = true;
            c.Padding       = new PaddingInfo(3, 2, 1, 1);
            c.Borders       = BorderSide.Right | BorderSide.Bottom;
            c.BorderWidth   = 0.5F;
            c.BorderColor   = Color.Black;
            if (addLeft) c.Borders |= BorderSide.Left;
            if (addTop)  c.Borders |= BorderSide.Top;
            return c;
        }

        private XRTableCell MakeValueCell(string expr, float weight, bool addTop)
        {
            var c = new XRTableCell();
            c.Weight        = weight;
            c.Font          = new DXFont("Times New Roman", 8F);
            c.TextAlignment = TextAlignment.MiddleLeft;
            c.WordWrap      = true;
            c.Padding       = new PaddingInfo(3, 2, 1, 1);
            c.Borders       = BorderSide.Right | BorderSide.Bottom;
            c.BorderWidth   = 0.5F;
            c.BorderColor   = Color.Black;
            if (addTop) c.Borders |= BorderSide.Top;
            if (!string.IsNullOrEmpty(expr))
                c.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expr));
            return c;
        }

        /// <summary>Builds a header or plain-text row for the reg/loc tables.</summary>
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

        /// <summary>Builds an expression-bound data row for the reg/loc tables.</summary>
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
        private XRLabel      xrLabelTitle;
        private XRTable      xrTablePersonalTop;
        private XRTableRow   xrRowName;
        private XRTableRow   xrRowNatDob;
        private XRTableRow   xrRowPassIssue;
        private XRTableRow   xrRowBirthPlace;
        private XRTable      xrTablePersonalBot;
        private XRTableRow   xrRowAddress;
        private XRTableRow   xrRowVisaType;
        private XRTableRow   xrRowVisaPlace;
        private XRTableRow   xrRowVisaDates;
        private XRTableRow   xrRowDuration;
        private XRTableRow   xrRowCompany;
        private XRPictureBox xrPicturePhoto;
        private XRTable      xrTableReg;
        private XRTableRow   xrRowRegHeader;
        private XRTableRow   xrRowRegData;
        private XRTable      xrTableLoc;
        private XRTableRow   xrRowLocHeader;
        private XRTableRow   xrRowLocData;
        private XRLabel      xrLabelDeregLabel;
        private XRLabel      xrLabelDeregLine;
        private XRLabel      xrLabelNoteLabel;
        private XRLabel      xrLabelNoteValue;
        private XRLabel      xrLabelPassportMLabel;
        private XRLabel      xrLabelPassportMValue;
        private XRLabel      xrLabelMilliLabel;
        private XRLabel      xrLabelMilliValue;
        private XRLabel      xrLabelEsasLabel;
        private XRLabel      xrLabelEsasValue;
    }
}
