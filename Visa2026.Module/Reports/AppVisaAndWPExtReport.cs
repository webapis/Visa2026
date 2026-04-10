using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level visa and work permit extension request for App_Visa_and_WP_Ext.
    /// Sent to the Ministry/Organization linked via Application.ProjectContract.Ministry.
    /// Recipient block and salutation come from Ministry.RecipientBlock (RTF) and Ministry.FormOfAddress.
    /// Contract reference paragraph comes from ProjectContract.Description (plain text).
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Visa_and_WP_Ext_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppVisaAndWPExtReport : AppGroupCBaseReport
    {
        public AppVisaAndWPExtReport()
        {
            // xrRichBody2 — Visa+WP extension request paragraph unique to this report
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyna \b [VisaPeriod_NameTm] [VisaCategory_NameTm]\b0  m\u246?hleti bilen uzaldylmagyna rugsat bermegini Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // xrLabelAttachments — Unique two-line list expression for this report
            this.xrLabelAttachments.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'"));

            // Set final Detail height according to §20A spacing
            this.Detail.HeightF = 540F;
        }
    }
}
