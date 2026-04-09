namespace Visa2026.Module.Reports
{
    partial class AppInvAndWPReport
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
            // xrLabelRecipient — Ministry recipient block. Standard §14.
            // H=80F (CanGrow+CanShrink): covers 3 lines; collapses unused space. Ends at Y=100F.
            //
            this.xrLabelRecipient.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ProjectContract_Ministry_RecipientBlock]")
            });
            this.xrLabelRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.CanShrink = true;
            this.xrLabelRecipient.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(220F, 20F);
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(406.7717F, 80F);
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelRecipient.WordWrap = true;
            //
            // xrLabelUrgency — Italic urgency line. Spacing §20B: Y=110F (10F after recipient end 100F).
            //
            this.xrLabelUrgency.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",    "[Urgency_NameTm]"),
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Visible", "[ApplicationType.ShowUrgency]")
            });
            this.xrLabelUrgency.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelUrgency.CanGrow = true;
            this.xrLabelUrgency.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelUrgency.LocationFloat = new DevExpress.Utils.PointFloat(0F, 110F);
            this.xrLabelUrgency.Name = "xrLabelUrgency";
            this.xrLabelUrgency.SizeF = new System.Drawing.SizeF(300F, 25F);
            this.xrLabelUrgency.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            //
            // xrLabelGreeting — Bold centered salutation. Standard §19 / Spacing §20B.
            // Y=150F: 15F after urgency end (110+25=135). Ends at 185F.
            //
            this.xrLabelGreeting.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ProjectContract_Ministry_FormOfAddress]")
            });
            this.xrLabelGreeting.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelGreeting.CanGrow = true;
            this.xrLabelGreeting.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelGreeting.LocationFloat = new DevExpress.Utils.PointFloat(0F, 150F);
            this.xrLabelGreeting.Name = "xrLabelGreeting";
            this.xrLabelGreeting.SizeF = new System.Drawing.SizeF(626.7717F, 35F);
            this.xrLabelGreeting.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelGreeting.WordWrap = true;
            //
            // xrRichBody1 — Contract reference paragraph. Spacing §20B: Y=200F (15F after greeting end 185F).
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 200F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 140F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 [ProjectContract_Description]\par}";
            //
            // xrRichBody2 — Invitation + WP request paragraph. Y=348F (8F after body1 end 200+140=340).
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 348F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 100F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen T\u252?rki\u253?e Respublikasyny\u328? \u8220?[Company.Name]\u8221? kompani\u253?asyna degi\u351?li bolan sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna \b [VisaPeriod_NameTm] m\u246?hlet\b0  bilen \b [VisaCategory_NameTm] \u231?akylyk we i\u351? rugsatnamasyny\b0  resmile\u351?dirilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody3 — Static responsibility paragraph. Y=456F (8F after body2 end 348+100=448).
            //
            this.xrRichBody3.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody3.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody3.CanGrow   = true;
            this.xrRichBody3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 456F);
            this.xrRichBody3.Name = "xrRichBody3";
            this.xrRichBody3.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).EndInit();
            this.xrRichBody3.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
            //
            // xrLabelAttachments — Two-line list. Y=534F (8F after body3 end 456+70=526).
            //
            this.xrLabelAttachments.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                    "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'")
            });
            this.xrLabelAttachments.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelAttachments.CanGrow = true;
            this.xrLabelAttachments.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            this.xrLabelAttachments.LocationFloat = new DevExpress.Utils.PointFloat(0F, 534F);
            this.xrLabelAttachments.Multiline = true;
            this.xrLabelAttachments.Name = "xrLabelAttachments";
            this.xrLabelAttachments.SizeF = new System.Drawing.SizeF(626.7717F, 60F);
            this.xrLabelAttachments.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelAttachments.WordWrap = true;
            //
            // Detail — HeightF = 605F: attachments end (534+60=594) + 11F. Standard §20.
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
            this.Detail.HeightF = 605F;
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
