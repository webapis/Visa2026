using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level invitation and work permit cancellation letter for App_Cancel_Inv_WP.
    /// Sent to the head of the State Migration Service requesting simultaneous cancellation
    /// of work permits and invitations for foreign nationals.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Cancel_Inv_WP_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppCancelInvWPReport : AppBaseReport
    {
        public AppCancelInvWPReport()
        {
            InitializeComponent();
        }
    }
}
