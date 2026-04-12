namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level personnel list for App_Visa_and_WP_Ext.
    /// Singular title: "Daşary ýurt raýatynyň sanawy" (one person per application).
    /// 14-column table on A4 Landscape; overrides xrLabelTitle.Text from base.
    /// Reference image: Resources/FormTemplates/App_Visa_and_WP_Ext_item.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public class AppVisaAndWPExtItemReport : AppItemInvSanawBaseReport
    {
        public AppVisaAndWPExtItemReport()
        {
            this.xrLabelTitle.Text = "Daşary ýurt raýatynyň sanawy";
        }
    }
}
