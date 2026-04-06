namespace Visa2026.Module.Reports
{
    partial class AppBaseReport
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
            this.TopMargin        = new DevExpress.XtraReports.UI.TopMarginBand();
            this.PageHeader       = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrPictureBoxBackground = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrLabelAppNumber = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelAppDate   = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyName = new DevExpress.XtraReports.UI.XRLabel();
            this.Detail           = new DevExpress.XtraReports.UI.DetailBand();
            this.ReportFooter     = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.xrLabelSignatoryPosition = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelSignatoryFullName = new DevExpress.XtraReports.UI.XRLabel();
            this.BottomMargin     = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.AppDataSource    = new DevExpress.Persistent.Base.ReportsV2.CollectionDataSource();
            ((System.ComponentModel.ISupportInitialize)(this.AppDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            //
            // TopMargin
            //
            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name = "TopMargin";
            //
            // PageHeader
            //
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrPictureBoxBackground,
                this.xrLabelAppNumber,
                this.xrLabelAppDate,
                this.xrLabelCompanyName
            });
            this.PageHeader.HeightF = 150F;
            this.PageHeader.Name = "PageHeader";
            //
            // xrPictureBoxBackground — full-width letterhead background; ZIndex=0 so labels render on top
            //
            this.xrPictureBoxBackground.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBoxBackground.Name = "xrPictureBoxBackground";
            this.xrPictureBoxBackground.SizeF = new System.Drawing.SizeF(786.7717F, 150F);
            //
            // xrLabelAppNumber — application number, right-aligned
            //
            this.xrLabelAppNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullApplicationNumber]")
            });
            this.xrLabelAppNumber.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelAppNumber.LocationFloat = new DevExpress.Utils.PointFloat(486F, 80F);
            this.xrLabelAppNumber.Name = "xrLabelAppNumber";
            this.xrLabelAppNumber.SizeF = new System.Drawing.SizeF(300F, 20F);
            this.xrLabelAppNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            //
            // xrLabelAppDate — application date formatted dd.MM.yyyy, right-aligned
            //
            this.xrLabelAppDate.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ApplicationDate]")
            });
            this.xrLabelAppDate.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelAppDate.LocationFloat = new DevExpress.Utils.PointFloat(486F, 102F);
            this.xrLabelAppDate.Name = "xrLabelAppDate";
            this.xrLabelAppDate.SizeF = new System.Drawing.SizeF(300F, 20F);
            this.xrLabelAppDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabelAppDate.TextFormatString = "{0:dd.MM.yyyy}";
            //
            // xrLabelCompanyName — sponsor company name, right-aligned
            //
            this.xrLabelCompanyName.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Company.Name]")
            });
            this.xrLabelCompanyName.CanGrow = true;
            this.xrLabelCompanyName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelCompanyName.LocationFloat = new DevExpress.Utils.PointFloat(486F, 110F);
            this.xrLabelCompanyName.Multiline = true;
            this.xrLabelCompanyName.Name = "xrLabelCompanyName";
            this.xrLabelCompanyName.SizeF = new System.Drawing.SizeF(300F, 35F);
            this.xrLabelCompanyName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabelCompanyName.WordWrap = true;
            //
            // Detail — empty; derived reports add their content here and resize as needed
            //
            this.Detail.HeightF = 10F;
            this.Detail.Name = "Detail";
            //
            // ReportFooter — signatory block
            //
            this.ReportFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelSignatoryPosition,
                this.xrLabelSignatoryFullName
            });
            this.ReportFooter.HeightF = 50F;
            this.ReportFooter.Name = "ReportFooter";
            //
            // xrLabelSignatoryPosition — CompanyHead position, left-aligned
            //
            this.xrLabelSignatoryPosition.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead.Position.NameTm]")
            });
            this.xrLabelSignatoryPosition.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelSignatoryPosition.LocationFloat = new DevExpress.Utils.PointFloat(0F, 15F);
            this.xrLabelSignatoryPosition.Name = "xrLabelSignatoryPosition";
            this.xrLabelSignatoryPosition.SizeF = new System.Drawing.SizeF(393F, 20F);
            this.xrLabelSignatoryPosition.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            //
            // xrLabelSignatoryFullName — CompanyHead full name, right-aligned
            //
            this.xrLabelSignatoryFullName.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead.FullName]")
            });
            this.xrLabelSignatoryFullName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelSignatoryFullName.LocationFloat = new DevExpress.Utils.PointFloat(393F, 15F);
            this.xrLabelSignatoryFullName.Name = "xrLabelSignatoryFullName";
            this.xrLabelSignatoryFullName.SizeF = new System.Drawing.SizeF(393F, 20F);
            this.xrLabelSignatoryFullName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            //
            // BottomMargin
            //
            this.BottomMargin.HeightF = 60F;
            this.BottomMargin.Name = "BottomMargin";
            //
            // AppDataSource
            //
            this.AppDataSource.Name = "AppDataSource";
            this.AppDataSource.ObjectTypeName = "Visa2026.Module.BusinessObjects.Application";
            this.AppDataSource.TopReturnedRecords = 0;
            //
            // AppBaseReport — A4 Portrait
            //
            this.BackColor = System.Drawing.Color.White;
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
                this.TopMargin,
                this.PageHeader,
                this.Detail,
                this.ReportFooter,
                this.BottomMargin
            });
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
                this.AppDataSource
            });
            this.DataSource = this.AppDataSource;
            this.Landscape = false;
            this.Margins = new DevExpress.Drawing.DXMargins(20F, 20F, 50F, 60F);
            this.PageHeightF = 1169.291F;
            this.PageWidthF = 826.7717F;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Version = "25.2";
            ((System.ComponentModel.ISupportInitialize)(this.AppDataSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        #endregion

        // Bands — protected so derived class Designer.cs can add controls to Detail
        protected DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        protected DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        protected DevExpress.XtraReports.UI.DetailBand Detail;
        protected DevExpress.XtraReports.UI.ReportFooterBand ReportFooter;
        protected DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        // Background picture box — protected so derived reports can override the image
        protected DevExpress.XtraReports.UI.XRPictureBox xrPictureBoxBackground;
        // Header labels — private, shared across all Application reports
        private DevExpress.XtraReports.UI.XRLabel xrLabelAppNumber;
        private DevExpress.XtraReports.UI.XRLabel xrLabelAppDate;
        private DevExpress.XtraReports.UI.XRLabel xrLabelCompanyName;
        // Footer labels — private, shared across all Application reports
        private DevExpress.XtraReports.UI.XRLabel xrLabelSignatoryPosition;
        private DevExpress.XtraReports.UI.XRLabel xrLabelSignatoryFullName;
        private DevExpress.Persistent.Base.ReportsV2.CollectionDataSource AppDataSource;
    }
}
