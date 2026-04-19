namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Standalone WorkPermitItem-level personnel list.
    /// 10-column table on A4 Portrait — all active work permit holders for the company.
    /// Header shows today's date and company name dynamically.
    /// Reference image: Resources/FormTemplates/WorkPermit_list.jpg
    /// Map: Resources/FormTemplates/WorkPermit_list_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class WorkPermitListReport : DevExpress.XtraReports.UI.XtraReport
    {
        public WorkPermitListReport()
        {
            InitializeComponent();
        }
    }
}
