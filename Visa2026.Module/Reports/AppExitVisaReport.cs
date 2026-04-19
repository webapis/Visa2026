using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level exit visa letter for App_Exit_Visa.
    /// Sent to a Ministry requesting a 1-month exit visa for foreign nationals whose work is incomplete.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Exit_Visa_app.jpg
    /// Map: Resources/FormTemplates/App_Exit_Visa_app_map.md
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppExitVisaReport : AppGroupABaseReport
    {
        public AppExitVisaReport()
        {
            // xrLabelUrgency — Italic urgency line as per §20B
            this.xrLabelUrgency.Font = new DevExpress.Drawing.DXFont("Times New Roman", 15F, DevExpress.Drawing.DXFontStyle.Italic);

            // xrRichBody2 — Exit visa request paragraph unique to this report
            this.xrRichBody2.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen T\u252?rki\u253?e Respublikasyny\u328? \ldblquote [Company.Name]\rdblquote kompani\u253?asyna degi\u351?li bolan sanawdaky \b [TotalPersonCount] ([TotalPersonCountText])\b0  da\u351?ary \u253?urt ra\u253?aty wizalaryny\u328? tamamlan\u253?an senesine \u231?enli \u246?z jogapk\u228?r\u231?iligine degi\u351?li bolan i\u351?leri doly tamamlap \u253?eti\u351?mey\u228?ndikleri seb\u228?pli olara T\u252?rkmenistany\u328? D\u246?wlet migrasiy\u253?a gullugy tarapyndan \b [VisaPeriod_NameTm] m\u246?hleti bilen \u231?yky\u351? wizasyny resmile\u351?dirmek\b0  meselesinde \u253?ardam bermegi\u328?izi Sizden ha\u253?y\u351? ed\u253?\u228?ris.\par}";

            // xrLabelAttachments — passport copies + person list
            this.xrLabelAttachments.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "'Goşundy:   1. ' + [TotalPersonCount] + '-pasport kopiýalary,' + Char(10) + '           2. Goşundy (' + [TotalPersonCount] + '-daşary ýurt raýatynyň maglumaty)'"));
        }
    }
}
