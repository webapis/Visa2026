using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level internal check-out letter for App_Reg_Check_Out_Internal.
    /// Sent to Migration Service requesting de-registration of foreign nationals
    /// moving between regions within Turkmenistan.
    /// Identical to AppRegCheckInInternalReport except body1 action is "hasapdan çykarmagyňyzy".
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Check_Out_Internal_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegCheckOutInternalReport : AppBaseReport
    {
        public AppRegCheckOutInternalReport()
        {
            InitializeComponent();
        }
    }
}
