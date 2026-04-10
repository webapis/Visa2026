using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppGroupDBaseReport
    {
        private void InitializeComponent()
        {
            this.xrLabelRecipient = new XRLabel();
            this.xrRichBody1 = new XRRichText();
            this.xrRichBody2 = new XRRichText();

            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).BeginInit();

            // 
            // xrLabelRecipient (§14, §21)
            // Fixed: national State Migration Service head.
            // Y=217F, H=65F ensures end at 282F for a 15F gap to Body1.
            // 
            this.xrLabelRecipient.BackColor = System.Drawing.Color.Transparent;
            this.xrLabelRecipient.CanGrow = true;
            this.xrLabelRecipient.CanShrink = true;
            this.xrLabelRecipient.Font = new DXFont("Times New Roman", 15F, DXFontStyle.Bold);
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(220F, 217F);
            this.xrLabelRecipient.Multiline = true;
            this.xrLabelRecipient.Name = "xrLabelRecipient";
            this.xrLabelRecipient.SizeF = new System.Drawing.SizeF(406.7717F, 65F);
            this.xrLabelRecipient.Text = "Türkmenistanyň Döwlet migrasiýa gullugynyň başlygyna";
            this.xrLabelRecipient.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelRecipient.WordWrap = true;

            // 
            // xrRichBody1 (Derived request paragraph - §21)
            // Y=297F (15F after recipient end 282F).
            // 
            this.xrRichBody1.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody1.CanGrow = true;
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 297F);
            this.xrRichBody1.Name = "xrRichBody1";
            this.xrRichBody1.SizeF = new System.Drawing.SizeF(626.7717F, 80F);

            // 
            // xrRichBody2 (Shared Responsibility Paragraph - §21)
            // Y=385F (8F after body1 end 297+80=377F).
            // 
            this.xrRichBody2.BackColor = System.Drawing.Color.Transparent;
            this.xrRichBody2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody2.CanGrow = true;
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 385F);
            this.xrRichBody2.Name = "xrRichBody2";
            this.xrRichBody2.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
            this.xrRichBody2.Rtf = AppBaseReport.RtfResponsibility;

            // 
            // Detail (§20, §21)
            // HeightF=492F provides vertical centering relative to background.
            // 
            this.Detail.Controls.AddRange(new XRControl[] {
                this.xrLabelRecipient,
                this.xrRichBody1,
                this.xrRichBody2
            });
            this.Detail.HeightF = 492F;

            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody2)).EndInit();
        }

        protected XRLabel xrLabelRecipient;
        protected XRRichText xrRichBody1;
        protected XRRichText xrRichBody2;
    }
}