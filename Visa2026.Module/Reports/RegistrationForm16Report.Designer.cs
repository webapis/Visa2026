using System.Drawing;
using DevExpress.Drawing;
using DevExpress.Utils;
using DevExpress.XtraReports.UI;
using DevExpress.XtraPrinting;

namespace Visa2026.Module.Reports
{
    partial class RegistrationForm16Report
    {
        // ── Upper section ──────────────────────────────────────────────────────────
        private XRLabel      xrLabelTitle;
        private XRPictureBox xrPicturePhoto;
        private XRTable      xrTableMain;
        private XRLabel      xrLabelDolduran;
        private XRLabel      xrLabelTDMG;
        private XRLabel      xrLabelDate;
        private XRLabel      xrLabelBarlan;
        private XRLabel      xrLabelSignature;

        // ── Separator ──────────────────────────────────────────────────────────────
        private XRLine xrLineSeparator;

        // ── Lower section ──────────────────────────────────────────────────────────
        private XRTable xrTableRegistration;   // 6-column registration log
        private XRTable xrTableTravel;          // 3-column travel / address log
        private XRTable xrTableBottom;          // Hasapdan + Başga bellikler

        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();

            xrLabelTitle        = new XRLabel();
            xrPicturePhoto      = new XRPictureBox();
            xrTableMain         = new XRTable();
            xrLabelDolduran     = new XRLabel();
            xrLabelTDMG         = new XRLabel();
            xrLabelDate         = new XRLabel();
            xrLabelBarlan       = new XRLabel();
            xrLabelSignature    = new XRLabel();
            xrLineSeparator     = new XRLine();
            xrTableRegistration = new XRTable();
            xrTableTravel       = new XRTable();
            xrTableBottom       = new XRTable();

            ((System.ComponentModel.ISupportInitialize)(xrTableMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableRegistration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableTravel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableBottom)).BeginInit();

            // ── Page setup ──────────────────────────────────────────────────────────
            const float pageW      = 767F;   // usable width (827 - 30 left - 30 right)
            const float mainTableW = 612F;   // main table leaves room for photo
            const float rH         = 24F;    // standard row height
            const float bw         = 2F;     // main table border width
            const float bwLow      = 1.5F;   // lower table border width
            Color       bc         = Color.Black;

            this.Landscape  = false;
            this.PageWidth  = 827;
            this.PageHeight = 1169;
            this.Margins    = new DXMargins(30, 30, 40, 45);

            // ══════════════════════════════════════════════════════════════════════
            //  TITLE
            // ══════════════════════════════════════════════════════════════════════
            xrLabelTitle.Text          = "DAŞARY ÝURT RAÝATLARYNY BELLIGE ALYŞ NAMASY";
            xrLabelTitle.Font          = new DXFont("Times New Roman", 11F, DXFontStyle.Bold);
            xrLabelTitle.LocationFloat = new PointFloat(0, 12);
            xrLabelTitle.SizeF         = new SizeF(pageW, 36);
            xrLabelTitle.TextAlignment = TextAlignment.MiddleCenter;
            xrLabelTitle.Borders       = BorderSide.All;
            xrLabelTitle.BorderWidth   = bw;
            xrLabelTitle.BorderColor   = bc;

            // ══════════════════════════════════════════════════════════════════════
            //  PHOTO  (top-right, spans first ~8 rows of the main table)
            // ══════════════════════════════════════════════════════════════════════
            xrPicturePhoto.LocationFloat = new PointFloat(mainTableW + 7, 55);
            xrPicturePhoto.SizeF         = new SizeF(pageW - mainTableW - 7, 195);
            xrPicturePhoto.Sizing        = ImageSizeMode.ZoomImage;
            xrPicturePhoto.Borders       = BorderSide.All;
            xrPicturePhoto.BorderWidth   = bw;
            xrPicturePhoto.BorderColor   = bc;
            xrPicturePhoto.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Image", "[Person_Photo]"));

            // ══════════════════════════════════════════════════════════════════════
            //  MAIN TABLE  (fields 1–15 + sub-rows)
            // ══════════════════════════════════════════════════════════════════════
            xrTableMain.LocationFloat = new PointFloat(0, 55);
            xrTableMain.BorderWidth   = bw;
            xrTableMain.BorderColor   = bc;
            xrTableMain.Borders       = BorderSide.All;

