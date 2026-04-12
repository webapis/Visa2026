using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppGroupABaseReport
    {
        private void InitializeComponent()
        {
            this.xrLabelRecipient = new XRLabel();
            this.xrLabelUrgency = new XRLabel();
            this.xrLabelGreeting = new XRLabel();
            this.xrRichBody1 = new XRRichText();
            this.xrRichBody2 = new XRRichText();
            this.xrRichBody3 = new XRRichText();
            this.xrLabelAttachments = new XRLabel();

            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).BeginInit();

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
            // xrRichBody1 (Standard Description - §20B)
            // 
            this.xrRichBody1.Font = new DXFont("Times New Roman", 15F);
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 200F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 140F);
            this.xrRichBody1.CanGrow = true;
            this.xrRichBody1.CanShrink = true;
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            // Standard RTF with [ProjectContract_Description] placeholder
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 [ProjectContract_Description]\par}";

            // 
            // xrRichBody2 (Request Sentence - Specific to Derived Reports)
            // 
            this.xrRichBody2.Font = new DXFont("Times New Roman", 15F);
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 348F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 40F);
            this.xrRichBody2.CanGrow = true;
            this.xrRichBody2.CanShrink = true;
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders = DevExpress.XtraPrinting.BorderSide.None;

            // 
            // xrRichBody3 (Shared Responsibility Paragraph - §21)
            // 
            this.xrRichBody3.Font = new DXFont("Times New Roman", 15F);
            this.xrRichBody3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 396F);
            this.xrRichBody3.Name = "xrRichBody3";
            this.xrRichBody3.SizeF = new System.Drawing.SizeF(626.7717F, 60F);
            this.xrRichBody3.CanGrow = true;
            this.xrRichBody3.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody3.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody3.Rtf = AppBaseReport.RtfResponsibility;

            // 
            // xrLabelAttachments (§16, §20B)
            // 
            this.xrLabelAttachments.Font = new DXFont("Times New Roman", 15F);
            this.xrLabelAttachments.LocationFloat = new DevExpress.Utils.PointFloat(0F, 464F);
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
            this.Detail.HeightF = 535F; // Attachments end (524) + 11F (§20B)
            this.Detail.Controls.AddRange(new XRControl[] {
                this.xrLabelRecipient,
                this.xrLabelUrgency,
                this.xrLabelGreeting,
                this.xrRichBody1,
                this.xrRichBody2,
                this.xrRichBody3,
                this.xrLabelAttachments
            });

            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody3)).EndInit();
        }

        protected XRLabel xrLabelRecipient;
        protected XRLabel xrLabelUrgency;
        protected XRLabel xrLabelGreeting;
        protected XRRichText xrRichBody1;
        protected XRRichText xrRichBody2;
        protected XRRichText xrRichBody3;
        protected XRLabel xrLabelAttachments;
    }
}