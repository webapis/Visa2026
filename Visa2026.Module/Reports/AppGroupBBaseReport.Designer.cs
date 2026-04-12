using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppGroupBBaseReport
    {
        private void InitializeComponent()
        {
            this.xrLabelRecipient = new XRLabel();
            this.xrLabelUrgency = new XRLabel();
            this.xrLabelGreeting = new XRLabel();
            this.xrRichBody1 = new XRRichText();
            this.xrRichBody2 = new XRRichText();
            this.xrRichBody3 = new XRRichText();
            this.xrRichBody4 = new XRRichText();
            this.xrLabelAttachments = new XRLabel();

            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody4)).BeginInit();

            // 
            // xrLabelRecipient (§14)
            // 
            this.xrLabelRecipient.Font = new DXFont("Times New Roman", 15F, DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(220F, 20F);
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(406.7717F, 80F);
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.CanShrink = true;
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.WordWrap = true;
            this.xrLabelRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelRecipient.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[ProjectContract_Ministry_RecipientBlock]"));

            // 
            // xrLabelUrgency (§15, §20B)
            // 
            this.xrLabelUrgency.Font = new DXFont("Times New Roman", 15F, DXFontStyle.Bold);
            this.xrLabelUrgency.LocationFloat = new DevExpress.Utils.PointFloat(0F, 110F);
            this.xrLabelUrgency.Name = "xrLabelUrgency";
            this.xrLabelUrgency.SizeF = new System.Drawing.SizeF(626.7717F, 25F);
            this.xrLabelUrgency.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabelUrgency.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelUrgency.ExpressionBindings.AddRange(new ExpressionBinding[] {
                new ExpressionBinding("BeforePrint", "Text", "[Urgency_NameTm]"),
                new ExpressionBinding("BeforePrint", "Visible", "[ApplicationType.ShowUrgency]")
            });

            // 
            // xrLabelGreeting (§19, §20B)
            // 
            this.xrLabelGreeting.Font = new DXFont("Times New Roman", 15F, DXFontStyle.Bold);
            this.xrLabelGreeting.LocationFloat = new DevExpress.Utils.PointFloat(0F, 150F);
            this.xrLabelGreeting.Name = "xrLabelGreeting";
            this.xrLabelGreeting.SizeF = new System.Drawing.SizeF(626.7717F, 35F);
            this.xrLabelGreeting.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelGreeting.CanGrow = true;
            this.xrLabelGreeting.WordWrap = true;
            this.xrLabelGreeting.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelGreeting.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[ProjectContract_Ministry_FormOfAddress]"));

            // 
            // xrRichBody1 (Berkarar Intro - §21)
            // 
            this.xrRichBody1.Font = new DXFont("Times New Roman", 15F);
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 200F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            this.xrRichBody1.CanGrow = true;
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Berkarar d\u246?wletimizi\u328? bagty\u253?arlyk d\u246?wr\u252?nde \b Hormatly Prezidentimizi\u328?\b0  t\u228?\u253?syz tagallalary netijesinde \u253?urdumyzy\u328? elektroenergetika pudagynda birn\u228?\u231?e iri taslamalar durmu\u351?a ge\u231?iril\u253?\u228?r.\par}";

            // 
            // xrRichBody2 (Company Partnership - §21)
            // 
            this.xrRichBody2.Font = new DXFont("Times New Roman", 15F);
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 278F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            this.xrRichBody2.CanGrow = true;
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 \u350?unu\u328? bilen baglylykda, elektroenergetika pudagyny k\u246?p \u253?yllardan b\u228?ri hyzmatda\u351?y bolup gel\u253?\u228?n \ldblquote [Company.Name]\rdblquote  kompani\u253?asy tarapyndan birn\u228?\u231?e taslamalar amala a\u351?yrl\u253?ar.\par}";

            // 
            // xrRichBody3 (Request Sentence - Specific to Derived Reports)
            // 
            this.xrRichBody3.Font = new DXFont("Times New Roman", 15F);
            this.xrRichBody3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 356F);
            this.xrRichBody3.Name = "xrRichBody3";
            this.xrRichBody3.SizeF = new System.Drawing.SizeF(626.7717F, 120F);
            this.xrRichBody3.CanGrow = true;
            this.xrRichBody3.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody3.Borders = DevExpress.XtraPrinting.BorderSide.None;

            // 
            // xrRichBody4 (Shared Responsibility Paragraph - §21)
            // 
            this.xrRichBody4.Font = new DXFont("Times New Roman", 15F);
            this.xrRichBody4.LocationFloat = new DevExpress.Utils.PointFloat(0F, 484F);
            this.xrRichBody4.Name = "xrRichBody4";
            this.xrRichBody4.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            this.xrRichBody4.CanGrow = true;
            this.xrRichBody4.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody4.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody4.Rtf = AppBaseReport.RtfResponsibility;

            // 
            // xrLabelAttachments (§16, §20B)
            // 
            this.xrLabelAttachments.Font = new DXFont("Times New Roman", 15F);
            this.xrLabelAttachments.LocationFloat = new DevExpress.Utils.PointFloat(0F, 562F);
            this.xrLabelAttachments.Name = "xrLabelAttachments";
            this.xrLabelAttachments.SizeF = new System.Drawing.SizeF(626.7717F, 60F);
            this.xrLabelAttachments.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelAttachments.CanGrow = true;
            this.xrLabelAttachments.Multiline = true;
            this.xrLabelAttachments.WordWrap = true;
            this.xrLabelAttachments.BackColor = System.Drawing.Color.Transparent;

            // 
            // Band Setup
            // 
            this.Detail.HeightF = 633F; // Attachments end (622) + 11F (§20B)
            this.Detail.Controls.AddRange(new XRControl[] {
                this.xrLabelRecipient,
                this.xrLabelUrgency,
                this.xrLabelGreeting,
                this.xrRichBody1,
                this.xrRichBody2,
                this.xrRichBody3,
                this.xrRichBody4,
                this.xrLabelAttachments
            });

            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody4)).EndInit();
        }

        protected XRLabel xrLabelRecipient;
        protected XRLabel xrLabelUrgency;
        protected XRLabel xrLabelGreeting;
        protected XRRichText xrRichBody1;
        protected XRRichText xrRichBody2;
        protected XRRichText xrRichBody3;
        protected XRRichText xrRichBody4;
        protected XRLabel xrLabelAttachments;
    }
}