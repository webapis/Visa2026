using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level visa cancellation letter for App_Cancel_Visa.
    /// Sent to the head of the national State Migration Service requesting visa cancellation
    /// for foreign nationals departing or terminating their stay.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Cancel_Visa_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppCancelVisaReport : AppBaseReport
    {
        public AppCancelVisaReport()
        {
            InitializeComponent();
        }
    }
}
