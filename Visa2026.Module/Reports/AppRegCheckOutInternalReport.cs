using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level internal check-out letter for App_Reg_Check_Out_Internal.
    /// Sent to Migration Service requesting de-registration of foreign nationals
    /// moving between regions within Turkmenistan.
    /// Identical to AppRegCheckInInternalReport except body1 action is "hasapdan çykarmagyňyzy".
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Check_Out_Internal_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegCheckOutInternalReport : AppGroupEBaseReport
    {
        public AppRegCheckOutInternalReport()
        {
            // xrRichBody1 — Internal address-change check-out paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyny\u328? \b \u253?a\u351?a\u253?an salgysyny [FromRegionName_Genitive] [FromCityName_Ablative] [ToRegionName_Genitive] [ToCityName_Dative] \u252?\u253?tge\u253?\u228?ndigi\b0  seb\u228?pli hasapdan \u231?ykarmagynyzy Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set height to 497F to accommodate the taller (90F) internal movement paragraph 
            // while maintaining vertical centering (§20).
            this.Detail.HeightF = 497F;
        }
    }
}
