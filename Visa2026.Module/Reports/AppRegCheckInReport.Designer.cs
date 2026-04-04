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
            this.xrLabelBody1     = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelBody2     = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            //
            // xrLabelRecipient — Migration Service name, bold, centered on right half of page
            //
            this.xrLabelRecipient.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[MigrationService_NameTm]")
            });
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(393F, 30F);
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(393.7717F, 70F);
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrLabelRecipient.WordWrap = true;
            //
            // xrLabelBody1 — paragraph 1: static text with embedded TotalPersonCount and TotalPersonCountText
            //
            this.xrLabelBody1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                    "'Hatymyzyň goşundysynda görkezilen sanawdaky ' + [TotalPersonCount] + ' (' + [TotalPersonCountText] + ') sany daşary ýurt raýatynyň Türkmenistana gelendigi sebäpli hasaba almagyňyzy Sizden haýyş edýäris.'")
            });
            this.xrLabelBody1.CanGrow = true;
            this.xrLabelBody1.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 140F);
            this.xrLabelBody1.Multiline = true;
            this.xrLabelBody1.Name = "xrLabelBody1";
            this.xrLabelBody1.SizeF = new System.Drawing.SizeF(786.7717F, 60F);
            this.xrLabelBody1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopJustify;
            this.xrLabelBody1.WordWrap = true;
            //
            // xrLabelBody2 — paragraph 2: static text, company takes responsibility
            //
            this.xrLabelBody2.CanGrow = true;
            this.xrLabelBody2.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 215F);
            this.xrLabelBody2.Multiline = true;
            this.xrLabelBody2.Name = "xrLabelBody2";
            this.xrLabelBody2.SizeF = new System.Drawing.SizeF(786.7717F, 50F);
            this.xrLabelBody2.Text = "Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.";
            this.xrLabelBody2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopJustify;
            this.xrLabelBody2.WordWrap = true;
            //
            // Detail — add controls, resize to fit content
            //
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelRecipient,
                this.xrLabelBody1,
                this.xrLabelBody2
            });
            this.Detail.HeightF = 290F;
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        #endregion

        private DevExpress.XtraReports.UI.XRLabel xrLabelRecipient;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody1;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody2;
    }
}
