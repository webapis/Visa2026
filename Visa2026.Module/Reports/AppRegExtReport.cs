using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level registration extension letter for App_Reg_ext.
    /// Sent to Migration Service requesting extension of registration period
    /// for foreign nationals whose visa period has been extended.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_ext_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegExtReport : AppGroupEBaseReport
    {
        public AppRegExtReport()
        {
            // xrRichBody1 — Registration extension request paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atlaryny\u328? \b wiza m\u246?hleti uzaldylandygy seb\u228?pli hasaba aly\u351? m\u246?hletini uzaltmagynyzy\b0  Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set final Detail height to clear content according to Group E spacing
            this.Detail.HeightF = 492F;
        }
    }
}
