using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level FM visa extension request for App_Visa_Ext_FM.
    /// Sent to the Ministry/Organization linked via Application.ProjectContract.Ministry.
    /// Requests visa extension for family members of a sponsoring employee.
    /// Nearly identical to AppInvFMReport — body3 ends with "wizalaryny resmileşdirilmegine"
    /// instead of "çakylyk resmileşdirilmegine".
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Visa_Ext_FM_app.jpg
    /// Map file: Resources/FormTemplates/App_Visa_Ext_FM_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppVisaExtFMReport : AppGroupBBaseReport
    {
        public AppVisaExtFMReport()
        {
            // xrLabelUrgency — Italic urgency line as per §20B
            this.xrLabelUrgency.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Italic);

            // xrRichBody3 — FM visa extension request paragraph unique to this report
            // Differs from AppInvFMReport: ends with "[VisaCategory_NameTm] wizalarynyň möhletiniň uzaldylmagyna"
            this.xrRichBody3.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 T\u252?rkmenistandaky \u231?\u228?klerinde amala a\u351?yrl\u253?an taslamalar utga\u351?dyrmak bo\u253?un\u231?a [Company.Name] kompani\u253?asyna degi\u351?li h\u252?n\u228?rmeni\u328? ma\u351?gala agzalaryna \u253?agny, hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna [FamilyMember_Relationship_NameTm] wiza m\u246?hletine g\u246?r\u228? (\b [SponsoringEmployee_FullName] - [SponsoringEmployee_PositionTm]\b0 ) \b [VisaCategory_NameTm] wizalaryny\u328? m\u246?hletini\u328? uzaldylmagyna\b0  \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // xrLabelAttachments — Unique two-line list expression for this report
            this.xrLabelAttachments.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'"));

            // Set final Detail height to clear content according to §20B spacing
            this.Detail.HeightF = 633F;
        }
    }
}
