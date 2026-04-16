using System.Drawing;
using DevExpress.Drawing;
using DevExpress.Utils;
using DevExpress.XtraReports.UI;
using DevExpress.XtraPrinting;

namespace Visa2026.Module.Reports
{
    partial class RegistrationForm16Report
    {
        private XRLabel      xrLabelTitle;
        private XRPictureBox xrPicturePhoto;
        private XRTable      xrTableMain;
        private XRLabel      xrLabelDolduran;
        private XRLabel      xrLabelTDMG;
        private XRLabel      xrLabelDate;
        private XRLabel      xrLabelBarlan;
        private XRLabel      xrLabelSignature;
        private XRLine       xrLineSeparator;
        private XRTable      xrTableRegistration;
        private XRTable      xrTableTravel;
        private XRTable      xrTableBottom;

        private void InitializeComponent()
        {
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();

            xrLabelTitle = new XRLabel();
            xrPicturePhoto = new XRPictureBox();
            xrTableMain = new XRTable();
            xrLabelDolduran = new XRLabel();
            xrLabelTDMG = new XRLabel();
            xrLabelDate = new XRLabel();
            xrLabelBarlan = new XRLabel();
            xrLabelSignature = new XRLabel();
            xrLineSeparator = new XRLine();
            xrTableRegistration = new XRTable();
            xrTableTravel = new XRTable();
            xrTableBottom = new XRTable();

            ((System.ComponentModel.ISupportInitialize)(xrTableMain)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableRegistration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableTravel)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableBottom)).BeginInit();

            const float pageW = 767F;
            const float mainTableW = 612F;
            const float bw = 3.5F;     // Very thick for web
            const float bwLow = 3.0F;
            Color bc = Color.Black;

            this.Landscape = false;
            this.PageWidth = 827;
            this.PageHeight = 1169;
            this.Margins = new DXMargins(30, 30, 40, 45);

            // ==================== TITLE ====================
            xrLabelTitle.Text = "DAŞARY ÝURT RAÝATLARYNY BELLIGE ALYŞ NAMASY";
            xrLabelTitle.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            xrLabelTitle.LocationFloat = new PointFloat(0, 12);
            xrLabelTitle.SizeF = new SizeF(pageW, 36);
            xrLabelTitle.TextAlignment = TextAlignment.MiddleCenter;
            xrLabelTitle.Borders = BorderSide.All;
            xrLabelTitle.BorderWidth = bw;
            xrLabelTitle.BorderColor = bc;
            xrLabelTitle.BorderDashStyle = BorderDashStyle.Solid;
            xrLabelTitle.StylePriority.UseBorders = false;
            xrLabelTitle.StylePriority.UseBorderWidth = false;

            // ==================== PHOTO ====================
            xrPicturePhoto.LocationFloat = new PointFloat(mainTableW + 7, 55);
            xrPicturePhoto.SizeF = new SizeF(pageW - mainTableW - 7, 195);
            xrPicturePhoto.Sizing = ImageSizeMode.ZoomImage;
            xrPicturePhoto.Borders = BorderSide.All;
            xrPicturePhoto.BorderWidth = bw;
            xrPicturePhoto.BorderColor = bc;
            xrPicturePhoto.BorderDashStyle = BorderDashStyle.Solid;
            xrPicturePhoto.StylePriority.UseBorders = false;

            // ==================== MAIN TABLE ====================
            xrTableMain.LocationFloat = new PointFloat(0, 55);
            xrTableMain.Borders = BorderSide.All;
            xrTableMain.BorderWidth = bw;
            xrTableMain.BorderColor = bc;
            xrTableMain.BorderDashStyle = BorderDashStyle.Solid;
            xrTableMain.StylePriority.UseBorders = false;
            BuildMainTable(xrTableMain, mainTableW, bw);

            float mainH = 0;
            foreach (XRTableRow r in xrTableMain.Rows) mainH += r.HeightF;
            xrTableMain.SizeF = new SizeF(mainTableW, mainH);

            float mainBottom = 55F + mainH;

            // Dolduran Section
            float dolY = mainBottom + 12F;
            xrLabelDolduran.Text = "Dolduran edara";
            xrLabelDolduran.Font = new DXFont("Times New Roman", 9.5F, DXFontStyle.Bold);
            xrLabelDolduran.LocationFloat = new PointFloat(0, dolY);
            xrLabelDolduran.SizeF = new SizeF(140, 25);

