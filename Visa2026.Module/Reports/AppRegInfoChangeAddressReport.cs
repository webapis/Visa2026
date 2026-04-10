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
    public partial class AppRegInfoChangeAddressReport : AppGroupEBaseReport
    {
        public AppRegInfoChangeAddressReport()
        {
            // xrRichBody1 — Address-change re-registration paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyny\u328? \b \u253?a\u351?a\u253?an salgysyny \u231?al\u253?\u351?andy\u253?y\b0  seb\u228?pli t\u228?ze \u246?\u253? salgysyna hasaba almagy\u328?yzy Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set final Detail height to clear content according to Group E spacing
            this.Detail.HeightF = 492F;
        }
    }
}
