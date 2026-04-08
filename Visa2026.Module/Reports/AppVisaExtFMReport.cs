using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level FM visa extension request for App_Visa_Ext_FM.
    /// Sent to the Ministry/Organization linked via Application.ProjectContract.Ministry.
    /// Requests visa extension for family members of a sponsoring employee.
    /// Nearly identical to AppInvFMReport — body3 ends with "wizalaryny resmileşdirilmegine"
    /// instead of "çakylyk resmileşdirilmegine".
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Visa_Ext_FM_app.jpg
    /// Map file: Resources/FormTemplates/App_Visa_Ext_FM_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppVisaExtFMReport : AppBaseReport
    {
        public AppVisaExtFMReport()
        {
            InitializeComponent();
        }
    }
}
