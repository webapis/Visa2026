namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Group base for all ApplicationItem-level registration list reports (check-in, check-out, ext).
    /// Provides the standard 11-column "Daşary ýurt raýatlarynyň sanawy" table on A4 Landscape.
    /// Columns: №, Familiýasy, Ady, Doglan senesi, Jynsy, Raýatlygy,
    ///          Pasportynyň belgisi, Pasportynyň möhleti,
    ///          Gelmeginiň maksady, Wiza maglumatlary, Türkmenistanaky salgysy.
    /// Inherits page header / signatory footer from AppItemBaseReport.
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppItemRegSanawBaseReport : AppItemBaseReport
    {
        public AppItemRegSanawBaseReport()
        {
            InitializeComponent();
        }
    }
}
