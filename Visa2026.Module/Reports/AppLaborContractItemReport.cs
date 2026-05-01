using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Per-person labor contract (Zähmet şertnamasy) for each <c>ApplicationItem</c> (any parent application type).
    /// Reference image: Resources/FormTemplates/App_Labor_Contract_item.png
    /// Visual standards: REPORT_STANDARDS.md (XRRichText \qj\fi720, TNR 15pt).
    /// </summary>
    public partial class AppLaborContractItemReport : AppItemBaseReport
    {
        public AppLaborContractItemReport()
        {
            InitializeComponent();
            this.Margins = new DXMargins(100F, 100F, 50F, 60F);
            this.ReportFooter.Visible = false;
            this.xrLabelAppNumber.LocationFloat = new DevExpress.Utils.PointFloat(326.7717F, 5F);
            this.xrLabelAppDate.LocationFloat = new DevExpress.Utils.PointFloat(326.7717F, 18F);
        }
    }
}
