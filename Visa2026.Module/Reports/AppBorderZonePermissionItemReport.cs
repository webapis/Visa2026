namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level personnel list for App_Border_Zone_Permission.
    /// 11-column "Daşary ýurt raýatlarynyň sanawy" table on A4 Landscape.
    /// Columns: №, Familiýasy, Ady, Doglan senesi we ýeri, Jynsy, Raýatlygy,
    ///          Pasport belgisi we möhleti, Wezipesi, Möhleti we gezekligi,
    ///          Türkmenistanaky salgysy, Barjak serhet ýakasy.
    /// Inherits page header / signatory footer from AppItemBaseReport.
    /// Reference image: Resources/FormTemplates/App_Border_Zone_Permission_item.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppBorderZonePermissionItemReport : AppItemBaseReport
    {
        public AppBorderZonePermissionItemReport()
        {
            InitializeComponent();
        }
    }
}
