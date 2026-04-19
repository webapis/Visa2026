using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class WorkPermitListReport
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
            this.TopMargin                  = new TopMarginBand();
            this.ReportHeader               = new ReportHeaderBand();
            this.xrLabelHeader              = new XRLabel();
            this.xrLabelMaglumat            = new XRLabel();
            this.PageHeader                 = new PageHeaderBand();
            this.xrTableHeader              = new XRTable();
            this.xrRowHeader                = new XRTableRow();
            this.xrHdrNo                    = new XRTableCell();
            this.xrHdrFaa                   = new XRTableCell();
            this.xrHdrDoglan                = new XRTableCell();
            this.xrHdrPasport               = new XRTableCell();
            this.xrHdrBilimi                = new XRTableCell();
            this.xrHdrWezipesi              = new XRTableCell();
            this.xrHdrSalgysy               = new XRTableCell();
            this.xrHdrWP                    = new XRTableCell();
            this.xrHdrWiza                  = new XRTableCell();
            this.xrHdrBellik                = new XRTableCell();
            this.Detail                     = new DetailBand();
            this.xrTableData                = new XRTable();
            this.xrRowData                  = new XRTableRow();
            this.xrCellNo                   = new XRTableCell();
            this.xrCellFaa                  = new XRTableCell();
            this.xrCellDoglan               = new XRTableCell();
            this.xrCellPasport              = new XRTableCell();
            this.xrCellBilimi               = new XRTableCell();
            this.xrCellWezipesi             = new XRTableCell();
            this.xrCellSalgysy              = new XRTableCell();
            this.xrCellWP                   = new XRTableCell();
            this.xrCellWiza                 = new XRTableCell();
            this.xrCellBellik               = new XRTableCell();
            this.ReportFooter               = new ReportFooterBand();
            this.xrLabelFooterNote          = new XRLabel();
            this.xrLabelSignatoryPosition   = new XRLabel();
            this.xrLabelSignatoryFullName   = new XRLabel();
            this.BottomMargin               = new BottomMarginBand();
            this.WPDataSource               = new DevExpress.Persistent.Base.ReportsV2.CollectionDataSource();

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.WPDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();

            // ----------------------------------------------------------------
            // Page — A4 Portrait, margins L=50 R=50 T=50 B=60, printable 726.7717F
            // ----------------------------------------------------------------
            this.Landscape      = false;
            this.PageWidthF     = 826.7717F;
            this.PageHeightF    = 1169.291F;
            this.Margins        = new DXMargins(50F, 50F, 50F, 60F);
            this.PaperKind      = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.BackColor      = System.Drawing.Color.White;
            this.Version        = "25.2";

            // ----------------------------------------------------------------
            // Data source — WorkPermitItem
            // ----------------------------------------------------------------
            this.WPDataSource.Name           = "WPDataSource";
            this.WPDataSource.ObjectTypeName = "Visa2026.Module.BusinessObjects.WorkPermitItem";
            this.WPDataSource.TopReturnedRecords = 0;
            this.DataSource = this.WPDataSource;

            // ----------------------------------------------------------------
            // TopMargin
            // ----------------------------------------------------------------
            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name    = "TopMargin";

            // ----------------------------------------------------------------
            // ReportHeader — dynamic header text + "MAGLUMAT" label
            // ----------------------------------------------------------------
            this.ReportHeader.HeightF = 70F;
            this.ReportHeader.Name    = "ReportHeader";
            this.ReportHeader.Controls.AddRange(new XRControl[] {
                this.xrLabelHeader,
                this.xrLabelMaglumat
            });

            this.xrLabelHeader.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "ToString(Today(), 'dd.MM.yyyy') + ' sene bo\u00FDun\u00E7a ' + [Company_Name] + ' T\u00FCrk k\u00E4rhanasyny\u0148 T\u00FCrkmenistandaky \u015Faham\u00E7asy i\u015Fle\u00FD\u00E4n da\u015Fary \u00FDurt ra\u00FDatlaryny\u0148 sanawy barada'"));
            this.xrLabelHeader.Font          = new DXFont("Times New Roman", 10F);
            this.xrLabelHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 5F);
            this.xrLabelHeader.Name          = "xrLabelHeader";
            this.xrLabelHeader.SizeF         = new System.Drawing.SizeF(726.7717F, 45F);
            this.xrLabelHeader.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelHeader.CanGrow       = true;
            this.xrLabelHeader.Multiline     = true;
            this.xrLabelHeader.WordWrap      = true;
            this.xrLabelHeader.BackColor     = System.Drawing.Color.Transparent;

            this.xrLabelMaglumat.Text          = "MAGLUMAT";
            this.xrLabelMaglumat.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelMaglumat.LocationFloat = new DevExpress.Utils.PointFloat(0F, 52F);
            this.xrLabelMaglumat.Name          = "xrLabelMaglumat";
            this.xrLabelMaglumat.SizeF         = new System.Drawing.SizeF(726.7717F, 16F);
            this.xrLabelMaglumat.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabelMaglumat.BackColor     = System.Drawing.Color.Transparent;

            // ----------------------------------------------------------------
            // PageHeader — column header table (726.7717F wide, 55F tall)
            // Column weights: 20+78+62+70+52+108+135+72+72+57.7717 = 726.7717F
            // ----------------------------------------------------------------
            this.PageHeader.HeightF = 55F;
            this.PageHeader.Name    = "PageHeader";
            this.PageHeader.Controls.Add(this.xrTableHeader);

            this.xrHdrNo.Name      = "xrHdrNo";
            this.xrHdrNo.Text      = "\u2116";
            this.xrHdrNo.Weight    = 20;

            this.xrHdrFaa.Name     = "xrHdrFaa";
            this.xrHdrFaa.Text     = "F.A.A";
            this.xrHdrFaa.Weight   = 78;

            this.xrHdrDoglan.Name   = "xrHdrDoglan";
            this.xrHdrDoglan.Text   = "Doglan \u00FDyly we ra\u00FDatlygy";
            this.xrHdrDoglan.Weight = 62;

            this.xrHdrPasport.Name   = "xrHdrPasport";
            this.xrHdrPasport.Text   = "Pasport belgisi we m\u00F6hleti";
            this.xrHdrPasport.Weight = 70;

            this.xrHdrBilimi.Name   = "xrHdrBilimi";
            this.xrHdrBilimi.Text   = "Bilimi";
            this.xrHdrBilimi.Weight = 52;

            this.xrHdrWezipesi.Name   = "xrHdrWezipesi";
            this.xrHdrWezipesi.Text   = "Wezipesi";
            this.xrHdrWezipesi.Weight = 108;

            this.xrHdrSalgysy.Name   = "xrHdrSalgysy";
            this.xrHdrSalgysy.Text   = "T\u00FCrkmenistan-daky anyk \u00FDA\u015Fa\u00FDan salgysy";
            this.xrHdrSalgysy.Weight = 135;

            this.xrHdrWP.Name   = "xrHdrWP";
            this.xrHdrWP.Text   = "Rugsatnama belgisi we m\u00F6hleti";
            this.xrHdrWP.Weight = 72;

            this.xrHdrWiza.Name   = "xrHdrWiza";
            this.xrHdrWiza.Text   = "Wiza belgisi we m\u00F6hleti";
            this.xrHdrWiza.Weight = 72;

            this.xrHdrBellik.Name   = "xrHdrBellik";
            this.xrHdrBellik.Text   = "Bellik";
            this.xrHdrBellik.Weight = 57.7717;

            foreach (var hc in new XRTableCell[] {
                this.xrHdrNo, this.xrHdrFaa, this.xrHdrDoglan, this.xrHdrPasport, this.xrHdrBilimi,
                this.xrHdrWezipesi, this.xrHdrSalgysy, this.xrHdrWP, this.xrHdrWiza, this.xrHdrBellik })
            {
                hc.Font          = new DXFont("Times New Roman", 7F, DXFontStyle.Bold);
                hc.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
                hc.WordWrap      = true;
                hc.Multiline     = true;
                hc.Borders       = DevExpress.XtraPrinting.BorderSide.All;
                hc.BorderWidth   = 0.5F;
                hc.BorderColor   = System.Drawing.Color.Black;
                hc.BackColor     = System.Drawing.Color.Transparent;
                hc.StylePriority.UseBorders     = true;
                hc.StylePriority.UseBorderWidth = true;
                hc.StylePriority.UseBorderColor = true;
                hc.StylePriority.UseBackColor   = true;
            }

            this.xrRowHeader.Cells.AddRange(new XRTableCell[] {
                this.xrHdrNo, this.xrHdrFaa, this.xrHdrDoglan, this.xrHdrPasport, this.xrHdrBilimi,
                this.xrHdrWezipesi, this.xrHdrSalgysy, this.xrHdrWP, this.xrHdrWiza, this.xrHdrBellik
            });
            this.xrRowHeader.HeightF = 55F;
            this.xrRowHeader.Name    = "xrRowHeader";

            this.xrTableHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableHeader.Name          = "xrTableHeader";
            this.xrTableHeader.Rows.Add(this.xrRowHeader);
            this.xrTableHeader.SizeF         = new System.Drawing.SizeF(726.7717F, 55F);

            // ----------------------------------------------------------------
            // Detail — one data row per WorkPermitItem
            // ----------------------------------------------------------------
            this.Detail.HeightF = 80F;
            this.Detail.CanGrow = true;
            this.Detail.Name    = "Detail";
            this.Detail.Controls.Add(this.xrTableData);

            this.xrCellNo.Name    = "xrCellNo";
            this.xrCellNo.Weight  = 20;
            this.xrCellNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "sumRecordNumber()"));
            this.xrCellNo.Summary = new XRSummary { Running = SummaryRunning.Report };

            this.xrCellFaa.Name   = "xrCellFaa";
            this.xrCellFaa.Weight = 78;
            this.xrCellFaa.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_FullName]"));

            this.xrCellDoglan.Name     = "xrCellDoglan";
            this.xrCellDoglan.Weight   = 62;
            this.xrCellDoglan.Multiline = true;
            this.xrCellDoglan.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Person_DateOfBirthText] + Char(10) + [Person_NationalityCode]"));

            this.xrCellPasport.Name     = "xrCellPasport";
            this.xrCellPasport.Weight   = 70;
            this.xrCellPasport.Multiline = true;
            this.xrCellPasport.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Passport_Number] + Char(10) + [Passport_ExpirationDateText]"));

            this.xrCellBilimi.Name   = "xrCellBilimi";
            this.xrCellBilimi.Weight = 52;
            this.xrCellBilimi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Education_LevelTm]"));

            this.xrCellWezipesi.Name   = "xrCellWezipesi";
            this.xrCellWezipesi.Weight = 108;
            this.xrCellWezipesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Position_NameTm]"));

            this.xrCellSalgysy.Name   = "xrCellSalgysy";
            this.xrCellSalgysy.Weight = 135;
            this.xrCellSalgysy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Address_FullAddress]"));

            this.xrCellWP.Name     = "xrCellWP";
            this.xrCellWP.Weight   = 72;
            this.xrCellWP.Multiline = true;
            this.xrCellWP.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[WorkPermitNumber] + Char(10) + [WP_StartDateText] + Char(10) + [WP_ExpirationDateText]"));

            this.xrCellWiza.Name     = "xrCellWiza";
            this.xrCellWiza.Weight   = 72;
            this.xrCellWiza.Multiline = true;
            this.xrCellWiza.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Visa_Number] + Char(10) + [Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText]"));

            this.xrCellBellik.Name   = "xrCellBellik";
            this.xrCellBellik.Weight = 57.7717;

            foreach (var dc in new XRTableCell[] {
                this.xrCellNo, this.xrCellFaa, this.xrCellDoglan, this.xrCellPasport, this.xrCellBilimi,
                this.xrCellWezipesi, this.xrCellSalgysy, this.xrCellWP, this.xrCellWiza, this.xrCellBellik })
            {
                dc.Font          = new DXFont("Times New Roman", 7F);
                dc.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
                dc.WordWrap      = true;
                dc.CanGrow       = true;
                dc.Borders       = DevExpress.XtraPrinting.BorderSide.Left
                                 | DevExpress.XtraPrinting.BorderSide.Right
                                 | DevExpress.XtraPrinting.BorderSide.Bottom;
                dc.BorderWidth   = 0.5F;
                dc.BorderColor   = System.Drawing.Color.Black;
                dc.BackColor     = System.Drawing.Color.Transparent;
                dc.StylePriority.UseBorders     = true;
                dc.StylePriority.UseBorderWidth = true;
                dc.StylePriority.UseBorderColor = true;
                dc.StylePriority.UseBackColor   = true;
            }

            this.xrRowData.Cells.AddRange(new XRTableCell[] {
                this.xrCellNo, this.xrCellFaa, this.xrCellDoglan, this.xrCellPasport, this.xrCellBilimi,
                this.xrCellWezipesi, this.xrCellSalgysy, this.xrCellWP, this.xrCellWiza, this.xrCellBellik
            });
            this.xrRowData.HeightF = 80F;
            this.xrRowData.CanGrow = true;
            this.xrRowData.Name    = "xrRowData";

            this.xrTableData.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableData.Name          = "xrTableData";
            this.xrTableData.Rows.Add(this.xrRowData);
            this.xrTableData.SizeF         = new System.Drawing.SizeF(726.7717F, 80F);
            this.xrTableData.CanGrow       = true;

            // ----------------------------------------------------------------
            // ReportFooter — disclaimer note + signatory block
            // ----------------------------------------------------------------
            this.ReportFooter.HeightF       = 130F;
            this.ReportFooter.Name          = "ReportFooter";
            this.ReportFooter.PrintAtBottom = false;
            this.ReportFooter.Controls.AddRange(new XRControl[] {
                this.xrLabelFooterNote,
                this.xrLabelSignatoryPosition,
                this.xrLabelSignatoryFullName
            });

            this.xrLabelFooterNote.Text          = "* K\u00E4rhanamyzy\u0148 hasabynda z\u00E4hmet \u00E7ek\u00FD\u00E4n T\u00FCrkmenistany\u0148 ra\u00FDatlary baradaky maglumatlaryn\u0148 doly we dogry g\u00F6rkezilmegine jogapk\u00E4r\u00E7iligi k\u00E4rhanamyz \u00F6z \u00FCst\u00FCne al\u00FDar.";
            this.xrLabelFooterNote.Font          = new DXFont("Times New Roman", 9F);
            this.xrLabelFooterNote.LocationFloat = new DevExpress.Utils.PointFloat(0F, 5F);
            this.xrLabelFooterNote.Name          = "xrLabelFooterNote";
            this.xrLabelFooterNote.SizeF         = new System.Drawing.SizeF(726.7717F, 40F);
            this.xrLabelFooterNote.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelFooterNote.CanGrow       = true;
            this.xrLabelFooterNote.Multiline     = true;
            this.xrLabelFooterNote.WordWrap      = true;
            this.xrLabelFooterNote.BackColor     = System.Drawing.Color.Transparent;

            this.xrLabelSignatoryPosition.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[CompanyHead_PositionTm]"));
            this.xrLabelSignatoryPosition.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelSignatoryPosition.LocationFloat = new DevExpress.Utils.PointFloat(0F, 65F);
            this.xrLabelSignatoryPosition.Name          = "xrLabelSignatoryPosition";
            this.xrLabelSignatoryPosition.SizeF         = new System.Drawing.SizeF(363F, 40F);
            this.xrLabelSignatoryPosition.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabelSignatoryPosition.CanGrow       = true;
            this.xrLabelSignatoryPosition.WordWrap      = true;
            this.xrLabelSignatoryPosition.BackColor     = System.Drawing.Color.Transparent;

            this.xrLabelSignatoryFullName.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[CompanyHead_FullName]"));
            this.xrLabelSignatoryFullName.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelSignatoryFullName.LocationFloat = new DevExpress.Utils.PointFloat(363F, 75F);
            this.xrLabelSignatoryFullName.Name          = "xrLabelSignatoryFullName";
            this.xrLabelSignatoryFullName.SizeF         = new System.Drawing.SizeF(363.7717F, 20F);
            this.xrLabelSignatoryFullName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabelSignatoryFullName.BackColor     = System.Drawing.Color.Transparent;

            // ----------------------------------------------------------------
            // BottomMargin
            // ----------------------------------------------------------------
            this.BottomMargin.HeightF = 60F;
            this.BottomMargin.Name    = "BottomMargin";

            // ----------------------------------------------------------------
            // Assemble bands and component storage
            // ----------------------------------------------------------------
            this.Bands.AddRange(new Band[] {
                this.TopMargin,
                this.ReportHeader,
                this.PageHeader,
                this.Detail,
                this.ReportFooter,
                this.BottomMargin
            });
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
                this.WPDataSource
            });

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.WPDataSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        #endregion

        private TopMarginBand    TopMargin;
        private ReportHeaderBand ReportHeader;
        private PageHeaderBand   PageHeader;
        private DetailBand       Detail;
        private ReportFooterBand ReportFooter;
        private BottomMarginBand BottomMargin;
        private XRLabel          xrLabelHeader;
        private XRLabel          xrLabelMaglumat;
        private XRTable          xrTableHeader;
        private XRTableRow       xrRowHeader;
        private XRTableCell      xrHdrNo;
        private XRTableCell      xrHdrFaa;
        private XRTableCell      xrHdrDoglan;
        private XRTableCell      xrHdrPasport;
        private XRTableCell      xrHdrBilimi;
        private XRTableCell      xrHdrWezipesi;
        private XRTableCell      xrHdrSalgysy;
        private XRTableCell      xrHdrWP;
        private XRTableCell      xrHdrWiza;
        private XRTableCell      xrHdrBellik;
        private XRTable          xrTableData;
        private XRTableRow       xrRowData;
        private XRTableCell      xrCellNo;
        private XRTableCell      xrCellFaa;
        private XRTableCell      xrCellDoglan;
        private XRTableCell      xrCellPasport;
        private XRTableCell      xrCellBilimi;
        private XRTableCell      xrCellWezipesi;
        private XRTableCell      xrCellSalgysy;
        private XRTableCell      xrCellWP;
        private XRTableCell      xrCellWiza;
        private XRTableCell      xrCellBellik;
        private XRLabel          xrLabelFooterNote;
        private XRLabel          xrLabelSignatoryPosition;
        private XRLabel          xrLabelSignatoryFullName;
        private DevExpress.Persistent.Base.ReportsV2.CollectionDataSource WPDataSource;
    }
}
