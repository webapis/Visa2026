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
            // xrRichBody1 — Berkarar intro paragraph. Spacing §20B: Y=200F (15F after greeting end 185F).
            //
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow   = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 200F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Berkarar d\u246?wletimizi\u328? bagty\u253?arlyk d\u246?wr\u252?nde \b Hormatly Prezidentimizi\u328?\b0  t\u228?\u253?syz tagallalary netijesinde \u253?urdumyzy\u328? elektroenergetika pudagynda birn\u228?\u231?e iri taslamalar durmu\u351?a ge\u231?iril\u253?\u228?r.\par}";
            //
            // xrRichBody2 — Company partnership paragraph. Y=278F (8F after body1 end 200+70=270).
            //
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow   = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 278F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 \u350?unu\u328? bilen baglylykda, elektroenergetika pudagyny k\u246?p \u253?yllardan b\u228?ri hyzmatda\u351?y bolup gel\u253?\u228?n [Company.Name] kompani\u253?asy tarapyndan birn\u228?\u231?e taslamalar amala a\u351?yrl\u253?ar.\par}";
            //
            // xrRichBody3 — FM invitation request paragraph. Y=356F (8F after body2 end 278+70=348).
            //
            this.xrRichBody3.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody3.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody3.CanGrow   = true;
            this.xrRichBody3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 356F);
            this.xrRichBody3.Name = "xrRichBody3";
            this.xrRichBody3.SizeF = new System.Drawing.SizeF(626.7717F, 120F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).EndInit();
            this.xrRichBody3.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 T\u252?rkmenistandaky \u231?\u228?klerinde amala a\u351?yrl\u253?an taslamalar utga\u351?dyrmak bo\u253?un\u231?a [Company.Name] kompani\u253?asyna degi\u351?li h\u252?n\u228?rmeni\u328? ma\u351?gala agzalaryna \u253?agny, hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna [FamilyMember_Relationship_NameTm] (\b [SponsoringEmployee_FullName] - [SponsoringEmployee_PositionTm]\b0 ) \b [VisaPeriod_NameTm] m\u246?hlet\b0  bilen \b [VisaCategory_NameTm]\b0  \u231?akylyk resmile\u351?dirilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";
            //
            // xrRichBody4 — Static responsibility paragraph. Y=484F (8F after body3 end 356+120=476).
            //
            this.xrRichBody4.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody4.Borders   = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody4.CanGrow   = true;
            this.xrRichBody4.LocationFloat = new DevExpress.Utils.PointFloat(0F, 484F);
            this.xrRichBody4.Name = "xrRichBody4";
            this.xrRichBody4.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody4)).EndInit();
            this.xrRichBody4.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Da\u351?ary \u253?urt ra\u253?atyny\u328? T\u252?rkmenistana gelmegini\u328?, onda bolmagyny\u328? we ondan gitmegini\u328? d\u252?zg\u252?nlerini berja\u253? etmegine jogapk\u228?r\u231?iligi kompani\u253?amyz \u246?z \u252?st\u252?ne al\u253?ar.\par}";
            //
            // xrLabelAttachments — Two-line list. Y=562F (8F after body4 end 484+70=554).
            //
            this.xrLabelAttachments.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                    "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'")
            });
            this.xrLabelAttachments.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelAttachments.CanGrow = true;
            this.xrLabelAttachments.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F);
            this.xrLabelAttachments.LocationFloat = new DevExpress.Utils.PointFloat(0F, 562F);
            this.xrLabelAttachments.Multiline = true;
            this.xrLabelAttachments.Name = "xrLabelAttachments";
            this.xrLabelAttachments.SizeF = new System.Drawing.SizeF(626.7717F, 60F);
            this.xrLabelAttachments.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelAttachments.WordWrap = true;
            //
            // Detail — HeightF = 633F: attachments end (562+60=622) + 11F. Standard §20.
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
            this.Detail.HeightF = 633F;
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
