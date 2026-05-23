using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class ApplicationTypeReferenceReport
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Designer generated code

        private void InitializeComponent()
        {
            this.TopMargin = new TopMarginBand();
            this.ReportHeader = new ReportHeaderBand();
            this.xrLabelTitle = new XRLabel();
            this.PageHeader = new PageHeaderBand();
            this.xrTableHeader = new XRTable();
            this.xrRowHeader = new XRTableRow();
            this.xrHdrCode = new XRTableCell();
            this.xrHdrTypeName = new XRTableCell();
            this.xrHdrCategory = new XRTableCell();
            this.Detail = new DetailBand();
            this.xrTableData = new XRTable();
            this.xrRowData = new XRTableRow();
            this.xrCellCode = new XRTableCell();
            this.xrCellTypeName = new XRTableCell();
            this.xrCellCategory = new XRTableCell();
            this.BottomMargin = new BottomMarginBand();
            this.AppTypeDataSource = new DevExpress.Persistent.Base.ReportsV2.CollectionDataSource();

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AppTypeDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();

            // A4 portrait — printable width 726.7717F (margins 50)
            this.Landscape = false;
            this.PageWidthF = 826.7717F;
            this.PageHeightF = 1169.291F;
            this.Margins = new DXMargins(50F, 50F, 50F, 60F);
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.BackColor = System.Drawing.Color.White;
            this.Version = "25.2";

            this.AppTypeDataSource.Name = "AppTypeDataSource";
            this.AppTypeDataSource.ObjectTypeName = "Visa2026.Module.BusinessObjects.ApplicationType";
            this.AppTypeDataSource.TopReturnedRecords = 0;
            this.DataSource = this.AppTypeDataSource;

            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name = "TopMargin";

            this.ReportHeader.HeightF = 40F;
            this.ReportHeader.Name = "ReportHeader";
            this.ReportHeader.Controls.Add(this.xrLabelTitle);

            this.xrLabelTitle.Text = "Arza g\u00F6rn\u00FC\u015Flerini\u0148 3 sanly kodlaryny\u0148 sanawy";
            this.xrLabelTitle.Font = new DXFont("Times New Roman", 12F, DXFontStyle.Bold);
            this.xrLabelTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 5F);
            this.xrLabelTitle.Name = "xrLabelTitle";
            this.xrLabelTitle.SizeF = new System.Drawing.SizeF(726.7717F, 30F);
            this.xrLabelTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelTitle.BackColor = System.Drawing.Color.Transparent;

            this.PageHeader.HeightF = 28F;
            this.PageHeader.Name = "PageHeader";
            this.PageHeader.Controls.Add(this.xrTableHeader);

            this.xrHdrCode.Name = "xrHdrCode";
            this.xrHdrCode.Text = "Kod";
            this.xrHdrCode.Weight = 70D;

            this.xrHdrTypeName.Name = "xrHdrTypeName";
            this.xrHdrTypeName.Text = "Arza g\u00F6rn\u00FC\u015Fi";
            this.xrHdrTypeName.Weight = 356D;

            this.xrHdrCategory.Name = "xrHdrCategory";
            this.xrHdrCategory.Text = "Topar";
            this.xrHdrCategory.Weight = 300.7717D;

            foreach (var hc in new[] { this.xrHdrCode, this.xrHdrTypeName, this.xrHdrCategory })
            {
                hc.Font = new DXFont("Times New Roman", 9F, DXFontStyle.Bold);
                hc.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
                hc.WordWrap = true;
                hc.Multiline = true;
                hc.Borders = DevExpress.XtraPrinting.BorderSide.All;
                hc.BorderWidth = 0.5F;
                hc.BorderColor = System.Drawing.Color.Black;
                hc.BackColor = System.Drawing.Color.Transparent;
                hc.StylePriority.UseBorders = true;
                hc.StylePriority.UseBorderWidth = true;
                hc.StylePriority.UseBorderColor = true;
                hc.StylePriority.UseBackColor = true;
            }

            this.xrRowHeader.Cells.AddRange(new XRTableCell[] {
                this.xrHdrCode, this.xrHdrTypeName, this.xrHdrCategory
            });
            this.xrRowHeader.HeightF = 28F;
            this.xrRowHeader.Name = "xrRowHeader";

            this.xrTableHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableHeader.Name = "xrTableHeader";
            this.xrTableHeader.Rows.Add(this.xrRowHeader);
            this.xrTableHeader.SizeF = new System.Drawing.SizeF(726.7717F, 28F);

            this.Detail.HeightF = 22F;
            this.Detail.CanGrow = true;
            this.Detail.Name = "Detail";
            this.Detail.Controls.Add(this.xrTableData);

            this.xrCellCode.Name = "xrCellCode";
            this.xrCellCode.Weight = 70D;
            this.xrCellCode.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[SelectionCode]"));

            this.xrCellTypeName.Name = "xrCellTypeName";
            this.xrCellTypeName.Weight = 356D;
            this.xrCellTypeName.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "Iif(IsNullOrEmpty([NameTm]), [Name], [NameTm])"));

            this.xrCellCategory.Name = "xrCellCategory";
            this.xrCellCategory.Weight = 300.7717D;
            this.xrCellCategory.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "Iif(IsNull([ApplicationTypeFilter]), '', [ApplicationTypeFilter.NameTm])"));

            foreach (var dc in new[] { this.xrCellCode, this.xrCellTypeName, this.xrCellCategory })
            {
                dc.Font = new DXFont("Times New Roman", 9F);
                dc.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
                dc.Padding = new DevExpress.XtraPrinting.PaddingInfo(4, 4, 2, 2, 100F);
                dc.WordWrap = true;
                dc.CanGrow = true;
                dc.Borders = DevExpress.XtraPrinting.BorderSide.Left
                             | DevExpress.XtraPrinting.BorderSide.Right
                             | DevExpress.XtraPrinting.BorderSide.Bottom;
                dc.BorderWidth = 0.5F;
                dc.BorderColor = System.Drawing.Color.Black;
                dc.BackColor = System.Drawing.Color.Transparent;
                dc.StylePriority.UseBorders = true;
                dc.StylePriority.UseBorderWidth = true;
                dc.StylePriority.UseBorderColor = true;
                dc.StylePriority.UseBackColor = true;
            }

            this.xrCellCode.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            this.xrRowData.Cells.AddRange(new XRTableCell[] {
                this.xrCellCode, this.xrCellTypeName, this.xrCellCategory
            });
            this.xrRowData.HeightF = 22F;
            this.xrRowData.CanGrow = true;
            this.xrRowData.Name = "xrRowData";

            this.xrTableData.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableData.Name = "xrTableData";
            this.xrTableData.Rows.Add(this.xrRowData);
            this.xrTableData.SizeF = new System.Drawing.SizeF(726.7717F, 22F);
            this.xrTableData.CanGrow = true;

            this.BottomMargin.HeightF = 60F;
            this.BottomMargin.Name = "BottomMargin";

            this.Bands.AddRange(new Band[] {
                this.TopMargin,
                this.ReportHeader,
                this.PageHeader,
                this.Detail,
                this.BottomMargin
            });
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
                this.AppTypeDataSource
            });

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AppTypeDataSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        #endregion

        private TopMarginBand TopMargin;
        private ReportHeaderBand ReportHeader;
        private PageHeaderBand PageHeader;
        private DetailBand Detail;
        private BottomMarginBand BottomMargin;
        private XRLabel xrLabelTitle;
        private XRTable xrTableHeader;
        private XRTableRow xrRowHeader;
        private XRTableCell xrHdrCode;
        private XRTableCell xrHdrTypeName;
        private XRTableCell xrHdrCategory;
        private XRTable xrTableData;
        private XRTableRow xrRowData;
        private XRTableCell xrCellCode;
        private XRTableCell xrCellTypeName;
        private XRTableCell xrCellCategory;
        private DevExpress.Persistent.Base.ReportsV2.CollectionDataSource AppTypeDataSource;
    }
}
