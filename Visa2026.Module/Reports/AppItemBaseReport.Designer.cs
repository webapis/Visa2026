namespace Visa2026.Module.Reports
{
    partial class AppItemBaseReport
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
            this.TopMargin              = new DevExpress.XtraReports.UI.TopMarginBand();
            this.PageHeader             = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrLabelAppNumber       = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelAppDate         = new DevExpress.XtraReports.UI.XRLabel();
            this.Detail                 = new DevExpress.XtraReports.UI.DetailBand();
            this.ReportFooter           = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.xrLabelSignatoryPosition = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelSignatoryFullName = new DevExpress.XtraReports.UI.XRLabel();
            this.BottomMargin           = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.AppItemDataSource      = new DevExpress.Persistent.Base.ReportsV2.CollectionDataSource();
            ((System.ComponentModel.ISupportInitialize)(this.AppItemDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            //
            // TopMargin
            //
            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name = "TopMargin";
            //
            // PageHeader — application number and date, no background
            //
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
                this.xrLabelAppNumber,
                this.xrLabelAppDate
            });
            this.PageHeader.HeightF = 40F;
            this.PageHeader.Name = "PageHeader";
            //
            // xrLabelAppNumber — bound to flattened Application_FullNumber on ApplicationItem
            //
            this.xrLabelAppNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Application_FullNumber]")
            });
            this.xrLabelAppNumber.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelAppNumber.LocationFloat = new DevExpress.Utils.PointFloat(486F, 5F);
            this.xrLabelAppNumber.Name = "xrLabelAppNumber";
            this.xrLabelAppNumber.SizeF = new System.Drawing.SizeF(300F, 20F);
            this.xrLabelAppNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            //
            // xrLabelAppDate — bound to flattened Application_DateText on ApplicationItem
            //
            this.xrLabelAppDate.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Application_DateText]")
            });
            this.xrLabelAppDate.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F);
            this.xrLabelAppDate.LocationFloat = new DevExpress.Utils.PointFloat(486F, 18F);
            this.xrLabelAppDate.Name = "xrLabelAppDate";
            this.xrLabelAppDate.SizeF = new System.Drawing.SizeF(300F, 20F);
            this.xrLabelAppDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
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
            this.ReportFooter.PrintAtBottom = false;
            //
            // xrLabelSignatoryPosition — CompanyHead_PositionTm (NotMapped on ApplicationItem)
            //
            this.xrLabelSignatoryPosition.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead_PositionTm]")
            });
            this.xrLabelSignatoryPosition.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelSignatoryPosition.LocationFloat = new DevExpress.Utils.PointFloat(0F, 15F);
            this.xrLabelSignatoryPosition.Name = "xrLabelSignatoryPosition";
            this.xrLabelSignatoryPosition.SizeF = new System.Drawing.SizeF(393F, 20F);
            this.xrLabelSignatoryPosition.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            //
            // xrLabelSignatoryFullName — CompanyHead_FullName (NotMapped on ApplicationItem)
            //
            this.xrLabelSignatoryFullName.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
                new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead_FullName]")
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
            // AppItemDataSource
            //
            this.AppItemDataSource.Name = "AppItemDataSource";
            this.AppItemDataSource.ObjectTypeName = "Visa2026.Module.BusinessObjects.ApplicationItem";
            this.AppItemDataSource.TopReturnedRecords = 0;
            //
            // AppItemBaseReport — A4 Portrait, no background
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
                this.AppItemDataSource
            });
            this.DataSource = this.AppItemDataSource;
            this.Landscape = false;
            this.Margins = new DevExpress.Drawing.DXMargins(20F, 20F, 50F, 60F);
            this.PageHeightF = 1169.291F;
            this.PageWidthF = 826.7717F;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Version = "25.2";
            ((System.ComponentModel.ISupportInitialize)(this.AppItemDataSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        #endregion

        protected DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        protected DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        protected DevExpress.XtraReports.UI.DetailBand Detail;
        protected DevExpress.XtraReports.UI.ReportFooterBand ReportFooter;
        protected DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        protected DevExpress.XtraReports.UI.XRLabel xrLabelAppNumber;
        protected DevExpress.XtraReports.UI.XRLabel xrLabelAppDate;
        protected DevExpress.XtraReports.UI.XRLabel xrLabelSignatoryPosition;
        protected DevExpress.XtraReports.UI.XRLabel xrLabelSignatoryFullName;
        private DevExpress.Persistent.Base.ReportsV2.CollectionDataSource AppItemDataSource;
    }
}
