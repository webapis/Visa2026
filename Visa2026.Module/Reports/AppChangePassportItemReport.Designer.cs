using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppChangePassportItemReport
    {
        private void InitializeComponent()
        {
            // ----------------------------------------------------------------
            // 14 columns, weights sum = 1129.291F
            // ----------------------------------------------------------------
            double[] weights = {
                20,      // 0  №
                65,      // 1  Familiýasy
                55,      // 2  Ady
                85,      // 3  Doglan senesi we ýeri
                38,      // 4  Jynsy
                38,      // 5  Raýatlygy
                80,      // 6  Pasport belgisi we möhleti
                80,      // 7  Bilimi we okan ýeri
                80,      // 8  Bilimine görä hünäri
                110,     // 9  Wezipesi
                90,      // 10 Möhleti we gezekligi
                125,     // 11 Türkmenistandaky salgysy
                120,     // 12 Daşary ýurtdaky salgysy
                143.291  // 13 Barjak serhet ýakasy
            };
            string[] hdrTexts = {
                "\u2116",
                "Famil\u00FDasy",
                "Ady",
                "Doglan senesi we \u00FDeri",
                "Jynsy",
                "Ra\u00FDatlygy",
                "Pasport belgisi we m\u00F6hleti",
                "Bilimi we okan \u00FDeri",
                "Bilimine g\u00F6r\u00E4 h\u00FCn\u00E4ri",
                "Wezipesi",
                "M\u00F6hleti we gezekligi",
                "T\u00FCrkmenistandaky salgysy",
                "Da\u015Fary \u00FDurtdaky salgysy",
                "Barjak serhet \u00FDakasy"
            };
            // Data cell expressions — bind to PassportChangeRow properties
            string[] dataExprs = {
                "sumRecordNumber()",         // 0  row number (resets per group)
                "[PersonLastName]",          // 1
                "[PersonFirstName]",         // 2
                "[PersonBirthInfo]",         // 3  pre-formatted multiline
                "[PersonGenderTm]",          // 4
                "[PersonNationalityCode]",   // 5
                "[PassportBelgisi]",         // 6  old or new passport (per group)
                "[BilimiWeOkanYeri]",        // 7  pre-formatted multiline
                "[BilimineGoreHunari]",      // 8
                "[Wezipesi]",                // 9
                "[MohletiWeGezekligi]",      // 10 pre-formatted multiline
                "[TmSalgysy]",              // 11
                "[DasarySalgysy]",          // 12
                "[BarjakSerhetYakasy]"       // 13
            };
            int[] multilineIdx = { 3, 6, 7, 9, 10, 11, 12, 13 };
            int N = weights.Length;

            this.xrLabelTitle = new XRLabel();

            // ----------------------------------------------------------------
            // Page — A4 Landscape
            // ----------------------------------------------------------------
            this.Landscape     = true;
            this.PageWidthF    = 1169.291F;
            this.PageHeightF   = 826.7717F;
            this.Margins       = new DXMargins(20F, 20F, 50F, 50F);

            this.xrLabelAppNumber.Visible = false;
            this.xrLabelAppDate.Visible   = false;

            // ----------------------------------------------------------------
            // Signatory — reposition for landscape (inherited ReportFooter)
            // ----------------------------------------------------------------
            this.ReportFooter.HeightF = 80F;
            this.xrLabelSignatoryPosition.LocationFloat = new DevExpress.Utils.PointFloat(0F, 40F);
            this.xrLabelSignatoryPosition.SizeF         = new System.Drawing.SizeF(564F, 20F);
            this.xrLabelSignatoryPosition.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelSignatoryFullName.LocationFloat = new DevExpress.Utils.PointFloat(565F, 40F);
            this.xrLabelSignatoryFullName.SizeF         = new System.Drawing.SizeF(564.291F, 20F);
            this.xrLabelSignatoryFullName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // ----------------------------------------------------------------
            // PageHeader (30F): report title
            // ----------------------------------------------------------------
            this.PageHeader.HeightF = 30F;

            this.xrLabelTitle.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 5F);
            this.xrLabelTitle.Name          = "xrLabelTitle";
            this.xrLabelTitle.SizeF         = new System.Drawing.SizeF(1129.291F, 22F);
            this.xrLabelTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelTitle.Text          = "Da\u015Fary \u00FDurt ra\u00FDatlary\u0148y\u0148 sanawy";
            this.xrLabelTitle.BackColor     = System.Drawing.Color.Transparent;
            this.PageHeader.Controls.Add(this.xrLabelTitle);

            // ----------------------------------------------------------------
            // GroupHeaderBand: grouped on IsKone descending
            //   IsKone=true  (1) → "Köne pasportyň maglumatlary"
            //   IsKone=false (0) → "Täze pasportyň maglumatlary"
            // Height: 16F section label + 45F column headers = 61F
            // ----------------------------------------------------------------
            var groupHeaderBand = new GroupHeaderBand();
            groupHeaderBand.Name    = "groupHeaderBand";
            groupHeaderBand.HeightF = 61F;
            groupHeaderBand.GroupFields.Add(new GroupField("IsKone", XRColumnSortOrder.Descending));
            groupHeaderBand.RepeatEveryPage = true;

            // Section label — text switches via ExpressionBinding on IsKone
            var xrLabelSection = new XRLabel
            {
                Name          = "xrLabelSection",
                Font          = new DXFont("Times New Roman", 8F, DXFontStyle.Bold),
                LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F),
                SizeF         = new System.Drawing.SizeF(1129.291F, 16F),
                TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter,
                BackColor     = System.Drawing.Color.Transparent
            };
            xrLabelSection.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "Iif([IsKone], 'K\u00F6ne pasporty\u0148 maglumatlary', 'T\u00E4ze pasporty\u0148 maglumatlary')"));
            groupHeaderBand.Controls.Add(xrLabelSection);

            // Column header table
            var tableHdr = new XRTable();
            var rowHdr   = new XRTableRow { HeightF = 45F, Name = "xrRowHeader" };
            ((System.ComponentModel.ISupportInitialize)tableHdr).BeginInit();

            for (int i = 0; i < N; i++)
            {
                var hc = new XRTableCell
                {
                    Name          = "xrHdr" + i,
                    Text          = hdrTexts[i],
                    Weight        = weights[i],
                    Font          = new DXFont("Times New Roman", 6F, DXFontStyle.Bold),
                    TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter,
                    WordWrap      = true,
                    Borders       = DevExpress.XtraPrinting.BorderSide.All,
                    BorderWidth   = 0.5F,
                    BorderColor   = System.Drawing.Color.Black,
                    BackColor     = System.Drawing.Color.Transparent
                };
                hc.StylePriority.UseBorders     = true;
                hc.StylePriority.UseBorderWidth = true;
                hc.StylePriority.UseBorderColor = true;
                hc.StylePriority.UseBackColor   = true;
                rowHdr.Cells.Add(hc);
            }

            tableHdr.LocationFloat = new DevExpress.Utils.PointFloat(0F, 16F);
            tableHdr.Name          = "xrTableHeader";
            tableHdr.SizeF         = new System.Drawing.SizeF(1129.291F, 45F);
            tableHdr.Rows.Add(rowHdr);
            ((System.ComponentModel.ISupportInitialize)tableHdr).EndInit();
            groupHeaderBand.Controls.Add(tableHdr);
            this.Bands.Add(groupHeaderBand);

            // ----------------------------------------------------------------
            // Detail: one row per PassportChangeRow
            // ----------------------------------------------------------------
            var tableData = new XRTable();
            var rowData   = new XRTableRow { HeightF = 70F, CanGrow = true, Name = "xrRowData" };
            ((System.ComponentModel.ISupportInitialize)tableData).BeginInit();

            for (int i = 0; i < N; i++)
            {
                var dc = new XRTableCell { Name = "xrCell" + i, Weight = weights[i] };
                dc.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", dataExprs[i]));
                dc.Font          = new DXFont("Times New Roman", 7F);
                dc.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
                dc.WordWrap      = true;
                dc.CanGrow       = true;
                dc.Borders       = DevExpress.XtraPrinting.BorderSide.Left
                                 | DevExpress.XtraPrinting.BorderSide.Right
                                 | DevExpress.XtraPrinting.BorderSide.Bottom;
                dc.BorderWidth   = 0.5F;
                dc.BorderColor   = System.Drawing.Color.Black;
                dc.BackColor     = System.Drawing.Color.Transparent;
                dc.Padding       = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 2, 2);
                dc.StylePriority.UseBorders     = true;
                dc.StylePriority.UseBorderWidth = true;
                dc.StylePriority.UseBorderColor = true;
                dc.StylePriority.UseBackColor   = true;
                rowData.Cells.Add(dc);
            }

            // Row number resets per group
            rowData.Cells[0].Summary = new XRSummary { Running = SummaryRunning.Group };

            // Multiline for pre-formatted and long-text cells
            foreach (int mi in multilineIdx)
                rowData.Cells[mi].Multiline = true;

            tableData.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            tableData.Name          = "xrTableData";
            tableData.SizeF         = new System.Drawing.SizeF(1129.291F, 70F);
            tableData.Rows.Add(rowData);
            ((System.ComponentModel.ISupportInitialize)tableData).EndInit();

            this.Detail.HeightF = 70F;
            this.Detail.CanGrow = true;
            this.Detail.Controls.Add(tableData);
        }

        private XRLabel xrLabelTitle;
    }
}
