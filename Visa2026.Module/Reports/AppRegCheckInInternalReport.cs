using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level internal check-in letter for App_Reg_Check_In_Internal.
    /// Sent to Migration Service requesting re-registration of foreign nationals
    /// moving between regions within Turkmenistan.
    /// Identical to AppRegCheckInReport except body1 references FromCity/ToCity movement.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Reg_Check_In_Internal_app.jpg
    /// Map: Resources/FormTemplates/App_Reg_Check_In_Internal_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppRegCheckInInternalReport : AppGroupEBaseReport
    {
        public AppRegCheckInInternalReport()
        {
            // xrRichBody1 — Internal address-change check-in paragraph unique to this report
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyny\u328? \b \u253?a\u351?a\u253?an salgysyny [FromRegionName_Genitive] [FromCityName_Ablative] [ToRegionName_Genitive] [ToCityName_Dative] \u252?\u253?tge\u253?\u228?ndigi\b0  seb\u228?pli hasaba almagy\u328?yzy Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // Set height to 497F to accommodate the taller (90F) internal movement paragraph 
            // while maintaining vertical centering (§20).
            this.Detail.HeightF = 497F;
        }
    }
}
