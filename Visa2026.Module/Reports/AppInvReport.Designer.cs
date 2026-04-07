namespace Visa2026.Module.Reports
{
    partial class AppInvReport
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
            this.xrRichRecipient    = new DevExpress.XtraReports.UI.XRRichText();
            this.xrLabelUrgency     = new DevExpress.XtraReports.UI.XRLabel();
            this.xrRichGreeting     = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody1        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody2        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody3        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichAttachments  = new DevExpress.XtraReports.UI.XRRichText();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichRecipient)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichGreeting)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichAttachments)).BeginInit();
            //
            // xrRichRecipient — Ministry recipient block, right half, bound to Ministry.RecipientBlock via NotMapped flat property.
            // Content formatted by user in XAF (bold, alignment stored in RTF).
            // To edit layout: open report in designer → double-click control.
            //
            this.xrRichRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrRichRecipient.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichRecipient.CanGrow   = true;
            this.xrRichRecipient.LocationFloat = new DevExpress.Utils.PointFloat(313F, 20F);
            this.xrRichRecipient.Name = "xrRichRecipient";
            this.xrRichRecipient.SizeF = new System.Drawing.SizeF(313.7717F, 120F);
            this.xrRichRecipient.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Rtf", "[ProjectContract_Ministry_RecipientBlock]")
            });
            ((System.ComponentModel.ISupportInitialize)(this.xrRichRecipient)).EndInit();
            //
            // xrLabelUrgency — Urgency label (e.g. "Gyssagly tertipde!"), italic, left-aligned.
            // Visible only when ApplicationType.ShowUrgency = true.
            //
            this.xrLabelUrgency.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",    "[Urgency_NameTm]"),
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Visible", "[ApplicationType.ShowUrgency]")
            });
            this.xrLabelUrgency.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelUrgency.CanGrow   = true;
            this.xrLabelUrgency.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelUrgency.LocationFloat = new DevExpress.Utils.PointFloat(0F, 150F);
            this.xrLabelUrgency.Name = "xrLabelUrgency";
            this.xrLabelUrgency.SizeF = new System.Drawing.SizeF(300F, 25F);
            this.xrLabelUrgency.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            //
            // xrRichGreeting — "Hormatly ...!" salutation, centered bold, from Ministry.FormOfAddress.
            //
            this.xrRichGreeting.BackColor = System.Drawing.Color.Transparent;
            this.xrRichGreeting.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichGreeting.CanGrow   = true;
            this.xrRichGreeting.LocationFloat = new DevExpress.Utils.PointFloat(0F, 185F);
            this.xrRichGreeting.Name = "xrRichGreeting";
            this.xrRichGreeting.SizeF = new System.Drawing.SizeF(626.7717F, 35F);
            this.xrRichGreeting.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Rtf", "[ProjectContract_Ministry_FormOfAddress]")
            });
            ((System.ComponentModel.ISupportInitialize)(this.xrRichGreeting)).EndInit();
            //
            // xrRichBody1 — Contract context paragraph, bound to ProjectContract.Description.
            // Full long-form text authored in ProjectContract.Description field.
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 230F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 140F);
            this.xrRichBody1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Rtf", "[ProjectContract_Description]")
            });
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            //
            // xrRichBody2 — Request paragraph with person count, visa period, visa category.
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            // Dynamic values wrapped in curly quotes per REPORT_STANDARDS.md Section 5.
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 378F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 100F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen T\u252?rki\u253?e Respublikasyny\u328? \u8220?[Company.Name]\u8221? kompani\u253?asyna degi\u351?li bolan sanawdaky \b \u8220?[TotalPersonCount]\u8221? (\u8220?[TotalPersonCountText]\u8221?)\b0 sany da\u351?ary \u253?urt ra\u253?atyna \b \u8220?[VisaPeriod_NameTm]\u8221?\b0  bilen \b \u8220?[VisaCategory_NameTm]\u8221?\b0  \u231?akylyk resmile\u351?dirilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody3 — Static responsibility paragraph (same as AppRegCheckInReport).
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            //
            this.xrRichBody3.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody3.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody3.CanGrow   = true;
            this.xrRichBody3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 486F);
            this.xrRichBody3.Name = "xrRichBody3";
            this.xrRichBody3.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).EndInit();
            this.xrRichBody3.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
            //
            // xrRichAttachments — Attachment list with dynamic count prefix, left-aligned, no indent.
            //
            this.xrRichAttachments.BackColor = System.Drawing.Color.Transparent;
            this.xrRichAttachments.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichAttachments.CanGrow   = true;
            this.xrRichAttachments.LocationFloat = new DevExpress.Utils.PointFloat(0F, 564F);
            this.xrRichAttachments.Name = "xrRichAttachments";
            this.xrRichAttachments.SizeF = new System.Drawing.SizeF(626.7717F, 60F);
            this.xrRichAttachments.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                    @"'Go\u351?undy:   1. ' + [TotalPersonCount] + '-pasport kopi\u253?alary,\n           2. Go\u351?undy (' + [TotalPersonCount] + '-da\u351?ary \u253?urt ra\u253?atyny\u328? maglumaty)'")
            });
            this.xrRichAttachments.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichAttachments)).EndInit();
            //
            // Detail
            //
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrRichRecipient,
                this.xrLabelUrgency,
                this.xrRichGreeting,
                this.xrRichBody1,
                this.xrRichBody2,
                this.xrRichBody3,
                this.xrRichAttachments
            });
            this.Detail.HeightF = 640F;
        }

        #endregion

        private DevExpress.XtraReports.UI.XRRichText xrRichRecipient;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelUrgency;
        private DevExpress.XtraReports.UI.XRRichText xrRichGreeting;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody1;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody2;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody3;
        private DevExpress.XtraReports.UI.XRRichText xrRichAttachments;
    }
}
