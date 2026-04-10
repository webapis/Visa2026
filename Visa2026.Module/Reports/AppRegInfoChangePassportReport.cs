using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level passport-change registration letter for App_Reg_Info_Change_Passport.
    /// Sent to Migration Service requesting re-registration because the foreign national's
    /// active visa has been re-issued into a new passport.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Info_Change_Passport_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegInfoChangePassportReport : AppGroupEBaseReport
    {
        public AppRegInfoChangePassportReport()
        {
            // xrRichBody1 — Passport-change registration transfer paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyny\u328? \b pasportyny \u231?al\u253?\u351?magy bilen baglan\u253?\u351?ykly hasaba durmagy\u328? m\u246?hletini t\u228?ze pasportyna ge\u231?irmegi\u328?izi\b0  Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set final Detail height to clear content according to Group E spacing
            this.Detail.HeightF = 492F;
        }
    }
}
