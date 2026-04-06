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
            this.xrLabelBody1Line1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelBody1Line2Prefix = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelBody1Count = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelBody1Line2Suffix = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelBody1Suffix = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelBody2Line1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelBody2Line2 = new DevExpress.XtraReports.UI.XRLabel();
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
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabelRecipient.WordWrap = true;
            //
            // xrLabelBody1Line1 — paragraph 1, first centered line
            //
            this.xrLabelBody1Line1.CanGrow = true;
            this.xrLabelBody1Line1.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelBody1Line1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 130F);
            this.xrLabelBody1Line1.Multiline = true;
            this.xrLabelBody1Line1.Name = "xrLabelBody1Line1";
            this.xrLabelBody1Line1.SizeF = new System.Drawing.SizeF(786.7717F, 20F);
            this.xrLabelBody1Line1.Text = "Hatymyzyň goşundysynda görkezilen sanawdaky";
            this.xrLabelBody1Line1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrLabelBody1Line1.WordWrap = true;
            //
            // xrLabelBody1Line2Prefix — line 2 prefix (right-aligned, ends at center)
            //
            this.xrLabelBody1Line2Prefix.CanGrow = false;
            this.xrLabelBody1Line2Prefix.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelBody1Line2Prefix.LocationFloat = new DevExpress.Utils.PointFloat(0F, 152F);
            this.xrLabelBody1Line2Prefix.Name = "xrLabelBody1Line2Prefix";
            this.xrLabelBody1Line2Prefix.SizeF = new System.Drawing.SizeF(343F, 20F);
            this.xrLabelBody1Line2Prefix.Text = "sany daşary ýurt raýatynyň Türkmenistana gelendigi sebäpli hasaba almagyňyzy";
            this.xrLabelBody1Line2Prefix.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabelBody1Line2Prefix.WordWrap = false;
            //
            // xrLabelBody1Count — bold count snippet (centered)
            //
            this.xrLabelBody1Count.CanGrow = false;
            this.xrLabelBody1Count.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelBody1Count.LocationFloat = new DevExpress.Utils.PointFloat(343F, 152F);
            this.xrLabelBody1Count.Name = "xrLabelBody1Count";
            this.xrLabelBody1Count.SizeF = new System.Drawing.SizeF(100F, 20F);
            this.xrLabelBody1Count.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrLabelBody1Count.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TotalPersonCount] + ' (' + [TotalPersonCountText] + ')'")
            });
            //
            // xrLabelBody1Line2Suffix — line 2 suffix (left-aligned, starts after count)
            //
            this.xrLabelBody1Line2Suffix.CanGrow = false;
            this.xrLabelBody1Line2Suffix.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelBody1Line2Suffix.LocationFloat = new DevExpress.Utils.PointFloat(443F, 152F);
            this.xrLabelBody1Line2Suffix.Name = "xrLabelBody1Line2Suffix";
            this.xrLabelBody1Line2Suffix.SizeF = new System.Drawing.SizeF(343.7717F, 20F);
            this.xrLabelBody1Line2Suffix.Text = "Sizden haýyş edýäris.";
            this.xrLabelBody1Line2Suffix.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelBody1Line2Suffix.WordWrap = false;
            //
            // xrLabelBody1Suffix — paragraph 1, remainder (centered)
            //
            this.xrLabelBody1Suffix.CanGrow = true;
            this.xrLabelBody1Suffix.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelBody1Suffix.LocationFloat = new DevExpress.Utils.PointFloat(0F, 174F);
            this.xrLabelBody1Suffix.Multiline = true;
            this.xrLabelBody1Suffix.Name = "xrLabelBody1Suffix";
            this.xrLabelBody1Suffix.SizeF = new System.Drawing.SizeF(786.7717F, 40F);
            this.xrLabelBody1Suffix.Text = "";
            this.xrLabelBody1Suffix.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrLabelBody1Suffix.WordWrap = true;
            //
            // xrLabelBody2Line1 — paragraph 2, line 1 (centered)
            //
            this.xrLabelBody2Line1.CanGrow = true;
            this.xrLabelBody2Line1.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelBody2Line1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 225F);
            this.xrLabelBody2Line1.Multiline = true;
            this.xrLabelBody2Line1.Name = "xrLabelBody2Line1";
            this.xrLabelBody2Line1.SizeF = new System.Drawing.SizeF(786.7717F, 20F);
            this.xrLabelBody2Line1.Text = "Daşary ýurt raýatynyň Türkmenistana gelmeginiň, onda bolmagynyň we ondan";
            this.xrLabelBody2Line1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrLabelBody2Line1.WordWrap = true;
            //
            // xrLabelBody2Line2 — paragraph 2, line 2 (centered)
            //
            this.xrLabelBody2Line2.CanGrow = true;
            this.xrLabelBody2Line2.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelBody2Line2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 245F);
            this.xrLabelBody2Line2.Multiline = true;
            this.xrLabelBody2Line2.Name = "xrLabelBody2Line2";
            this.xrLabelBody2Line2.SizeF = new System.Drawing.SizeF(786.7717F, 20F);
            this.xrLabelBody2Line2.Text = "gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.";
            this.xrLabelBody2Line2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            this.xrLabelBody2Line2.WordWrap = true;
            //
            // Detail — add controls, resize to fit content
            //
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelRecipient,
                this.xrLabelBody1Line1,
                this.xrLabelBody1Line2Prefix,
                this.xrLabelBody1Count,
                this.xrLabelBody1Line2Suffix,
                this.xrLabelBody2Line1,
                this.xrLabelBody2Line2
            });
            this.Detail.HeightF = 290F;
        }

        #endregion

        private DevExpress.XtraReports.UI.XRLabel xrLabelRecipient;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody1Line1;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody1Line2Prefix;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody1Count;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody1Line2Suffix;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody1Suffix;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody2Line1;
        private DevExpress.XtraReports.UI.XRLabel xrLabelBody2Line2;
    }
}
