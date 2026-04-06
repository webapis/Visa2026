namespace Visa2026.Module.Reports
{
    partial class AppRegCheckInReport
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
            this.xrLabelRecipient = new DevExpress.XtraReports.UI.XRLabel();
            this.xrRichBody1      = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody2      = new DevExpress.XtraReports.UI.XRRichText();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();
            //
            // xrLabelRecipient — Migration Service name, bold, right-aligned on right half of page
            //
            this.xrLabelRecipient.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[MigrationService_NameTm]")
            });
            this.xrLabelRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(313F, 30F);
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(313.7717F, 100F);
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabelRecipient.WordWrap = true;
            //
            // xrRichBody1 — Paragraph 1.
            // Font: Times New Roman 15pt | Alignment: Justified | First-line indent: 0.5 inch (\fi720).
            // Dynamic values «[TotalPersonCount]» and «[TotalPersonCountText]» use mail merge field syntax.
            // Bold applied around the count via \b ... \b0.
            // To edit: open report in designer → double-click this control → rich text editor opens.
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 155F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            // RTF set after EndInit.
            // [FieldName] inside RTF text is evaluated by XtraReports at render time — no special delimiters needed.
            // Surround with regular " " quotes for display. Bold via \b ... \b0.
            // Turkmen Unicode escapes — see REPORT_STANDARDS.md Section 6.
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b \u8220?[TotalPersonCount]\u8221? (\u8220?[TotalPersonCountText]\u8221?)\b0  sany da\u351?ary \u253?urt ra\u253?atyny\u328? \b T\u252?rkmenistana gelendigi seb\u228?pli\b0  hasaba almagy\u328?yzy Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody2 — Paragraph 2.
            // Font: Times New Roman 15pt | Alignment: Justified | First-line indent: 0.5 inch (\fi720).
            // Static text — no dynamic fields.
            // To edit: open report in designer → double-click this control → rich text editor opens.
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 243F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
            //
            // Detail — body1 (y=155, h=80) + 8F gap + body2 (y=243, h=80); recipient above at y=30.
            //
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelRecipient,
                this.xrRichBody1,
                this.xrRichBody2
            });
            this.Detail.HeightF = 350F;
        }

        #endregion

        private DevExpress.XtraReports.UI.XRLabel    xrLabelRecipient;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody1;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody2;
    }
}
