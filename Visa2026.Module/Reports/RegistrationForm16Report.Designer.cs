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
        private XRTable xrTableRegistration;
        private XRTable xrTableTravel;
        private XRTable xrTableBottom;

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
            const float pageW      = 767F;
            const float mainTableW = 612F;
            const float rH         = 24F;
            const float bw         = 2F;
            const float bwLow      = 1.5F;
            Color       bc         = Color.Black;

            this.Landscape  = false;
            this.PageWidth  = 827;
            this.PageHeight = 1169;
            this.Margins    = new DXMargins(30, 30, 40, 45);

            // ══════════════════════════════════════════════════════════════════════
            //  TITLE
            // ══════════════════════════════════════════════════════════════════════
            xrLabelTitle.Text          = "DAŞARY ÝURT RAÝATLARYNY BELLIGE ALYŞ NAMASY";
            xrLabelTitle.Font          = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            xrLabelTitle.LocationFloat = new PointFloat(0, 12);
            xrLabelTitle.SizeF         = new SizeF(pageW, 36);
            xrLabelTitle.TextAlignment = TextAlignment.MiddleCenter;
            xrLabelTitle.Borders       = BorderSide.All;
            xrLabelTitle.BorderWidth   = bw;
            xrLabelTitle.BorderColor   = bc;

            // ══════════════════════════════════════════════════════════════════════
            //  PHOTO
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
            //  MAIN TABLE
            // ══════════════════════════════════════════════════════════════════════
            xrTableMain.LocationFloat = new PointFloat(0, 55);
            xrTableMain.BorderWidth   = bw;
            xrTableMain.BorderColor   = bc;
            xrTableMain.Borders       = BorderSide.All;

            BuildMainTable(xrTableMain, mainTableW, bw, rH);

            float mainH = 0;
            foreach (XRTableRow r in xrTableMain.Rows) mainH += r.HeightF;
            xrTableMain.SizeF = new SizeF(mainTableW, mainH);

            float mainBottom = 55F + mainH;

            // ══════════════════════════════════════════════════════════════════════
            //  DOLDURAN EDARA SECTION
            // ══════════════════════════════════════════════════════════════════════
            float dolY = mainBottom + 8F;

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
            xrLabelDate.SizeF         = new SizeF(300, 25);
            xrLabelDate.TextAlignment = TextAlignment.MiddleLeft;
            xrLabelDate.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "'wagty: ' + [Application_DateText]"));

            // ══════════════════════════════════════════════════════════════════════
            //  BARLAN GÖZEGÇI & SIGNATURE
            // ══════════════════════════════════════════════════════════════════════
            float barY = dolY + 32F;

            xrLabelBarlan.Text          = "Barlan gözegçi";
            xrLabelBarlan.Font          = new DXFont("Times New Roman", 9.5F, DXFontStyle.Bold);
            xrLabelBarlan.LocationFloat = new PointFloat(0, barY);
            xrLabelBarlan.SizeF         = new SizeF(155, 25);
            xrLabelBarlan.TextAlignment = TextAlignment.MiddleLeft;

            float sigY = barY + 32F;

            xrLabelSignature.Text          = "(goly, familiýasy, ady)";
            xrLabelSignature.Font          = new DXFont("Times New Roman", 8.5F);
            xrLabelSignature.LocationFloat = new PointFloat(0, sigY);
            xrLabelSignature.SizeF         = new SizeF(pageW, 22);
            xrLabelSignature.Borders       = BorderSide.Top;
            xrLabelSignature.BorderWidth   = bw;
            xrLabelSignature.BorderColor   = bc;
            xrLabelSignature.TextAlignment = TextAlignment.MiddleCenter;

            // ══════════════════════════════════════════════════════════════════════
            //  DASHED SEPARATOR - FIXED
            // ══════════════════════════════════════════════════════════════════════
            xrLineSeparator.LocationFloat = new PointFloat(0, sigY + 28F);
            xrLineSeparator.SizeF         = new SizeF(pageW, 2);
            xrLineSeparator.LineStyle     = DXDashStyle.Dash;   // ← This fixes the CS0266 error
            xrLineSeparator.LineWidth     = 1F;
            xrLineSeparator.ForeColor     = bc;

            float currentY = sigY + 65F;

            // Registration Log Table
            BuildRegistrationTable(currentY);
            currentY += 145F;

            // Travel / Address Log Table
            BuildTravelTable(currentY);
            currentY += 125F;

            // Bottom Table
            BuildBottomTable(currentY);

            // ══════════════════════════════════════════════════════════════════════
            //  ADD CONTROLS TO DETAIL BAND
            // ══════════════════════════════════════════════════════════════════════
            Detail.Controls.AddRange(new XRControl[] {
                xrLabelTitle,
                xrPicturePhoto,
                xrTableMain,
                xrLabelDolduran,
                xrLabelTDMG,
                xrLabelDate,
                xrLabelBarlan,
                xrLabelSignature,
                xrLineSeparator,
                xrTableRegistration,
                xrTableTravel,
                xrTableBottom
            });

            Detail.HeightF = currentY + 130F;

            ((System.ComponentModel.ISupportInitialize)(xrTableMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableRegistration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableTravel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        // ===================================================================
        //  MAIN TABLE BUILDER
        // ===================================================================
        private void BuildMainTable(XRTable table, float tW, float bw, float rH)
        {
            MTwo(table, tW, rH, bw, "1. Familiýasy, ady, atasynyň ady:", "[Person_FullName]");
            MFour(table, tW, rH, bw, "2. Raýatlygy:", "[Person_NationalityCode]", "3. Doglan senesi:", "[Person_DateOfBirthText]");
            MFour(table, tW, rH, bw, "4. Pasportynyň belgisi:", "[Passport_Number]", "5. Berleni:", "[Passport_IssueDateText]");
            MTwo(table, tW, rH, bw, "6. Doglan ýeri, ýurdy:", "[Person_CountryOfBirthCode] / [Person_BirthPlace]");
            MFour(table, tW, rH, bw, "7. Jynsy:", "[Person_Gender]", "8. Öý salgysy:", "[Person_HomeCountryCode]");
            MSpan(table, tW, 42F, bw, "[Person_ForeignAddress]", italic: true);
            MSub(table, tW, 17F, bw, "(ýurt, şäher, köçe, jaý №, öý №)");
            MTwo(table, tW, 32F, bw, "9. Gelmeginiň maksady:", "[Visa_PurposeTm]");
            MTwo(table, tW, 36F, bw, "10. Türkmenistanda bolýan ýeri:", "[Address_FullAddress]");
            MSub(table, tW, 17F, bw, "(doly salgysy)");
            MTwo(table, tW, rH, bw, "11. Wizanyň derejesi, görnüşi we No:", "[Visa_TypeFull]");
            MTwo(table, tW, rH, bw, "12. Wizanyň berlen ýeri (ýurdy):", "[Visa_IssuedPlaceTm]");
            MTwo(table, tW, rH, bw, "13. Wizanyň berlen senesi we möhleti:", "[Visa_StartDateText] — [Visa_ExpirationDateText]");
            MFour(table, tW, rH, bw, "14. Giren wagty:", "[Travel_DateText]", "15. Giren ýeri:", "[Travel_CheckPointTm]");
            MTwo(table, tW, 44F, bw, "16. Kabul edýän edara ýa-da şahsyýet:", "[Person_CompanyName]");
            MSub(table, tW, 17F, bw, "(familiýasy, ady, doglan ýyly, öý salgysy)");
            MSpan(table, tW, 26F, bw, "[Person_CompanyAddress]", italic: true);
        }

        // ===================================================================
        //  HELPER METHODS (All kept from your original code)
        // ===================================================================
        private void MTwo(XRTable t, float tW, float h, float bw, string label, string expr)
        {
            const float lW = 240F;
            var row = new XRTableRow { HeightF = h };
            row.Cells.Add(MkLblCell(label, lW, bw));
            row.Cells.Add(MkExprCell(expr, tW - lW, bw, bold: true, italic: true));
            t.Rows.Add(row);
        }

        private void MFour(XRTable t, float tW, float h, float bw, string l1, string e1, string l2, string e2)
        {
            float w1 = 148, w2 = 116, w3 = 158;
            float w4 = tW - w1 - w2 - w3;
            var row = new XRTableRow { HeightF = h };
            row.Cells.Add(MkLblCell(l1, w1, bw));
            row.Cells.Add(MkExprCell(e1, w2, bw, bold: true, italic: true));
            row.Cells.Add(MkLblCell(l2, w3, bw));
            row.Cells.Add(MkExprCell(e2, w4, bw, bold: true, italic: true));
            t.Rows.Add(row);
        }

        private void MSpan(XRTable t, float tW, float h, float bw, string expr, bool italic = false)
        {
            var row = new XRTableRow { HeightF = h };
            var c = new XRTableCell
            {
                WidthF = tW,
                Font = new DXFont("Times New Roman", 8.5F, DXFontStyle.Bold | (italic ? DXFontStyle.Italic : DXFontStyle.Regular)),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                WordWrap = true,
                Padding = new PaddingInfo(6, 4, 2, 2)
            };
            if (!string.IsNullOrEmpty(expr))
                c.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expr));
            row.Cells.Add(c);
            t.Rows.Add(row);
        }

        private void MSub(XRTable t, float tW, float h, float bw, string text)
        {
            var row = new XRTableRow { HeightF = h };
            row.Cells.Add(new XRTableCell
            {
                Text = text,
                WidthF = tW,
                Font = new DXFont("Times New Roman", 7.5F),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                Padding = new PaddingInfo(4, 4, 1, 1)
            });
            t.Rows.Add(row);
        }

        private void BuildRegistrationTable(float y)
        {
            float[] rW = { 175F, 82F, 118F, 98F, 152F, 142F };
            xrTableRegistration.LocationFloat = new PointFloat(0, y);
            xrTableRegistration.BorderWidth = 1.5F;
            xrTableRegistration.BorderColor = Color.Black;
            xrTableRegistration.Borders = BorderSide.All;

            string[] regHdr = { "Hasaba alan, möhletini uzaldan TDMG-nyň edarasy", "Hasaba alyş, uzaldyş belgisi",
                                "Hasaba alnan, uzaldylan wagty", "Möhleti", "Esas(belgisi we wagty)", "Jogapkär işgäriň familiýasy we goly" };

            AddTableRow(xrTableRegistration, rW, 50F, 1.5F, regHdr, null, true);
            AddTableRow(xrTableRegistration, rW, 30F, 1.5F, null, new string[] { "[Registration_TDMG]", "", "[Registration_DateText]", "[Registration_ExpiryText]", "[Registration_BasisText]", "" }, false);
            AddTableRow(xrTableRegistration, rW, 30F, 1.5F, null, null, false);
            AddTableRow(xrTableRegistration, rW, 30F, 1.5F, null, null, false);

            float h = 0; foreach (XRTableRow r in xrTableRegistration.Rows) h += r.HeightF;
            xrTableRegistration.SizeF = new SizeF(767, h);
        }

        private void BuildTravelTable(float y)
        {
            float[] tW = { 256F, 254F, 257F };
            xrTableTravel.LocationFloat = new PointFloat(0, y);
            xrTableTravel.BorderWidth = 1.5F;
            xrTableTravel.BorderColor = Color.Black;
            xrTableTravel.Borders = BorderSide.All;

            string[] trvHdr = { "Türkmenistanyň çäginde bolýan ýeriniň salgysy", "Gelen, giden ýeri", "Kabul edýän edara ýa-da şahsyýet" };
            AddTableRow(xrTableTravel, tW, 38F, 1.5F, trvHdr, null, true);
            AddTableRow(xrTableTravel, tW, 50F, 1.5F, null, new string[] { "[Address_FullAddress]", "[Travel_CheckPointTm]", "[Person_CompanyName]" }, false);
            AddTableRow(xrTableTravel, tW, 30F, 1.5F, null, null, false);

            float h = 0; foreach (XRTableRow r in xrTableTravel.Rows) h += r.HeightF;
            xrTableTravel.SizeF = new SizeF(767, h);
        }

        private void BuildBottomTable(float y)
        {
            xrTableBottom.LocationFloat = new PointFloat(0, y);
            xrTableBottom.BorderWidth = 1.5F;
            xrTableBottom.BorderColor = Color.Black;
            xrTableBottom.Borders = BorderSide.All;

            AddSpanRow(xrTableBottom, 767, 25, 1.5F, "Hasapdan aýyrmak üçin esas", true);
            AddSpanRow(xrTableBottom, 767, 25, 1.5F, "Başga bellikler", true);

            // Passport and Nationality rows can be added here if needed
            float h = 0; foreach (XRTableRow r in xrTableBottom.Rows) h += r.HeightF;
            xrTableBottom.SizeF = new SizeF(767, h);
        }

        // Generic row builder
        private void AddTableRow(XRTable t, float[] widths, float h, float bw, string[] texts, string[] exprs, bool isHeader)
        {
            var row = new XRTableRow { HeightF = h };
            for (int i = 0; i < widths.Length; i++)
            {
                var c = new XRTableCell
                {
                    WidthF = widths[i],
                    Font = isHeader ? new DXFont("Times New Roman", 7.5F, DXFontStyle.Underline) :
                                      new DXFont("Times New Roman", 8.5F, DXFontStyle.Bold | DXFontStyle.Italic),
                    TextAlignment = TextAlignment.MiddleCenter,
                    Borders = BorderSide.All,
                    BorderWidth = bw,
                    BorderColor = Color.Black,
                    WordWrap = true,
                    Padding = new PaddingInfo(3, 3, 2, 2)
                };
                if (texts != null && i < texts.Length && texts[i] != null) c.Text = texts[i];
                if (exprs != null && i < exprs.Length && !string.IsNullOrEmpty(exprs[i]))
                    c.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", exprs[i]));
                row.Cells.Add(c);
            }
            t.Rows.Add(row);
        }

        private void AddSpanRow(XRTable t, float tW, float h, float bw, string text, bool underline = false)
        {
            var row = new XRTableRow { HeightF = h };
            var style = underline ? DXFontStyle.Underline : DXFontStyle.Regular;
            row.Cells.Add(new XRTableCell
            {
                Text = text,
                WidthF = tW,
                Font = new DXFont("Times New Roman", 8.5F, style),
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                Padding = new PaddingInfo(6, 4, 2, 2)
            });
            t.Rows.Add(row);
        }

        private XRTableCell MkLblCell(string text, float width, float bw)
        {
            return new XRTableCell
            {
                Text = text,
                WidthF = width,
                Font = new DXFont("Times New Roman", 8.5F, DXFontStyle.Bold),
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                Padding = new PaddingInfo(6, 3, 2, 2)
            };
        }

        private XRTableCell MkExprCell(string expr, float width, float bw, bool bold = false, bool italic = false)
        {
            var style = DXFontStyle.Regular;
            if (bold) style |= DXFontStyle.Bold;
            if (italic) style |= DXFontStyle.Italic;

            var c = new XRTableCell
            {
                WidthF = width,
                Font = new DXFont("Times New Roman", 8.5F, style),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                WordWrap = true,
                Padding = new PaddingInfo(4, 4, 2, 2)
            };
            if (!string.IsNullOrEmpty(expr))
                c.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expr));
            return c;
        }
    }
}