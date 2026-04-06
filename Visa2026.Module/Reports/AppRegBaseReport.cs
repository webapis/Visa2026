using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Base report for Registration-level reports.
    /// Provides: application number + date header, signatory footer.
    /// A4 Landscape — no background image.
    /// Signatory uses CompanyHead_FullName / CompanyHead_PositionTm (NotMapped on Registration).
    /// </summary>
    public partial class AppRegBaseReport : XtraReport
    {
        public AppRegBaseReport()
        {
            InitializeComponent();
        }
    }
}
