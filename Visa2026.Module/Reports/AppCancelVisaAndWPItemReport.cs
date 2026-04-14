namespace Visa2026.Module.Reports
{
    /// <summary>
    /// ApplicationItem-level personnel list for App_Cancel_Visa_and_WP.
    /// 12-column "Daşary ýurt raýatlarynyň sanawy" table on A4 Landscape.
    /// Columns: №, AS-№, Tassyk-nama belgisi, Familiýasy, Ady,
    ///          Doglan senesi we ýurdy, Pasport belgisi, Hünäri we bilimi,
    ///          Hereket edýän çägi, Rugsat edililen möhleti,
    ///          Wiza belgisi, Wiza möhleti başlanýan we tamamlanýan senesi.
    /// Inherits page header / signatory footer from AppItemBaseReport.
    /// Reference image: Resources/FormTemplates/App_Cancel_Visa_and_WP_item.jpg
    /// </summary>
    public partial class AppCancelVisaAndWPItemReport : AppItemBaseReport
    {
        public AppCancelVisaAndWPItemReport()
        {
            InitializeComponent();
        }
    }
}
