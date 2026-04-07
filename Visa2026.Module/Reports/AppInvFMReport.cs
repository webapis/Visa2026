using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level invitation letter for App_Inv_FM (Family Member).
    /// Sent to a Ministry requesting visa invitation for family members of an employee.
    /// Includes two extra introductory paragraphs and references the sponsoring employee inline.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Inv_FM_app.jpg
    /// Map: Resources/FormTemplates/App_Inv_FM_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppInvFMReport : AppBaseReport
    {
        public AppInvFMReport()
        {
            InitializeComponent();
        }
    }
}
