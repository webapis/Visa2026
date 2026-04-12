using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level additional work permit location request for App_Additional_WP_location.
    /// Sent to the Ministry/Organization linked via Application.ProjectContract.Ministry.
    /// Recipient block and salutation come from Ministry.RecipientBlock and Ministry.FormOfAddress.
    /// Contract reference paragraph comes from ProjectContract.Description (plain text).
    /// Work permit location comes from Application.MovementPermitLocation.NameTm (flat: MovementPermitLocation_NameTm).
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Additional_WP_location_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppAdditionalWPLocationReport : AppGroupCBaseReport
    {
        public AppAdditionalWPLocationReport()
        {
            // xrRichBody2 — Additional WP location request paragraph unique to this report
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 \u350?ertname esasynda, \u246?\u328?de go\u253?lan wezipeleri \u253?etinlikli durmu\u351?a ge\u231?irmek \u252?\u231?in hatymyzy\u328? go\u351?undysynda g\u246?rkezilen \ldblquote [Company.Name]\rdblquote  kompani\u253?asyna degi\u351?li bolan \b [TotalPersonCount] ([TotalPersonCountText])\b0  sany da\u351?ary \u253?urt ra\u253?atyny\u328? \b [MovementPermitLocation_NameTm]\b0  i\u351? rugsatnamalaryn\u253?\u328? berilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // xrLabelAttachments — Unique attachment list expression
            this.xrLabelAttachments.ExpressionBindings.Add(new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'"));

            // Set final Detail height to clear content according to §20A spacing
            this.Detail.HeightF = 580F;
        }
    }
}
