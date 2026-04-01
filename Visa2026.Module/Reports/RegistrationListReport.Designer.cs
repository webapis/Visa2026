namespace Visa2026.Module.Reports
{
    partial class RegistrationListReport
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
        /// the contents of this method by the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            DevExpress.XtraReports.UI.XRSummary xrSummary1 = new DevExpress.XtraReports.UI.XRSummary();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrTable_Detail = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow_Detail = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell_RowNumber = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_FamilyName = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_FirstName = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_BirthDate = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_Gender = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_Nationality = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_PassportNumber = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_PassportExpiration = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_Purpose = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_VisaInfo = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_Address = new DevExpress.XtraReports.UI.XRTableCell();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.xrLabel_Title = new DevExpress.XtraReports.UI.XRLabel();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.ReportFooter = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.xrLabel_SignaturePosition = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_SignatureFullName = new DevExpress.XtraReports.UI.XRLabel();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrTable_Header = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow_Header = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell_HeaderRowNum = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderFamily = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderName = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderBirth = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderGender = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderNationality = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderPassportNum = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderPassportExp = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderPurpose = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderVisa = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell_HeaderAddress = new DevExpress.XtraReports.UI.XRTableCell();
            this.RegistrationDataSource = new DevExpress.Persistent.Base.ReportsV2.CollectionDataSource();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable_Detail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable_Header)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RegistrationDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable_Detail});
            this.Detail.HeightF = 35F;
            this.Detail.Name = "Detail";
            // 
            // xrTable_Detail
            // 
            this.xrTable_Detail.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTable_Detail.Name = "xrTable_Detail";
            this.xrTable_Detail.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow_Detail});
            this.xrTable_Detail.SizeF = new System.Drawing.SizeF(1129F, 35F);
            // 
            // xrTableRow_Detail
            // 
            this.xrTableRow_Detail.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell_RowNumber,
            this.xrTableCell_FamilyName,
            this.xrTableCell_FirstName,
            this.xrTableCell_BirthDate,
            this.xrTableCell_Gender,
            this.xrTableCell_Nationality,
            this.xrTableCell_PassportNumber,
            this.xrTableCell_PassportExpiration,
            this.xrTableCell_Purpose,
            this.xrTableCell_VisaInfo,
            this.xrTableCell_Address});
            this.xrTableRow_Detail.Name = "xrTableRow_Detail";
            this.xrTableRow_Detail.Weight = 1D;
            // 
            // xrTableCell_RowNumber
            // 
            this.xrTableCell_RowNumber.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_RowNumber.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_RowNumber.BorderWidth = 0.5F;
            this.xrTableCell_RowNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumRecordNumber()")});
            this.xrTableCell_RowNumber.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_RowNumber.Name = "xrTableCell_RowNumber";
            this.xrTableCell_RowNumber.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            xrSummary1.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrTableCell_RowNumber.Summary = xrSummary1;
            this.xrTableCell_RowNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_RowNumber.Weight = 35D;
            // 
            // xrTableCell_FamilyName
            // 
            this.xrTableCell_FamilyName.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_FamilyName.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_FamilyName.BorderWidth = 0.5F;
            this.xrTableCell_FamilyName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_FamilyName.Name = "xrTableCell_FamilyName";
            this.xrTableCell_FamilyName.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_FamilyName.Text = "[Person.LastName]";
            this.xrTableCell_FamilyName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_FamilyName.Weight = 85D;
            // 
            // xrTableCell_FirstName
            // 
            this.xrTableCell_FirstName.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_FirstName.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_FirstName.BorderWidth = 0.5F;
            this.xrTableCell_FirstName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_FirstName.Name = "xrTableCell_FirstName";
            this.xrTableCell_FirstName.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_FirstName.Text = "[Person.FirstName]";
            this.xrTableCell_FirstName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_FirstName.Weight = 85D;
            // 
            // xrTableCell_BirthDate
            // 
            this.xrTableCell_BirthDate.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_BirthDate.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_BirthDate.BorderWidth = 0.5F;
            this.xrTableCell_BirthDate.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_BirthDate.Name = "xrTableCell_BirthDate";
            this.xrTableCell_BirthDate.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_BirthDate.Text = "[Person_DateOfBirthText]";
            this.xrTableCell_BirthDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_BirthDate.Weight = 90D;
            // 
            // xrTableCell_Gender
            // 
            this.xrTableCell_Gender.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_Gender.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_Gender.BorderWidth = 0.5F;
            this.xrTableCell_Gender.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_Gender.Name = "xrTableCell_Gender";
            this.xrTableCell_Gender.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_Gender.Text = "[Person_GenderTm]";
            this.xrTableCell_Gender.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_Gender.Weight = 65D;
            // 
            // xrTableCell_Nationality
            // 
            this.xrTableCell_Nationality.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_Nationality.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_Nationality.BorderWidth = 0.5F;
            this.xrTableCell_Nationality.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_Nationality.Name = "xrTableCell_Nationality";
            this.xrTableCell_Nationality.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_Nationality.Text = "[Person_NationalityCode]";
            this.xrTableCell_Nationality.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_Nationality.Weight = 79D;
            // 
            // xrTableCell_PassportNumber
            // 
            this.xrTableCell_PassportNumber.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_PassportNumber.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_PassportNumber.BorderWidth = 0.5F;
            this.xrTableCell_PassportNumber.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_PassportNumber.Name = "xrTableCell_PassportNumber";
            this.xrTableCell_PassportNumber.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_PassportNumber.Text = "[Passport_Number]";
            this.xrTableCell_PassportNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_PassportNumber.Weight = 105D;
            // 
            // xrTableCell_PassportExpiration
            // 
            this.xrTableCell_PassportExpiration.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_PassportExpiration.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_PassportExpiration.BorderWidth = 0.5F;
            this.xrTableCell_PassportExpiration.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_PassportExpiration.Name = "xrTableCell_PassportExpiration";
            this.xrTableCell_PassportExpiration.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_PassportExpiration.Text = "[Passport_ExpirationDateText]";
            this.xrTableCell_PassportExpiration.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_PassportExpiration.Weight = 110D;
            // 
            // xrTableCell_Purpose
            // 
            this.xrTableCell_Purpose.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_Purpose.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_Purpose.BorderWidth = 0.5F;
            this.xrTableCell_Purpose.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_Purpose.Name = "xrTableCell_Purpose";
            this.xrTableCell_Purpose.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_Purpose.Text = "[Travel_PurposeOfTravelTm]";
            this.xrTableCell_Purpose.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_Purpose.Weight = 150D;
            // 
            // xrTableCell_VisaInfo
            // 
            this.xrTableCell_VisaInfo.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_VisaInfo.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_VisaInfo.BorderWidth = 0.5F;
            this.xrTableCell_VisaInfo.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Visa_Number] + Char(10) + [Visa_TypeTm] + Char(10) + [Visa_StartDateText] + Char" +
                    "(10) + [Visa_ExpirationDateText]")});
            this.xrTableCell_VisaInfo.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_VisaInfo.Multiline = true;
            this.xrTableCell_VisaInfo.Name = "xrTableCell_VisaInfo";
            this.xrTableCell_VisaInfo.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_VisaInfo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_VisaInfo.Weight = 125D;
            // 
            // xrTableCell_Address
            // 
            this.xrTableCell_Address.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_Address.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left
            | DevExpress.XtraPrinting.BorderSide.Right)
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_Address.BorderWidth = 0.5F;
            this.xrTableCell_Address.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_Address.Name = "xrTableCell_Address";
            this.xrTableCell_Address.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_Address.Text = "[Address_FullAddress]";
            this.xrTableCell_Address.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_Address.Weight = 200D;
            // 
            // TopMargin
            // 
            this.TopMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel_Title});
            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name = "TopMargin";
            // 
            // xrLabel_Title
            // 
            this.xrLabel_Title.Font = new DevExpress.Drawing.DXFont("Times New Roman", 18F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_Title.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.xrLabel_Title.LocationFloat = new DevExpress.Utils.PointFloat(0F, 10F);
            this.xrLabel_Title.Multiline = true;
            this.xrLabel_Title.Name = "xrLabel_Title";
            this.xrLabel_Title.SizeF = new System.Drawing.SizeF(1129F, 40F);
            this.xrLabel_Title.Text = "Dasary ýurt raýatlarynyn sanawy";
            this.xrLabel_Title.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // BottomMargin
            //
            this.BottomMargin.HeightF = 20F;
            this.BottomMargin.Name = "BottomMargin";
            //
            // ReportFooter
            //
            this.ReportFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel_SignaturePosition,
            this.xrLabel_SignatureFullName});
            this.ReportFooter.HeightF = 40F;
            this.ReportFooter.Name = "ReportFooter";
            //
            // xrLabel_SignaturePosition
            //
            this.xrLabel_SignaturePosition.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead_PositionTm]")});
            this.xrLabel_SignaturePosition.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_SignaturePosition.LocationFloat = new DevExpress.Utils.PointFloat(150F, 15F);
            this.xrLabel_SignaturePosition.Name = "xrLabel_SignaturePosition";
            this.xrLabel_SignaturePosition.SizeF = new System.Drawing.SizeF(400F, 20F);
            this.xrLabel_SignaturePosition.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            //
            // xrLabel_SignatureFullName
            //
            this.xrLabel_SignatureFullName.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead_FullName]")});
            this.xrLabel_SignatureFullName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_SignatureFullName.LocationFloat = new DevExpress.Utils.PointFloat(679F, 15F);
            this.xrLabel_SignatureFullName.Name = "xrLabel_SignatureFullName";
            this.xrLabel_SignatureFullName.SizeF = new System.Drawing.SizeF(300F, 20F);
            this.xrLabel_SignatureFullName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // PageHeader
            // 
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable_Header});
            this.PageHeader.HeightF = 45F;
            this.PageHeader.Name = "PageHeader";
            // 
            // xrTable_Header
            // 
            this.xrTable_Header.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTable_Header.Name = "xrTable_Header";
            this.xrTable_Header.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow_Header});
            this.xrTable_Header.SizeF = new System.Drawing.SizeF(1129F, 45F);
            // 
            // xrTableRow_Header
            // 
            this.xrTableRow_Header.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell_HeaderRowNum,
            this.xrTableCell_HeaderFamily,
            this.xrTableCell_HeaderName,
            this.xrTableCell_HeaderBirth,
            this.xrTableCell_HeaderGender,
            this.xrTableCell_HeaderNationality,
            this.xrTableCell_HeaderPassportNum,
            this.xrTableCell_HeaderPassportExp,
            this.xrTableCell_HeaderPurpose,
            this.xrTableCell_HeaderVisa,
            this.xrTableCell_HeaderAddress});
            this.xrTableRow_Header.Name = "xrTableRow_Header";
            this.xrTableRow_Header.Weight = 1D;
            // 
            // xrTableCell_HeaderRowNum
            // 
            this.xrTableCell_HeaderRowNum.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderRowNum.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderRowNum.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderRowNum.BorderWidth = 0.5F;
            this.xrTableCell_HeaderRowNum.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderRowNum.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderRowNum.Multiline = true;
            this.xrTableCell_HeaderRowNum.Name = "xrTableCell_HeaderRowNum";
            this.xrTableCell_HeaderRowNum.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderRowNum.Text = "№";
            this.xrTableCell_HeaderRowNum.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderRowNum.Weight = 35D;
            // 
            // xrTableCell_HeaderFamily
            // 
            this.xrTableCell_HeaderFamily.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderFamily.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderFamily.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderFamily.BorderWidth = 0.5F;
            this.xrTableCell_HeaderFamily.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderFamily.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderFamily.Multiline = true;
            this.xrTableCell_HeaderFamily.Name = "xrTableCell_HeaderFamily";
            this.xrTableCell_HeaderFamily.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderFamily.Text = "Familiyasy";
            this.xrTableCell_HeaderFamily.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderFamily.Weight = 85D;
            // 
            // xrTableCell_HeaderName
            // 
            this.xrTableCell_HeaderName.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderName.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderName.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderName.BorderWidth = 0.5F;
            this.xrTableCell_HeaderName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderName.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderName.Multiline = true;
            this.xrTableCell_HeaderName.Name = "xrTableCell_HeaderName";
            this.xrTableCell_HeaderName.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderName.Text = "Ady";
            this.xrTableCell_HeaderName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderName.Weight = 85D;
            // 
            // xrTableCell_HeaderBirth
            // 
            this.xrTableCell_HeaderBirth.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderBirth.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderBirth.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderBirth.BorderWidth = 0.5F;
            this.xrTableCell_HeaderBirth.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderBirth.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderBirth.Multiline = true;
            this.xrTableCell_HeaderBirth.Name = "xrTableCell_HeaderBirth";
            this.xrTableCell_HeaderBirth.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderBirth.Text = "Doğlan senesi";
            this.xrTableCell_HeaderBirth.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderBirth.Weight = 90D;
            // 
            // xrTableCell_HeaderGender
            // 
            this.xrTableCell_HeaderGender.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderGender.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderGender.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderGender.BorderWidth = 0.5F;
            this.xrTableCell_HeaderGender.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderGender.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderGender.Multiline = true;
            this.xrTableCell_HeaderGender.Name = "xrTableCell_HeaderGender";
            this.xrTableCell_HeaderGender.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderGender.Text = "Jynsy";
            this.xrTableCell_HeaderGender.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderGender.Weight = 65D;
            // 
            // xrTableCell_HeaderNationality
            // 
            this.xrTableCell_HeaderNationality.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderNationality.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderNationality.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderNationality.BorderWidth = 0.5F;
            this.xrTableCell_HeaderNationality.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderNationality.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderNationality.Multiline = true;
            this.xrTableCell_HeaderNationality.Name = "xrTableCell_HeaderNationality";
            this.xrTableCell_HeaderNationality.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderNationality.Text = "Raýatlygy";
            this.xrTableCell_HeaderNationality.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderNationality.Weight = 79D;
            // 
            // xrTableCell_HeaderPassportNum
            // 
            this.xrTableCell_HeaderPassportNum.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderPassportNum.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderPassportNum.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderPassportNum.BorderWidth = 0.5F;
            this.xrTableCell_HeaderPassportNum.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderPassportNum.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderPassportNum.Multiline = true;
            this.xrTableCell_HeaderPassportNum.Name = "xrTableCell_HeaderPassportNum";
            this.xrTableCell_HeaderPassportNum.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderPassportNum.Text = "Pasportynyn belgisi";
            this.xrTableCell_HeaderPassportNum.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderPassportNum.Weight = 105D;
            // 
            // xrTableCell_HeaderPassportExp
            // 
            this.xrTableCell_HeaderPassportExp.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderPassportExp.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderPassportExp.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderPassportExp.BorderWidth = 0.5F;
            this.xrTableCell_HeaderPassportExp.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderPassportExp.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderPassportExp.Multiline = true;
            this.xrTableCell_HeaderPassportExp.Name = "xrTableCell_HeaderPassportExp";
            this.xrTableCell_HeaderPassportExp.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderPassportExp.Text = "Pasportynyn möhleti";
            this.xrTableCell_HeaderPassportExp.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderPassportExp.Weight = 110D;
            // 
            // xrTableCell_HeaderPurpose
            // 
            this.xrTableCell_HeaderPurpose.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderPurpose.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderPurpose.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderPurpose.BorderWidth = 0.5F;
            this.xrTableCell_HeaderPurpose.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderPurpose.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderPurpose.Multiline = true;
            this.xrTableCell_HeaderPurpose.Name = "xrTableCell_HeaderPurpose";
            this.xrTableCell_HeaderPurpose.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderPurpose.Text = "Gelmeginiin maksady";
            this.xrTableCell_HeaderPurpose.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderPurpose.Weight = 150D;
            // 
            // xrTableCell_HeaderVisa
            // 
            this.xrTableCell_HeaderVisa.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderVisa.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderVisa.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderVisa.BorderWidth = 0.5F;
            this.xrTableCell_HeaderVisa.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderVisa.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderVisa.Multiline = true;
            this.xrTableCell_HeaderVisa.Name = "xrTableCell_HeaderVisa";
            this.xrTableCell_HeaderVisa.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderVisa.Text = "Wiza maglumatary";
            this.xrTableCell_HeaderVisa.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderVisa.Weight = 125D;
            // 
            // xrTableCell_HeaderAddress
            // 
            this.xrTableCell_HeaderAddress.BackColor = System.Drawing.Color.White;
            this.xrTableCell_HeaderAddress.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderAddress.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell_HeaderAddress.BorderWidth = 0.5F;
            this.xrTableCell_HeaderAddress.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrTableCell_HeaderAddress.ForeColor = System.Drawing.Color.Black;
            this.xrTableCell_HeaderAddress.Multiline = true;
            this.xrTableCell_HeaderAddress.Name = "xrTableCell_HeaderAddress";
            this.xrTableCell_HeaderAddress.Padding = new DevExpress.XtraPrinting.PaddingInfo(3F, 3F, 3F, 3F, 100F);
            this.xrTableCell_HeaderAddress.Text = "Türkmenistandaky salgysy";
            this.xrTableCell_HeaderAddress.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_HeaderAddress.Weight = 200D;
            // 
            // RegistrationDataSource
            // 
            this.RegistrationDataSource.Name = "RegistrationDataSource";
            this.RegistrationDataSource.ObjectTypeName = "Visa2026.Module.BusinessObjects.Registration";
            this.RegistrationDataSource.TopReturnedRecords = 0;
            // 
            // RegistrationListReport
            // 
            this.BackColor = System.Drawing.Color.White;
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.PageHeader,
            this.Detail,
            this.ReportFooter,
            this.BottomMargin});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.RegistrationDataSource});
            this.DataSource = this.RegistrationDataSource;
            this.Landscape = true;
            this.Margins = new DevExpress.Drawing.DXMargins(20F, 20F, 50F, 60F);
            this.PageHeightF = 826.7717F;
            this.PageWidthF = 1169.291F;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Version = "25.2";
            ((System.ComponentModel.ISupportInitialize)(this.xrTable_Detail)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable_Header)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.RegistrationDataSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        private DevExpress.Persistent.Base.ReportsV2.CollectionDataSource RegistrationDataSource;
        // Detail table
        private DevExpress.XtraReports.UI.XRTable xrTable_Detail;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow_Detail;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_RowNumber;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_FamilyName;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_FirstName;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_BirthDate;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_Gender;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_Nationality;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_PassportNumber;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_PassportExpiration;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_Purpose;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_VisaInfo;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_Address;
        // Header table
        private DevExpress.XtraReports.UI.XRTable xrTable_Header;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow_Header;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderRowNum;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderFamily;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderName;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderBirth;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderGender;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderNationality;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderPassportNum;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderPassportExp;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderPurpose;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderVisa;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell_HeaderAddress;
        // Other labels and bands
        private DevExpress.XtraReports.UI.XRLabel xrLabel_Title;
        private DevExpress.XtraReports.UI.ReportFooterBand ReportFooter;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_SignaturePosition;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_SignatureFullName;
    }
}
