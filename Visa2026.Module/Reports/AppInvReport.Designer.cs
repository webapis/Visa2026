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
            this.xrLabelRecipient   = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelUrgency     = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelGreeting    = new DevExpress.XtraReports.UI.XRLabel();
            this.xrRichBody1        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody2        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody3        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrLabelAttachments = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).BeginInit();
            //
            // xrLabelRecipient — Ministry recipient block, right half, bold, right-aligned.
            // Plain text from Ministry.RecipientBlock — formatting (bold, alignment) owned by this control.
            //
            this.xrLabelRecipient.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ProjectContract_Ministry_RecipientBlock]")
            });
            this.xrLabelRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(313F, 20F);
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(313.7717F, 120F);
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabelRecipient.WordWrap = true;
            //
            // xrLabelUrgency — Urgency (e.g. "Gyssagly tertipde!"), italic, left-aligned.
            // Visible only when ApplicationType.ShowUrgency = true.
            //
            this.xrLabelUrgency.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",    "[Urgency_NameTm]"),
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Visible", "[ApplicationType.ShowUrgency]")
            });
            this.xrLabelUrgency.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelUrgency.CanGrow = true;
            this.xrLabelUrgency.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelUrgency.LocationFloat = new DevExpress.Utils.PointFloat(0F, 150F);
            this.xrLabelUrgency.Name = "xrLabelUrgency";
            this.xrLabelUrgency.SizeF = new System.Drawing.SizeF(300F, 25F);
            this.xrLabelUrgency.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            //
            // xrLabelGreeting — "Hormatly ...!" salutation, centered bold.
            // Plain text from Ministry.FormOfAddress — formatting owned by this control.
            //
            this.xrLabelGreeting.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ProjectContract_Ministry_FormOfAddress]")
            });
            this.xrLabelGreeting.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelGreeting.CanGrow = true;
            this.xrLabelGreeting.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelGreeting.LocationFloat = new DevExpress.Utils.PointFloat(0F, 185F);
            this.xrLabelGreeting.Name = "xrLabelGreeting";
            this.xrLabelGreeting.SizeF = new System.Drawing.SizeF(626.7717F, 35F);
            this.xrLabelGreeting.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelGreeting.WordWrap = true;
            //
            // xrRichBody1 — Contract context paragraph, bound to ProjectContract.Description (plain text).
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 230F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 140F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            // RTF template: \qj = justified, \fi720 = 0.5 inch first-line indent, \fs30 = 15pt.
            // [ProjectContract_Description] is evaluated at render time inside the RTF stream.
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 [ProjectContract_Description]\par}";
            //
            // xrRichBody2 — Request paragraph: person count + visa period + visa category, bold inline.
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 378F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 100F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen T\u252?rki\u253?e Respublikasyny\u328? \u8220?[Company.Name]\u8221? kompani\u253?asyna degi\u351?li bolan sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna \b [VisaPeriod_NameTm]\b0  bilen \b [VisaCategory_NameTm]\b0  \u231?akylyk resmile\u351?dirilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody3 — Static responsibility paragraph.
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
            // xrLabelAttachments — Two-line attachment list with dynamic count.
            // Uses Char(10) for line break, actual Turkmen characters in expression.
            //
            this.xrLabelAttachments.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                    "'Goşundy:   1. ' + [TotalPersonCount] + '-pasport kopiýalary,' + Char(10) + '           2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumaty)'")
            });
            this.xrLabelAttachments.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelAttachments.CanGrow = true;
            this.xrLabelAttachments.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            this.xrLabelAttachments.LocationFloat = new DevExpress.Utils.PointFloat(0F, 564F);
            this.xrLabelAttachments.Multiline = true;
            this.xrLabelAttachments.Name = "xrLabelAttachments";
            this.xrLabelAttachments.SizeF = new System.Drawing.SizeF(626.7717F, 60F);
            this.xrLabelAttachments.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelAttachments.WordWrap = true;
            //
            // Detail
            //
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelRecipient,
                this.xrLabelUrgency,
                this.xrLabelGreeting,
                this.xrRichBody1,
                this.xrRichBody2,
                this.xrRichBody3,
                this.xrLabelAttachments
            });
            this.Detail.HeightF = 640F;
        }

        #endregion

        private DevExpress.XtraReports.UI.XRLabel    xrLabelRecipient;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelUrgency;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelGreeting;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody1;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody2;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody3;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelAttachments;
    }
}
