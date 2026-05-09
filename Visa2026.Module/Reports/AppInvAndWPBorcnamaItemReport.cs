using System;
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
            ReportFooter.HeightF = 0f;
            ReportFooter.PrintAtBottom = false;
            BottomMargin.HeightF = 40f;

            var w = PageWidthF - Margins.Left - Margins.Right - 2f;
            xrRichHeader.WidthF = w;
            xrLabelTitle.WidthF = w;
            xrRichBody.WidthF = w;

            // DevExpress: controls whose right edge meets or exceeds printable width can force an extra (often blank) page.
            ClampDetailControlsToPrintableWidth();
        }

        private void ClampDetailControlsToPrintableWidth()
        {
            float printableRight = PageWidthF - Margins.Left - Margins.Right - 1f;
            foreach (XRControl c in Detail.Controls)
            {
                float right = c.LocationFloat.X + c.WidthF;
                if (right > printableRight)
                    c.WidthF = Math.Max(1f, printableRight - c.LocationFloat.X);
            }
        }
    }
}
