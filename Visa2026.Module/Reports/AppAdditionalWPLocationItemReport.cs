namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level personnel list for App_Additional_WP_location.
    /// Singular title: "Daşary ýurt raýatynyň sanawy" (one person per application).
    /// 14-column table on A4 Landscape; overrides xrLabelTitle.Text from base.
    /// Reference image: Resources/FormTemplates/App_Additional_WP_location_item.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public class AppAdditionalWPLocationItemReport : AppItemInvSanawBaseReport
    {
        public AppAdditionalWPLocationItemReport()
        {
            this.xrLabelTitle.Text = "Daşary ýurt raýatynyň sanawy";
        }
    }
}
