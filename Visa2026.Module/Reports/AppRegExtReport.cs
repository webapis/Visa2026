using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level registration extension letter for App_Reg_ext.
    /// Sent to Migration Service requesting extension of registration period
    /// for foreign nationals whose visa period has been extended.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_ext_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegExtReport : AppBaseReport
    {
        public AppRegExtReport()
        {
            InitializeComponent();
        }
    }
}
