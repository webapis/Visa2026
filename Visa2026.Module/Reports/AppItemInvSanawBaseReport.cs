namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Group base for all ApplicationItem-level invitation / work-permit list reports.
    /// Provides the standard 14-column "Daşary ýurt raýatlarynyň sanawy" table on A4 Landscape.
    /// Columns: №, Familiýasy, Ady, Doglan senesi we ýeri, Jynsy, Raýatlygy,
    ///          Pasport belgisi we möhleti, Bilimi we okan ýeri, Bilimine görä hünäri,
    ///          Wezipesi, Möhleti we gezekligi, Türkmenistanaky salgysy,
    ///          Daşary ýurtdaky salgysy, Barjak serhet ýakasy.
    /// Inherits page header / signatory footer from AppItemBaseReport.
    /// Derived reports that use the singular title ("raýatynyň") override xrLabelTitle.Text.
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppItemInvSanawBaseReport : AppItemBaseReport
    {
        public AppItemInvSanawBaseReport()
        {
            InitializeComponent();
        }
    }
}
