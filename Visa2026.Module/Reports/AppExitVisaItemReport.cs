using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level personnel list for App_Exit_Visa.
    /// 14-column "Daşary ýurt raýatlarynyň sanawy" table on A4 Landscape.
    /// Inherits layout and all column bindings from AppItemInvSanawBaseReport.
    /// Overrides xrCellMohleti: shows actual visa dates + number + category
    /// instead of the application-level VisaPeriod + VisaCategory.
    /// Reference image: Resources/FormTemplates/App_Exit_Visa_item.jpg
    /// Map: Resources/FormTemplates/App_Exit_Visa_item_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public class AppExitVisaItemReport : AppItemInvSanawBaseReport
    {
        public AppExitVisaItemReport()
        {
            // xrCellMohleti — actual current visa dates + number + category
            this.xrCellMohleti.ExpressionBindings.Clear();
            this.xrCellMohleti.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText] + Char(10) + '(' + [Visa_Number] + ') ' + [Visa_CategoryTm]"));
        }
    }
}