            // ── Row 1: Full name ───────────────────────────────────────────────────
            MTwo(xrTableMain, mainTableW, rH, bw,
                "1. Familiýasy, ady, atasynyň ady:",
                "[Person_FullName]");

            // ── Row 2: Nationality / Date-of-birth ────────────────────────────────
            MFour(xrTableMain, mainTableW, rH, bw,
                "2. Raýatlygy:", "[Person_NationalityCode]",
                "3. Doglan senesi:", "[Person_DateOfBirthText]",
                w1: 148, w2: 116, w3: 158);

            // ── Row 3: Passport number / Issued date ──────────────────────────────
            MFour(xrTableMain, mainTableW, rH, bw,
                "4. Pasportynyň belgisi:", "[Passport_Number]",
                "5. Berleni:", "[Passport_IssueDateText]",
                w1: 148, w2: 116, w3: 158);

            // ── Row 4: Birth country/place ────────────────────────────────────────
            MTwo(xrTableMain, mainTableW, rH, bw,
                "5. Doglan ýeri, ýurdy:",
                "[Person_CountryOfBirthCode] / [Person_BirthPlace]");

            // ── Row 5: Gender / Home country ──────────────────────────────────────
            MFour(xrTableMain, mainTableW, rH, bw,
                "6. Jynsy:", "[Person_Gender]",
                "7. Öý salgysy:", "[Person_HomeCountryCode]",
                w1: 100, w2: 88, w3: 160);

            // ── Row 6: Foreign address (full-span italic) ─────────────────────────
            MSpan(xrTableMain, mainTableW, 42F, bw, "[Person_ForeignAddress]", italic: true);

            // ── Row 6a: Sub-label ─────────────────────────────────────────────────
            MSub(xrTableMain, mainTableW, 17F, bw,
                "(ýurt, şäher, köçe, jaý №, öý №)");

            // ── Row 7: Purpose of visit ───────────────────────────────────────────
            MTwo(xrTableMain, mainTableW, 32F, bw,
                "8. Gelmeginiň maksady:",
                "[Visa_PurposeTm]");

            // ── Row 8: Address in Turkmenistan ────────────────────────────────────
            MTwo(xrTableMain, mainTableW, 36F, bw,
                "9. Türkmenistanda bolýan ýeri:",
                "[Address_FullAddress]");

            // ── Row 8a: Sub-label ─────────────────────────────────────────────────
            MSub(xrTableMain, mainTableW, 17F, bw, "(doly salgysy)");

            // ── Row 9: Visa type / number ─────────────────────────────────────────
            MTwo(xrTableMain, mainTableW, rH, bw,
                "10. Wizanyň derejesi, görnüşi we No:",
                "[Visa_TypeFull]");

            // ── Row 10: Visa issued place ─────────────────────────────────────────
            MTwo(xrTableMain, mainTableW, rH, bw,
                "11. Wizanyň berlen ýeri (ýurdy):",
                "[Visa_IssuedPlaceTm]");

            // ── Row 11: Visa validity dates ───────────────────────────────────────
            MTwo(xrTableMain, mainTableW, rH, bw,
                "12. Wizanyň berlen senesi we möhleti:",
                "[Visa_StartDateText] — [Visa_ExpirationDateText]");

            // ── Row 12: Entry date / entry point ─────────────────────────────────
            MFour(xrTableMain, mainTableW, rH, bw,
                "13. Giren wagty:", "[Travel_DateText]",
                "14. Giren ýeri:", "[Travel_CheckPointTm]",
                w1: 112, w2: 92, w3: 152);

            // ── Row 13: Hosting organisation ─────────────────────────────────────
            MTwo(xrTableMain, mainTableW, 44F, bw,
                "15. Kabul edýän edara ýa-da şahsyýet:",
                "[Person_CompanyName]");

            // ── Row 13a: Sub-label ────────────────────────────────────────────────
            MSub(xrTableMain, mainTableW, 17F, bw,
                "(familiýasy, ady, doglan ýyly, öý salgysy)");

            // ── Row 13b: Company address (full-span italic) ───────────────────────
            MSpan(xrTableMain, mainTableW, 26F, bw, "[Person_CompanyAddress]", italic: true);

            // Measure total row heights and lock the table size
            float mainH = 0;
            foreach (XRTableRow r in xrTableMain.Rows) mainH += r.HeightF;
            xrTableMain.SizeF = new SizeF(mainTableW, mainH);

            float mainBottom = 55F + mainH;   // absolute Y of bottom edge of main table

