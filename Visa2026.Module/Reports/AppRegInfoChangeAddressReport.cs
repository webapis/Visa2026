using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level address-change registration letter for App_Reg_Info_Change_Address.
    /// Sent to Migration Service requesting re-registration due to a change of residential address.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Info_Change_Address_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegInfoChangeAddressReport : AppBaseReport
    {
        public AppRegInfoChangeAddressReport()
        {
            InitializeComponent();
        }
    }
}
