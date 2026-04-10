using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level visa cancellation letter for App_Cancel_Visa.
    /// Sent to the head of the national State Migration Service requesting visa cancellation
    /// for foreign nationals departing or terminating their stay.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Cancel_Visa_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppCancelVisaReport : AppGroupDBaseReport
    {
        public AppCancelVisaReport()
        {
            // xrRichBody1 — Visa cancellation request paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyny\u328? \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany \b wizasyny \u253?atyrmagy\u328?yzy\b0  Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set final Detail height to clear content according to Group D spacing
            this.Detail.HeightF = 492F;
        }
    }
}
