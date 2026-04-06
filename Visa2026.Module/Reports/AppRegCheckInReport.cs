using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level cover letter for App_Reg_Check_In.
    /// Sent to the State Migration Service requesting registration of arriving foreign nationals.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Check_In_app.jpg
    /// </summary>
    public partial class AppRegCheckInReport : AppBaseReport
    {
        public AppRegCheckInReport()
        {
            InitializeComponent();
            // Force a specific background for testing, regardless of Company.Code.
            // Replace "CLK" with the company code you want to preview.
            LoadBackground("CLK");
        }
    }
}
