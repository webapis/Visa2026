using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Application-level business trip departure notification letter for App_Business_Trip_Departure.
    /// Sent to the State Migration Service notifying of departing foreign nationals on a business trip.
    /// Inherits letterhead background from AppBaseReport (loaded dynamically by Company.Code).
    /// Reference image: Resources/FormTemplates/App_Business_Trip_Departure_app.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppBusinessTripDepartureReport : AppGroupEBaseReport
    {
        public AppBusinessTripDepartureReport()
        {
            // Shift all content up: base places recipient at Y=218F, wasting 218F at top.
            // Moving to Y=20F saves ~198F, keeping the signatory on page 1 for typical descriptions.
            this.xrLabelRecipient.LocationFloat = new DevExpress.Utils.PointFloat(220F, 20F);

            // Body1: Y=108F (recipient bottom 100F + 8F gap)
            this.xrRichBody1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 108F);
            this.xrRichBody1.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 Hatymyzy\u328? go\u351?undysynda g\u246?rkezilen sanawdaky \b [TotalPersonCountText] ([TotalPersonCount]) sany\b0  da\u351?ary \u253?urt ra\u253?atlaryny\u328? \b [BusinessTripStartDateText]-den [BusinessTripEndDateText]-ne\b0  \u231?enli \b [BusinessTripDurationDays] g\u252?n\b0  m\u246?hlet bilen \b [ToRegionName_Genitive]\b0  i\u351? saparyna \b gid\u253?\u228?ndigini\b0  size habar ber\u253?\u228?ris.\par}";

            // Body3 (Maksady:Description): Y=196F (body1 bottom 188F + 8F gap); CanGrow for long text
            var xrRichBody3 = new XRRichText();
            ((System.ComponentModel.ISupportInitialize)xrRichBody3).BeginInit();
            xrRichBody3.BackColor = System.Drawing.Color.Transparent;
            xrRichBody3.Borders = DevExpress.XtraPrinting.BorderSide.None;
            xrRichBody3.CanGrow = true;
            xrRichBody3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 196F);
            xrRichBody3.Name = "xrRichBody3";
            xrRichBody3.SizeF = new System.Drawing.SizeF(626.7717F, 80F);
            xrRichBody3.Rtf = @"{\rtf1\ansi\deff0{\fonttbl{\f0\froman\fcharset0 Times New Roman;}}\f0\fs30\pard\qj\fi720 \b Maksady:\b0  [ProjectContract_Description]\par}";
            ((System.ComponentModel.ISupportInitialize)xrRichBody3).EndInit();

            // Body2 (Responsibility): Y=284F (body3 initial bottom 276F + 8F gap)
            this.xrRichBody2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 284F);

            this.Detail.Controls.Add(xrRichBody3);
            this.Detail.HeightF = 400F;
        }
    }
}
