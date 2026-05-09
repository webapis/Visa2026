using DevExpress.Drawing;
using DevExpress.Utils;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    public partial class AppInvAndWPBorcnamaItemReport
    {
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AppInvAndWPBorcnamaItemReport));
            this.xrRichHeader = new DevExpress.XtraReports.UI.XRRichText();
            this.xrLabelTitle = new DevExpress.XtraReports.UI.XRLabel();
            this.xrRichBody = new DevExpress.XtraReports.UI.XRRichText();
            this.xrLabelCompanyLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyRegistryLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyRegistryCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelWorkerIntro = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelWorkerNameLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelWorkerNameCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelWorkerDobLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelWorkerDobCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyHeadLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyHeadNameLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyHeadNameCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyHeadPassportLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyHeadPassportLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelCompanyHeadPassportCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelRepLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelRepNameLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelRepNameCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelRepPassportLabel = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelRepPassportLine = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelRepPassportCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelConfirm = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelSignHead = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLineSignHead = new DevExpress.XtraReports.UI.XRLine();
            this.xrLabelSignHeadName = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelSignHeadCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelSignRep = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLineSignRep = new DevExpress.XtraReports.UI.XRLine();
            this.xrLabelSignRepName = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelSignRepCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelSignRepSigCaption = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabelWorkerResponsibility = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 35F;
            // 
            // PageHeader
            // 
            this.PageHeader.Expanded = false;
            this.PageHeader.HeightF = 0F;
            this.PageHeader.Visible = false;
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel1,
            this.xrRichHeader,
            this.xrLabelTitle,
            this.xrRichBody,
            this.xrLabelCompanyLine,
            this.xrLabelCompanyCaption,
            this.xrLabelCompanyRegistryLine,
            this.xrLabelCompanyRegistryCaption,
            this.xrLabelWorkerIntro,
            this.xrLabelWorkerNameLine,
            this.xrLabelWorkerNameCaption,
            this.xrLabelWorkerDobLine,
            this.xrLabelWorkerDobCaption,
            this.xrLabelWorkerResponsibility,
            this.xrLabelCompanyHeadLabel,
            this.xrLabelCompanyHeadNameLine,
            this.xrLabelCompanyHeadNameCaption,
            this.xrLabelCompanyHeadPassportLabel,
            this.xrLabelCompanyHeadPassportLine,
            this.xrLabelCompanyHeadPassportCaption,
            this.xrLabelRepLabel,
            this.xrLabelRepNameLine,
            this.xrLabelRepNameCaption,
            this.xrLabelRepPassportLabel,
            this.xrLabelRepPassportLine,
            this.xrLabelRepPassportCaption,
            this.xrLabelConfirm,
            this.xrLabelSignHead,
            this.xrLineSignHead,
            this.xrLabelSignHeadName,
            this.xrLabelSignHeadCaption,
            this.xrLabelSignRep,
            this.xrLineSignRep,
            this.xrLabelSignRepName,
            this.xrLabelSignRepCaption,
            this.xrLabelSignRepSigCaption});
            this.Detail.HeightF = 803.3889F;
            // 
            // ReportFooter
            // 
            this.ReportFooter.HeightF = 0F;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 40F;
            // 
            // xrLabelAppNumber
            // 
            this.xrLabelAppNumber.Visible = false;
            // 
            // xrLabelAppDate
            // 
            this.xrLabelAppDate.Visible = false;
            // 
            // xrLabelSignatoryPosition
            // 
            this.xrLabelSignatoryPosition.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            // 
            // xrLabelSignatoryFullName
            // 
            this.xrLabelSignatoryFullName.LocationFloat = new DevExpress.Utils.PointFloat(393F, 0F);
            // 
            // xrRichHeader
            // 
            this.xrRichHeader.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichHeader.CanGrow = false;
            this.xrRichHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrRichHeader.Name = "xrRichHeader";
            this.xrRichHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(0F, 0F, 0F, 0F, 100F);
            this.xrRichHeader.SerializableRtfString = resources.GetString("xrRichHeader.SerializableRtfString");
            this.xrRichHeader.SizeF = new System.Drawing.SizeF(746.7717F, 88F);
            // 
            // xrLabelTitle
            // 
            this.xrLabelTitle.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabelTitle.CanGrow = false;
            this.xrLabelTitle.Font = new DevExpress.Drawing.DXFont("Times New Roman", 16F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 92F);
            this.xrLabelTitle.Name = "xrLabelTitle";
            this.xrLabelTitle.Padding = new DevExpress.XtraPrinting.PaddingInfo(0F, 0F, 0F, 0F, 100F);
            this.xrLabelTitle.SizeF = new System.Drawing.SizeF(746.7717F, 28F);
            this.xrLabelTitle.Text = "BORÇNAMA";
            this.xrLabelTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrRichBody
            // 
            this.xrRichBody.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichBody.LocationFloat = new DevExpress.Utils.PointFloat(0F, 434F);
            this.xrRichBody.Name = "xrRichBody";
            this.xrRichBody.Padding = new DevExpress.XtraPrinting.PaddingInfo(0F, 0F, 0F, 4F, 100F);
            this.xrRichBody.SerializableRtfString = resources.GetString("xrRichBody.SerializableRtfString");
            this.xrRichBody.SizeF = new System.Drawing.SizeF(746.7717F, 260F);
            // 
            // xrLabelCompanyLine
            // 
            this.xrLabelCompanyLine.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabelCompanyLine.BorderWidth = 1F;
            this.xrLabelCompanyLine.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Application_SponsorName]")});
            this.xrLabelCompanyLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 12F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelCompanyLine.LocationFloat = new DevExpress.Utils.PointFloat(0F, 124F);
            this.xrLabelCompanyLine.Name = "xrLabelCompanyLine";
            this.xrLabelCompanyLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelCompanyLine.SizeF = new System.Drawing.SizeF(746.7717F, 22F);
            this.xrLabelCompanyLine.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomLeft;
            // 
            // xrLabelCompanyCaption
            // 
            this.xrLabelCompanyCaption.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabelCompanyCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelCompanyCaption.LocationFloat = new DevExpress.Utils.PointFloat(0F, 146F);
            this.xrLabelCompanyCaption.Name = "xrLabelCompanyCaption";
            this.xrLabelCompanyCaption.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelCompanyCaption.SizeF = new System.Drawing.SizeF(746.7717F, 14F);
            this.xrLabelCompanyCaption.Text = "(kärhananyň ady, hukuk guramasyçylyk görnüşi)";
            this.xrLabelCompanyCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelCompanyRegistryLine
            // 
            this.xrLabelCompanyRegistryLine.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabelCompanyRegistryLine.BorderWidth = 1F;
            this.xrLabelCompanyRegistryLine.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Application_CompanyRegistryAddressLine]")});
            this.xrLabelCompanyRegistryLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelCompanyRegistryLine.LocationFloat = new DevExpress.Utils.PointFloat(0F, 162F);
            this.xrLabelCompanyRegistryLine.Name = "xrLabelCompanyRegistryLine";
            this.xrLabelCompanyRegistryLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelCompanyRegistryLine.SizeF = new System.Drawing.SizeF(746.7717F, 22F);
            this.xrLabelCompanyRegistryLine.StylePriority.UseBorderWidth = false;
            this.xrLabelCompanyRegistryLine.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomLeft;
            // 
            // xrLabelCompanyRegistryCaption
            // 
            this.xrLabelCompanyRegistryCaption.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabelCompanyRegistryCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelCompanyRegistryCaption.LocationFloat = new DevExpress.Utils.PointFloat(0F, 184F);
            this.xrLabelCompanyRegistryCaption.Name = "xrLabelCompanyRegistryCaption";
            this.xrLabelCompanyRegistryCaption.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelCompanyRegistryCaption.SizeF = new System.Drawing.SizeF(746.7717F, 14F);
            this.xrLabelCompanyRegistryCaption.Text = "(hasaba alynan belgisi, ýuridiki salgysy)";
            this.xrLabelCompanyRegistryCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelWorkerIntro
            // 
            this.xrLabelWorkerIntro.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabelWorkerIntro.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelWorkerIntro.LocationFloat = new DevExpress.Utils.PointFloat(0F, 206F);
            this.xrLabelWorkerIntro.Name = "xrLabelWorkerIntro";
            this.xrLabelWorkerIntro.Padding = new DevExpress.XtraPrinting.PaddingInfo(0F, 0F, 0F, 0F, 100F);
            this.xrLabelWorkerIntro.SizeF = new System.Drawing.SizeF(746.7717F, 18F);
            this.xrLabelWorkerIntro.Text = "Ýokarda ady görkezilen kärhana tarapyndan Türkmenistanyň çägine işlemek üçin çagy" +
    "rylan";
            this.xrLabelWorkerIntro.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabelWorkerNameLine
            // 
            this.xrLabelWorkerNameLine.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabelWorkerNameLine.BorderWidth = 1F;
            this.xrLabelWorkerNameLine.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Person_FullName]")});
            this.xrLabelWorkerNameLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, ((DevExpress.Drawing.DXFontStyle)((DevExpress.Drawing.DXFontStyle.Italic | DevExpress.Drawing.DXFontStyle.Underline))));
            this.xrLabelWorkerNameLine.LocationFloat = new DevExpress.Utils.PointFloat(0F, 226F);
            this.xrLabelWorkerNameLine.Name = "xrLabelWorkerNameLine";
            this.xrLabelWorkerNameLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelWorkerNameLine.SizeF = new System.Drawing.SizeF(334F, 20F);
            this.xrLabelWorkerNameLine.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            // 
            // xrLabelWorkerNameCaption
            // 
            this.xrLabelWorkerNameCaption.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabelWorkerNameCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelWorkerNameCaption.LocationFloat = new DevExpress.Utils.PointFloat(0F, 246F);
            this.xrLabelWorkerNameCaption.Name = "xrLabelWorkerNameCaption";
            this.xrLabelWorkerNameCaption.Padding = new DevExpress.XtraPrinting.PaddingInfo(0F, 0F, 0F, 0F, 100F);
            this.xrLabelWorkerNameCaption.SizeF = new System.Drawing.SizeF(334F, 12F);
            this.xrLabelWorkerNameCaption.Text = "(ady, familliýasy, atasynyň ady, doglan senesi)";
            this.xrLabelWorkerNameCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelWorkerDobLine
            // 
            this.xrLabelWorkerDobLine.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabelWorkerDobLine.BorderWidth = 1F;
            this.xrLabelWorkerDobLine.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Person_DateOfBirthText]")});
            this.xrLabelWorkerDobLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelWorkerDobLine.LocationFloat = new DevExpress.Utils.PointFloat(335F, 226F);
            this.xrLabelWorkerDobLine.Name = "xrLabelWorkerDobLine";
            this.xrLabelWorkerDobLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelWorkerDobLine.SizeF = new System.Drawing.SizeF(119F, 20F);
            this.xrLabelWorkerDobLine.StylePriority.UseBorderWidth = false;
            this.xrLabelWorkerDobLine.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            // 
            // xrLabelWorkerDobCaption
            // 
            this.xrLabelWorkerDobCaption.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabelWorkerDobCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelWorkerDobCaption.LocationFloat = new DevExpress.Utils.PointFloat(335F, 246F);
            this.xrLabelWorkerDobCaption.Name = "xrLabelWorkerDobCaption";
            this.xrLabelWorkerDobCaption.Padding = new DevExpress.XtraPrinting.PaddingInfo(0F, 0F, 0F, 0F, 100F);
            this.xrLabelWorkerDobCaption.SizeF = new System.Drawing.SizeF(119F, 12F);
            this.xrLabelWorkerDobCaption.Text = "(doglan senesi)";
            this.xrLabelWorkerDobCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelCompanyHeadLabel
            // 
            this.xrLabelCompanyHeadLabel.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelCompanyHeadLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 270F);
            this.xrLabelCompanyHeadLabel.Name = "xrLabelCompanyHeadLabel";
            this.xrLabelCompanyHeadLabel.SizeF = new System.Drawing.SizeF(254.7222F, 18.00003F);
            this.xrLabelCompanyHeadLabel.Text = "Kärhananyň ýolbaşçysy";
            this.xrLabelCompanyHeadLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabelCompanyHeadNameLine
            // 
            this.xrLabelCompanyHeadNameLine.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabelCompanyHeadNameLine.BorderWidth = 1F;
            this.xrLabelCompanyHeadNameLine.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead_FullName]")});
            this.xrLabelCompanyHeadNameLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, ((DevExpress.Drawing.DXFontStyle)((DevExpress.Drawing.DXFontStyle.Italic | DevExpress.Drawing.DXFontStyle.Underline))));
            this.xrLabelCompanyHeadNameLine.LocationFloat = new DevExpress.Utils.PointFloat(272.0833F, 270F);
            this.xrLabelCompanyHeadNameLine.Name = "xrLabelCompanyHeadNameLine";
            this.xrLabelCompanyHeadNameLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelCompanyHeadNameLine.SizeF = new System.Drawing.SizeF(474.6884F, 18.00003F);
            this.xrLabelCompanyHeadNameLine.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            // 
            // xrLabelCompanyHeadNameCaption
            // 
            this.xrLabelCompanyHeadNameCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelCompanyHeadNameCaption.LocationFloat = new DevExpress.Utils.PointFloat(272.0833F, 288F);
            this.xrLabelCompanyHeadNameCaption.Name = "xrLabelCompanyHeadNameCaption";
            this.xrLabelCompanyHeadNameCaption.SizeF = new System.Drawing.SizeF(474.6884F, 12F);
            this.xrLabelCompanyHeadNameCaption.Text = "(familliýasy, ady, atasynyň ady)";
            this.xrLabelCompanyHeadNameCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelCompanyHeadPassportLabel
            // 
            this.xrLabelCompanyHeadPassportLabel.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelCompanyHeadPassportLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 308F);
            this.xrLabelCompanyHeadPassportLabel.Name = "xrLabelCompanyHeadPassportLabel";
            this.xrLabelCompanyHeadPassportLabel.SizeF = new System.Drawing.SizeF(70F, 18F);
            this.xrLabelCompanyHeadPassportLabel.Text = "pasporty";
            this.xrLabelCompanyHeadPassportLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabelCompanyHeadPassportLine
            // 
            this.xrLabelCompanyHeadPassportLine.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabelCompanyHeadPassportLine.BorderWidth = 1F;
            this.xrLabelCompanyHeadPassportLine.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead_PassportLine]")});
            this.xrLabelCompanyHeadPassportLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelCompanyHeadPassportLine.LocationFloat = new DevExpress.Utils.PointFloat(75F, 308F);
            this.xrLabelCompanyHeadPassportLine.Name = "xrLabelCompanyHeadPassportLine";
            this.xrLabelCompanyHeadPassportLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelCompanyHeadPassportLine.SizeF = new System.Drawing.SizeF(671.7717F, 18F);
            this.xrLabelCompanyHeadPassportLine.StylePriority.UseBorderWidth = false;
            this.xrLabelCompanyHeadPassportLine.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomLeft;
            // 
            // xrLabelCompanyHeadPassportCaption
            // 
            this.xrLabelCompanyHeadPassportCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelCompanyHeadPassportCaption.LocationFloat = new DevExpress.Utils.PointFloat(75F, 326F);
            this.xrLabelCompanyHeadPassportCaption.Name = "xrLabelCompanyHeadPassportCaption";
            this.xrLabelCompanyHeadPassportCaption.SizeF = new System.Drawing.SizeF(671.7717F, 12F);
            this.xrLabelCompanyHeadPassportCaption.Text = "(pasportyň seriýasy, belgisi, nirede we haçan berildi)";
            this.xrLabelCompanyHeadPassportCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelRepLabel
            // 
            this.xrLabelRepLabel.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelRepLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 346F);
            this.xrLabelRepLabel.Name = "xrLabelRepLabel";
            this.xrLabelRepLabel.SizeF = new System.Drawing.SizeF(330F, 18F);
            this.xrLabelRepLabel.Text = "we Kärhananyň wiza işleri boýunça ygtyýarly wekili:";
            this.xrLabelRepLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabelRepNameLine
            // 
            this.xrLabelRepNameLine.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabelRepNameLine.BorderWidth = 1F;
            this.xrLabelRepNameLine.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Representative_FullName]")});
            this.xrLabelRepNameLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelRepNameLine.LocationFloat = new DevExpress.Utils.PointFloat(335F, 346F);
            this.xrLabelRepNameLine.Name = "xrLabelRepNameLine";
            this.xrLabelRepNameLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelRepNameLine.SizeF = new System.Drawing.SizeF(411.7717F, 18F);
            this.xrLabelRepNameLine.StylePriority.UseBorderWidth = false;
            this.xrLabelRepNameLine.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            // 
            // xrLabelRepNameCaption
            // 
            this.xrLabelRepNameCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelRepNameCaption.LocationFloat = new DevExpress.Utils.PointFloat(335F, 364F);
            this.xrLabelRepNameCaption.Name = "xrLabelRepNameCaption";
            this.xrLabelRepNameCaption.SizeF = new System.Drawing.SizeF(411.7717F, 12F);
            this.xrLabelRepNameCaption.Text = "(familliýasy, ady, atasynyň ady)";
            this.xrLabelRepNameCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelRepPassportLabel
            // 
            this.xrLabelRepPassportLabel.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelRepPassportLabel.LocationFloat = new DevExpress.Utils.PointFloat(0F, 384F);
            this.xrLabelRepPassportLabel.Name = "xrLabelRepPassportLabel";
            this.xrLabelRepPassportLabel.SizeF = new System.Drawing.SizeF(70F, 18F);
            this.xrLabelRepPassportLabel.Text = "pasporty";
            this.xrLabelRepPassportLabel.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabelRepPassportLine
            // 
            this.xrLabelRepPassportLine.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabelRepPassportLine.BorderWidth = 1F;
            this.xrLabelRepPassportLine.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Representative_PassportLine]")});
            this.xrLabelRepPassportLine.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelRepPassportLine.LocationFloat = new DevExpress.Utils.PointFloat(75F, 384F);
            this.xrLabelRepPassportLine.Name = "xrLabelRepPassportLine";
            this.xrLabelRepPassportLine.Padding = new DevExpress.XtraPrinting.PaddingInfo(2F, 2F, 0F, 0F, 100F);
            this.xrLabelRepPassportLine.SizeF = new System.Drawing.SizeF(671.7717F, 18F);
            this.xrLabelRepPassportLine.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomLeft;
            // 
            // xrLabelRepPassportCaption
            // 
            this.xrLabelRepPassportCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelRepPassportCaption.LocationFloat = new DevExpress.Utils.PointFloat(75F, 402F);
            this.xrLabelRepPassportCaption.Name = "xrLabelRepPassportCaption";
            this.xrLabelRepPassportCaption.SizeF = new System.Drawing.SizeF(671.7717F, 12F);
            this.xrLabelRepPassportCaption.Text = "(pasportyň seriýasy, belgisi, nirede we haçan berildi, telefon belgisi)";
            this.xrLabelRepPassportCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelConfirm
            // 
            this.xrLabelConfirm.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelConfirm.LocationFloat = new DevExpress.Utils.PointFloat(0F, 697F);
            this.xrLabelConfirm.Name = "xrLabelConfirm";
            this.xrLabelConfirm.SizeF = new System.Drawing.SizeF(300F, 18F);
            this.xrLabelConfirm.Text = "Borçnamany tassyklaýarys:";
            // 
            // xrLabelSignHead
            // 
            this.xrLabelSignHead.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelSignHead.LocationFloat = new DevExpress.Utils.PointFloat(0F, 721F);
            this.xrLabelSignHead.Name = "xrLabelSignHead";
            this.xrLabelSignHead.SizeF = new System.Drawing.SizeF(160F, 18F);
            this.xrLabelSignHead.Text = "Kärhananyň ýolbaşçysy:";
            // 
            // xrLineSignHead
            // 
            this.xrLineSignHead.LocationFloat = new DevExpress.Utils.PointFloat(166F, 737F);
            this.xrLineSignHead.Name = "xrLineSignHead";
            this.xrLineSignHead.SizeF = new System.Drawing.SizeF(580.7717F, 2.083374F);
            // 
            // xrLabelSignHeadName
            // 
            this.xrLabelSignHeadName.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CompanyHead_FullName]")});
            this.xrLabelSignHeadName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelSignHeadName.LocationFloat = new DevExpress.Utils.PointFloat(220F, 719F);
            this.xrLabelSignHeadName.Name = "xrLabelSignHeadName";
            this.xrLabelSignHeadName.SizeF = new System.Drawing.SizeF(350F, 18F);
            this.xrLabelSignHeadName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelSignHeadCaption
            // 
            this.xrLabelSignHeadCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelSignHeadCaption.LocationFloat = new DevExpress.Utils.PointFloat(220F, 741F);
            this.xrLabelSignHeadCaption.Name = "xrLabelSignHeadCaption";
            this.xrLabelSignHeadCaption.SizeF = new System.Drawing.SizeF(350F, 12F);
            this.xrLabelSignHeadCaption.Text = "(familliýasy, ady, atasynyň ady)";
            this.xrLabelSignHeadCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelSignRep
            // 
            this.xrLabelSignRep.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelSignRep.LocationFloat = new DevExpress.Utils.PointFloat(0F, 769F);
            this.xrLabelSignRep.Name = "xrLabelSignRep";
            this.xrLabelSignRep.SizeF = new System.Drawing.SizeF(320F, 18F);
            this.xrLabelSignRep.Text = "Kärhananyň wiza işleri boýunça ygtyýarly wezipeli işgäri:";
            // 
            // xrLineSignRep
            // 
            this.xrLineSignRep.LocationFloat = new DevExpress.Utils.PointFloat(325F, 785F);
            this.xrLineSignRep.Name = "xrLineSignRep";
            this.xrLineSignRep.SizeF = new System.Drawing.SizeF(421.7716F, 2.083374F);
            // 
            // xrLabelSignRepName
            // 
            this.xrLabelSignRepName.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Representative_FullName]")});
            this.xrLabelSignRepName.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F, DevExpress.Drawing.DXFontStyle.Bold);
            this.xrLabelSignRepName.LocationFloat = new DevExpress.Utils.PointFloat(330F, 767F);
            this.xrLabelSignRepName.Name = "xrLabelSignRepName";
            this.xrLabelSignRepName.SizeF = new System.Drawing.SizeF(295F, 18F);
            this.xrLabelSignRepName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelSignRepCaption
            // 
            this.xrLabelSignRepCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelSignRepCaption.LocationFloat = new DevExpress.Utils.PointFloat(330F, 789F);
            this.xrLabelSignRepCaption.Name = "xrLabelSignRepCaption";
            this.xrLabelSignRepCaption.SizeF = new System.Drawing.SizeF(295F, 12F);
            this.xrLabelSignRepCaption.Text = "(familliýasy, ady, atasynyň ady)";
            this.xrLabelSignRepCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelSignRepSigCaption
            // 
            this.xrLabelSignRepSigCaption.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabelSignRepSigCaption.LocationFloat = new DevExpress.Utils.PointFloat(635F, 789F);
            this.xrLabelSignRepSigCaption.Name = "xrLabelSignRepSigCaption";
            this.xrLabelSignRepSigCaption.SizeF = new System.Drawing.SizeF(111.7717F, 12F);
            this.xrLabelSignRepSigCaption.Text = "(gol)";
            this.xrLabelSignRepSigCaption.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabelWorkerResponsibility
            // 
            this.xrLabelWorkerResponsibility.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabelWorkerResponsibility.Font = new DevExpress.Drawing.DXFont("Times New Roman", 11F);
            this.xrLabelWorkerResponsibility.LocationFloat = new DevExpress.Utils.PointFloat(454F, 226F);
            this.xrLabelWorkerResponsibility.Name = "xrLabelWorkerResponsibility";
            this.xrLabelWorkerResponsibility.Padding = new DevExpress.XtraPrinting.PaddingInfo(0F, 0F, 0F, 0F, 100F);
            this.xrLabelWorkerResponsibility.SizeF = new System.Drawing.SizeF(292.7716F, 31.99998F);
            this.xrLabelWorkerResponsibility.Text = "jogapkärçiligini öz üstümize alýarys:";
            this.xrLabelWorkerResponsibility.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel1
            // 
            this.xrLabel1.Font = new DevExpress.Drawing.DXFont("Times New Roman", 8F, DevExpress.Drawing.DXFontStyle.Italic);
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(636F, 743.3889F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.SizeF = new System.Drawing.SizeF(111.7717F, 12F);
            this.xrLabel1.Text = "(gol)";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // AppInvAndWPBorcnamaItemReport
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.TopMargin,
            this.PageHeader,
            this.Detail,
            this.ReportFooter,
            this.BottomMargin});
            this.Margins = new DevExpress.Drawing.DXMargins(27F, 43F, 35F, 40F);
            this.Version = "25.2";
            ((System.ComponentModel.ISupportInitialize)(this.xrRichHeader)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichBody)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        private XRRichText xrRichHeader;
        private XRLabel xrLabelTitle;
        private XRRichText xrRichBody;

        private XRLabel xrLabelCompanyLine;
        private XRLabel xrLabelCompanyCaption;
        private XRLabel xrLabelCompanyRegistryLine;
        private XRLabel xrLabelCompanyRegistryCaption;
        private XRLabel xrLabelWorkerIntro;
        private XRLabel xrLabelWorkerNameLine;
        private XRLabel xrLabelWorkerNameCaption;
        private XRLabel xrLabelWorkerDobLine;
        private XRLabel xrLabelWorkerDobCaption;
        private XRLabel xrLabelWorkerResponsibility;
        private XRLabel xrLabelCompanyHeadLabel;
        private XRLabel xrLabelCompanyHeadNameLine;
        private XRLabel xrLabelCompanyHeadNameCaption;
        private XRLabel xrLabelCompanyHeadPassportLabel;
        private XRLabel xrLabelCompanyHeadPassportLine;
        private XRLabel xrLabelCompanyHeadPassportCaption;
        private XRLabel xrLabelRepLabel;
        private XRLabel xrLabelRepNameLine;
        private XRLabel xrLabelRepNameCaption;
        private XRLabel xrLabelRepPassportLabel;
        private XRLabel xrLabelRepPassportLine;
        private XRLabel xrLabelRepPassportCaption;
        private XRLabel xrLabelConfirm;
        private XRLabel xrLabelSignHead;
        private XRLine xrLineSignHead;
        private XRLabel xrLabelSignHeadName;
        private XRLabel xrLabelSignHeadCaption;
        private XRLabel xrLabelSignRep;
        private XRLine xrLineSignRep;
        private XRLabel xrLabelSignRepName;
        private XRLabel xrLabelSignRepCaption;
        private XRLabel xrLabelSignRepSigCaption;
        private XRLabel xrLabel1;
    }
}