            xrLabelTDMG.Text = "TDMG";
            xrLabelTDMG.Font = new DXFont("Times New Roman", 9.5F, DXFontStyle.Bold);
            xrLabelTDMG.LocationFloat = new PointFloat(145, dolY);
            xrLabelTDMG.SizeF = new SizeF(110, 25);
            xrLabelTDMG.Borders = BorderSide.Bottom;
            xrLabelTDMG.BorderWidth = 3.5F;
            xrLabelTDMG.BorderColor = bc;
            xrLabelTDMG.BorderDashStyle = BorderDashStyle.Solid;

            xrLabelDate.LocationFloat = new PointFloat(260, dolY);
            xrLabelDate.SizeF = new SizeF(300, 25);
            xrLabelDate.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "'wagty: ' + [Application_DateText]"));

            // Barlan & Signature
            float barY = dolY + 38F;
            xrLabelBarlan.Text = "Barlan gözegçi";
            xrLabelBarlan.Font = new DXFont("Times New Roman", 9.5F, DXFontStyle.Bold);
            xrLabelBarlan.LocationFloat = new PointFloat(0, barY);
            xrLabelBarlan.SizeF = new SizeF(155, 25);

            float sigY = barY + 38F;
            xrLabelSignature.Text = "(goly, familiýasy, ady)";
            xrLabelSignature.Font = new DXFont("Times New Roman", 8.5F);
            xrLabelSignature.LocationFloat = new PointFloat(0, sigY);
            xrLabelSignature.SizeF = new SizeF(pageW, 22);
            xrLabelSignature.Borders = BorderSide.Top;
            xrLabelSignature.BorderWidth = 3.5F;
            xrLabelSignature.BorderColor = bc;
            xrLabelSignature.BorderDashStyle = BorderDashStyle.Solid;

            // Separator
            xrLineSeparator.LocationFloat = new PointFloat(0, sigY + 32);
            xrLineSeparator.SizeF = new SizeF(pageW, 4);
            xrLineSeparator.LineStyle = DXDashStyle.Dash;
            xrLineSeparator.LineWidth = 3F;
            xrLineSeparator.ForeColor = bc;

            // Lower Tables
            float y = sigY + 80;
            BuildRegistrationTable(y, bwLow, bc);
            y += 155;
            BuildTravelTable(y, bwLow, bc);
            y += 135;
            BuildBottomTable(y, bwLow, bc);

            Detail.Controls.AddRange(new XRControl[] {
                xrLabelTitle, xrPicturePhoto, xrTableMain,
                xrLabelDolduran, xrLabelTDMG, xrLabelDate,
                xrLabelBarlan, xrLabelSignature, xrLineSeparator,
                xrTableRegistration, xrTableTravel, xrTableBottom
            });

            Detail.HeightF = y + 160F;

            ((System.ComponentModel.ISupportInitialize)(xrTableMain)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableRegistration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableTravel)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(xrTableBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        // ================== MAIN TABLE BUILD ==================
        private void BuildMainTable(XRTable table, float tW, float bw)
        {
            MTwo(table, tW, 26, bw, "1. Familiýasy, ady, atasynyň ady:", "[Person_FullName]");
            MFour(table, tW, 26, bw, "2. Raýatlygy:", "[Person_NationalityCode]", "3. Doglan senesi:", "[Person_DateOfBirthText]");
            MFour(table, tW, 26, bw, "4. Pasportynyň belgisi:", "[Passport_Number]", "5. Berleni:", "[Passport_IssueDateText]");
            MTwo(table, tW, 26, bw, "6. Doglan ýeri, ýurdy:", "[Person_CountryOfBirthCode] / [Person_BirthPlace]");
            MFour(table, tW, 26, bw, "7. Jynsy:", "[Person_Gender]", "8. Öý salgysy:", "[Person_HomeCountryCode]");
            MSpan(table, tW, 45, bw, "[Person_ForeignAddress]", true);
            MSub(table, tW, 19, bw, "(ýurt, şäher, köçe, jaý №, öý №)");
            MTwo(table, tW, 34, bw, "9. Gelmeginiň maksady:", "[Visa_PurposeTm]");
            MTwo(table, tW, 38, bw, "10. Türkmenistanda bolýan ýeri:", "[Address_FullAddress]");
            MSub(table, tW, 19, bw, "(doly salgysy)");
            MTwo(table, tW, 26, bw, "11. Wizanyň derejesi, görnüşi we No:", "[Visa_TypeFull]");
            MTwo(table, tW, 26, bw, "12. Wizanyň berlen ýeri (ýurdy):", "[Visa_IssuedPlaceTm]");
            MTwo(table, tW, 26, bw, "13. Wizanyň berlen senesi we möhleti:", "[Visa_StartDateText] — [Visa_ExpirationDateText]");
            MFour(table, tW, 26, bw, "14. Giren wagty:", "[Travel_DateText]", "15. Giren ýeri:", "[Travel_CheckPointTm]");
            MTwo(table, tW, 46, bw, "16. Kabul edýän edara ýa-da şahsyýet:", "[Person_CompanyName]");
            MSub(table, tW, 19, bw, "(familiýasy, ady, doglan ýyly, öý salgysy)");
            MSpan(table, tW, 30, bw, "[Person_CompanyAddress]", true);
        }

        // ================== CELL HELPERS (Strong) ==================
        private XRTableCell MkLblCell(string text, float width, float bw)
        {
            var cell = new XRTableCell
            {
                Text = text,
                WidthF = width,
                Font = new DXFont("Times New Roman", 8.5F, DXFontStyle.Bold),
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                BorderDashStyle = BorderDashStyle.Solid,
                Padding = new PaddingInfo(6, 4, 2, 2)
            };
            cell.StylePriority.UseBorders = false;
            cell.StylePriority.UseBorderWidth = false;
            return cell;
        }

        private XRTableCell MkExprCell(string expr, float width, float bw)
        {
            var cell = new XRTableCell
            {
                WidthF = width,
                Font = new DXFont("Times New Roman", 8.5F, DXFontStyle.Bold | DXFontStyle.Italic),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                BorderDashStyle = BorderDashStyle.Solid,
                WordWrap = true,
                Padding = new PaddingInfo(4, 4, 2, 2)
            };
            if (!string.IsNullOrEmpty(expr))
                cell.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expr));
            cell.StylePriority.UseBorders = false;
            cell.StylePriority.UseBorderWidth = false;
            return cell;
        }

        private void MTwo(XRTable t, float tW, float h, float bw, string label, string expr)
        {
            var row = new XRTableRow { HeightF = h };
            row.Cells.Add(MkLblCell(label, 240, bw));
            row.Cells.Add(MkExprCell(expr, tW - 240, bw));
            t.Rows.Add(row);
        }

        private void MFour(XRTable t, float tW, float h, float bw, string l1, string e1, string l2, string e2)
        {
            var row = new XRTableRow { HeightF = h };
            row.Cells.Add(MkLblCell(l1, 148, bw));
            row.Cells.Add(MkExprCell(e1, 116, bw));
            row.Cells.Add(MkLblCell(l2, 158, bw));
            row.Cells.Add(MkExprCell(e2, tW - 422, bw));
            t.Rows.Add(row);
        }

        private void MSpan(XRTable t, float tW, float h, float bw, string expr, bool italic)
        {
            var row = new XRTableRow { HeightF = h };
            var c = new XRTableCell
            {
                WidthF = tW,
                Font = new DXFont("Times New Roman", 8.5F, DXFontStyle.Bold | (italic ? DXFontStyle.Italic : 0)),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                BorderDashStyle = BorderDashStyle.Solid,
                WordWrap = true,
                Padding = new PaddingInfo(6, 4, 2, 2)
            };
            if (!string.IsNullOrEmpty(expr)) c.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", expr));
            c.StylePriority.UseBorders = false;
            row.Cells.Add(c);
            t.Rows.Add(row);
        }

        private void MSub(XRTable t, float tW, float h, float bw, string text)
        {
            var row = new XRTableRow { HeightF = h };
            var c = new XRTableCell
            {
                Text = text,
                WidthF = tW,
                Font = new DXFont("Times New Roman", 7.5F),
                TextAlignment = TextAlignment.MiddleCenter,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                BorderDashStyle = BorderDashStyle.Solid,
                Padding = new PaddingInfo(4, 4, 1, 1)
            };
            c.StylePriority.UseBorders = false;
            row.Cells.Add(c);
            t.Rows.Add(row);
        }

        private void BuildRegistrationTable(float y, float bw, Color bc)
        {
            float[] rW = { 175, 82, 118, 98, 152, 142 };
            xrTableRegistration.LocationFloat = new PointFloat(0, y);
            xrTableRegistration.Borders = BorderSide.All;
            xrTableRegistration.BorderWidth = bw;
            xrTableRegistration.BorderColor = bc;
            xrTableRegistration.BorderDashStyle = BorderDashStyle.Solid;
            xrTableRegistration.StylePriority.UseBorders = false;

            string[] hdr = { "Hasaba alan, möhletini uzaldan TDMG-nyň edarasy", "Hasaba alyş, uzaldyş belgisi", "Hasaba alnan, uzaldylan wagty", "Möhleti", "Esas(belgisi we wagty)", "Jogapkär işgäriň familiýasy we goly" };
            AddTableRow(xrTableRegistration, rW, 52, bw, hdr, null, true);
            AddTableRow(xrTableRegistration, rW, 32, bw, null, new[] { "[Registration_TDMG]", "", "[Registration_DateText]", "[Registration_ExpiryText]", "[Registration_BasisText]", "" }, false);
            AddTableRow(xrTableRegistration, rW, 32, bw, null, null, false);
            AddTableRow(xrTableRegistration, rW, 32, bw, null, null, false);

            xrTableRegistration.SizeF = new SizeF(767, 148);
        }

        private void BuildTravelTable(float y, float bw, Color bc)
        {
            float[] tW = { 256, 254, 257 };
            xrTableTravel.LocationFloat = new PointFloat(0, y);
            xrTableTravel.Borders = BorderSide.All;
            xrTableTravel.BorderWidth = bw;
            xrTableTravel.BorderColor = bc;
            xrTableTravel.BorderDashStyle = BorderDashStyle.Solid;
            xrTableTravel.StylePriority.UseBorders = false;

            string[] hdr = { "Türkmenistanyň çäginde bolýan ýeriniň salgysy", "Gelen, giden ýeri", "Kabul edýän edara ýa-da şahsyýet" };
            AddTableRow(xrTableTravel, tW, 40, bw, hdr, null, true);
            AddTableRow(xrTableTravel, tW, 52, bw, null, new[] { "[Address_FullAddress]", "[Travel_CheckPointTm]", "[Person_CompanyName]" }, false);
            AddTableRow(xrTableTravel, tW, 32, bw, null, null, false);

            xrTableTravel.SizeF = new SizeF(767, 124);
        }

        private void BuildBottomTable(float y, float bw, Color bc)
        {
            xrTableBottom.LocationFloat = new PointFloat(0, y);
            xrTableBottom.Borders = BorderSide.All;
            xrTableBottom.BorderWidth = bw;
            xrTableBottom.BorderColor = bc;
            xrTableBottom.BorderDashStyle = BorderDashStyle.Solid;
            xrTableBottom.StylePriority.UseBorders = false;

            AddSpanRow(xrTableBottom, 767, 28, bw, "Hasapdan aýyrmak üçin esas", true);
            AddSpanRow(xrTableBottom, 767, 28, bw, "Başga bellikler", true);

            xrTableBottom.SizeF = new SizeF(767, 56);
        }

        private void AddTableRow(XRTable t, float[] widths, float h, float bw, string[] texts, string[] exprs, bool isHeader)
        {
            var row = new XRTableRow { HeightF = h };
            for (int i = 0; i < widths.Length; i++)
            {
                var c = new XRTableCell
                {
                    WidthF = widths[i],
                    Font = isHeader ? new DXFont("Times New Roman", 7.5F, DXFontStyle.Bold | DXFontStyle.Underline)
                                   : new DXFont("Times New Roman", 8.5F, DXFontStyle.Bold | DXFontStyle.Italic),
                    TextAlignment = TextAlignment.MiddleCenter,
                    Borders = BorderSide.All,
                    BorderWidth = bw,
                    BorderColor = Color.Black,
                    BorderDashStyle = BorderDashStyle.Solid,
                    Padding = new PaddingInfo(3, 3, 2, 2)
                };
                if (texts != null && i < texts.Length && texts[i] != null) c.Text = texts[i];
                if (exprs != null && i < exprs.Length && !string.IsNullOrEmpty(exprs[i]))
                    c.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", exprs[i]));

                c.StylePriority.UseBorders = false;
                row.Cells.Add(c);
            }
            t.Rows.Add(row);
        }

        private void AddSpanRow(XRTable t, float tW, float h, float bw, string text, bool underline)
        {
            var row = new XRTableRow { HeightF = h };
            var c = new XRTableCell
            {
                Text = text,
                WidthF = tW,
                Font = new DXFont("Times New Roman", 8.5F, DXFontStyle.Bold | (underline ? DXFontStyle.Underline : 0)),
                TextAlignment = TextAlignment.MiddleLeft,
                Borders = BorderSide.All,
                BorderWidth = bw,
                BorderColor = Color.Black,
                BorderDashStyle = BorderDashStyle.Solid,
                Padding = new PaddingInfo(6, 4, 2, 2)
            };
            c.StylePriority.UseBorders = false;
            row.Cells.Add(c);
            t.Rows.Add(row);
        }
    }
}