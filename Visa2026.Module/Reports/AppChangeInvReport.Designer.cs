namespace Visa2026.Module.Reports
{
    partial class AppChangeInvReport
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            this.xrLabelRecipient   = new DevExpress.XtraReports.UI.XRLabel();
            this.xrRichBody1        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody2        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrLabelTableTitle  = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableHeader      = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableHeaderRow   = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrCellHeaderNo     = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrCellHeaderNumber = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrCellHeaderStart  = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrCellHeaderExpiry = new DevExpress.XtraReports.UI.XRTableCell();
            this.invDetailBand      = new DevExpress.XtraReports.UI.DetailReportBand();
            this.invDetail          = new DevExpress.XtraReports.UI.DetailBand();
            this.xrTableData        = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableDataRow     = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrCellNo           = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrCellNumber       = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrCellStart        = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrCellExpiry       = new DevExpress.XtraReports.UI.XRTableCell();
            this.invTableFooter     = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.invSpacerLabel     = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).BeginInit();
            //
            // xrLabelRecipient — Fixed: national Migration Service head. Standard §14.
            // H=80F (CanGrow+CanShrink). No vertical centering (height is dynamic). Ends at 100F.
            //
            this.xrLabelRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.CanShrink = true;
            this.xrLabelRecipient.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(220F, 20F);
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(406.7717F, 80F);
            this.xrLabelRecipient.Text = "T\u00FCrkmenistany\u0148 D\u00F6wlet migrasi\u00FDa gullugyny\u0148 ba\u015Flygyna";
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelRecipient.WordWrap = true;
            //
            // xrRichBody1 — Passport-change invitation re-issue request. Y=115F (15F after recipient end 100F).
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 115F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 100F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText]) sany\b0  da\u351?ary \u253?urt ra\u253?atyny\u328? \b pasportyny \u231?aly\u351?andygy\b0  seb\u228?pli \b a\u351?akda g\u246?rkezilen\b0  \u231?akylyklary t\u228?ze pasportyna g\u246?r\u228? resmile\u351?dirip bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody2 — Static responsibility paragraph. Y=223F (8F after body1 end 115+100=215F).
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 223F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
            //
            // xrLabelTableTitle — Section heading. Y=311F (8F after body2 end 223+80=303F).
            //
            this.xrLabelTableTitle.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelTableTitle.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelTableTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 311F);
            this.xrLabelTableTitle.Name = "xrLabelTableTitle";
            this.xrLabelTableTitle.SizeF = new System.Drawing.SizeF(626.7717F, 25F);
            this.xrLabelTableTitle.Text = "\u00DC\u00FDtgedilmeli \u00E7akylyklar:";
            this.xrLabelTableTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            //
            // xrTableHeader column cells
            //
            this.xrCellHeaderNo.BackColor = System.Drawing.Color.Transparent;
            this.xrCellHeaderNo.BorderColor = System.Drawing.Color.Black;
            this.xrCellHeaderNo.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrCellHeaderNo.BorderWidth = 0.5F;
            this.xrCellHeaderNo.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrCellHeaderNo.Name = "xrCellHeaderNo";
            this.xrCellHeaderNo.SizeF = new System.Drawing.SizeF(40F, 30F);
            this.xrCellHeaderNo.Text = "\u2116";
            this.xrCellHeaderNo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            this.xrCellHeaderNumber.BackColor = System.Drawing.Color.Transparent;
            this.xrCellHeaderNumber.BorderColor = System.Drawing.Color.Black;
            this.xrCellHeaderNumber.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrCellHeaderNumber.BorderWidth = 0.5F;
            this.xrCellHeaderNumber.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrCellHeaderNumber.Name = "xrCellHeaderNumber";
            this.xrCellHeaderNumber.SizeF = new System.Drawing.SizeF(200F, 30F);
            this.xrCellHeaderNumber.Text = "\u00C7akylygy\u0148 belgisi";
            this.xrCellHeaderNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            this.xrCellHeaderStart.BackColor = System.Drawing.Color.Transparent;
            this.xrCellHeaderStart.BorderColor = System.Drawing.Color.Black;
            this.xrCellHeaderStart.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrCellHeaderStart.BorderWidth = 0.5F;
            this.xrCellHeaderStart.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrCellHeaderStart.Name = "xrCellHeaderStart";
            this.xrCellHeaderStart.SizeF = new System.Drawing.SizeF(183F, 30F);
            this.xrCellHeaderStart.Text = "Resmile\u015Fdirilen senesi";
            this.xrCellHeaderStart.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            this.xrCellHeaderExpiry.BackColor = System.Drawing.Color.Transparent;
            this.xrCellHeaderExpiry.BorderColor = System.Drawing.Color.Black;
            this.xrCellHeaderExpiry.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrCellHeaderExpiry.BorderWidth = 0.5F;
            this.xrCellHeaderExpiry.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrCellHeaderExpiry.Name = "xrCellHeaderExpiry";
            this.xrCellHeaderExpiry.SizeF = new System.Drawing.SizeF(203.7717F, 30F);
            this.xrCellHeaderExpiry.Text = "M\u00F6hleti";
            this.xrCellHeaderExpiry.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            //
            // xrTableHeaderRow
            //
            this.xrTableHeaderRow.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
                this.xrCellHeaderNo,
                this.xrCellHeaderNumber,
                this.xrCellHeaderStart,
                this.xrCellHeaderExpiry
            });
            this.xrTableHeaderRow.Name = "xrTableHeaderRow";
            this.xrTableHeaderRow.Weight = 1D;
            //
            // xrTableHeader — Column headers. Y=336F (0F after title end 311+25=336F). Ends at 366F.
            //
            this.xrTableHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 336F);
            this.xrTableHeader.Name = "xrTableHeader";
            this.xrTableHeader.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
                this.xrTableHeaderRow
            });
            this.xrTableHeader.SizeF = new System.Drawing.SizeF(626.7717F, 30F);
            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).EndInit();
            //
            // Detail — HeightF = 366F. No bottom padding — DetailReportBand rows follow directly.
            //
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelRecipient,
                this.xrRichBody1,
                this.xrRichBody2,
                this.xrLabelTableTitle,
                this.xrTableHeader
            });
            this.Detail.HeightF = 366F;
            //
            // Data row cells — bound to Invitation fields
            //
            this.xrCellNo.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DataSource.CurrentRowIndex] + 1")
            });
            this.xrCellNo.BackColor = System.Drawing.Color.Transparent;
            this.xrCellNo.BorderColor = System.Drawing.Color.Black;
            this.xrCellNo.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrCellNo.BorderWidth = 0.5F;
            this.xrCellNo.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            this.xrCellNo.Name = "xrCellNo";
            this.xrCellNo.SizeF = new System.Drawing.SizeF(40F, 25F);
            this.xrCellNo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            this.xrCellNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[InvitationNumber]")
            });
            this.xrCellNumber.BackColor = System.Drawing.Color.Transparent;
            this.xrCellNumber.BorderColor = System.Drawing.Color.Black;
            this.xrCellNumber.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrCellNumber.BorderWidth = 0.5F;
            this.xrCellNumber.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            this.xrCellNumber.Name = "xrCellNumber";
            this.xrCellNumber.SizeF = new System.Drawing.SizeF(200F, 25F);
            this.xrCellNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            this.xrCellStart.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "FormatString('{0:dd.MM.yyyy}', [StartDate])")
            });
            this.xrCellStart.BackColor = System.Drawing.Color.Transparent;
            this.xrCellStart.BorderColor = System.Drawing.Color.Black;
            this.xrCellStart.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrCellStart.BorderWidth = 0.5F;
            this.xrCellStart.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            this.xrCellStart.Name = "xrCellStart";
            this.xrCellStart.SizeF = new System.Drawing.SizeF(183F, 25F);
            this.xrCellStart.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            this.xrCellExpiry.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "FormatString('{0:dd.MM.yyyy}', [ExpirationDate])")
            });
            this.xrCellExpiry.BackColor = System.Drawing.Color.Transparent;
            this.xrCellExpiry.BorderColor = System.Drawing.Color.Black;
            this.xrCellExpiry.Borders = DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right | DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrCellExpiry.BorderWidth = 0.5F;
            this.xrCellExpiry.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            this.xrCellExpiry.Name = "xrCellExpiry";
            this.xrCellExpiry.SizeF = new System.Drawing.SizeF(203.7717F, 25F);
            this.xrCellExpiry.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            //
            // xrTableDataRow
            //
            this.xrTableDataRow.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
                this.xrCellNo,
                this.xrCellNumber,
                this.xrCellStart,
                this.xrCellExpiry
            });
            this.xrTableDataRow.Name = "xrTableDataRow";
            this.xrTableDataRow.Weight = 1D;
            //
            // xrTableData — one row per invitation, Y=0F within invDetail band
            //
            this.xrTableData.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableData.Name = "xrTableData";
            this.xrTableData.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
                this.xrTableDataRow
            });
            this.xrTableData.SizeF = new System.Drawing.SizeF(626.7717F, 25F);
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).EndInit();
            //
            // invDetail — inner Detail band, 25F per invitation row
            //
            this.invDetail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrTableData
            });
            this.invDetail.HeightF = 25F;
            this.invDetail.Name = "invDetail";
            //
            // invSpacerLabel — invisible label that forces invTableFooter to render at full height
            //
            this.invSpacerLabel.BackColor = System.Drawing.Color.Transparent;
            this.invSpacerLabel.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.invSpacerLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.invSpacerLabel.Name = "invSpacerLabel";
            this.invSpacerLabel.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
            //
            // invTableFooter — spacer band after last invitation row, before signatory
            //
            this.invTableFooter.Controls.Add(this.invSpacerLabel);
            this.invTableFooter.HeightF = 80F;
            this.invTableFooter.Name = "invTableFooter";
            //
            // invDetailBand — DetailReportBand bound to Application.Invitations
            //
            this.invDetailBand.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
                this.invDetail,
                this.invTableFooter
            });
            this.invDetailBand.DataMember = "Invitations";
            this.invDetailBand.Level = 0;
            this.invDetailBand.Name = "invDetailBand";
            //
            // Add DetailReportBand to report — renders between Detail and ReportFooter
            //
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
                this.invDetailBand
            });
        }

        #endregion

        private DevExpress.XtraReports.UI.XRLabel       xrLabelRecipient;
        private DevExpress.XtraReports.UI.XRRichText    xrRichBody1;
        private DevExpress.XtraReports.UI.XRRichText    xrRichBody2;
        private DevExpress.XtraReports.UI.XRLabel       xrLabelTableTitle;
        private DevExpress.XtraReports.UI.XRTable       xrTableHeader;
        private DevExpress.XtraReports.UI.XRTableRow    xrTableHeaderRow;
        private DevExpress.XtraReports.UI.XRTableCell   xrCellHeaderNo;
        private DevExpress.XtraReports.UI.XRTableCell   xrCellHeaderNumber;
        private DevExpress.XtraReports.UI.XRTableCell   xrCellHeaderStart;
        private DevExpress.XtraReports.UI.XRTableCell   xrCellHeaderExpiry;
        private DevExpress.XtraReports.UI.DetailReportBand invDetailBand;
        private DevExpress.XtraReports.UI.DetailBand    invDetail;
        private DevExpress.XtraReports.UI.ReportFooterBand invTableFooter;
        private DevExpress.XtraReports.UI.XRLabel        invSpacerLabel;
        private DevExpress.XtraReports.UI.XRTable       xrTableData;
        private DevExpress.XtraReports.UI.XRTableRow    xrTableDataRow;
        private DevExpress.XtraReports.UI.XRTableCell   xrCellNo;
        private DevExpress.XtraReports.UI.XRTableCell   xrCellNumber;
        private DevExpress.XtraReports.UI.XRTableCell   xrCellStart;
        private DevExpress.XtraReports.UI.XRTableCell   xrCellExpiry;
    }
}
