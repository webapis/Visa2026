using DevExpress.Drawing;
using DevExpress.Utils;
using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;

namespace Visa2026.Module.Reports
{
    /// <summary>
    /// Redesigned labor contract (Zähmet şertnamasy) for each <see cref="BusinessObjects.ApplicationItem"/>.
    /// Reference layout: Resources/FormTemplates/App_Labor_Contract_item.png.
    /// </summary>
    public partial class AppLaborContractItemReportV2 : AppItemBaseReport
    {
        public AppLaborContractItemReportV2()
        {
            InitializeComponent();

            Margins = new DXMargins(40F, 40F, 30F, 40F);
            ReportFooter.Visible = false;
            PageHeader.Visible = false;

            var contentWidth = PageWidthF - (Margins.Left + Margins.Right);

            XRLabel[] fullWidthLabels =
            {
                lblTitle,
                lblCity,
                lblIntroParagraph,
                lblSection1Header,
                lblSection1Body,
                lblSection2Header,
                lblSection2Body,
                lblSection3Header,
                lblSection3Body,
                lblSection4Header,
                lblSection4Body,
                lblSection5Header,
                lblSection5Line1,
                lblSection5Line2,
                lblSection6Header,
                lblSection6Line1,
                lblSection6Line2,
                lblSection7Header
            };

            foreach (var label in fullWidthLabels)
            {
                label.WidthF = contentWidth;
            }

            foreach (var paragraphLabel in new[]
                     {
                         lblIntroParagraph,
                         lblSection1Body,
                         lblSection2Body,
                         lblSection3Body,
                         lblSection4Body,
                         lblSection5Line2
                     })
            {
                paragraphLabel.TextAlignment = TextAlignment.TopJustify;
            }

            panelSignatures.WidthF = contentWidth;

            const float columnGap = 20F;
            var columnWidth = (contentWidth - columnGap) / 2F;

            XRLabel[] employerColumn =
            {
                lblEmployerHeader,
                lblEmployerSignatory,
                lblEmployerCompany,
                lblEmployerAddress
            };

            foreach (var label in employerColumn)
            {
                label.WidthF = columnWidth;
                label.LocationFloat = new PointFloat(0F, label.LocationFloat.Y);
            }

            XRLabel[] employeeColumn =
            {
                lblEmployeeHeader,
                lblEmployeeName,
                lblEmployeePassport
            };

            foreach (var label in employeeColumn)
            {
                label.WidthF = columnWidth;
                label.LocationFloat = new PointFloat(columnWidth + columnGap, label.LocationFloat.Y);
            }
        }
    }
}
