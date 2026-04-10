using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level invitation and work permit cancellation letter for App_Cancel_Inv_WP.
    /// Sent to the head of the State Migration Service requesting simultaneous cancellation
    /// of work permits and invitations for foreign nationals.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Cancel_Inv_WP_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppCancelInvWPReport : AppGroupDBaseReport
    {
        public AppCancelInvWPReport()
        {
            // xrRichBody1 — Invitation and WP cancellation request paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [CancelPersonCount] ([CancelPersonCountText]) sany\b0  da\u351?ary \u253?urt ra\u253?atyny\u328? \b [CancelWPCount] ([CancelWPCountText]) sany\b0  i\u351?lemek \u252?\u231?\u252?n rugsatnamasyny we \b [CancelInvCount] ([CancelInvCountText]) sany\b0  \u231?akylygyny \u253?atyrmagy\u328?yzy Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set final Detail height to clear content according to Group D spacing
            this.Detail.HeightF = 492F;
        }
    }
}
