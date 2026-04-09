using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level change-of-invitation letter for App_Change_Inv.
    /// Sent to the head of the national State Migration Service requesting
    /// that existing invitations be re-issued under the foreign national's new passport.
    /// Lists all affected invitations in a detail table (InvitationNumber, StartDate, ExpirationDate).
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Change_Inv_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppChangeInvReport : AppBaseReport
    {
        public AppChangeInvReport()
        {
            InitializeComponent();
        }
    }
}
