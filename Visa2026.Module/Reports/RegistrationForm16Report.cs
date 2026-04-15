using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Per-person registration card (Form 16) for all Registration-type ApplicationTypes.
    /// Title: "DAŞARY ÝURT RAÝATLARYNY BELLIGE ALYŞ NAMASY"
    /// One card rendered per Registration record (one page per person).
    ///
    /// Data source: Registration (via inherited RegDataSource)
    /// Shared across all Registration ApplicationTypes — empty visibility criteria.
    /// Covers: App_Reg_Check_In, App_Reg_Check_In_Internal, App_Reg_Check_Out,
    ///         App_Reg_Check_Out_Internal, App_Reg_ext, App_Reg_Info_Change_Address,
    ///         App_Reg_Info_Change_Passport, App_Reg_Info_Change_Visa.
    ///
    /// Reference: Resources/FormTemplates/Registration_Form16.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class RegistrationForm16Report : AppRegBaseReport
    {
        public RegistrationForm16Report()
        {
            InitializeComponent();
        }
    }
}
