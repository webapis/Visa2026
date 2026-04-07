using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level invitation + work permit letter for App_Inv_And_WP.
    /// Identical to AppInvReport except the request paragraph appends
    /// "çakylyk we iş rugsatnamasyny" after the visa category.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Inv_And_WP_app.jpg
    /// Map: Resources/FormTemplates/App_Inv_And_WP_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppInvAndWPReport : AppBaseReport
    {
        public AppInvAndWPReport()
        {
            InitializeComponent();
        }
    }
}
