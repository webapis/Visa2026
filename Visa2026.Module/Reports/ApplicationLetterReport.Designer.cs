namespace Visa2026.Module.Reports
{
    partial class ApplicationLetterReport
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrPictureBoxBackground = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrPictureBoxBackgroundTop = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrPictureBoxBackgroundBottom = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrLblApplicationContent = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblCurrentStatus = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblStatusLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblApplicationType = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblApplicationTypeLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblCompanyName = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblCompanyNameLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.xrLblApplicationDate = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblApplicationDateLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblApplicationNumber = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblApplicationNumberLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblHeaderTitle = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblSubHeading = new DevExpress.XtraReports.UI.XRLabel();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.xrLblFooterCompanyAddress = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblFooterContactInfo = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblSignatureLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLblPlaceForSignature = new DevExpress.XtraReports.UI.XRLabel();
            this.AppColDataSource = new DevExpress.Persistent.Base.ReportsV2.CollectionDataSource();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            ((System.ComponentModel.ISupportInitialize)(this.AppColDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrPictureBoxBackground,
            this.xrLblApplicationContent,
            this.xrLblCurrentStatus,
            this.xrLblStatusLabel,
            this.xrLblApplicationType,
            this.xrLblApplicationTypeLabel,
            this.xrLblCompanyName,
            this.xrLblCompanyNameLabel});
            this.Detail.HeightF = 200F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(20F, 20F, 10F, 10F, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            
            // 
            // xrPictureBoxBackground
            // 
            this.xrPictureBoxBackground.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBoxBackground.Name = "xrPictureBoxBackground";
            this.xrPictureBoxBackground.SizeF = new System.Drawing.SizeF(650F, 650F);
            
            // 
            // xrPictureBoxBackgroundTop
            // 
            this.xrPictureBoxBackgroundTop.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBoxBackgroundTop.Name = "xrPictureBoxBackgroundTop";
            this.xrPictureBoxBackgroundTop.SizeF = new System.Drawing.SizeF(650F, 120F);
            
            // 
            // xrPictureBoxBackgroundBottom
            // 
            this.xrPictureBoxBackgroundBottom.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBoxBackgroundBottom.Name = "xrPictureBoxBackgroundBottom";
            this.xrPictureBoxBackgroundBottom.SizeF = new System.Drawing.SizeF(650F, 100F);
            
            // 
            // xrLblApplicationContent
            // 
            this.xrLblApplicationContent.LocationFloat = new DevExpress.Utils.PointFloat(45F, 140F);
            this.xrLblApplicationContent.Multiline = true;
            this.xrLblApplicationContent.Name = "xrLblApplicationContent";
            this.xrLblApplicationContent.Padding = new DevExpress.XtraPrinting.PaddingInfo(5F, 5F, 5F, 5F, 100F);
            this.xrLblApplicationContent.SizeF = new System.Drawing.SizeF(500F, 50F);
            this.xrLblApplicationContent.Text = "This letter confirms the receipt and processing of your application.";
            this.xrLblApplicationContent.WordWrap = true;
            
            // 
            // xrLblCurrentStatus
            // 
            this.xrLblCurrentStatus.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CurrentState.State]")});
            this.xrLblCurrentStatus.LocationFloat = new DevExpress.Utils.PointFloat(180F, 110F);
            this.xrLblCurrentStatus.Name = "xrLblCurrentStatus";
            this.xrLblCurrentStatus.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblCurrentStatus.SizeF = new System.Drawing.SizeF(200F, 23F);
            this.xrLblCurrentStatus.StylePriority.UseFont = false;
            this.xrLblCurrentStatus.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            
            // 
            // xrLblStatusLabel
            // 
            this.xrLblStatusLabel.LocationFloat = new DevExpress.Utils.PointFloat(45F, 110F);
            this.xrLblStatusLabel.Name = "xrLblStatusLabel";
            this.xrLblStatusLabel.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblStatusLabel.SizeF = new System.Drawing.SizeF(130F, 23F);
            this.xrLblStatusLabel.Text = "Current Status:";
            this.xrLblStatusLabel.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            
            // 
            // xrLblApplicationType
            // 
            this.xrLblApplicationType.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ApplicationType.Name]")});
            this.xrLblApplicationType.LocationFloat = new DevExpress.Utils.PointFloat(180F, 80F);
            this.xrLblApplicationType.Name = "xrLblApplicationType";
            this.xrLblApplicationType.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblApplicationType.SizeF = new System.Drawing.SizeF(200F, 23F);
            
            // 
            // xrLblApplicationTypeLabel
            // 
            this.xrLblApplicationTypeLabel.LocationFloat = new DevExpress.Utils.PointFloat(45F, 80F);
            this.xrLblApplicationTypeLabel.Name = "xrLblApplicationTypeLabel";
            this.xrLblApplicationTypeLabel.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblApplicationTypeLabel.SizeF = new System.Drawing.SizeF(130F, 23F);
            this.xrLblApplicationTypeLabel.Text = "Application Type:";
            this.xrLblApplicationTypeLabel.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            
            // 
            // xrLblCompanyName
            // 
            this.xrLblCompanyName.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Company.CompanyName]")});
            this.xrLblCompanyName.LocationFloat = new DevExpress.Utils.PointFloat(180F, 50F);
            this.xrLblCompanyName.Name = "xrLblCompanyName";
            this.xrLblCompanyName.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblCompanyName.SizeF = new System.Drawing.SizeF(200F, 23F);
            
            // 
            // xrLblCompanyNameLabel
            // 
            this.xrLblCompanyNameLabel.LocationFloat = new DevExpress.Utils.PointFloat(45F, 50F);
            this.xrLblCompanyNameLabel.Name = "xrLblCompanyNameLabel";
            this.xrLblCompanyNameLabel.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblCompanyNameLabel.SizeF = new System.Drawing.SizeF(130F, 23F);
            this.xrLblCompanyNameLabel.Text = "Company:";
            this.xrLblCompanyNameLabel.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            
            // 
            // TopMargin
            // 
            this.TopMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrPictureBoxBackgroundTop,
            this.xrLblApplicationDate,
            this.xrLblApplicationDateLabel,
            this.xrLblApplicationNumber,
            this.xrLblApplicationNumberLabel,
            this.xrLblHeaderTitle,
            this.xrLblSubHeading,
            this.xrPictureBox1});
            this.TopMargin.HeightF = 120F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(20F, 20F, 20F, 20F, 100F);
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            
            // 
            // xrLblApplicationDate
            // 
            this.xrLblApplicationDate.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "FormatString('{0:dd.MM.yyyy}', [ApplicationDate])")});
            this.xrLblApplicationDate.LocationFloat = new DevExpress.Utils.PointFloat(400F, 35F);
            this.xrLblApplicationDate.Name = "xrLblApplicationDate";
            this.xrLblApplicationDate.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblApplicationDate.SizeF = new System.Drawing.SizeF(150F, 20F);
            this.xrLblApplicationDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            
            // 
            // xrLblApplicationDateLabel
            // 
            this.xrLblApplicationDateLabel.LocationFloat = new DevExpress.Utils.PointFloat(400F, 15F);
            this.xrLblApplicationDateLabel.Name = "xrLblApplicationDateLabel";
            this.xrLblApplicationDateLabel.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblApplicationDateLabel.SizeF = new System.Drawing.SizeF(150F, 18F);
            this.xrLblApplicationDateLabel.Text = "Date:";
            this.xrLblApplicationDateLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLblApplicationDateLabel.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            
            // 
            // xrLblApplicationNumber
            // 
            this.xrLblApplicationNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullApplicationNumber]")});
            this.xrLblApplicationNumber.LocationFloat = new DevExpress.Utils.PointFloat(45F, 35F);
            this.xrLblApplicationNumber.Name = "xrLblApplicationNumber";
            this.xrLblApplicationNumber.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblApplicationNumber.SizeF = new System.Drawing.SizeF(200F, 20F);
            this.xrLblApplicationNumber.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            
            // 
            // xrLblApplicationNumberLabel
            // 
            this.xrLblApplicationNumberLabel.LocationFloat = new DevExpress.Utils.PointFloat(45F, 15F);
            this.xrLblApplicationNumberLabel.Name = "xrLblApplicationNumberLabel";
            this.xrLblApplicationNumberLabel.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblApplicationNumberLabel.SizeF = new System.Drawing.SizeF(200F, 18F);
            this.xrLblApplicationNumberLabel.Text = "Application No:";
            this.xrLblApplicationNumberLabel.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            
            // 
            // xrLblHeaderTitle
            // 
            this.xrLblHeaderTitle.LocationFloat = new DevExpress.Utils.PointFloat(75F, 60F);
            this.xrLblHeaderTitle.Multiline = true;
            this.xrLblHeaderTitle.Name = "xrLblHeaderTitle";
            this.xrLblHeaderTitle.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblHeaderTitle.SizeF = new System.Drawing.SizeF(420F, 40F);
            this.xrLblHeaderTitle.Text = "APPLICATION PROCESSING LETTER";
            this.xrLblHeaderTitle.Font = new System.Drawing.Font("Arial", 16F, System.Drawing.FontStyle.Bold);
            this.xrLblHeaderTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            
            // 
            // xrLblSubHeading
            // 
            this.xrLblSubHeading.LocationFloat = new DevExpress.Utils.PointFloat(75F, 100F);
            this.xrLblSubHeading.Name = "xrLblSubHeading";
            this.xrLblSubHeading.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblSubHeading.SizeF = new System.Drawing.SizeF(420F, 18F);
            this.xrLblSubHeading.Text = "Migration Services Division";
            this.xrLblSubHeading.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLblSubHeading.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Italic);
            
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.BorderColor = System.Drawing.Color.Black;
            this.xrPictureBox1.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(45F, 117F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(500F, 1F);
            
            // 
            // BottomMargin
            // 
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrPictureBoxBackgroundBottom,
            this.xrLblFooterCompanyAddress,
            this.xrLblFooterContactInfo,
            this.xrLblSignatureLine,
            this.xrLblPlaceForSignature});
            this.BottomMargin.HeightF = 100F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(20F, 20F, 20F, 20F, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            
            // 
            // xrLblFooterCompanyAddress
            // 
            this.xrLblFooterCompanyAddress.LocationFloat = new DevExpress.Utils.PointFloat(45F, 50F);
            this.xrLblFooterCompanyAddress.Name = "xrLblFooterCompanyAddress";
            this.xrLblFooterCompanyAddress.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblFooterCompanyAddress.SizeF = new System.Drawing.SizeF(400F, 18F);
            this.xrLblFooterCompanyAddress.Text = "Ashgabat - Turkmenistan";
            this.xrLblFooterCompanyAddress.Font = new System.Drawing.Font("Arial", 9F);
            
            // 
            // xrLblFooterContactInfo
            // 
            this.xrLblFooterContactInfo.LocationFloat = new DevExpress.Utils.PointFloat(45F, 68F);
            this.xrLblFooterContactInfo.Name = "xrLblFooterContactInfo";
            this.xrLblFooterContactInfo.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblFooterContactInfo.SizeF = new System.Drawing.SizeF(400F, 18F);
            this.xrLblFooterContactInfo.Text = "www.visa2026.tm | info@visa2026.tm";
            this.xrLblFooterContactInfo.Font = new System.Drawing.Font("Arial", 9F);
            
            // 
            // xrLblSignatureLine
            // 
            this.xrLblSignatureLine.BorderColor = System.Drawing.Color.Black;
            this.xrLblSignatureLine.Borders = DevExpress.XtraPrinting.BorderSide.Top;
            this.xrLblSignatureLine.LocationFloat = new DevExpress.Utils.PointFloat(45F, 15F);
            this.xrLblSignatureLine.Name = "xrLblSignatureLine";
            this.xrLblSignatureLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblSignatureLine.SizeF = new System.Drawing.SizeF(200F, 23F);
            
            // 
            // xrLblPlaceForSignature
            // 
            this.xrLblPlaceForSignature.LocationFloat = new DevExpress.Utils.PointFloat(45F, 0F);
            this.xrLblPlaceForSignature.Name = "xrLblPlaceForSignature";
            this.xrLblPlaceForSignature.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLblPlaceForSignature.SizeF = new System.Drawing.SizeF(200F, 18F);
            this.xrLblPlaceForSignature.Text = "Place for Signature";
            this.xrLblPlaceForSignature.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Italic);
            
            // 
            // AppColDataSource
            // 
            this.AppColDataSource.Name = "AppColDataSource";
            this.AppColDataSource.ObjectTypeName = "Visa2026.Module.BusinessObjects.Application";
            this.AppColDataSource.TopReturnedRecords = 0;
            
            // 
            // PageHeader
            // 
            this.PageHeader.Name = "PageHeader";
            this.PageHeader.HeightF = 0F;
            
            // 
            // ApplicationLetterReport
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.PageHeader,
            this.TopMargin,
            this.Detail,
            this.BottomMargin});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.AppColDataSource});
            this.DataSource = this.AppColDataSource;
            this.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Version = "25.2";
            ((System.ComponentModel.ISupportInitialize)(this.AppColDataSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBoxBackground;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBoxBackgroundTop;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBoxBackgroundBottom;
        private DevExpress.Persistent.Base.ReportsV2.CollectionDataSource AppColDataSource;
        private DevExpress.XtraReports.UI.XRLabel xrLblApplicationContent;
        private DevExpress.XtraReports.UI.XRLabel xrLblCurrentStatus;
        private DevExpress.XtraReports.UI.XRLabel xrLblStatusLabel;
        private DevExpress.XtraReports.UI.XRLabel xrLblApplicationType;
        private DevExpress.XtraReports.UI.XRLabel xrLblApplicationTypeLabel;
        private DevExpress.XtraReports.UI.XRLabel xrLblCompanyName;
        private DevExpress.XtraReports.UI.XRLabel xrLblCompanyNameLabel;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.XRLabel xrLblApplicationDate;
        private DevExpress.XtraReports.UI.XRLabel xrLblApplicationDateLabel;
        private DevExpress.XtraReports.UI.XRLabel xrLblApplicationNumber;
        private DevExpress.XtraReports.UI.XRLabel xrLblApplicationNumberLabel;
        private DevExpress.XtraReports.UI.XRLabel xrLblHeaderTitle;
        private DevExpress.XtraReports.UI.XRLabel xrLblSubHeading;
        private DevExpress.XtraReports.UI.XRLabel xrLblFooterCompanyAddress;
        private DevExpress.XtraReports.UI.XRLabel xrLblFooterContactInfo;
        private DevExpress.XtraReports.UI.XRLabel xrLblSignatureLine;
        private DevExpress.XtraReports.UI.XRLabel xrLblPlaceForSignature;
    }
}
