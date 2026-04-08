using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level visa and work permit extension request for App_Visa_and_WP_Ext.
    /// Sent to the Ministry/Organization linked via Application.ProjectContract.Ministry.
    /// Recipient block and salutation come from Ministry.RecipientBlock (RTF) and Ministry.FormOfAddress.
    /// Contract reference paragraph comes from ProjectContract.Description (plain text).
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Visa_and_WP_Ext_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppVisaAndWPExtReport : AppBaseReport
    {
        public AppVisaAndWPExtReport()
        {
            InitializeComponent();
        }
    }
}
