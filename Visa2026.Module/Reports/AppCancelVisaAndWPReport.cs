using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level visa and work permit cancellation letter for App_Cancel_Visa_and_WP.
    /// Sent to the head of the State Migration Service requesting simultaneous cancellation
    /// of visas and work permits for foreign nationals who have departed Turkmenistan.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Cancel_Visa_and_WP_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppCancelVisaAndWPReport : AppBaseReport
    {
        public AppCancelVisaAndWPReport()
        {
            InitializeComponent();
        }
    }
}
