using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level passport-change registration letter for App_Reg_Info_Change_Passport.
    /// Sent to Migration Service requesting re-registration because the foreign national's
    /// active visa has been re-issued into a new passport.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Info_Change_Passport_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegInfoChangePassportReport : AppBaseReport
    {
        public AppRegInfoChangePassportReport()
        {
            InitializeComponent();
        }
    }
}
