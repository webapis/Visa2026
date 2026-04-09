namespace Visa2026.Module.Reports
{
    partial class AppAdditionalWPLocationReport
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
            this.xrLabelRecipient    = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelGreeting     = new DevExpress.XtraReports.UI.XRLabel();
            this.xrRichBody1         = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody2         = new DevExpress.XtraReports.UI.XRRichText();
            this.xrRichBody3         = new DevExpress.XtraReports.UI.XRRichText();
            this.xrLabelAttachments  = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).BeginInit();
            //
            // xrLabelRecipient — Ministry recipient block.
            // XRLabel + Text binding (standard §14). RecipientBlock plain text stored on Ministry BO.
            // Bold, TopLeft, X=220F, W=406.77F (right two-thirds of page).
            // H=80F — covers 3 lines at 15pt. CanGrow handles longer texts; CanShrink collapses unused space.
            // Y=20F — fixed, does not move.
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
            // xrLabelGreeting — Salutation line (e.g. "Hormatly Durdy Batjanowiç!")
            // Bold, MiddleCenter, full width — standard §19.
            // Y=115F — 15F gap after recipient end (20+80=100). Recipient→Greeting = 15F (~4mm).
            //
            this.xrLabelGreeting.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ProjectContract_Ministry_FormOfAddress]")
            });
            this.xrLabelGreeting.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelGreeting.CanGrow = true;
            this.xrLabelGreeting.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelGreeting.LocationFloat = new DevExpress.Utils.PointFloat(0F, 115F);
            this.xrLabelGreeting.Name = "xrLabelGreeting";
            this.xrLabelGreeting.SizeF = new System.Drawing.SizeF(626.7717F, 35F);
            this.xrLabelGreeting.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelGreeting.WordWrap = true;
            //
            // xrRichBody1 — Contract reference paragraph (ProjectContract.Description plain text).
            // [ProjectContract_Description] evaluated by XtraReports at render time.
            // Font: Times New Roman 15pt | Justified | First-line indent: 0.5 inch (\fi720).
            // Y=165F — 15F gap after greeting end (115+35=150). Greeting→Body1 = 15F (~4mm).
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 165F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 140F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 [ProjectContract_Description]\par}";
            //
            // xrRichBody2 — Additional WP location request paragraph.
            // [Company.Name] inline; [TotalPersonCount] and [TotalPersonCountText] bold (person count);
            // [MovementPermitLocation_NameTm] bold (location name from lookup).
            // Font: Times New Roman 15pt | Justified | First-line indent: 0.5 inch (\fi720).
            // Y=313F — 8F gap after body1 end (165+140=305). Body→Body = 8F (~2mm).
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 313F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 100F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 \u350?ertname esasynda, \u246?\u328?de go\u253?lan wezipeleri \u253?etinlikli durmu\u351?a ge\u231?irmek \u252?\u231?in hatymyzy\u328? go\u351?undysynda g\u246?rkezilen \ldblquote [Company.Name]\rdblquote  kompani\u253?asyna degi\u351?li bolan \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna \b [MovementPermitLocation_NameTm]\b0  i\u351? rugsatnamalaryný\u328? berilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody3 — Static responsibility paragraph.
            // Font: Times New Roman 15pt | Justified | First-line indent: 0.5 inch (\fi720).
            // Y=421F — 8F gap after body2 end (313+100=413). Body→Body = 8F (~2mm).
            //
            this.xrRichBody3.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody3.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody3.CanGrow   = true;
            this.xrRichBody3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 421F);
            this.xrRichBody3.Name = "xrRichBody3";
            this.xrRichBody3.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).EndInit();
            this.xrRichBody3.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
            //
            // xrLabelAttachments — Two-line attachment list with dynamic count.
            // XRLabel + Char(10) expression — standard §16.
            // Y=509F — 8F gap after body3 end (421+80=501). Body→Attachments = 8F (~2mm).
            //
            this.xrLabelAttachments.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                    "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'")
            });
            this.xrLabelAttachments.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelAttachments.CanGrow = true;
            this.xrLabelAttachments.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            this.xrLabelAttachments.LocationFloat = new DevExpress.Utils.PointFloat(0F, 509F);
            this.xrLabelAttachments.Multiline = true;
            this.xrLabelAttachments.Name = "xrLabelAttachments";
            this.xrLabelAttachments.SizeF = new System.Drawing.SizeF(626.7717F, 60F);
            this.xrLabelAttachments.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelAttachments.WordWrap = true;
            //
            // Detail
            // HeightF = 580F: attachments end (509+60=569) + 11F before ReportFooter signatory Y offset (10F).
            // ReportFooter.PrintAtBottom = false, so signatory renders immediately after Detail.
            //
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelRecipient,
                this.xrLabelGreeting,
                this.xrRichBody1,
                this.xrRichBody2,
                this.xrRichBody3,
                this.xrLabelAttachments
            });
            this.Detail.HeightF = 580F;
        }

        #endregion

        private DevExpress.XtraReports.UI.XRLabel    xrLabelRecipient;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelGreeting;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody1;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody2;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody3;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelAttachments;
    }
}
