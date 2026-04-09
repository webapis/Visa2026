namespace Visa2026.Module.Reports
{
    partial class AppCancelVisaAndWPReport
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
            // xrLabelRecipient — Fixed: national Migration Service head. Standard §14.
            // H=60F (CanGrow+CanShrink). Y=20F. Ends at 80F.
            //
            this.xrLabelRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.CanShrink = true;
            this.xrLabelRecipient.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(220F, 20F);
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(406.7717F, 60F);
            this.xrLabelRecipient.Text = "T\u00FCrkmenistany\u0148 D\u00F6wlet migrasi\u00FDa gullugyny\u0148 ba\u015Flygyna";
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelRecipient.WordWrap = true;
            //
            // xrRichBody1 — Visa and WP cancellation request paragraph. Y=100F. H=120F. Ends at 220F.
            // Bold spans: person count (×2 for person + visa), static departure phrase, WP count.
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 100F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [CancelPersonCount] ([CancelPersonCountText]) sany\b0  da\u351?ary \u253?urt ra\u253?atyny\u328? \b T\u252?rkmenistany\u328? \u231?\u228?ginden \u231?ykyp gidendigi\b0  seb\u228?pli \b [CancelPersonCount] ([CancelPersonCountText]) sany\b0  wizasyny we \b [CancelWPCount] ([CancelWPCountText]) sany\b0  i\u351?lemek \u252?\u231?\u252?n rugsatnamasyny \u253?atyrmagy\u328?yzy Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody2 — Static responsibility paragraph. Y=188F. H=72F. Ends at 260F.
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 188F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 72F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
            //
            // Detail — HeightF = 260F: last control end (188+72=260F). Standard §20.
            //
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelRecipient,
                this.xrRichBody1,
                this.xrRichBody2
            });
            this.Detail.HeightF = 260F;
        }

        #endregion

        private DevExpress.XtraReports.UI.XRLabel    xrLabelRecipient;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody1;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody2;
    }
}
