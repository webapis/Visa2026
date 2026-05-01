using System.Drawing;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    partial class AppLaborContractItemReport
    {
        private static class LaborContractRtf
        {
            public const string RtfFontHdr = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30";

            public const string Title = RtfFontHdr + @"\pard\qc\b ZÄHMET ŞERTNAMASY\b0\par}";

            /// <summary>City + parties; embeds merge fields.</summary>
            public const string Intro = RtfFontHdr
                + @"\pard\qc Aşgabat şäheri.\par\pard\qj\fi720 Bu zähmet şertnamasynda bir tarapdan \b [Application_SponsorName]\b0 "
                + @"(mundan beýläk \u8212? IŞ BERIJI), onuň \b [Application_SponsorSignatory]\b0 arkaly wekillerçilik edilýän tarap bilen, beýleki tarapdan "
                + @"\b [Person_FullName]\b0 (mundan beýläk \u8212? IŞGÄR) arasynda baglaşyldy. IŞGÄR \b [Position_PositionTm]\b0 wezipesinde işe kabul edilýär.\par}";

            public const string Sections1to4 = RtfFontHdr + @"\pard\qj\fi720\b 1. Iş berijiniň borçlary\par\b0"
                + @"1.1. Işgäre wezipe boýunça iş bermek.\par"
                + @"1.2. Aýlyk zähmet hakyýyny iş tertibinde bellenen günlerde tölemek.\par"
                + @"1.3. Türkmenistanyň zähmet hukugy boýunça ýyllyk dynç alyş möhletini üpjün etmek.\par"
                + @"1.4. Ýaragly iş şertlerini we sosial goraglary üpjün etmek.\par"
                + @"\par\b 2. Işgäriň borçlary\par\b0"
                + @"2.1. Tabşyrylan işleri dogry we doly ýerine ýetirmek.\par"
                + @"2.2. Içerki iş tertipnamalaryna, tehniki howpsuzlyk we zähmet goragy düzgünlerine eýermek.\par"
                + @"2.3. Iş ýerini we enjamlary arassa saklamak we olary ähtiyatly ulanyş.\par"
                + @"2.4. Kompaniýanyň syrlaryny üçürtmekden gaça durmak.\par"
                + @"2.5. Bölüm başlygynyň görkezmelerine eýermek we zähmet wazypalaryny ýerine ýetirmek.\par"
                + @"\par\b 3. Iş we dynç alyş düzgüni\par\b0"
                + @"3.1. Içerki iş tertibine we Türkmenistanyň zähmet hukugyna eýermek.\par"
                + @"3.2. Iş wagty \u8212? günde 8 sagat, hepdede 6 iş güni.\par"
                + @"3.3. Önümçilik zerurlygy bar bolanda, kanunçylykda göz öňünde tutulşy ýaly goşmaça işe çagyrylyş mümkin.\par"
                + @"3.4. Aýlyk zähmet haky kadrlar boýunça işçileriň sanawy esasynda tölenýär.\par"
                + @"\par\b 4. Zähmet şertnamasynyň ýatyrylmagy\par\b0"
                + @"Iş beriji aşakdaky ýagdaýlarda zähmet şertnamasyny ýatýar:\par"
                + @"4.1. Şertnamanyň möhleti gutaran wagty.\par"
                + @"4.2. Işi tamamlananda.\par"
                + @"4.3. Işgärleriň sany azalanda.\par"
                + @"4.4. Içgilik ýa-da täsirçi serişdeleriň täsirinde işe gelende.\par"
                + @"4.5. Zähmet borçlaryny ýerine ýetirmek ýa-da içerki düzgünleri eýermezlikde düzgünleýin ýalňyşlyk görkezende.\par"
                + @"4.6. Kompaniýanyň emlägini ogurlananda.\par"
                + @"4.7. Galan çaknyşmalar Türkmenistanyň kanunçylygy boýunça çözlener.\par}";

            public const string Section5 = RtfFontHdr + @"\pard\qj\fi720\b 5. Zähmet şertnamasynyň hereket edýän möhleti\par\b0"
                + @"Şertnama \b [Contract_StartDateText]\b0 \u8212? \b [Contract_ExpirationDateText]\b0 aralygynda hereket edýär.\par"
                + @"Taraplar gol çeken pursatdan başlap güýje girýär. Şertnamanyň möhleti gutarandan soň işläp başlyşaýan bolsa we taraplaryň biri garşy çykmasa, şertnama bir möhletlik uzaldylan hasaplanylýär.\par}";

            public const string Section6 = RtfFontHdr + @"\pard\qj\fi720\b 6. Türkmenistan döwletinde alýan aýlyk zähmet haky\par\b0"
                + @"Aýlyk zähmet haky: \b [Contract_SalaryText] [Salary_CurrencyCode]\b0.\par"
                + @"Töleg: zähmet haky Türkiýädäki bank hasabyna geçirilýär.\par}";

            public const string Section7 = RtfFontHdr + @"\pard\qj\fi720\b 7. Taraplaryň gollary we salgylary\par\b0"
                + @"\b IŞ BERIJI:\b0\par"
                + @"[Application_SponsorSignatory]\par"
                + @"[Application_SponsorName]\par"
                + @"[Application_CompanyAddress]\par"
                + @"\par\b IŞGÄR:\b0\par"
                + @"[Person_FullName]\par"
                + @"Pasport belgisi: [Passport_Number]\par}";
        }

        private XRRichText xrRichTitle;
        private XRRichText xrRichIntro;
        private XRRichText xrRichSections1to4;
        private XRRichText xrRichSection5;
        private XRRichText xrRichSection6;
        private XRRichText xrRichSection7;

        private void InitializeComponent()
        {
            this.xrRichTitle = new XRRichText();
            this.xrRichIntro = new XRRichText();
            this.xrRichSections1to4 = new XRRichText();
            this.xrRichSection5 = new XRRichText();
            this.xrRichSection6 = new XRRichText();
            this.xrRichSection7 = new XRRichText();

            ((System.ComponentModel.ISupportInitialize)(this.xrRichTitle)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichIntro)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichSections1to4)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichSection5)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichSection6)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichSection7)).BeginInit();

            const float W = 626.7717F;

            this.xrRichTitle.BackColor = Color.Transparent;
            this.xrRichTitle.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichTitle.CanGrow = true;
            this.xrRichTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrRichTitle.Name = "xrRichTitle";
            this.xrRichTitle.SizeF = new System.Drawing.SizeF(W, 38F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichTitle)).EndInit();
            this.xrRichTitle.Rtf = LaborContractRtf.Title;

            this.xrRichIntro.BackColor = Color.Transparent;
            this.xrRichIntro.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichIntro.CanGrow = true;
            this.xrRichIntro.LocationFloat = new DevExpress.Utils.PointFloat(0F, 46F);
            this.xrRichIntro.Name = "xrRichIntro";
            this.xrRichIntro.SizeF = new System.Drawing.SizeF(W, 110F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichIntro)).EndInit();
            this.xrRichIntro.Rtf = LaborContractRtf.Intro;

            this.xrRichSections1to4.BackColor = Color.Transparent;
            this.xrRichSections1to4.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichSections1to4.CanGrow = true;
            this.xrRichSections1to4.LocationFloat = new DevExpress.Utils.PointFloat(0F, 164F);
            this.xrRichSections1to4.Name = "xrRichSections1to4";
            this.xrRichSections1to4.SizeF = new System.Drawing.SizeF(W, 280F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichSections1to4)).EndInit();
            this.xrRichSections1to4.Rtf = LaborContractRtf.Sections1to4;

            this.xrRichSection5.BackColor = Color.Transparent;
            this.xrRichSection5.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichSection5.CanGrow = true;
            this.xrRichSection5.LocationFloat = new DevExpress.Utils.PointFloat(0F, 452F);
            this.xrRichSection5.Name = "xrRichSection5";
            this.xrRichSection5.SizeF = new System.Drawing.SizeF(W, 90F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichSection5)).EndInit();
            this.xrRichSection5.Rtf = LaborContractRtf.Section5;

            this.xrRichSection6.BackColor = Color.Transparent;
            this.xrRichSection6.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichSection6.CanGrow = true;
            this.xrRichSection6.LocationFloat = new DevExpress.Utils.PointFloat(0F, 550F);
            this.xrRichSection6.Name = "xrRichSection6";
            this.xrRichSection6.SizeF = new System.Drawing.SizeF(W, 75F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichSection6)).EndInit();
            this.xrRichSection6.Rtf = LaborContractRtf.Section6;

            this.xrRichSection7.BackColor = Color.Transparent;
            this.xrRichSection7.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrRichSection7.CanGrow = true;
            this.xrRichSection7.LocationFloat = new DevExpress.Utils.PointFloat(0F, 633F);
            this.xrRichSection7.Name = "xrRichSection7";
            this.xrRichSection7.SizeF = new System.Drawing.SizeF(W, 150F);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichSection7)).EndInit();
            this.xrRichSection7.Rtf = LaborContractRtf.Section7;

            this.Detail.CanGrow = true;
            this.Detail.Controls.AddRange(new XRControl[] {
                this.xrRichTitle,
                this.xrRichIntro,
                this.xrRichSections1to4,
                this.xrRichSection5,
                this.xrRichSection6,
                this.xrRichSection7
            });
            this.Detail.HeightF = 800F;
        }
    }
}
