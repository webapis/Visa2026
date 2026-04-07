using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level invitation letter for App_Inv.
    /// Sent to a Ministry requesting visa invitation for foreign nationals.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Inv_app.jpg
    /// Map: Resources/FormTemplates/App_Inv_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppInvReport : AppBaseReport
    {
        public AppInvReport()
        {
            InitializeComponent();
        }
    }
}
