namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level personnel list for App_Cancel_Inv_WP.
    /// 13-column "Daşary ýurt raýatlarynyň sanawy" table on A4 Portrait.
    /// Columns: №, AS-№, Tassyk-nama belgisi, Familiýasy, Ady,
    ///          Doglan senesi we şurdy, Pasport belgisi, Hünäri we bilimi,
    ///          Hereket edýän çägi, Rugsat edilen möhleti,
    ///          Çakylyk belgisi, Çakylygyň resmileşdirilen senesi,
    ///          Çakylygyň möhleti tamamlanýan sene.
    /// Inherits page header / signatory footer from AppItemBaseReport (portrait A4).
    /// Reference image: Resources/FormTemplates/App_Cancel_Inv_WP_item.jpg
    /// Standards: Reports/REPORT_STANDARDS.md
    /// </summary>
    public partial class AppCancelInvWPItemReport : AppItemBaseReport
    {
        public AppCancelInvWPItemReport()
        {
            InitializeComponent();
        }
    }
}
