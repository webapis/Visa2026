using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level invitation letter for App_Inv.
    /// Sent to a Ministry requesting visa invitation for foreign nationals.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Inv_app.jpg
    /// Map: Resources/FormTemplates/App_Inv_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppInvReport : AppGroupABaseReport
    {
        public AppInvReport()
        {
            // xrLabelUrgency — Italic urgency line as per §20B
            this.xrLabelUrgency.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Italic);

            // xrRichBody2 — Invitation request paragraph unique to this report
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen T\u252?rki\u253?e Respublikasyny\u328? \ldblquote [Application_Company_Name]\rdblquote kompani\u253?asyna degi\u351?li bolan sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna \b [VisaPeriod_NameTm] m\u246?hlet\b0  bilen \b [VisaCategory_NameTm]\b0  \u231?akylyk resmile\u351?dirilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // xrLabelAttachments — Unique two-line list expression for this report
            this.xrLabelAttachments.ExpressionBindings.Add(new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'"));

            // Detail.HeightF is inherited from AppGroupABaseReport (535F = attachments end 524F + 11F)
        }
    }
}
