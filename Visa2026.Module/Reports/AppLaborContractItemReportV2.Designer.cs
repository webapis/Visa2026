using DevExpress.Drawing;
using DevExpress.Utils;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
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
            this.lblIntroParagraph = new XRLabel();
            this.lblSection1Header = new XRLabel();
            this.lblSection1Body = new XRLabel();
            this.lblSection2Header = new XRLabel();
            this.lblSection2Body = new XRLabel();
            this.lblSection3Header = new XRLabel();
            this.lblSection3Body = new XRLabel();
            this.lblSection4Header = new XRLabel();
            this.lblSection4Body = new XRLabel();
            this.lblSection5Header = new XRLabel();
            this.lblSection5Line1 = new XRLabel();
            this.lblSection5Line2 = new XRLabel();
            this.lblSection6Header = new XRLabel();
            this.lblSection6Line1 = new XRLabel();
            this.lblSection6Line2 = new XRLabel();
            this.lblSection7Header = new XRLabel();
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
            this.lblTitle.Text = "ZÄHMET ŞERTNAMASY";
            this.lblTitle.TextAlignment = TextAlignment.MiddleCenter;

            //
            // lblCity
            //
            this.lblCity.Borders = BorderSide.None;
            this.lblCity.CanGrow = false;
            this.lblCity.Font = new DXFont("Times New Roman", 11F);
            this.lblCity.LocationFloat = new PointFloat(0F, 30F);
            this.lblCity.Multiline = true;
            this.lblCity.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblCity.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblCity.Text = "Aşgabat şäheri";
            this.lblCity.TextAlignment = TextAlignment.TopCenter;

            //
            // lblIntroParagraph
            //
            this.lblIntroParagraph.Borders = BorderSide.None;
            this.lblIntroParagraph.CanGrow = false;
            this.lblIntroParagraph.Font = new DXFont("Times New Roman", 11F);
            this.lblIntroParagraph.LocationFloat = new PointFloat(0F, 52F);
            this.lblIntroParagraph.Multiline = true;
            this.lblIntroParagraph.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblIntroParagraph.SizeF = new System.Drawing.SizeF(626.7717F, 70F);
            this.lblIntroParagraph.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "FormatString('Bu zähmet şertnamasynda bir tarapdan {0} (mundan beýläk \"IŞ BERIJI\"), onuň {1} arkaly wekillerçilik edilýän tarap bilen, beýleki tarapdan {2} (mundan beýläk \"IŞGÄR\") arasynda baglaşyldy. IŞGÄR {3} wezipesinde işe kabul edilýär.', [Application_SponsorName], [Application_SponsorSignatory], [Person_FullName], [Position_PositionTm])")
            });
            this.lblIntroParagraph.TextAlignment = TextAlignment.TopLeft;
            this.lblIntroParagraph.WordWrap = true;

            //
            // lblSection1Header
            //
            this.lblSection1Header.Borders = BorderSide.None;
            this.lblSection1Header.CanGrow = false;
            this.lblSection1Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.lblSection1Header.LocationFloat = new PointFloat(0F, 128F);
            this.lblSection1Header.Multiline = true;
            this.lblSection1Header.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection1Header.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblSection1Header.Text = "1. Iş berijiniň borçlary";
            this.lblSection1Header.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection1Body
            //
            this.lblSection1Body.Borders = BorderSide.None;
            this.lblSection1Body.CanGrow = false;
            this.lblSection1Body.Font = new DXFont("Times New Roman", 11F);
            this.lblSection1Body.LocationFloat = new PointFloat(0F, 148F);
            this.lblSection1Body.Multiline = true;
            this.lblSection1Body.Padding = new PaddingInfo(20F, 0, 0, 0, 100F);
            this.lblSection1Body.SizeF = new System.Drawing.SizeF(626.7717F, 86F);
            this.lblSection1Body.Text = "1.1. Işgäre hünärine laýyk iş bermeli.\r\n1.2. Her aý aýlyk zähmet hakyny bellenilen günde tölemeli.\r\n1.3. Türkmenistanyň zähmet kanunlaryna laýyklykda ýyllyk zähmet rugsadyny üpjün etmeli.\r\n1.4. Şertnamanyň möhletinde zähmet şertlerini we sosial goraglary kanuna laýyk üpjün etmeli.";
            this.lblSection1Body.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection2Header
            //
            this.lblSection2Header.Borders = BorderSide.None;
            this.lblSection2Header.CanGrow = false;
            this.lblSection2Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.lblSection2Header.LocationFloat = new PointFloat(0F, 236F);
            this.lblSection2Header.Multiline = true;
            this.lblSection2Header.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection2Header.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblSection2Header.Text = "2. Işgäriň borçlary";
            this.lblSection2Header.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection2Body
            //
            this.lblSection2Body.Borders = BorderSide.None;
            this.lblSection2Body.CanGrow = false;
            this.lblSection2Body.Font = new DXFont("Times New Roman", 11F);
            this.lblSection2Body.LocationFloat = new PointFloat(0F, 256F);
            this.lblSection2Body.Multiline = true;
            this.lblSection2Body.Padding = new PaddingInfo(20F, 0, 0, 0, 100F);
            this.lblSection2Body.SizeF = new System.Drawing.SizeF(626.7717F, 110F);
            this.lblSection2Body.Text = "2.1. Tabşyrylan işleri dogry we doly ýerine ýetirmeli.\r\n2.2. Kärhananyň içerki tertipnamalaryna, tehniki howpsuzlyk we zähmet gorag düzgünlerine eýermeli.\r\n2.3. Iş ýerini arassa saklamaly we enjamlary ähtiyatly ulanmaly.\r\n2.4. Kompaniýanyň hyzmat syrlaryny gizlin saklamaly.\r\n2.5. Bölüm başlygynyň görkezmelerini ýerine ýetirmeli.";
            this.lblSection2Body.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection3Header
            //
            this.lblSection3Header.Borders = BorderSide.None;
            this.lblSection3Header.CanGrow = false;
            this.lblSection3Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.lblSection3Header.LocationFloat = new PointFloat(0F, 370F);
            this.lblSection3Header.Multiline = true;
            this.lblSection3Header.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection3Header.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblSection3Header.Text = "3. Iş we dynç alyş düzgüni";
            this.lblSection3Header.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection3Body
            //
            this.lblSection3Body.Borders = BorderSide.None;
            this.lblSection3Body.CanGrow = false;
            this.lblSection3Body.Font = new DXFont("Times New Roman", 11F);
            this.lblSection3Body.LocationFloat = new PointFloat(0F, 390F);
            this.lblSection3Body.Multiline = true;
            this.lblSection3Body.Padding = new PaddingInfo(20F, 0, 0, 0, 100F);
            this.lblSection3Body.SizeF = new System.Drawing.SizeF(626.7717F, 74F);
            this.lblSection3Body.Text = "3.1. Içerki iş tertibine we Türkmenistanyň zähmet kanunlaryna eýermeli.\r\n3.2. Iş wagty — günde 8 sagat, hepdede 6 iş güni.\r\n3.3. Zerur bolanda kanunçylykdaky tertipde goşmaça işe çagyrylyp bilinýär.\r\n3.4. Aýlyk zähmet haky kadr bölüminiň sanawynyň esasynda tölenýär.";
            this.lblSection3Body.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection4Header
            //
            this.lblSection4Header.Borders = BorderSide.None;
            this.lblSection4Header.CanGrow = false;
            this.lblSection4Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.lblSection4Header.LocationFloat = new PointFloat(0F, 466F);
            this.lblSection4Header.Multiline = true;
            this.lblSection4Header.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection4Header.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblSection4Header.Text = "4. Zähmet şertnamasynyň ýatyrylmagy";
            this.lblSection4Header.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection4Body
            //
            this.lblSection4Body.Borders = BorderSide.None;
            this.lblSection4Body.CanGrow = false;
            this.lblSection4Body.Font = new DXFont("Times New Roman", 11F);
            this.lblSection4Body.LocationFloat = new PointFloat(0F, 486F);
            this.lblSection4Body.Multiline = true;
            this.lblSection4Body.Padding = new PaddingInfo(20F, 0F, 0F, 0F, 100F);
            this.lblSection4Body.SizeF = new System.Drawing.SizeF(626.7717F, 124F);
            this.lblSection4Body.Text = "Şertnama aşakdaky ýagdaýlarda ýatyrylýar:\r\n4.1. Şertnamanyň möhleti gutaranda.\r\n4.2. Işi tamamlananda.\r\n4.3. Işgärleriň sany azalanda.\r\n4.4. Işe serhoş, täsirli serişde ýa-da neşe arkaly gelende.\r\n4.5. Zähmet borçlaryny ýerine ýetirmeýände ýa-da içerki düzgünleri bozanda.\r\n4.6. Kompaniýanyň emlägine zeper ýetirende ýa-da ogurlanda.\r\n4.7. Galan dawalar Türkmenistanyň hereket edýän kanunlaryna laýyk çözülýär.";
            this.lblSection4Body.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection5Header
            //
            this.lblSection5Header.Borders = BorderSide.None;
            this.lblSection5Header.CanGrow = false;
            this.lblSection5Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.lblSection5Header.LocationFloat = new PointFloat(0F, 614F);
            this.lblSection5Header.Multiline = true;
            this.lblSection5Header.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection5Header.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblSection5Header.Text = "5. Zähmet şertnamasynyň hereket edýän möhleti";
            this.lblSection5Header.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection5Line1
            //
            this.lblSection5Line1.Borders = BorderSide.None;
            this.lblSection5Line1.CanGrow = false;
            this.lblSection5Line1.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "FormatString('Şertnama {0} — {1} aralygynda hereket edýär.', [Contract_StartDateText], [Contract_ExpirationDateText])")
            });
            this.lblSection5Line1.Font = new DXFont("Times New Roman", 11F);
            this.lblSection5Line1.LocationFloat = new PointFloat(0F, 634F);
            this.lblSection5Line1.Multiline = true;
            this.lblSection5Line1.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection5Line1.SizeF = new System.Drawing.SizeF(626.7717F, 20F);
            this.lblSection5Line1.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection5Line2
            //
            this.lblSection5Line2.Borders = BorderSide.None;
            this.lblSection5Line2.CanGrow = false;
            this.lblSection5Line2.Font = new DXFont("Times New Roman", 11F);
            this.lblSection5Line2.LocationFloat = new PointFloat(0F, 656F);
            this.lblSection5Line2.Multiline = true;
            this.lblSection5Line2.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection5Line2.SizeF = new System.Drawing.SizeF(626.7717F, 36F);
            this.lblSection5Line2.Text = "Taraplar gol çeken pursatdan başlap güýje girýär. Möhleti gutaran ýagdaýynda taraplaryň biri garşy bolmasa şertnama şol bir möhlet üçin awtomatiki uzaldylan hasaplanylýar.";
            this.lblSection5Line2.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection6Header
            //
            this.lblSection6Header.Borders = BorderSide.None;
            this.lblSection6Header.CanGrow = false;
            this.lblSection6Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.lblSection6Header.LocationFloat = new PointFloat(0F, 698F);
            this.lblSection6Header.Multiline = true;
            this.lblSection6Header.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection6Header.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblSection6Header.Text = "6. Türkmenistan döwletinde alýan aýlyk zähmet haky";
            this.lblSection6Header.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection6Line1
            //
            this.lblSection6Line1.Borders = BorderSide.None;
            this.lblSection6Line1.CanGrow = false;
            this.lblSection6Line1.ExpressionBindings.AddRange(new ExpressionBinding[]
            {
                new ExpressionBinding("BeforePrint", "Text", "FormatString('Aýlyk zähmet haky: {0} {1}.', [Contract_SalaryText], [Salary_CurrencyCode])")
            });
            this.lblSection6Line1.Font = new DXFont("Times New Roman", 11F);
            this.lblSection6Line1.LocationFloat = new PointFloat(0F, 718F);
            this.lblSection6Line1.Multiline = true;
            this.lblSection6Line1.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection6Line1.SizeF = new System.Drawing.SizeF(626.7717F, 20F);
            this.lblSection6Line1.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection6Line2
            //
            this.lblSection6Line2.Borders = BorderSide.None;
            this.lblSection6Line2.CanGrow = false;
            this.lblSection6Line2.Font = new DXFont("Times New Roman", 11F);
            this.lblSection6Line2.LocationFloat = new PointFloat(0F, 740F);
            this.lblSection6Line2.Multiline = true;
            this.lblSection6Line2.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection6Line2.SizeF = new System.Drawing.SizeF(626.7717F, 20F);
            this.lblSection6Line2.Text = "Töleg: zähmet haky Türkiýedäki bank hasabyna geçirilýär.";
            this.lblSection6Line2.TextAlignment = TextAlignment.TopLeft;

            //
            // lblSection7Header
            //
            this.lblSection7Header.Borders = BorderSide.None;
            this.lblSection7Header.CanGrow = false;
            this.lblSection7Header.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.lblSection7Header.LocationFloat = new PointFloat(0F, 766F);
            this.lblSection7Header.Multiline = true;
            this.lblSection7Header.Padding = new PaddingInfo(0, 0, 0, 0, 100F);
            this.lblSection7Header.SizeF = new System.Drawing.SizeF(626.7717F, 18F);
            this.lblSection7Header.Text = "7. Taraplaryň gollary we salgylary";
            this.lblSection7Header.TextAlignment = TextAlignment.TopLeft;

            //
            // panelSignatures
            //
            this.panelSignatures.Borders = BorderSide.None;
            this.panelSignatures.CanGrow = false;
            this.panelSignatures.LocationFloat = new PointFloat(0F, 788F);
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
            this.lblEmployerHeader.Text = "IŞ BERIJI:";
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
            this.lblEmployeeHeader.Text = "IŞGÄR:";
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
                this.lblIntroParagraph,
                this.lblSection1Header,
                this.lblSection1Body,
                this.lblSection2Header,
                this.lblSection2Body,
                this.lblSection3Header,
                this.lblSection3Body,
                this.lblSection4Header,
                this.lblSection4Body,
                this.lblSection5Header,
                this.lblSection5Line1,
                this.lblSection5Line2,
                this.lblSection6Header,
                this.lblSection6Line1,
                this.lblSection6Line2,
                this.lblSection7Header,
                this.panelSignatures
            });

            this.Detail.CanGrow = true;
            this.Detail.HeightF = 915F;

        }

        #endregion

        private XRLabel lblTitle;
        private XRLabel lblCity;
        private XRLabel lblIntroParagraph;
        private XRLabel lblSection1Header;
        private XRLabel lblSection1Body;
        private XRLabel lblSection2Header;
        private XRLabel lblSection2Body;
        private XRLabel lblSection3Header;
        private XRLabel lblSection3Body;
        private XRLabel lblSection4Header;
        private XRLabel lblSection4Body;
        private XRLabel lblSection5Header;
        private XRLabel lblSection5Line1;
        private XRLabel lblSection5Line2;
        private XRLabel lblSection6Header;
        private XRLabel lblSection6Line1;
        private XRLabel lblSection6Line2;
        private XRLabel lblSection7Header;
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
