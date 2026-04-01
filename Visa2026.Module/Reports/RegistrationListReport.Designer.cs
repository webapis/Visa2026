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
            this.xrLabel_SignatureLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_SignatureTitle = new DevExpress.XtraReports.UI.XRLabel();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrLabel_HeaderRowNum = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderFamily = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderName = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderBirth = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderGender = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderNationality = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderPassportNum = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderPassportExp = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderPurpose = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderVisa = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_HeaderAddress = new DevExpress.XtraReports.UI.XRLabel();
            this.RegistrationDataSource = new DevExpress.Persistent.Base.ReportsV2.CollectionDataSource();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable_Detail)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.RegistrationDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();

            // Detail
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable_Detail});
            this.Detail.HeightF = 35F;
            this.Detail.CanGrow = true;
            this.Detail.Name = "Detail";

            // xrTable_Detail
            this.xrTable_Detail.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTable_Detail.Name = "xrTable_Detail";
            this.xrTable_Detail.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow_Detail});
            this.xrTable_Detail.SizeF = new System.Drawing.SizeF(1129F, 35F);

            // xrTableRow_Detail
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
            this.xrTableRow_Detail.SizeF = new System.Drawing.SizeF(1129F, 35F);
            this.xrTableRow_Detail.Weight = 1D;

            // xrTableCell_RowNumber  weight=35
            this.xrTableCell_RowNumber.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_RowNumber.BorderWidth = 0.5F;
            this.xrTableCell_RowNumber.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_RowNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumRecordNumber()")});
            xrSummary1.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrTableCell_RowNumber.Summary = xrSummary1;
            this.xrTableCell_RowNumber.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_RowNumber.Name = "xrTableCell_RowNumber";
            this.xrTableCell_RowNumber.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_RowNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_RowNumber.Weight = 35D;

            // xrTableCell_FamilyName  weight=85
            this.xrTableCell_FamilyName.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_FamilyName.BorderWidth = 0.5F;
            this.xrTableCell_FamilyName.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_FamilyName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_FamilyName.Name = "xrTableCell_FamilyName";
            this.xrTableCell_FamilyName.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_FamilyName.Text = "[Person.LastName]";
            this.xrTableCell_FamilyName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_FamilyName.Weight = 85D;

            // xrTableCell_FirstName  weight=85
            this.xrTableCell_FirstName.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_FirstName.BorderWidth = 0.5F;
            this.xrTableCell_FirstName.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_FirstName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_FirstName.Name = "xrTableCell_FirstName";
            this.xrTableCell_FirstName.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_FirstName.Text = "[Person.FirstName]";
            this.xrTableCell_FirstName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_FirstName.Weight = 85D;

            // xrTableCell_BirthDate  weight=90
            this.xrTableCell_BirthDate.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_BirthDate.BorderWidth = 0.5F;
            this.xrTableCell_BirthDate.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_BirthDate.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_BirthDate.Name = "xrTableCell_BirthDate";
            this.xrTableCell_BirthDate.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_BirthDate.Text = "[Person_DateOfBirthText]";
            this.xrTableCell_BirthDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_BirthDate.Weight = 90D;

            // xrTableCell_Gender  weight=65
            this.xrTableCell_Gender.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_Gender.BorderWidth = 0.5F;
            this.xrTableCell_Gender.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_Gender.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_Gender.Name = "xrTableCell_Gender";
            this.xrTableCell_Gender.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_Gender.Text = "[Person_GenderTm]";
            this.xrTableCell_Gender.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_Gender.Weight = 65D;

            // xrTableCell_Nationality  weight=79
            this.xrTableCell_Nationality.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_Nationality.BorderWidth = 0.5F;
            this.xrTableCell_Nationality.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_Nationality.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_Nationality.Name = "xrTableCell_Nationality";
            this.xrTableCell_Nationality.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_Nationality.Text = "[Person_NationalityCode]";
            this.xrTableCell_Nationality.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_Nationality.Weight = 79D;

            // xrTableCell_PassportNumber  weight=105
            this.xrTableCell_PassportNumber.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_PassportNumber.BorderWidth = 0.5F;
            this.xrTableCell_PassportNumber.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_PassportNumber.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_PassportNumber.Name = "xrTableCell_PassportNumber";
            this.xrTableCell_PassportNumber.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_PassportNumber.Text = "[Passport_Number]";
            this.xrTableCell_PassportNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_PassportNumber.Weight = 105D;

            // xrTableCell_PassportExpiration  weight=110
            this.xrTableCell_PassportExpiration.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_PassportExpiration.BorderWidth = 0.5F;
            this.xrTableCell_PassportExpiration.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_PassportExpiration.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_PassportExpiration.Name = "xrTableCell_PassportExpiration";
            this.xrTableCell_PassportExpiration.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_PassportExpiration.Text = "[Passport_ExpirationDateText]";
            this.xrTableCell_PassportExpiration.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_PassportExpiration.Weight = 110D;

            // xrTableCell_Purpose  weight=150
            this.xrTableCell_Purpose.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_Purpose.BorderWidth = 0.5F;
            this.xrTableCell_Purpose.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_Purpose.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_Purpose.Name = "xrTableCell_Purpose";
            this.xrTableCell_Purpose.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_Purpose.Text = "[Travel_PurposeOfTravelTm]";
            this.xrTableCell_Purpose.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_Purpose.Weight = 150D;

            // xrTableCell_VisaInfo  weight=125
            this.xrTableCell_VisaInfo.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_VisaInfo.BorderWidth = 0.5F;
            this.xrTableCell_VisaInfo.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_VisaInfo.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                "[Visa_Number] + Char(10) + [Visa_TypeTm] + Char(10) + [Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText]")});
            this.xrTableCell_VisaInfo.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_VisaInfo.Multiline = true;
            this.xrTableCell_VisaInfo.Name = "xrTableCell_VisaInfo";
            this.xrTableCell_VisaInfo.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_VisaInfo.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_VisaInfo.Weight = 125D;

            // xrTableCell_Address  weight=200
            this.xrTableCell_Address.BorderColor = System.Drawing.Color.Black;
            this.xrTableCell_Address.BorderWidth = 0.5F;
            this.xrTableCell_Address.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrTableCell_Address.Font = new DevExpress.Drawing.DXFont("Times New Roman", 9F);
            this.xrTableCell_Address.Name = "xrTableCell_Address";
            this.xrTableCell_Address.Padding = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 3, 3, 100F);
            this.xrTableCell_Address.Text = "[Address_FullAddress]";
            this.xrTableCell_Address.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell_Address.Weight = 200D;

            ((System.ComponentModel.ISupportInitialize)(this.xrTable_Detail)).EndInit();

            // TopMargin
            this.TopMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel_Title});
            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name = "TopMargin";

            // xrLabel_Title
            this.xrLabel_Title.Font = new DevExpress.Drawing.DXFont("Times New Roman", 18F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_Title.ForeColor = System.Drawing.Color.FromArgb(50, 50, 50);
            this.xrLabel_Title.LocationFloat = new DevExpress.Utils.PointFloat(0F, 10F);
            this.xrLabel_Title.Multiline = true;
            this.xrLabel_Title.Name = "xrLabel_Title";
            this.xrLabel_Title.SizeF = new System.Drawing.SizeF(1129F, 40F);
            this.xrLabel_Title.Text = "Dasary ýurt raýatlarynyn sanawy";
            this.xrLabel_Title.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // BottomMargin
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel_SignatureLine,
            this.xrLabel_SignatureTitle});
            this.BottomMargin.HeightF = 60F;
            this.BottomMargin.Name = "BottomMargin";

            // xrLabel_SignatureLine
            this.xrLabel_SignatureLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_SignatureLine.LocationFloat = new DevExpress.Utils.PointFloat(0F, 35F);
            this.xrLabel_SignatureLine.Name = "xrLabel_SignatureLine";
            this.xrLabel_SignatureLine.SizeF = new System.Drawing.SizeF(400F, 15F);
            this.xrLabel_SignatureLine.Text = "Türkmenistândaky Sahamçasynyň müdiri";

            // xrLabel_SignatureTitle
            this.xrLabel_SignatureTitle.Font = new DevExpress.Drawing.DXFont("Times New Roman", 10F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_SignatureTitle.LocationFloat = new DevExpress.Utils.PointFloat(700F, 35F);
            this.xrLabel_SignatureTitle.Name = "xrLabel_SignatureTitle";
            this.xrLabel_SignatureTitle.SizeF = new System.Drawing.SizeF(200F, 15F);
            this.xrLabel_SignatureTitle.Text = "Mehmet Çirak";

            // PageHeader
            this.PageHeader.BackColor = System.Drawing.Color.White;
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel_HeaderRowNum,
            this.xrLabel_HeaderFamily,
            this.xrLabel_HeaderName,
            this.xrLabel_HeaderBirth,
            this.xrLabel_HeaderGender,
            this.xrLabel_HeaderNationality,
            this.xrLabel_HeaderPassportNum,
            this.xrLabel_HeaderPassportExp,
            this.xrLabel_HeaderPurpose,
            this.xrLabel_HeaderVisa,
            this.xrLabel_HeaderAddress});
            this.PageHeader.HeightF = 45F;
            this.PageHeader.Name = "PageHeader";

            // xrLabel_HeaderRowNum  x=0, w=35
            this.xrLabel_HeaderRowNum.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderRowNum.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderRowNum.BorderWidth = 0.5F;
            this.xrLabel_HeaderRowNum.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderRowNum.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderRowNum.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderRowNum.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel_HeaderRowNum.Multiline = true;
            this.xrLabel_HeaderRowNum.Name = "xrLabel_HeaderRowNum";
            this.xrLabel_HeaderRowNum.SizeF = new System.Drawing.SizeF(35F, 45F);
            this.xrLabel_HeaderRowNum.Text = "№";
            this.xrLabel_HeaderRowNum.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderFamily  x=35, w=85
            this.xrLabel_HeaderFamily.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderFamily.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderFamily.BorderWidth = 0.5F;
            this.xrLabel_HeaderFamily.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderFamily.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderFamily.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderFamily.LocationFloat = new DevExpress.Utils.PointFloat(35F, 0F);
            this.xrLabel_HeaderFamily.Multiline = true;
            this.xrLabel_HeaderFamily.Name = "xrLabel_HeaderFamily";
            this.xrLabel_HeaderFamily.SizeF = new System.Drawing.SizeF(85F, 45F);
            this.xrLabel_HeaderFamily.Text = "Familiyasy";
            this.xrLabel_HeaderFamily.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderName  x=120, w=85
            this.xrLabel_HeaderName.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderName.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderName.BorderWidth = 0.5F;
            this.xrLabel_HeaderName.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderName.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderName.LocationFloat = new DevExpress.Utils.PointFloat(120F, 0F);
            this.xrLabel_HeaderName.Multiline = true;
            this.xrLabel_HeaderName.Name = "xrLabel_HeaderName";
            this.xrLabel_HeaderName.SizeF = new System.Drawing.SizeF(85F, 45F);
            this.xrLabel_HeaderName.Text = "Ady";
            this.xrLabel_HeaderName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderBirth  x=205, w=90
            this.xrLabel_HeaderBirth.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderBirth.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderBirth.BorderWidth = 0.5F;
            this.xrLabel_HeaderBirth.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderBirth.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderBirth.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderBirth.LocationFloat = new DevExpress.Utils.PointFloat(205F, 0F);
            this.xrLabel_HeaderBirth.Multiline = true;
            this.xrLabel_HeaderBirth.Name = "xrLabel_HeaderBirth";
            this.xrLabel_HeaderBirth.SizeF = new System.Drawing.SizeF(90F, 45F);
            this.xrLabel_HeaderBirth.Text = "Doğlan senesi";
            this.xrLabel_HeaderBirth.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderGender  x=295, w=65
            this.xrLabel_HeaderGender.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderGender.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderGender.BorderWidth = 0.5F;
            this.xrLabel_HeaderGender.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderGender.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderGender.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderGender.LocationFloat = new DevExpress.Utils.PointFloat(295F, 0F);
            this.xrLabel_HeaderGender.Multiline = true;
            this.xrLabel_HeaderGender.Name = "xrLabel_HeaderGender";
            this.xrLabel_HeaderGender.SizeF = new System.Drawing.SizeF(65F, 45F);
            this.xrLabel_HeaderGender.Text = "Jynsy";
            this.xrLabel_HeaderGender.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderNationality  x=360, w=79
            this.xrLabel_HeaderNationality.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderNationality.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderNationality.BorderWidth = 0.5F;
            this.xrLabel_HeaderNationality.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderNationality.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderNationality.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderNationality.LocationFloat = new DevExpress.Utils.PointFloat(360F, 0F);
            this.xrLabel_HeaderNationality.Multiline = true;
            this.xrLabel_HeaderNationality.Name = "xrLabel_HeaderNationality";
            this.xrLabel_HeaderNationality.SizeF = new System.Drawing.SizeF(79F, 45F);
            this.xrLabel_HeaderNationality.Text = "Raýatlygy";
            this.xrLabel_HeaderNationality.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderPassportNum  x=439, w=105
            this.xrLabel_HeaderPassportNum.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderPassportNum.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPassportNum.BorderWidth = 0.5F;
            this.xrLabel_HeaderPassportNum.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderPassportNum.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderPassportNum.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPassportNum.LocationFloat = new DevExpress.Utils.PointFloat(439F, 0F);
            this.xrLabel_HeaderPassportNum.Multiline = true;
            this.xrLabel_HeaderPassportNum.Name = "xrLabel_HeaderPassportNum";
            this.xrLabel_HeaderPassportNum.SizeF = new System.Drawing.SizeF(105F, 45F);
            this.xrLabel_HeaderPassportNum.Text = "Pasportynyn belgisi";
            this.xrLabel_HeaderPassportNum.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderPassportExp  x=544, w=110
            this.xrLabel_HeaderPassportExp.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderPassportExp.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPassportExp.BorderWidth = 0.5F;
            this.xrLabel_HeaderPassportExp.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderPassportExp.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderPassportExp.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPassportExp.LocationFloat = new DevExpress.Utils.PointFloat(544F, 0F);
            this.xrLabel_HeaderPassportExp.Multiline = true;
            this.xrLabel_HeaderPassportExp.Name = "xrLabel_HeaderPassportExp";
            this.xrLabel_HeaderPassportExp.SizeF = new System.Drawing.SizeF(110F, 45F);
            this.xrLabel_HeaderPassportExp.Text = "Pasportynyn möhleti";
            this.xrLabel_HeaderPassportExp.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderPurpose  x=654, w=150
            this.xrLabel_HeaderPurpose.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderPurpose.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPurpose.BorderWidth = 0.5F;
            this.xrLabel_HeaderPurpose.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderPurpose.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderPurpose.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPurpose.LocationFloat = new DevExpress.Utils.PointFloat(654F, 0F);
            this.xrLabel_HeaderPurpose.Multiline = true;
            this.xrLabel_HeaderPurpose.Name = "xrLabel_HeaderPurpose";
            this.xrLabel_HeaderPurpose.SizeF = new System.Drawing.SizeF(150F, 45F);
            this.xrLabel_HeaderPurpose.Text = "Gelmeginiin maksady";
            this.xrLabel_HeaderPurpose.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderVisa  x=804, w=125
            this.xrLabel_HeaderVisa.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderVisa.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderVisa.BorderWidth = 0.5F;
            this.xrLabel_HeaderVisa.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderVisa.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderVisa.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderVisa.LocationFloat = new DevExpress.Utils.PointFloat(804F, 0F);
            this.xrLabel_HeaderVisa.Multiline = true;
            this.xrLabel_HeaderVisa.Name = "xrLabel_HeaderVisa";
            this.xrLabel_HeaderVisa.SizeF = new System.Drawing.SizeF(125F, 45F);
            this.xrLabel_HeaderVisa.Text = "Wiza maglumatary";
            this.xrLabel_HeaderVisa.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // xrLabel_HeaderAddress  x=929, w=200
            this.xrLabel_HeaderAddress.BackColor = System.Drawing.Color.White;
            this.xrLabel_HeaderAddress.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderAddress.BorderWidth = 0.5F;
            this.xrLabel_HeaderAddress.Borders = DevExpress.XtraPrinting.BorderSide.All;
            this.xrLabel_HeaderAddress.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabel_HeaderAddress.ForeColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderAddress.LocationFloat = new DevExpress.Utils.PointFloat(929F, 0F);
            this.xrLabel_HeaderAddress.Multiline = true;
            this.xrLabel_HeaderAddress.Name = "xrLabel_HeaderAddress";
            this.xrLabel_HeaderAddress.SizeF = new System.Drawing.SizeF(200F, 45F);
            this.xrLabel_HeaderAddress.Text = "Türkmenistândaky salgysy";
            this.xrLabel_HeaderAddress.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // RegistrationDataSource
            this.RegistrationDataSource.Name = "RegistrationDataSource";
            this.RegistrationDataSource.ObjectTypeName = "Visa2026.Module.BusinessObjects.Registration";
            this.RegistrationDataSource.TopReturnedRecords = 0;

            // RegistrationListReport
            this.BackColor = System.Drawing.Color.White;
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.PageHeader,
            this.Detail,
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
            ((System.ComponentModel.ISupportInitialize)(this.RegistrationDataSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        private DevExpress.Persistent.Base.ReportsV2.CollectionDataSource RegistrationDataSource;
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
        private DevExpress.XtraReports.UI.XRLabel xrLabel_Title;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_SignatureLine;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_SignatureTitle;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderRowNum;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderFamily;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderName;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderBirth;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderGender;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderNationality;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderPassportNum;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderPassportExp;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderPurpose;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderVisa;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_HeaderAddress;
    }
}
