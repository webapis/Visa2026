namespace Visa2026.Module.Reports
{
    /// <summary>
    /// BusinessTrip-level personnel list for App_Business_Trip_Arrival and App_Business_Trip_Departure.
    /// 11-column "Daşary ýurt raýatlarynyň sanawy" table on A4 Landscape.
    /// Shared between both ApplicationTypes — registered once with visibility covering both.
    /// Reference images: Resources/FormTemplates/App_Business_Trip_Arrival_BusinessTrip.jpg
    ///                   Resources/FormTemplates/App_Business_Trip_Departure_BusinessTrip.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppBusinessTripSanawReport : DevExpress.XtraReports.UI.XtraReport
    {
        public AppBusinessTripSanawReport()
        {
            InitializeComponent();
        }
    }
}
