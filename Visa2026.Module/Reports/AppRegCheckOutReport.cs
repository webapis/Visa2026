using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level check-out letter for App_Reg_Check_Out.
    /// Sent to Migration Service requesting de-registration of departing foreign nationals.
    /// Identical to AppRegCheckInReport except body1 requests departure de-registration.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Check_Out_app.jpg
    /// Map: Resources/FormTemplates/App_Reg_Check_Out_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegCheckOutReport : AppBaseReport
    {
        public AppRegCheckOutReport()
        {
            InitializeComponent();
        }
    }
}
