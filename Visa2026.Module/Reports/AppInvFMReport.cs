using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level invitation letter for App_Inv_FM (Family Member).
    /// Sent to a Ministry requesting visa invitation for family members of an employee.
    /// Includes two extra introductory paragraphs and references the sponsoring employee inline.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Inv_FM_app.jpg
    /// Map: Resources/FormTemplates/App_Inv_FM_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppInvFMReport : AppGroupBBaseReport
    {
        public AppInvFMReport()
        {
            // xrLabelUrgency — Italic urgency line as per §20B
            this.xrLabelUrgency.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Italic);

            // xrRichBody3 — FM invitation request paragraph unique to this report
            this.xrRichBody3.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 T\u252?rkmenistandaky \u231?\u228?klerinde amala a\u351?yrl\u253?an taslamalar utga\u351?dyrmak bo\u253?un\u231?a \ldblquote [Application_Company_Name]\rdblquote  kompani\u253?asyna degi\u351?li h\u252?n\u228?rmeni\u328? ma\u351?gala agzalaryna \u253?agny, hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna [FamilyMember_Relationship_NameTm] (\b [SponsoringEmployee_FullName] - [SponsoringEmployee_PositionTm]\b0 ) \b [VisaPeriod_NameTm] m\u246?hlet\b0  bilen \b [VisaCategory_NameTm]\b0  \u231?akylyk resmile\u351?dirilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // xrLabelAttachments — Unique two-line list expression for this report
            this.xrLabelAttachments.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'"));

            // Set final Detail height to clear content according to §20B spacing
            this.Detail.HeightF = 633F;
        }
    }
}
