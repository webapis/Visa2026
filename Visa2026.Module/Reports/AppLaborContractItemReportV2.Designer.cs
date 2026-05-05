using DevExpress.Drawing;
using DevExpress.Utils;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
﻿namespace Visa2026.Module.Reports
{
    partial class AppLaborContractItemReportV2
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            this.lblTitle = new XRLabel();
            this.lblCity = new XRLabel();
            this.tableBody = new XRTable();
            this.tableRowIntro = new XRTableRow();
            this.tableCellIntro = new XRTableCell();
            this.tableRowSpacer1 = new XRTableRow();
            this.tableCellSpacer1 = new XRTableCell();
            this.tableRowSection1Header = new XRTableRow();
            this.tableCellSection1Header = new XRTableCell();
            this.tableRowSection1Body = new XRTableRow();
            this.tableCellSection1Body = new XRTableCell();
            this.tableRowSpacer2 = new XRTableRow();
            this.tableCellSpacer2 = new XRTableCell();
            this.tableRowSection2Header = new XRTableRow();
            this.tableCellSection2Header = new XRTableCell();
            this.tableRowSection2Body = new XRTableRow();
            this.tableCellSection2Body = new XRTableCell();
            this.tableRowSpacer3 = new XRTableRow();
            this.tableCellSpacer3 = new XRTableCell();
            this.tableRowSection3Header = new XRTableRow();
            this.tableCellSection3Header = new XRTableCell();
            this.tableRowSection3Body = new XRTableRow();
            this.tableCellSection3Body = new XRTableCell();
            this.tableRowSpacer4 = new XRTableRow();
            this.tableCellSpacer4 = new XRTableCell();
            this.tableRowSection4Header = new XRTableRow();
            this.tableCellSection4Header = new XRTableCell();
            this.tableRowSection4Body = new XRTableRow();
            this.tableCellSection4Body = new XRTableCell();
            this.tableRowSpacer5 = new XRTableRow();
            this.tableCellSpacer5 = new XRTableCell();
            this.tableRowSection5Header = new XRTableRow();
            this.tableCellSection5Header = new XRTableCell();
            this.tableRowSection5Line1 = new XRTableRow();
            this.tableCellSection5Line1 = new XRTableCell();
            this.tableRowSection5Line2 = new XRTableRow();
            this.tableCellSection5Line2 = new XRTableCell();
            this.tableRowSpacer6 = new XRTableRow();
            this.tableCellSpacer6 = new XRTableCell();
            this.tableRowSection6Header = new XRTableRow();
            this.tableCellSection6Header = new XRTableCell();
            this.tableRowSection6Line1 = new XRTableRow();
            this.tableCellSection6Line1 = new XRTableCell();
            this.tableRowSection6Line2 = new XRTableRow();
            this.tableCellSection6Line2 = new XRTableCell();
            this.tableRowSpacer7 = new XRTableRow();
            this.tableCellSpacer7 = new XRTableCell();
            this.tableRowSection7Header = new XRTableRow();
            this.tableCellSection7Header = new XRTableCell();
            this.tableRowSpacer8 = new XRTableRow();
            this.tableCellSpacer8 = new XRTableCell();
            this.tableRowSignatures = new XRTableRow();
            this.tableCellSignatures = new XRTableCell();
            this.panelSignatures = new XRPanel();
            this.lblEmployerHeader = new XRLabel();
            this.lblEmployerSignatory = new XRLabel();
            this.lblEmployerCompany = new XRLabel();
            this.lblEmployerAddress = new XRLabel();
            this.lblEmployeeHeader = new XRLabel();
            this.lblEmployeeName = new XRLabel();
            this.lblEmployeePassport = new XRLabel();

            this.TopMargin.HeightF = 30F;
            this.BottomMargin.HeightF = 40F;

            //
            // lblTitle
            //
            this.lblTitle.Borders = BorderSide.None;
            this.lblTitle.CanGrow = false;
            this.lblTitle.Font = new DXFont("Times New Roman", 13F, DXFontStyle.Bold);
            this.lblTitle.LocationFloat = new PointFloat(0F, 0F);
            this.lblTitle.Multiline = true;
            this.lblTitle.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblTitle.SizeF = new System.Drawing.SizeF(626.7717F, 26F);
            this.lblTitle.Text = "Z\u00C4HMET \u015EERTNAMASY";
            this.lblTitle.TextAlignment = TextAlignment.MiddleCenter;

            //
            // lblCity
            //
            this.lblCity.Borders = BorderSide.None;
            this.lblCity.CanGrow = false;
            this.lblCity.Font = new DXFont("Times New Roman", 11F, DXFontStyle.Bold);
            this.lblCity.LocationFloat = new PointFloat(0F, 30F);
            this.lblCity.Multiline = true;
            this.lblCity.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblCity.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblCity.Text = "A\u015Egabat \u015E\u00E4heri";
            this.lblCity.TextAlignment = TextAlignment.TopCenter;

            //
            // tableBody
            //
            this.tableBody.Borders = BorderSide.None;
            this.tableBody.Font = new DXFont("Times New Roman", 11F);
            this.tableBody.LocationFloat = new PointFloat(0F, 52F);
            this.tableBody.Name = "tableBody";
            this.tableBody.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.tableBody.Rows.AddRange(new XRTableRow[]
            {
                this.tableRowIntro,
                this.tableRowSpacer1,
                this.tableRowSection1Header,
                this.tableRowSection1Body,
                this.tableRowSpacer2,
                this.tableRowSection2Header,
                this.tableRowSection2Body,
                this.tableRowSpacer3,
                this.tableRowSection3Header,
                this.tableRowSection3Body,
                this.tableRowSpacer4,
                this.tableRowSection4Header,
                this.tableRowSection4Body,
                this.tableRowSpacer5,
                this.tableRowSection5Header,
                this.tableRowSection5Line1,
                this.tableRowSection5Line2,
                this.tableRowSpacer6,
                this.tableRowSection6Header,
                this.tableRowSection6Line1,
                this.tableRowSection6Line2,
                this.tableRowSpacer7,
                this.tableRowSection7Header,
                this.tableRowSpacer8,
                this.tableRowSignatures
            });
            this.tableBody.SizeF = new System.Drawing.SizeF(626.7717F, 0F);
            this.tableBody.StylePriority.UseBorders = false;
            this.tableBody.StylePriority.UseFont = false;
            this.tableBody.StylePriority.UseTextAlignment = false;
            this.tableBody.TextAlignment = TextAlignment.TopLeft;

            this.tableRowIntro.Cells.AddRange(new XRTableCell[] { this.tableCellIntro });
            this.tableRowIntro.HeightF = 78F;
            this.tableCellIntro.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "FormatString(\"\\\"\\u00C7alyk Enerji Senagat we Tijaret A.\\u015E.\\\" T\\u00FCrk k\\u00E4rhanasyny\\u0148 T\\u00FCrkmenistandaky \\u015Faham\\u00E7asyny\\u0148 M\\u00FAdiri {0} bilen mundan be\\u00FDl\\u00E4k \\\"I\\u015FG\\u00C4R\\\" di\\u00FDip atlandyryl\\u00FDan {1} arasynda z\\u00E4hmet \\u015Fertnamasy bagla\\u015Fyldy. I\\u015FG\\u00C4R {2} wezipesine i\\u015Fe kabul edil\\u00FD\\u00E4r.\", [Application_SponsorSignatory], [Person_FullName], [Position_PositionTm])")
            });
            this.tableCellIntro.Multiline = true;
            this.tableCellIntro.Padding = new PaddingInfo(0F, 0F, 0F, 4F, 100F);
            this.tableCellIntro.TextAlignment = TextAlignment.TopJustify;

            this.tableRowSpacer1.Cells.AddRange(new XRTableCell[] { this.tableCellSpacer1 });
            this.tableRowSpacer1.HeightF = 26F;
            this.tableCellSpacer1.Text = "";

            this.tableRowSection1Header.Cells.AddRange(new XRTableCell[] { this.tableCellSection1Header });
            this.tableRowSection1Header.HeightF = 24F;
            this.tableCellSection1Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.tableCellSection1Header.Text = "1. I\u015f berijini\u0148 bor\u00e7lary";
            this.tableCellSection1Header.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSection1Body.Cells.AddRange(new XRTableCell[] { this.tableCellSection1Body });
            this.tableRowSection1Body.HeightF = 96F;
            this.tableCellSection1Body.Multiline = true;
            this.tableCellSection1Body.Padding = new PaddingInfo(20F, 0F, 0F, 4F, 100F);
            this.tableCellSection1Body.Text = "1.1. H\u00fcn\u00e4rine g\u00f6r\u00e4 i\u015f bilen \u00fcpj\u00fcn etmelidir.\r\n1.2. Her a\u00fd a\u00fdlyk z\u00e4hmet hakyny bellenilen g\u00fcnde t\u00f6lemelidir.\r\n1.3. Hereket ed\u00fd\u00e4n T\u00fcrkmenistany\u0148 Z\u00e4hmet baradaky kanunlar kodeksine la\u00fdklykda kesgitlenen m\u00f6hletde \u00fdyllyk z\u00e4hmet rugsadyny bermelidir.\r\n1.4. \u015eertnamany\u0148 m\u00f6hleti boyunca hereket ed\u00fd\u00e4n T\u00fcrkmenistany\u0148 Z\u00e4hmet baradaky kanunlar kodeksine la\u00fdklykda i\u015f \u00fc\u00e7in orna\u015fdyrylan d\u00fcz\u00fcmli, sosial goraglary we be\u00fdleki kepillikleri bermelidir.";
            this.tableCellSection1Body.TextAlignment = TextAlignment.TopJustify;

            this.tableRowSpacer2.Cells.AddRange(new XRTableCell[] { this.tableCellSpacer2 });
            this.tableRowSpacer2.HeightF = 26F;
            this.tableCellSpacer2.Text = "";

            this.tableRowSection2Header.Cells.AddRange(new XRTableCell[] { this.tableCellSection2Header });
            this.tableRowSection2Header.HeightF = 24F;
            this.tableCellSection2Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.tableCellSection2Header.Text = "2. I\u015fg\u00e4ri\u0148 bor\u00e7lary";
            this.tableCellSection2Header.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSection2Body.Cells.AddRange(new XRTableCell[] { this.tableCellSection2Body });
            this.tableRowSection2Body.HeightF = 118F;
            this.tableCellSection2Body.Multiline = true;
            this.tableCellSection2Body.Padding = new PaddingInfo(20F, 0F, 0F, 4F, 100F);
            this.tableCellSection2Body.Text = "2.1. Bu \u015fertnama bo\u00fdun\u00e7a tab\u015fyrylan i\u015fleri dogry we doly \u00fdyerine \u00fdyetirmeli.\r\n2.2. K\u00e4rhanadaky i\u00e7erki d\u00fczg\u00fcn, tehniki howpsuzlyk hem-de \u00f6n\u00fcm\u00e7ilik tertibine la\u00fdk bolmaly, z\u00e4hmet howpsuzlygy we z\u00e4hmeti goramak d\u00fczg\u00fcnlerini berja\u00fd etmeli.\r\n2.3. I\u015f \u00fdyerini arassa saklamaly we enjamlary \u00e4ti\u00fda\u00e7lylyk bilen ulanmaly.\r\n2.4. Kompani\u00fdany\u0148 hyzmat syrlaryny gizlin saklamaly.\r\n2.5. B\u00f6l\u00fcn ba\u015flygyny\u0148 g\u00f6rkezmelerini \u00fdyerine \u00fdyetirmeli.";
            this.tableCellSection2Body.TextAlignment = TextAlignment.TopJustify;

            this.tableRowSpacer3.Cells.AddRange(new XRTableCell[] { this.tableCellSpacer3 });
            this.tableRowSpacer3.HeightF = 26F;
            this.tableCellSpacer3.Text = "";

            this.tableRowSection3Header.Cells.AddRange(new XRTableCell[] { this.tableCellSection3Header });
            this.tableRowSection3Header.HeightF = 24F;
            this.tableCellSection3Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.tableCellSection3Header.Text = "3. I\u015f we dyn\u00e7 aly\u015f d\u00fczg\u00fcn\u00fd";
            this.tableCellSection3Header.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSection3Body.Cells.AddRange(new XRTableCell[] { this.tableCellSection3Body });
            this.tableRowSection3Body.HeightF = 90F;
            this.tableCellSection3Body.Multiline = true;
            this.tableCellSection3Body.Padding = new PaddingInfo(20F, 0F, 0F, 4F, 100F);
            this.tableCellSection3Body.Text = "3.1. I\u015f tertibine we T\u00fcrkmenistany\u0148 Z\u00e4hmet baradaky kanunlar kodeksine la\u00fdk bolmaly.\r\n3.2. I\u015f wagty \u2014 g\u00fcnde 8 sagat, hepdede 6 g\u00fcn.\r\n3.3. Zerur bolanda kanun\u00e7ylykdaky tertipde go\u015fma\u00e7a i\u015fe \u00e7agyrylyp bilin\u00fd\u00e4r.\r\n3.4. A\u00fdlyk z\u00e4hmet haky kadr b\u00f6l\u00fcn\u00fd\u0148 sanawyny\u0148 esasynda t\u00f6len\u00fd\u00e4r.";
            this.tableCellSection3Body.TextAlignment = TextAlignment.TopJustify;

            this.tableRowSpacer4.Cells.AddRange(new XRTableCell[] { this.tableCellSpacer4 });
            this.tableRowSpacer4.HeightF = 26F;
            this.tableCellSpacer4.Text = "";

            this.tableRowSection4Header.Cells.AddRange(new XRTableCell[] { this.tableCellSection4Header });
            this.tableRowSection4Header.HeightF = 24F;
            this.tableCellSection4Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.tableCellSection4Header.Text = "4. Z\u00e4hmet \u015fertnamasyny\u0148 \u00fdatyrylmagy";
            this.tableCellSection4Header.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSection4Body.Cells.AddRange(new XRTableCell[] { this.tableCellSection4Body });
            this.tableRowSection4Body.HeightF = 162F;
            this.tableCellSection4Body.Multiline = true;
            this.tableCellSection4Body.Padding = new PaddingInfo(20F, 0F, 0F, 4F, 100F);
            this.tableCellSection4Body.Text = "Z\u00e4hmet \u015fertnamasy \"I\u015f BERIJI\" tarapyndan a\u015fakdaky \u00fdagda\u00fdlarda \u00fdatyryl\u00fd\u00e4r:\r\n4.1. Z\u00e4hmet \u015fertnamasyny\u0148 m\u00f6hletini\u0148 gutarmagy;\r\n4.2. I\u015fleri\u0148 gutarmagy;\r\n4.3. I\u015fg\u00e4ri\u0148 azalmagy;\r\n4.4. I\u015fe serho\u015f bolup, narkotiki \u00fdya-da z\u00e4herli maddalary\u0148 t\u00e4siri astynda gelmegi;\r\n4.5. Z\u00e4hmet \u015fertnamasyna \u00fdya-da k\u00e4rhanany\u0148 i\u00e7erki tertip d\u00fczg\u00fcnlerine la\u00fdklykda \u00f6z\u00fcn\u00e9 tab\u015fyrylan bor\u00e7lary i\u015fg\u00e4ri\u0148 berja\u00fd etm\u00e4n \u00fdyeri\u0148e \u00fdyetirmezligi;\r\n4.6. Kompani\u00fdany\u0148 eml\u00e4gine zeper \u00fdetirende \u00fdya-da ogurlanda;\r\n4.7. Galan dawalar T\u00fcrkmenistany\u0148 hereket ed\u00fd\u00e4n kanunlaryna la\u00fdk \u00e7\u00f6z\u00fcl\u00fd\u00e4r.";
            this.tableCellSection4Body.TextAlignment = TextAlignment.TopJustify;

            this.tableRowSpacer5.Cells.AddRange(new XRTableCell[] { this.tableCellSpacer5 });
            this.tableRowSpacer5.HeightF = 26F;
            this.tableCellSpacer5.Text = "";

            this.tableRowSection5Header.Cells.AddRange(new XRTableCell[] { this.tableCellSection5Header });
            this.tableRowSection5Header.HeightF = 24F;
            this.tableCellSection5Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.tableCellSection5Header.Text = "5. Z\u00e4hmet \u015fertnamasyny\u0148 hereket ed\u00fd\u00e4n m\u00f6hleti";
            this.tableCellSection5Header.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSection5Line1.Cells.AddRange(new XRTableCell[] { this.tableCellSection5Line1 });
            this.tableRowSection5Line1.HeightF = 26F;
            this.tableCellSection5Line1.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "FormatString('Z\u00e4hmet \u015fertnamasy {0} \u2014 {1} aralygynda hereket ed\u00fd\u00e4r.', [Contract_StartDateText], [Contract_ExpirationDateText])")
            });
            this.tableCellSection5Line1.Padding = new PaddingInfo(0F, 0F, 0F, 4F, 100F);
            this.tableCellSection5Line1.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSection5Line2.Cells.AddRange(new XRTableCell[] { this.tableCellSection5Line2 });
            this.tableRowSection5Line2.HeightF = 46F;
            this.tableCellSection5Line2.Multiline = true;
            this.tableCellSection5Line2.Padding = new PaddingInfo(0F, 0F, 0F, 4F, 100F);
            this.tableCellSection5Line2.Text = "\u015eertnama Taraplar gol \u00E7ekenlerinden so\u0148 g\u00FC\u00YDje gir\u00YD\u00E4r. M\u00F6hleti gutaran \u00YDagda\u00FDynda, eger i\u015F gatna\u015Fyklaryny\u0148 hakykaty \u00YD\u00FCz\u00FCnde dowam ed\u00YD\u00E4n bolsa \u00FDya taraplary\u0148 hi\u00E7 biri \u015Fertnamany \u00FDatyrmak barada ikinji tarapa \u00FD\u00Fcz tutmasa, \u015Fertnama ilkinji bagla\u015Fylan g\u00F6rkezilen m\u00F6hlet m\u00F6\u00E7berinde uzadylan hasap edil\u00YD\u00E4r.";
            this.tableCellSection5Line2.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSpacer6.Cells.AddRange(new XRTableCell[] { this.tableCellSpacer6 });
            this.tableRowSpacer6.HeightF = 26F;
            this.tableCellSpacer6.Text = "";

            this.tableRowSection6Header.Cells.AddRange(new XRTableCell[] { this.tableCellSection6Header });
            this.tableRowSection6Header.HeightF = 24F;
            this.tableCellSection6Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.tableCellSection6Header.Text = "6. T\u00fcrkmenistany\u0148 d\u00f6wletinde al\u00fdan a\u00fdlyk z\u00E4hmet haky";
            this.tableCellSection6Header.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSection6Line1.Cells.AddRange(new XRTableCell[] { this.tableCellSection6Line1 });
            this.tableRowSection6Line1.HeightF = 26F;
            this.tableCellSection6Line1.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "FormatString('A\u00fdlyk z\u00e4hmet haky: {0} {1}.', [Contract_SalaryText], [Salary_CurrencyCode])")
            });
            this.tableCellSection6Line1.Padding = new PaddingInfo(0F, 0F, 0F, 4F, 100F);
            this.tableCellSection6Line1.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSection6Line2.Cells.AddRange(new XRTableCell[] { this.tableCellSection6Line2 });
            this.tableRowSection6Line2.HeightF = 26F;
            this.tableCellSection6Line2.Padding = new PaddingInfo(0F, 0F, 0F, 4F, 100F);
            this.tableCellSection6Line2.Text = "T\u00f6leg: z\u00e4hmet haky T\u00fcrki\u00fdyed\u00e4ki bank hasabyna ge\u00e7iril\u00fd\u00e4r.";
            this.tableCellSection6Line2.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSpacer7.Cells.AddRange(new XRTableCell[] { this.tableCellSpacer7 });
            this.tableRowSpacer7.HeightF = 26F;
            this.tableCellSpacer7.Text = "";

            this.tableRowSection7Header.Cells.AddRange(new XRTableCell[] { this.tableCellSection7Header });
            this.tableRowSection7Header.HeightF = 24F;
            this.tableCellSection7Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.tableCellSection7Header.Text = "7. Taraplary\u0148 gollary we salgylary";
            this.tableCellSection7Header.TextAlignment = TextAlignment.TopLeft;

            this.tableRowSpacer8.Cells.AddRange(new XRTableCell[] { this.tableCellSpacer8 });
            this.tableRowSpacer8.HeightF = 26F;
            this.tableCellSpacer8.Text = "";

            this.tableRowSignatures.Cells.AddRange(new XRTableCell[] { this.tableCellSignatures });
            this.tableRowSignatures.HeightF = 110F;
            this.tableCellSignatures.Controls.AddRange(new XRControl[] { this.panelSignatures });
            this.tableCellSignatures.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.tableCellSignatures.Text = "";

            // panelSignatures
            //
            this.panelSignatures.Borders = BorderSide.None;
            this.panelSignatures.CanGrow = false;
            this.panelSignatures.LocationFloat = new PointFloat(0F, 0F);
            this.panelSignatures.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.panelSignatures.SizeF = new System.Drawing.SizeF(626.7717F, 100F);

            //
            // lblEmployerHeader
            //
            this.lblEmployerHeader.Borders = BorderSide.None;
            this.lblEmployerHeader.CanGrow = false;
            this.lblEmployerHeader.Font = new DXFont("Times New Roman", 11F, DXFontStyle.Bold);
            this.lblEmployerHeader.LocationFloat = new PointFloat(0F, 0F);
            this.lblEmployerHeader.Multiline = true;
            this.lblEmployerHeader.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblEmployerHeader.SizeF = new System.Drawing.SizeF(298F, 18F);
            this.lblEmployerHeader.Text = "I\u015E BERIJI:";
            this.lblEmployerHeader.TextAlignment = TextAlignment.TopLeft;

            //
            // lblEmployerSignatory
            //
            this.lblEmployerSignatory.Borders = BorderSide.None;
            this.lblEmployerSignatory.CanGrow = false;
            this.lblEmployerSignatory.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "[Application_SponsorSignatory]")
            });
            this.lblEmployerSignatory.Font = new DXFont("Times New Roman", 11F);
            this.lblEmployerSignatory.LocationFloat = new PointFloat(0F, 20F);
            this.lblEmployerSignatory.Multiline = true;
            this.lblEmployerSignatory.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblEmployerSignatory.SizeF = new System.Drawing.SizeF(298F, 18F);
            this.lblEmployerSignatory.TextAlignment = TextAlignment.TopLeft;

            //
            // lblEmployerCompany
            //
            this.lblEmployerCompany.Borders = BorderSide.None;
            this.lblEmployerCompany.CanGrow = false;
            this.lblEmployerCompany.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "[Application_SponsorName]")
            });
            this.lblEmployerCompany.Font = new DXFont("Times New Roman", 11F);
            this.lblEmployerCompany.LocationFloat = new PointFloat(0F, 40F);
            this.lblEmployerCompany.Multiline = true;
            this.lblEmployerCompany.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblEmployerCompany.SizeF = new System.Drawing.SizeF(298F, 18F);
            this.lblEmployerCompany.TextAlignment = TextAlignment.TopLeft;

            //
            // lblEmployerAddress
            //
            this.lblEmployerAddress.Borders = BorderSide.None;
            this.lblEmployerAddress.CanGrow = true;
            this.lblEmployerAddress.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "[Application_CompanyAddress]")
            });
            this.lblEmployerAddress.Font = new DXFont("Times New Roman", 11F);
            this.lblEmployerAddress.LocationFloat = new PointFloat(0F, 60F);
            this.lblEmployerAddress.Multiline = true;
            this.lblEmployerAddress.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblEmployerAddress.SizeF = new System.Drawing.SizeF(298F, 38F);
            this.lblEmployerAddress.TextAlignment = TextAlignment.TopLeft;

            //
            // lblEmployeeHeader
            //
            this.lblEmployeeHeader.Borders = BorderSide.None;
            this.lblEmployeeHeader.CanGrow = false;
            this.lblEmployeeHeader.Font = new DXFont("Times New Roman", 11F, DXFontStyle.Bold);
            this.lblEmployeeHeader.LocationFloat = new PointFloat(328.7717F, 0F);
            this.lblEmployeeHeader.Multiline = true;
            this.lblEmployeeHeader.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblEmployeeHeader.SizeF = new System.Drawing.SizeF(298F, 18F);
            this.lblEmployeeHeader.Text = "I\u015EG\u00C4R:";
            this.lblEmployeeHeader.TextAlignment = TextAlignment.TopLeft;

            //
            // lblEmployeeName
            //
            this.lblEmployeeName.Borders = BorderSide.None;
            this.lblEmployeeName.CanGrow = false;
            this.lblEmployeeName.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "[Person_FullName]")
            });
            this.lblEmployeeName.Font = new DXFont("Times New Roman", 11F);
            this.lblEmployeeName.LocationFloat = new PointFloat(328.7717F, 20F);
            this.lblEmployeeName.Multiline = true;
            this.lblEmployeeName.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblEmployeeName.SizeF = new System.Drawing.SizeF(298F, 18F);
            this.lblEmployeeName.TextAlignment = TextAlignment.TopLeft;

            //
            // lblEmployeePassport
            //
            this.lblEmployeePassport.Borders = BorderSide.None;
            this.lblEmployeePassport.CanGrow = false;
            this.lblEmployeePassport.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "FormatString('Pasport belgisi: {0}', [Passport_Number])")
            });
            this.lblEmployeePassport.Font = new DXFont("Times New Roman", 11F);
            this.lblEmployeePassport.LocationFloat = new PointFloat(328.7717F, 40F);
            this.lblEmployeePassport.Multiline = true;
            this.lblEmployeePassport.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblEmployeePassport.SizeF = new System.Drawing.SizeF(298F, 18F);
            this.lblEmployeePassport.TextAlignment = TextAlignment.TopLeft;

            this.panelSignatures.Controls.AddRange(new XRControl[]
            {
                this.lblEmployerHeader,
                this.lblEmployerSignatory,
                this.lblEmployerCompany,
                this.lblEmployerAddress,
                this.lblEmployeeHeader,
                this.lblEmployeeName,
                this.lblEmployeePassport
            });

            this.Detail.Controls.AddRange(new XRControl[]
            {
                this.lblTitle,
                this.lblCity,
                this.tableBody
            });

            this.Detail.CanGrow = true;
            this.Detail.HeightF = 980F;

        }

        #endregion

        private XRLabel lblTitle;
        private XRLabel lblCity;
        private XRTable tableBody;
        private XRTableRow tableRowIntro;
        private XRTableCell tableCellIntro;
        private XRTableRow tableRowSpacer1;
        private XRTableCell tableCellSpacer1;
        private XRTableRow tableRowSection1Header;
        private XRTableCell tableCellSection1Header;
        private XRTableRow tableRowSection1Body;
        private XRTableCell tableCellSection1Body;
        private XRTableRow tableRowSpacer2;
        private XRTableCell tableCellSpacer2;
        private XRTableRow tableRowSection2Header;
        private XRTableCell tableCellSection2Header;
        private XRTableRow tableRowSection2Body;
        private XRTableCell tableCellSection2Body;
        private XRTableRow tableRowSpacer3;
        private XRTableCell tableCellSpacer3;
        private XRTableRow tableRowSection3Header;
        private XRTableCell tableCellSection3Header;
        private XRTableRow tableRowSection3Body;
        private XRTableCell tableCellSection3Body;
        private XRTableRow tableRowSpacer4;
        private XRTableCell tableCellSpacer4;
        private XRTableRow tableRowSection4Header;
        private XRTableCell tableCellSection4Header;
        private XRTableRow tableRowSection4Body;
        private XRTableCell tableCellSection4Body;
        private XRTableRow tableRowSpacer5;
        private XRTableCell tableCellSpacer5;
        private XRTableRow tableRowSection5Header;
        private XRTableCell tableCellSection5Header;
        private XRTableRow tableRowSection5Line1;
        private XRTableCell tableCellSection5Line1;
        private XRTableRow tableRowSection5Line2;
        private XRTableCell tableCellSection5Line2;
        private XRTableRow tableRowSpacer6;
        private XRTableCell tableCellSpacer6;
        private XRTableRow tableRowSection6Header;
        private XRTableCell tableCellSection6Header;
        private XRTableRow tableRowSection6Line1;
        private XRTableCell tableCellSection6Line1;
        private XRTableRow tableRowSection6Line2;
        private XRTableCell tableCellSection6Line2;
        private XRTableRow tableRowSpacer7;
        private XRTableCell tableCellSpacer7;
        private XRTableRow tableRowSection7Header;
        private XRTableCell tableCellSection7Header;
        private XRTableRow tableRowSpacer8;
        private XRTableCell tableCellSpacer8;
        private XRTableRow tableRowSignatures;
        private XRTableCell tableCellSignatures;
        private XRPanel panelSignatures;
        private XRLabel lblEmployerHeader;
        private XRLabel lblEmployerSignatory;
        private XRLabel lblEmployerCompany;
        private XRLabel lblEmployerAddress;
        private XRLabel lblEmployeeHeader;
        private XRLabel lblEmployeeName;
        private XRLabel lblEmployeePassport;
    }
}
