using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level visa transfer letter for App_Change_Passport.
    /// Sent to the head of the national State Migration Service requesting
    /// that a foreign national's visa be transferred from the old passport to the new passport.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Change_Passport_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppChangePassportReport : AppBaseReport
    {
        public AppChangePassportReport()
        {
            InitializeComponent();
        }
    }
}
