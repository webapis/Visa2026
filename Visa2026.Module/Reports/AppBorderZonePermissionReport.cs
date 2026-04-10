using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level border zone permission request letter for App_Border_Zone_Permission.
    /// Sent to the Ministry head requesting border zone visa registration for foreign nationals.
    /// Recipient and greeting are dynamic via ProjectContract.Ministry.
    /// Body1 uses ProjectContract.Description; body2 uses BorderZoneLocation.NameTm.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Border_Zone_Permission_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppBorderZonePermissionReport : AppBaseReport
    {
        public AppBorderZonePermissionReport()
        {
            InitializeComponent();
        }
    }
}
