namespace Visa2026.Module.Reports
{
    partial class AppInvFMReport
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
            this.xrRichBody4        = new DevExpress.XtraReports.UI.XRRichText();
            this.xrLabelAttachments = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody4)).BeginInit();
            //
            // xrLabelRecipient — Ministry recipient block, wider right area, left-aligned.
            // Plain text from Ministry.RecipientBlock. Wider + left-aligned so multi-line wraps naturally.
            //
            this.xrLabelRecipient.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ProjectContract_Ministry_RecipientBlock]")
            });
            this.xrLabelRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(220F, 20F);
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(406.7717F, 120F);
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
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
            // Plain text from Ministry.FormOfAddress.
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
            // xrRichBody1 — Intro paragraph 1: Berkarar döwlet context.
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            // Bold applied to "Hormatly Prezidentimiziň".
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 230F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Berkarar d\u246?wletimizi\u328? bagty\u253?arlyk d\u246?wr\u252?nde \b Hormatly Prezidentimizi\u328?\b0  t\u228?\u253?syz tagallalary netijesinde \u253?urdumyzy\u328? elektroenergetika pudagynda birn\u228?\u231?e iri taslamalar durmu\u351?a ge\u231?iril\u253?\u228?r.\par}";
            //
            // xrRichBody2 — Intro paragraph 2: Company partnership context, [Company.Name] inline.
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 308F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 \u350?unu\u328? bilen baglylykda, elektroenergetika pudagyny k\u246?p \u253?yllardan b\u228?ri hyzmatda\u351?y bolup gel\u253?\u228?n [Company.Name] kompani\u253?asy tarapyndan birn\u228?\u231?e taslamalar amala a\u351?yrl\u253?ar.\par}";
            //
            // xrRichBody3 — Request paragraph: FM count + relationship + sponsoring employee + visa period/category.
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            // Bold: person count, sponsoring employee name+position, visa period, visa category.
            // Dynamic fields: [Company.Name], [TotalPersonCount], [TotalPersonCountText],
            //   [FamilyMember_Relationship_NameTm], [SponsoringEmployee_FullName],
            //   [SponsoringEmployee_PositionTm], [VisaPeriod_NameTm], [VisaCategory_NameTm].
            //
            this.xrRichBody3.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody3.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody3.CanGrow   = true;
            this.xrRichBody3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 386F);
            this.xrRichBody3.Name = "xrRichBody3";
            this.xrRichBody3.SizeF = new System.Drawing.SizeF(626.7717F, 120F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).EndInit();
            this.xrRichBody3.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 T\u252?rkmenistandaky \u231?\u228?klerinde amala a\u351?yrl\u253?an taslamalar utga\u351?dyrmak bo\u253?un\u231?a [Company.Name] kompani\u253?asyna degi\u351?li h\u252?n\u228?rmeni\u328? ma\u351?gala agzalaryna \u253?agny, hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna [FamilyMember_Relationship_NameTm] (\b [SponsoringEmployee_FullName] - [SponsoringEmployee_PositionTm]\b0 ) \b [VisaPeriod_NameTm] m\u246?hlet\b0  bilen \b [VisaCategory_NameTm]\b0  \u231?akylyk resmile\u351?dirilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody4 — Static responsibility paragraph (same as AppInvReport).
            // Font: Times New Roman 15pt | Justified | First-line indent 0.5 inch.
            //
            this.xrRichBody4.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody4.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody4.CanGrow   = true;
            this.xrRichBody4.LocationFloat = new DevExpress.Utils.PointFloat(0F, 514F);
            this.xrRichBody4.Name = "xrRichBody4";
            this.xrRichBody4.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody4)).EndInit();
            this.xrRichBody4.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
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
            this.xrLabelAttachments.LocationFloat = new DevExpress.Utils.PointFloat(0F, 592F);
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
                this.xrRichBody4,
                this.xrLabelAttachments
            });
            this.Detail.HeightF = 670F;
        }

        #endregion

        private DevExpress.XtraReports.UI.XRLabel    xrLabelRecipient;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelUrgency;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelGreeting;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody1;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody2;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody3;
        private DevExpress.XtraReports.UI.XRRichText xrRichBody4;
        private DevExpress.XtraReports.UI.XRLabel    xrLabelAttachments;
    }
}