            // ══════════════════════════════════════════════════════════════════════
            //  DOLDURAN EDARA line
            // ══════════════════════════════════════════════════════════════════════
            float dolY = mainBottom + 6F;

            xrLabelDolduran.Text          = "Dolduran edara";
            xrLabelDolduran.Font          = new DXFont("Times New Roman", 9.5F, DXFontStyle.Bold);
            xrLabelDolduran.LocationFloat = new PointFloat(0, dolY);
            xrLabelDolduran.SizeF         = new SizeF(140, 25);
            xrLabelDolduran.TextAlignment = TextAlignment.MiddleLeft;

            xrLabelTDMG.Text          = "TDMG";
            xrLabelTDMG.Font          = new DXFont("Times New Roman", 9.5F, DXFontStyle.Bold);
            xrLabelTDMG.LocationFloat = new PointFloat(145, dolY);
            xrLabelTDMG.SizeF         = new SizeF(110, 25);
            xrLabelTDMG.Borders       = BorderSide.Bottom;
            xrLabelTDMG.BorderWidth   = bw;
            xrLabelTDMG.BorderColor   = bc;
            xrLabelTDMG.TextAlignment = TextAlignment.MiddleCenter;

            xrLabelDate.Font          = new DXFont("Times New Roman", 9F);
            xrLabelDate.LocationFloat = new PointFloat(260, dolY);
            xrLabelDate.SizeF         = new SizeF(260, 25);
            xrLabelDate.TextAlignment = TextAlignment.MiddleLeft;
            xrLabelDate.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text",
                    "'wagty: ' + [Application_DateText]"));

            // ══════════════════════════════════════════════════════════════════════
            //  BARLAN GÖZEGCI line
            // ══════════════════════════════════════════════════════════════════════
            float barY = dolY + 28F;

            xrLabelBarlan.Text          = "Barlan gözegci";
            xrLabelBarlan.Font          = new DXFont("Times New Roman", 9.5F, DXFontStyle.Bold);
            xrLabelBarlan.LocationFloat = new PointFloat(0, barY);
            xrLabelBarlan.SizeF         = new SizeF(155, 25);
            xrLabelBarlan.TextAlignment = TextAlignment.MiddleLeft;

            // ══════════════════════════════════════════════════════════════════════
            //  SIGNATURE line  "(goly, familiýasy, ady)"
            // ══════════════════════════════════════════════════════════════════════
            float sigY = barY + 28F;

            xrLabelSignature.Text          = "(goly, familiýasy, ady)";
            xrLabelSignature.Font          = new DXFont("Times New Roman", 8.5F);
            xrLabelSignature.LocationFloat = new PointFloat(0, sigY);
            xrLabelSignature.SizeF         = new SizeF(pageW, 22);
            xrLabelSignature.Borders       = BorderSide.Top;
            xrLabelSignature.BorderWidth   = bw;
            xrLabelSignature.BorderColor   = bc;
            xrLabelSignature.TextAlignment = TextAlignment.MiddleCenter;

            float upperEnd = sigY + 22F;

            // ══════════════════════════════════════════════════════════════════════
            //  DASHED SEPARATOR
            // ══════════════════════════════════════════════════════════════════════
            float sepY = upperEnd + 8F;

            xrLineSeparator.LocationFloat = new PointFloat(0, sepY);
            xrLineSeparator.SizeF         = new SizeF(pageW, 2);
      xrLineSeparator.LineStyle = DXDashStyle.Dash;
      xrLineSeparator.LineWidth     = 1F;
            xrLineSeparator.ForeColor     = bc;

            // ══════════════════════════════════════════════════════════════════════
            //  REGISTRATION LOG TABLE  (6 columns)
            //
            //  Col widths must sum to pageW = 767
            //  175 + 82 + 118 + 98 + 152 + 142 = 767
            // ══════════════════════════════════════════════════════════════════════
            float regY = sepY + 10F;
            float[] rW = { 175F, 82F, 118F, 98F, 152F, 142F };

            xrTableRegistration.LocationFloat = new PointFloat(0, regY);
            xrTableRegistration.BorderWidth   = bwLow;
            xrTableRegistration.BorderColor   = bc;
            xrTableRegistration.Borders       = BorderSide.All;

            // Header row
            string[] regHdr = {
                "Hasaba alan, möhletini uzaldan TDMG-nyň edarasy",
                "Hasaba alyş, uzaldyş belgisi",
                "Hasaba alnan, uzaldylan wagty",
                "Möhleti",
                "Esas(belgisi we wagty)",
                "Jogapkär işgäriň familiýasy we goly"
            };
            AddTableRow(xrTableRegistration, rW, 50F, bwLow, regHdr, null, isHeader: true);

            // Data row (bound expressions)
            string[] regVal = {
                "[Registration_TDMG]",
                "",
                "[Registration_DateText]",
                "[Registration_ExpiryText]",
                "[Registration_BasisText]",
                ""
            };
            AddTableRow(xrTableRegistration, rW, 30F, bwLow, null, regVal, isHeader: false);

            // Two spare / empty rows
            AddTableRow(xrTableRegistration, rW, 30F, bwLow, null, null, isHeader: false);
            AddTableRow(xrTableRegistration, rW, 30F, bwLow, null, null, isHeader: false);

            float regTotalH = 0;
            foreach (XRTableRow r in xrTableRegistration.Rows) regTotalH += r.HeightF;
            xrTableRegistration.SizeF = new SizeF(pageW, regTotalH);   // 50+30+30+30 = 140

            // ══════════════════════════════════════════════════════════════════════
            //  TRAVEL / ADDRESS LOG TABLE  (3 columns)
            //
            //  Col widths: 256 + 254 + 257 = 767
            // ══════════════════════════════════════════════════════════════════════
            float trvY = regY + regTotalH;
            float[] tW = { 256F, 254F, 257F };

            xrTableTravel.LocationFloat = new PointFloat(0, trvY);
            xrTableTravel.BorderWidth   = bwLow;
            xrTableTravel.BorderColor   = bc;
            xrTableTravel.Borders       = BorderSide.All;

            string[] trvHdr = {
                "Türkmenistanyň çäginde bolýan ýeriniň salgysy",
                "Gelen, giden ýeri",
                "Kabul edýän edara ýa-da şahsyýet"
            };
            AddTableRow(xrTableTravel, tW, 38F, bwLow, trvHdr, null, isHeader: true);

            string[] trvVal = {
                "[Address_FullAddress]",
                "[Travel_CheckPointTm]",
                "[Person_CompanyName]"
            };
            AddTableRow(xrTableTravel, tW, 50F, bwLow, null, trvVal, isHeader: false);
            AddTableRow(xrTableTravel, tW, 30F, bwLow, null, null,   isHeader: false);

            float trvTotalH = 0;
            foreach (XRTableRow r in xrTableTravel.Rows) trvTotalH += r.HeightF;
            xrTableTravel.SizeF = new SizeF(pageW, trvTotalH);   // 38+50+30 = 118

            // ══════════════════════════════════════════════════════════════════════
            //  BOTTOM TABLE  —  Hasapdan + Başga bellikler + Passport + Milleti
            //
            //  4-col widths (used by last two rows): 195+155+205+212 = 767
            // ══════════════════════════════════════════════════════════════════════
            float botY = trvY + trvTotalH;
            float[] b4 = { 195F, 155F, 205F, pageW - 555F };   // 195+155+205+212=767

            xrTableBottom.LocationFloat = new PointFloat(0, botY);
            xrTableBottom.BorderWidth   = bwLow;
            xrTableBottom.BorderColor   = bc;
            xrTableBottom.Borders       = BorderSide.All;

            // "Hasapdan aýyrmak üçin esas"  — full-width single-cell row
            AddSpanRow(xrTableBottom, pageW, 25F, bwLow,
                "Hasapdan aýyrmak üçin esas", underline: true);

            // "Başga bellikler"  — full-width single-cell row
            AddSpanRow(xrTableBottom, pageW, 25F, bwLow,
                "Başga bellikler", underline: true);

            // Passport validity row
            var passRow = new XRTableRow { HeightF = 30F };
            passRow.Cells.Add(MkLblCell("Pasportynyň möhleti", b4[0], bwLow, underline: true));
            passRow.Cells.Add(MkExprCell("[Passport_IssueDateText]",  b4[1], bwLow, bold: true));
            passRow.Cells.Add(MkExprCell("[Passport_ExpiryDateText]", b4[2], bwLow, bold: true));
            passRow.Cells.Add(MkEmptyCell(b4[3], bwLow));
            xrTableBottom.Rows.Add(passRow);

            // Nationality row
            var natRow = new XRTableRow { HeightF = 31F };
            natRow.Cells.Add(MkLblCell("Milleti", b4[0], bwLow, underline: true));
            natRow.Cells.Add(MkExprCell("[Person_NationalityCode]", b4[1], bwLow, bold: true));
            natRow.Cells.Add(MkLblCell("Esas we ýazgyň wagty", b4[2], bwLow,
                align: TextAlignment.MiddleCenter));
            natRow.Cells.Add(MkEmptyCell(b4[3], bwLow));
            xrTableBottom.Rows.Add(natRow);

            float botTotalH = 0;
            foreach (XRTableRow r in xrTableBottom.Rows) botTotalH += r.HeightF;
            xrTableBottom.SizeF = new SizeF(pageW, botTotalH);   // 25+25+30+31 = 111

            // ══════════════════════════════════════════════════════════════════════
            //  ASSEMBLE DETAIL BAND
            // ══════════════════════════════════════════════════════════════════════
            Detail.HeightF = botY + botTotalH + 12F;

            Detail.Controls.AddRange(new XRControl[] {
                xrLabelTitle,
                xrPicturePhoto,
                xrTableMain,
                xrLabelDolduran, xrLabelTDMG, xrLabelDate,
                xrLabelBarlan,
                xrLabelSignature,
                xrLineSeparator,
                xrTableRegistration,
                xrTableTravel,
                xrTableBottom
            });

            ((System.ComponentModel.ISupportInitialize)(xrTableMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableRegistration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableTravel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  MAIN-TABLE ROW HELPERS
        // ══════════════════════════════════════════════════════════════════════════

        /// <summary>2-cell row: bold label | bold-italic value.</summary>
        private void MTwo(XRTable t, float tW, float h, float bw,
            string label, string expr)
        {
            const float lW = 240F;
            var row = new XRTableRow { HeightF = h };
            row.Cells.Add(MkLblCell(label, lW,         bw, leftPad: 6));
            row.Cells.Add(MkExprCell(expr, tW - lW,    bw, bold: true, italic: true));
            t.Rows.Add(row);
        }

        /// <summary>4-cell row with custom column widths.</summary>
        private void MFour(XRTable t, float tW, float h, float bw,
            string l1, string e1, string l2, string e2,
            float w1, float w2, float w3)
        {
            float w4 = tW - w1 - w2 - w3;
            var row = new XRTableRow { HeightF = h };
            row.Cells.Add(MkLblCell(l1, w1, bw, leftPad: 6));
            row.Cells.Add(MkExprCell(e1, w2, bw, bold: true, italic: true));
            row.Cells.Add(MkLblCell(l2, w3, bw, leftPad: 6));
            row.Cells.Add(MkExprCell(e2, w4, bw, bold: true, italic: true));
            t.Rows.Add(row);
        }

        /// <summary>Full-width single-cell row (bound expression).</summary>
        private void MSpan(XRTable t, float tW, float h, float bw,
            string expr, bool italic = false)
        {
            var row = new XRTableRow { HeightF = h };
            var c = new XRTableCell {
                WidthF        = tW,
                Font          = new DXFont("Times New Roman", 8.5F,
                                    DXFontStyle.Bold | (italic ? DXFontStyle.Italic : DXFontStyle.Regular)),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders       = BorderSide.All,
                BorderWidth   = bw,
                BorderColor   = Color.Black,
                WordWrap      = true,
                Padding       = new PaddingInfo(6, 4, 2, 2)
            };
            if (!string.IsNullOrEmpty(expr))
                c.ExpressionBindings.Add(
                    new ExpressionBinding("BeforePrint", "Text", expr));
            row.Cells.Add(c);
            t.Rows.Add(row);
        }

        /// <summary>Full-width sub-label row (small, centred, static text).</summary>
        private void MSub(XRTable t, float tW, float h, float bw, string text)
        {
            var row = new XRTableRow { HeightF = h };
            row.Cells.Add(new XRTableCell {
                Text          = text,
                WidthF        = tW,
                Font          = new DXFont("Times New Roman", 7.5F),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders       = BorderSide.All,
                BorderWidth   = bw,
                BorderColor   = Color.Black,
                Padding       = new PaddingInfo(4, 4, 1, 1)
            });
            t.Rows.Add(row);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  GENERIC TABLE-ROW BUILDER  (used for registration & travel tables)
        // ══════════════════════════════════════════════════════════════════════════

        private void AddTableRow(XRTable t, float[] widths, float h, float bw,
            string[] texts, string[] exprs, bool isHeader)
        {
            var row = new XRTableRow { HeightF = h };
            for (int i = 0; i < widths.Length; i++)
            {
                var c = new XRTableCell {
                    WidthF        = widths[i],
                    Font          = isHeader
                        ? new DXFont("Times New Roman", 7.5F, DXFontStyle.Underline)
                        : new DXFont("Times New Roman", 8.5F,
                              DXFontStyle.Bold | DXFontStyle.Italic),
                    TextAlignment = TextAlignment.MiddleCenter,
                    Borders       = BorderSide.All,
                    BorderWidth   = bw,
                    BorderColor   = Color.Black,
                    WordWrap      = true,
                    Padding       = new PaddingInfo(3, 3, 2, 2)
                };
                if (texts != null && i < texts.Length && texts[i] != null)
                    c.Text = texts[i];
                if (exprs != null && i < exprs.Length && !string.IsNullOrEmpty(exprs[i]))
                    c.ExpressionBindings.Add(
                        new ExpressionBinding("BeforePrint", "Text", exprs[i]));
                row.Cells.Add(c);
            }
            t.Rows.Add(row);
        }

        /// <summary>Full-width single-cell row for the bottom table headers.</summary>
        private void AddSpanRow(XRTable t, float tW, float h, float bw,
            string text, bool underline = false)
        {
            var row = new XRTableRow { HeightF = h };
            var style = underline ? DXFontStyle.Underline : DXFontStyle.Regular;
            row.Cells.Add(new XRTableCell {
                Text          = text,
                WidthF        = tW,
                Font          = new DXFont("Times New Roman", 8.5F, style),
                TextAlignment = TextAlignment.MiddleLeft,
                Borders       = BorderSide.All,
                BorderWidth   = bw,
                BorderColor   = Color.Black,
                Padding       = new PaddingInfo(6, 4, 2, 2)
            });
            t.Rows.Add(row);
        }

        // ══════════════════════════════════════════════════════════════════════════
        //  CELL FACTORIES
        // ══════════════════════════════════════════════════════════════════════════

        private XRTableCell MkLblCell(string text, float width, float bw,
            bool underline   = false,
            int  leftPad     = 3,
            TextAlignment align = TextAlignment.MiddleLeft)
        {
            var style = DXFontStyle.Bold;
            if (underline) style |= DXFontStyle.Underline;
            return new XRTableCell {
                Text          = text,
                WidthF        = width,
                Font          = new DXFont("Times New Roman", 8.5F, style),
                TextAlignment = align,
                Borders       = BorderSide.All,
                BorderWidth   = bw,
                BorderColor   = Color.Black,
                WordWrap      = true,
                Padding       = new PaddingInfo(leftPad, 3, 2, 2)
            };
        }

        private XRTableCell MkExprCell(string expr, float width, float bw,
            bool bold   = false,
            bool italic = false)
        {
            var style = DXFontStyle.Regular;
            if (bold)   style |= DXFontStyle.Bold;
            if (italic) style |= DXFontStyle.Italic;

            var c = new XRTableCell {
                WidthF        = width,
                Font          = new DXFont("Times New Roman", 8.5F, style),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders       = BorderSide.All,
                BorderWidth   = bw,
                BorderColor   = Color.Black,
                WordWrap      = true,
                Padding       = new PaddingInfo(4, 4, 2, 2)
            };
            if (!string.IsNullOrEmpty(expr))
                c.ExpressionBindings.Add(
                    new ExpressionBinding("BeforePrint", "Text", expr));
            return c;
        }

        private XRTableCell MkEmptyCell(float width, float bw)
            => new XRTableCell {
                WidthF      = width,
                Borders     = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black
            };

        // ══════════════════════════════════════════════════════════════════════════
        //  LEGACY COMPATIBILITY WRAPPERS  (unchanged callers compile without error)
        // ══════════════════════════════════════════════════════════════════════════

        private void AddTwoCellRow(XRTable table, float height, string label, string expr)
        {
            MTwo(table, 612F, height, 2F, label, expr);
        }

        private void AddFourCellRow(XRTable table, float height,
            string l1, string e1, string l2, string e2)
        {
            MFour(table, 612F, height, 2F, l1, e1, l2, e2, w1: 162, w2: 112, w3: 155);
        }

        private XRTableCell CreateLabelCell(string text, float width)
            => MkLblCell(text, width, bw: 2F, leftPad: 6);

        private XRTableCell CreateValueCell(string expr, float width)
            => MkExprCell(expr, width, bw: 2F, bold: true, italic: true);
    }
}
