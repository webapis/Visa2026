namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level personnel list for App_Change_Inv.
    /// 13-column "Daşary ýurt raýatlarynyň sanawy" table on A4 Landscape.
    /// Columns: №, Familiýasy, Ady, Doglan senesi we ýurdy, Jynsy, Raýatlygy,
    ///          Pasport belgisi we möhleti, Bilimi we okan ýeri,
    ///          Bilimine görä hünäri, Wezipesi,
    ///          Türkmenistandaky salgysy, Daşary ýurtdaky salgysy,
    ///          Barjak serhet ýakasy.
    /// Inherits page header / signatory footer from AppItemBaseReport.
    /// Reference image: Resources/FormTemplates/App_Change_Inv_item.jpg
    /// </summary>
    public partial class AppChangeInvItemReport : AppItemBaseReport
    {
        public AppChangeInvItemReport()
        {
            InitializeComponent();
        }
    }
}
