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
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel_RowNumber = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_FamilyName = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_FirstName = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_BirthDate = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_Gender = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_Nationality = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_PassportNumber = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_PassportExpiration = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_VisaInfo = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_Address = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel_Purpose = new DevExpress.XtraReports.UI.XRLabel();
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
            ((System.ComponentModel.ISupportInitialize)(this.RegistrationDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            
            // Detail
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel_RowNumber,
            this.xrLabel_FamilyName,
            this.xrLabel_FirstName,
            this.xrLabel_BirthDate,
            this.xrLabel_Gender,
            this.xrLabel_Nationality,
            this.xrLabel_PassportNumber,
            this.xrLabel_PassportExpiration,
            this.xrLabel_Purpose,
            this.xrLabel_VisaInfo,
            this.xrLabel_Address});
            this.Detail.HeightF = 30F;
            this.Detail.Name = "Detail";
            
            // xrLabel_RowNumber
            this.xrLabel_RowNumber.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel_RowNumber.Name = "xrLabel_RowNumber";
            this.xrLabel_RowNumber.SizeF = new System.Drawing.SizeF(30F, 30F);
            this.xrLabel_RowNumber.Text = "[RowNumber]";
            this.xrLabel_RowNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_RowNumber.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_RowNumber.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_FamilyName
            this.xrLabel_FamilyName.LocationFloat = new DevExpress.Utils.PointFloat(30F, 0F);
            this.xrLabel_FamilyName.Name = "xrLabel_FamilyName";
            this.xrLabel_FamilyName.SizeF = new System.Drawing.SizeF(70F, 30F);
            this.xrLabel_FamilyName.Text = "[Person_FullName]";
            this.xrLabel_FamilyName.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_FamilyName.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_FirstName
            this.xrLabel_FirstName.LocationFloat = new DevExpress.Utils.PointFloat(100F, 0F);
            this.xrLabel_FirstName.Name = "xrLabel_FirstName";
            this.xrLabel_FirstName.SizeF = new System.Drawing.SizeF(70F, 30F);
            this.xrLabel_FirstName.Text = "[Person_FullName]";
            this.xrLabel_FirstName.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_FirstName.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_BirthDate
            this.xrLabel_BirthDate.LocationFloat = new DevExpress.Utils.PointFloat(170F, 0F);
            this.xrLabel_BirthDate.Name = "xrLabel_BirthDate";
            this.xrLabel_BirthDate.SizeF = new System.Drawing.SizeF(80F, 30F);
            this.xrLabel_BirthDate.Text = "[Person_DateOfBirthText]";
            this.xrLabel_BirthDate.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_BirthDate.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_Gender
            this.xrLabel_Gender.LocationFloat = new DevExpress.Utils.PointFloat(250F, 0F);
            this.xrLabel_Gender.Name = "xrLabel_Gender";
            this.xrLabel_Gender.SizeF = new System.Drawing.SizeF(60F, 30F);
            this.xrLabel_Gender.Text = "[Person_GenderTm]";
            this.xrLabel_Gender.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_Gender.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_Nationality
            this.xrLabel_Nationality.LocationFloat = new DevExpress.Utils.PointFloat(310F, 0F);
            this.xrLabel_Nationality.Name = "xrLabel_Nationality";
            this.xrLabel_Nationality.SizeF = new System.Drawing.SizeF(70F, 30F);
            this.xrLabel_Nationality.Text = "[Person_NationalityCode]";
            this.xrLabel_Nationality.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_Nationality.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_PassportNumber
            this.xrLabel_PassportNumber.LocationFloat = new DevExpress.Utils.PointFloat(380F, 0F);
            this.xrLabel_PassportNumber.Name = "xrLabel_PassportNumber";
            this.xrLabel_PassportNumber.SizeF = new System.Drawing.SizeF(80F, 30F);
            this.xrLabel_PassportNumber.Text = "[Passport_Number]";
            this.xrLabel_PassportNumber.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_PassportNumber.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_PassportExpiration
            this.xrLabel_PassportExpiration.LocationFloat = new DevExpress.Utils.PointFloat(460F, 0F);
            this.xrLabel_PassportExpiration.Name = "xrLabel_PassportExpiration";
            this.xrLabel_PassportExpiration.SizeF = new System.Drawing.SizeF(90F, 30F);
            this.xrLabel_PassportExpiration.Text = "[Passport_ExpirationDateText]";
            this.xrLabel_PassportExpiration.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_PassportExpiration.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_Purpose
            this.xrLabel_Purpose.LocationFloat = new DevExpress.Utils.PointFloat(550F, 0F);
            this.xrLabel_Purpose.Name = "xrLabel_Purpose";
            this.xrLabel_Purpose.SizeF = new System.Drawing.SizeF(100F, 30F);
            this.xrLabel_Purpose.Text = "[Travel_PurposeOfTravelTm]";
            this.xrLabel_Purpose.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_Purpose.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_VisaInfo
            this.xrLabel_VisaInfo.LocationFloat = new DevExpress.Utils.PointFloat(650F, 0F);
            this.xrLabel_VisaInfo.Name = "xrLabel_VisaInfo";
            this.xrLabel_VisaInfo.SizeF = new System.Drawing.SizeF(100F, 30F);
            this.xrLabel_VisaInfo.Text = "[Visa_Number]";
            this.xrLabel_VisaInfo.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_VisaInfo.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_Address
            this.xrLabel_Address.LocationFloat = new DevExpress.Utils.PointFloat(750F, 0F);
            this.xrLabel_Address.Name = "xrLabel_Address";
            this.xrLabel_Address.SizeF = new System.Drawing.SizeF(150F, 30F);
            this.xrLabel_Address.Text = "[Address_FullAddress]";
            this.xrLabel_Address.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_Address.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // TopMargin
            this.TopMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel_Title});
            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name = "TopMargin";
            
            // xrLabel_Title
            this.xrLabel_Title.LocationFloat = new DevExpress.Utils.PointFloat(0F, 10F);
            this.xrLabel_Title.Multiline = true;
            this.xrLabel_Title.Name = "xrLabel_Title";
            this.xrLabel_Title.SizeF = new System.Drawing.SizeF(900F, 30F);
            this.xrLabel_Title.Text = "Dasary ýurt raýatlarynyn sanawy";
            this.xrLabel_Title.Font = new System.Drawing.Font("Arial", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel_Title.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            
            // BottomMargin
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel_SignatureLine,
            this.xrLabel_SignatureTitle});
            this.BottomMargin.HeightF = 60F;
            this.BottomMargin.Name = "BottomMargin";
            
            // xrLabel_SignatureLine
            this.xrLabel_SignatureLine.LocationFloat = new DevExpress.Utils.PointFloat(0F, 10F);
            this.xrLabel_SignatureLine.Name = "xrLabel_SignatureLine";
            this.xrLabel_SignatureLine.SizeF = new System.Drawing.SizeF(200F, 15F);
            this.xrLabel_SignatureLine.Text = "Türkmenistândaky Sahamçasynyň müdiri";
            this.xrLabel_SignatureLine.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            
            // xrLabel_SignatureTitle
            this.xrLabel_SignatureTitle.LocationFloat = new DevExpress.Utils.PointFloat(600F, 10F);
            this.xrLabel_SignatureTitle.Name = "xrLabel_SignatureTitle";
            this.xrLabel_SignatureTitle.SizeF = new System.Drawing.SizeF(200F, 15F);
            this.xrLabel_SignatureTitle.Text = "Mehmet Çirak";
            this.xrLabel_SignatureTitle.Font = new System.Drawing.Font("Arial", 10F, System.Drawing.FontStyle.Bold);
            
            // PageHeader
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
            this.PageHeader.HeightF = 40F;
            this.PageHeader.Name = "PageHeader";
            
            // xrLabel_HeaderRowNum
            this.xrLabel_HeaderRowNum.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel_HeaderRowNum.Name = "xrLabel_HeaderRowNum";
            this.xrLabel_HeaderRowNum.SizeF = new System.Drawing.SizeF(30F, 40F);
            this.xrLabel_HeaderRowNum.Text = "№";
            this.xrLabel_HeaderRowNum.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderRowNum.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderRowNum.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderRowNum.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderFamily
            this.xrLabel_HeaderFamily.LocationFloat = new DevExpress.Utils.PointFloat(30F, 0F);
            this.xrLabel_HeaderFamily.Name = "xrLabel_HeaderFamily";
            this.xrLabel_HeaderFamily.SizeF = new System.Drawing.SizeF(70F, 40F);
            this.xrLabel_HeaderFamily.Text = "Familiyasy";
            this.xrLabel_HeaderFamily.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderFamily.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderFamily.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderFamily.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderName
            this.xrLabel_HeaderName.LocationFloat = new DevExpress.Utils.PointFloat(100F, 0F);
            this.xrLabel_HeaderName.Name = "xrLabel_HeaderName";
            this.xrLabel_HeaderName.SizeF = new System.Drawing.SizeF(70F, 40F);
            this.xrLabel_HeaderName.Text = "Ady";
            this.xrLabel_HeaderName.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderName.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderName.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderBirth
            this.xrLabel_HeaderBirth.LocationFloat = new DevExpress.Utils.PointFloat(170F, 0F);
            this.xrLabel_HeaderBirth.Name = "xrLabel_HeaderBirth";
            this.xrLabel_HeaderBirth.SizeF = new System.Drawing.SizeF(80F, 40F);
            this.xrLabel_HeaderBirth.Text = "Doğlan senesi";
            this.xrLabel_HeaderBirth.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderBirth.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderBirth.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderBirth.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderGender
            this.xrLabel_HeaderGender.LocationFloat = new DevExpress.Utils.PointFloat(250F, 0F);
            this.xrLabel_HeaderGender.Name = "xrLabel_HeaderGender";
            this.xrLabel_HeaderGender.SizeF = new System.Drawing.SizeF(60F, 40F);
            this.xrLabel_HeaderGender.Text = "Jynsy";
            this.xrLabel_HeaderGender.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderGender.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderGender.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderGender.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderNationality
            this.xrLabel_HeaderNationality.LocationFloat = new DevExpress.Utils.PointFloat(310F, 0F);
            this.xrLabel_HeaderNationality.Name = "xrLabel_HeaderNationality";
            this.xrLabel_HeaderNationality.SizeF = new System.Drawing.SizeF(70F, 40F);
            this.xrLabel_HeaderNationality.Text = "Raýatlygü";
            this.xrLabel_HeaderNationality.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderNationality.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderNationality.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderNationality.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderPassportNum
            this.xrLabel_HeaderPassportNum.LocationFloat = new DevExpress.Utils.PointFloat(380F, 0F);
            this.xrLabel_HeaderPassportNum.Name = "xrLabel_HeaderPassportNum";
            this.xrLabel_HeaderPassportNum.SizeF = new System.Drawing.SizeF(80F, 40F);
            this.xrLabel_HeaderPassportNum.Text = "Pasportynyn belgisi";
            this.xrLabel_HeaderPassportNum.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderPassportNum.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderPassportNum.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPassportNum.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderPassportExp
            this.xrLabel_HeaderPassportExp.LocationFloat = new DevExpress.Utils.PointFloat(460F, 0F);
            this.xrLabel_HeaderPassportExp.Name = "xrLabel_HeaderPassportExp";
            this.xrLabel_HeaderPassportExp.SizeF = new System.Drawing.SizeF(90F, 40F);
            this.xrLabel_HeaderPassportExp.Text = "Pasportynyn möhleti";
            this.xrLabel_HeaderPassportExp.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderPassportExp.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderPassportExp.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPassportExp.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderPurpose
            this.xrLabel_HeaderPurpose.LocationFloat = new DevExpress.Utils.PointFloat(550F, 0F);
            this.xrLabel_HeaderPurpose.Name = "xrLabel_HeaderPurpose";
            this.xrLabel_HeaderPurpose.SizeF = new System.Drawing.SizeF(100F, 40F);
            this.xrLabel_HeaderPurpose.Text = "Gelmeginiin maksady";
            this.xrLabel_HeaderPurpose.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderPurpose.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderPurpose.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderPurpose.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderVisa
            this.xrLabel_HeaderVisa.LocationFloat = new DevExpress.Utils.PointFloat(650F, 0F);
            this.xrLabel_HeaderVisa.Name = "xrLabel_HeaderVisa";
            this.xrLabel_HeaderVisa.SizeF = new System.Drawing.SizeF(100F, 40F);
            this.xrLabel_HeaderVisa.Text = "Wiza maglumatary";
            this.xrLabel_HeaderVisa.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderVisa.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderVisa.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderVisa.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // xrLabel_HeaderAddress
            this.xrLabel_HeaderAddress.LocationFloat = new DevExpress.Utils.PointFloat(750F, 0F);
            this.xrLabel_HeaderAddress.Name = "xrLabel_HeaderAddress";
            this.xrLabel_HeaderAddress.SizeF = new System.Drawing.SizeF(150F, 40F);
            this.xrLabel_HeaderAddress.Text = "Türkmenistândaky salgysy";
            this.xrLabel_HeaderAddress.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel_HeaderAddress.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel_HeaderAddress.BorderColor = System.Drawing.Color.Black;
            this.xrLabel_HeaderAddress.Borders = DevExpress.XtraPrinting.BorderSide.All;
            
            // RegistrationDataSource
            this.RegistrationDataSource.Name = "RegistrationDataSource";
            this.RegistrationDataSource.ObjectTypeName = "Visa2026.Module.BusinessObjects.Registration";
            this.RegistrationDataSource.TopReturnedRecords = 0;
            
            // RegistrationListReport
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.PageHeader,
            this.Detail,
            this.BottomMargin});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.RegistrationDataSource});
            this.DataSource = this.RegistrationDataSource;
            this.Margins = new System.Drawing.Printing.Margins(20, 20, 20, 20);
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Landscape = true;
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
        private DevExpress.XtraReports.UI.XRLabel xrLabel_RowNumber;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_FamilyName;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_FirstName;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_BirthDate;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_Gender;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_Nationality;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_PassportNumber;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_PassportExpiration;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_Purpose;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_VisaInfo;
        private DevExpress.XtraReports.UI.XRLabel xrLabel_Address;
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
