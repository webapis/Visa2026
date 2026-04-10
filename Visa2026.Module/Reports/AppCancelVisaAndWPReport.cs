using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level visa and work permit cancellation letter for App_Cancel_Visa_and_WP.
    /// Sent to the head of the State Migration Service requesting simultaneous cancellation
    /// of visas and work permits for foreign nationals who have departed Turkmenistan.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Cancel_Visa_and_WP_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppCancelVisaAndWPReport : AppGroupDBaseReport
    {
        public AppCancelVisaAndWPReport()
        {
            // xrRichBody1 — Visa and WP cancellation request paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [CancelPersonCount] ([CancelPersonCountText]) sany\b0  da\u351?ary \u253?urt ra\u253?atyny\u328? \b T\u252?rkmenistany\u328? \u231?\u228?ginden \u231?ykyp gidendigi\b0  seb\u228?pli \b [CancelPersonCount] ([CancelPersonCountText]) sany\b0  wizasyny we \b [CancelWPCount] ([CancelWPCountText]) sany\b0  i\u351?lemek \u252?\u231?\u252?n rugsatnamasyny \u253?atyrmagy\u328?yzy Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set final Detail height to clear content according to Group D spacing
            this.Detail.HeightF = 492F;
        }
    }
}
