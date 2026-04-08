using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level internal check-in letter for App_Reg_Check_In_Internal.
    /// Sent to Migration Service requesting re-registration of foreign nationals
    /// moving between regions within Turkmenistan.
    /// Identical to AppRegCheckInReport except body1 references FromCity/ToCity movement.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Check_In_Internal_app.jpg
    /// Map: Resources/FormTemplates/App_Reg_Check_In_Internal_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegCheckInInternalReport : AppBaseReport
    {
        public AppRegCheckInInternalReport()
        {
            InitializeComponent();
        }
    }
}
