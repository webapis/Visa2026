using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Base report for ApplicationItem-level reports.
    /// Provides: application number + date header, signatory footer.
    /// No background image — derived variant reports add their own form overlay.
    /// Signatory uses CompanyHead_FullName / CompanyHead_PositionTm (NotMapped on ApplicationItem).
    /// </summary>
    public partial class AppItemBaseReport : XtraReport
    {
        public AppItemBaseReport()
        {
            InitializeComponent();
        }
    }
}
