using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level border zone permission request letter for App_Border_Zone_Permission.
    /// Sent to the Ministry head requesting border zone visa registration for foreign nationals.
    /// Recipient and greeting are dynamic via ProjectContract.Ministry.
    /// Body1 uses ProjectContract.Description; body2 uses BorderZoneLocation.NameTm.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Border_Zone_Permission_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppBorderZonePermissionReport : AppGroupCBaseReport
    {
        public AppBorderZonePermissionReport()
        {
            // xrRichBody2 — Border zone permission request paragraph unique to this report
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 \u350?ertname esasynda, \u246?\u328?de go\u351?lan wezipeleri \u253?etinlikli durmu\u351?a ge\u231?irmek \u252?\u231?\u252?in hatymyzy\u328? go\u351?undysynda g\u246?rkezilen \u8220?[Company.Name]\u8221? kompani\u253?asyny\u328? i\u351?\u231?i bolup \b [TotalPersonCount] ([TotalPersonCountText]) sany\b0  da\u351?ary \u253?urt ra\u253?atyny\u328? \b [BorderZoneLocation_NameTm]\b0  serhet \u253?aka wizasyny\u328? resmile\u351?dirilmegine \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // xrLabelAttachments — Unique attachment list expression
            this.xrLabelAttachments.ExpressionBindings.Add(new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text",
                "'Goşundy: 1. Daşary ýurt raýatlarynyň sanawy-' + [TotalPersonCount] + Char(10) + '                2. ' + [TotalPersonCount] + '(' + [TotalPersonCountText] + ')- sany daşary ýurt raýatynyň maglumaty'"));

            // Set final Detail height to clear content according to §20A spacing
            this.Detail.HeightF = 540F;
        }
    }
}
