using DevExpress.Drawing;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Per-person Borçnama (commitment) for <c>App_Inv_And_WP</c> — foreign worker WP visa and work permit.
    /// Reference: <c>Resources/FormTemplates/App_Inv_And_WP_item_borcnama.png</c>.
    /// </summary>
    public partial class AppInvAndWPBorcnamaItemReport : AppItemBaseReport
    {
        public AppInvAndWPBorcnamaItemReport()
        {
            InitializeComponent();

            Margins = new DXMargins(40F, 40F, 30F, 30F);
            ReportFooter.Visible = false;

            var w = PageWidthF - Margins.Left - Margins.Right;
            xrRichHeader.WidthF = w;
            xrLabelTitle.WidthF = w;
            xrRichBody.WidthF = w;
        }
    }
}
