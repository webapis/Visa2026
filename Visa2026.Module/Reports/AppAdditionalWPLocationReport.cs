using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level additional work permit location request for App_Additional_WP_location.
    /// Sent to the Ministry/Organization linked via Application.ProjectContract.Ministry.
    /// Recipient block and salutation come from Ministry.RecipientBlock and Ministry.FormOfAddress.
    /// Contract reference paragraph comes from ProjectContract.Description (plain text).
    /// Work permit location comes from Application.MovementPermitLocation.NameTm (flat: MovementPermitLocation_NameTm).
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Additional_WP_location_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppAdditionalWPLocationReport : AppBaseReport
    {
        public AppAdditionalWPLocationReport()
        {
            InitializeComponent();
        }
    }
}
