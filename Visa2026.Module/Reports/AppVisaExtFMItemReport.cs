using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level personnel list for App_Visa_Ext_FM.
    /// Inherits the standard 14-column "Daşary ýurt raýatlarynyň sanawy" layout
    /// from AppItemInvSanawBaseReport.
    ///
    /// Overrides for Family Member context:
    ///
    ///   Möhleti we gezekligi — shows current visa start date, expiry date,
    ///   visa number, and visa type (köp/bir gezekli) from the person's current Visa.
    ///
    ///   Bilimi we okan ýeri — uses FM_EducationLevelTm:
    ///     "Çaga" if person is under 18, "Orta" if adult family member.
    ///
    ///   Bilimine görä hünäri — uses FM_SpecialtyTm (same Çaga/Orta logic).
    ///
    ///   Wezipesi — uses FM_WezipesiTm:
    ///     "[Employee Position] [Employee FullName]-ň [Relationship]"
    ///     e.g. "Zähmeti goramak we tehniki howpsuzlyk boýunça başlyk Bóra Yolcu-ň gyzy"
    ///
    /// Reference image: Resources/FormTemplates/App_Visa_Ext_FM_item.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public class AppVisaExtFMItemReport : AppItemInvSanawBaseReport
    {
        public AppVisaExtFMItemReport()
        {
            // --- Möhleti we gezekligi: current visa period and frequency ---
            // Base placeholder ([Application_VisaPeriod_NameTm]) is not a valid
            // ApplicationItem field — replace with actual visa date/number/type.
            this.xrCellMohleti.ExpressionBindings.Clear();
            this.xrCellMohleti.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text",
                    "[Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText] + Char(10) + [Visa_Number] + Char(10) + [Visa_TypeTm]"));
            this.xrCellMohleti.Multiline = true;

            // --- Bilimi we okan ýeri: FM education display ---
            // Family members show "Çaga" (under 18) or "Orta" (adult); no institution line.
            this.xrCellBilimi.ExpressionBindings.Clear();
            this.xrCellBilimi.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[FM_EducationLevelTm]"));

            // --- Bilimine görä hünäri: FM specialty display ---
            this.xrCellHunari.ExpressionBindings.Clear();
            this.xrCellHunari.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[FM_SpecialtyTm]"));

            // --- Wezipesi: employee position + name + relationship ---
            // Pattern: "[Employee Position] [Employee FullName]-nyň [Relationship]"
            this.xrCellWezipesi.ExpressionBindings.Clear();
            this.xrCellWezipesi.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[FM_WezipesiTm]"));
        }
    }
}
