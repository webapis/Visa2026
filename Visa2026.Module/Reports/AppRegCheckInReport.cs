using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level cover letter for App_Reg_Check_In.
    /// Sent to the State Migration Service requesting registration of arriving foreign nationals.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Check_In_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegCheckInReport : AppGroupEBaseReport
    {
        public AppRegCheckInReport()
        {
            // xrRichBody1 — Check-in request paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyny\u328? \b T\u252?rkmenistana gelendigi seb\u228?pli\b0  hasaba almagy\u328?yzy Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set final Detail height to clear content according to Group E spacing
            this.Detail.HeightF = 492F;
        }
    }
}
