using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppBusinessTripSanawReport
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
            this.TopMargin              = new TopMarginBand();
            this.ReportHeader           = new ReportHeaderBand();
            this.xrLabelTitle           = new XRLabel();
            this.PageHeader             = new PageHeaderBand();
            this.xrTableHeader          = new XRTable();
            this.xrRowHeader            = new XRTableRow();
            this.xrHdrNo                = new XRTableCell();
            this.xrHdrFamiliyasy        = new XRTableCell();
            this.xrHdrAdy               = new XRTableCell();
            this.xrHdrDoglanSenesi      = new XRTableCell();
            this.xrHdrJynsy             = new XRTableCell();
            this.xrHdrRayatlygy         = new XRTableCell();
            this.xrHdrPasport           = new XRTableCell();
            this.xrHdrWezipesi          = new XRTableCell();
            this.xrHdrMohleti           = new XRTableCell();
            this.xrHdrTmSalgysy         = new XRTableCell();
            this.xrHdrIsapSalgysy       = new XRTableCell();
            this.Detail                 = new DetailBand();
            this.xrTableData            = new XRTable();
            this.xrRowData              = new XRTableRow();
            this.xrCellNo               = new XRTableCell();
            this.xrCellFamiliyasy       = new XRTableCell();
            this.xrCellAdy              = new XRTableCell();
            this.xrCellDoglanSenesi     = new XRTableCell();
            this.xrCellJynsy            = new XRTableCell();
            this.xrCellRayatlygy        = new XRTableCell();
            this.xrCellPasport          = new XRTableCell();
            this.xrCellWezipesi         = new XRTableCell();
            this.xrCellMohleti          = new XRTableCell();
            this.xrCellTmSalgysy        = new XRTableCell();
            this.xrCellIsapSalgysy      = new XRTableCell();
            this.ReportFooter           = new ReportFooterBand();
            this.xrLabelSignatoryPosition = new XRLabel();
            this.xrLabelSignatoryFullName = new XRLabel();
            this.BottomMargin           = new BottomMarginBand();
            this.AppDataSource          = new DevExpress.Persistent.Base.ReportsV2.CollectionDataSource();

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.AppDataSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();

            // ----------------------------------------------------------------
            // Page — A4 Landscape, 100F margins, printable width = 969.291F
            // ----------------------------------------------------------------
            this.Landscape     = true;
            this.PageWidthF    = 1169.291F;
            this.PageHeightF   = 826.7717F;
            this.Margins       = new DXMargins(100F, 100F, 50F, 100F);
            this.PaperKind     = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.BackColor     = System.Drawing.Color.White;
            this.Version       = "25.2";

            // ----------------------------------------------------------------
            // Data source — BusinessTrip
            // ----------------------------------------------------------------
            this.AppDataSource.Name            = "AppDataSource";
            this.AppDataSource.ObjectTypeName  = "Visa2026.Module.BusinessObjects.BusinessTrip";
            this.AppDataSource.TopReturnedRecords = 0;
            this.DataSource = this.AppDataSource;

            // ----------------------------------------------------------------
            // TopMargin
            // ----------------------------------------------------------------
            this.TopMargin.HeightF = 50F;
            this.TopMargin.Name    = "TopMargin";

            // ----------------------------------------------------------------
            // ReportHeader — centered title
            // ----------------------------------------------------------------
            this.ReportHeader.HeightF = 55F;
            this.ReportHeader.Name    = "ReportHeader";
            this.ReportHeader.Controls.Add(this.xrLabelTitle);

            this.xrLabelTitle.Font          = new DXFont("Times New Roman", 15F, DXFontStyle.Bold);
            this.xrLabelTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 10F);
            this.xrLabelTitle.Name          = "xrLabelTitle";
            this.xrLabelTitle.SizeF         = new System.Drawing.SizeF(969.291F, 35F);
            this.xrLabelTitle.Text          = "Da\u015Fary \u00FDurt ra\u00FDatlary\u0148y\u0148 sanawy";
            this.xrLabelTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelTitle.BackColor     = System.Drawing.Color.Transparent;

            // ----------------------------------------------------------------
            // PageHeader — column header table (969.291F wide, 80F tall)
            // ----------------------------------------------------------------
            this.PageHeader.HeightF = 80F;
            this.PageHeader.Name    = "PageHeader";
            this.PageHeader.Controls.Add(this.xrTableHeader);

            // Column weights: 28+72+60+80+40+50+80+145+85+165+164.291 = 969.291
            this.xrHdrNo.Name           = "xrHdrNo";
            this.xrHdrNo.Text           = "\u2116";
            this.xrHdrNo.Weight         = 28;

            this.xrHdrFamiliyasy.Name   = "xrHdrFamiliyasy";
            this.xrHdrFamiliyasy.Text   = "Famili\u00FDasy";
            this.xrHdrFamiliyasy.Weight = 72;

            this.xrHdrAdy.Name          = "xrHdrAdy";
            this.xrHdrAdy.Text          = "Ady";
            this.xrHdrAdy.Weight        = 60;

            this.xrHdrDoglanSenesi.Name   = "xrHdrDoglanSenesi";
            this.xrHdrDoglanSenesi.Text   = "Doglan senesi we \u00FDeri";
            this.xrHdrDoglanSenesi.Weight = 80;

            this.xrHdrJynsy.Name        = "xrHdrJynsy";
            this.xrHdrJynsy.Text        = "Jynsy";
            this.xrHdrJynsy.Weight      = 40;

            this.xrHdrRayatlygy.Name    = "xrHdrRayatlygy";
            this.xrHdrRayatlygy.Text    = "Ra\u00FDatlygy";
            this.xrHdrRayatlygy.Weight  = 50;

            this.xrHdrPasport.Name      = "xrHdrPasport";
            this.xrHdrPasport.Text      = "Pasport belgisi we m\u00F6hleti";
            this.xrHdrPasport.Weight    = 80;

            this.xrHdrWezipesi.Name     = "xrHdrWezipesi";
            this.xrHdrWezipesi.Text     = "Wezipesi";
            this.xrHdrWezipesi.Weight   = 145;

            this.xrHdrMohleti.Name      = "xrHdrMohleti";
            this.xrHdrMohleti.Text      = "M\u00F6hleti we gezekligi";
            this.xrHdrMohleti.Weight    = 85;

            this.xrHdrTmSalgysy.Name    = "xrHdrTmSalgysy";
            this.xrHdrTmSalgysy.Text    = "T\u00FCrkmenistandaky salgysy";
            this.xrHdrTmSalgysy.Weight  = 165;

            this.xrHdrIsapSalgysy.Name   = "xrHdrIsapSalgysy";
            this.xrHdrIsapSalgysy.Text   = "I\u015F saparynda boljak salgysy";
            this.xrHdrIsapSalgysy.Weight = 164.291;

            foreach (var hc in new XRTableCell[] {
                this.xrHdrNo, this.xrHdrFamiliyasy, this.xrHdrAdy, this.xrHdrDoglanSenesi,
                this.xrHdrJynsy, this.xrHdrRayatlygy, this.xrHdrPasport,
                this.xrHdrWezipesi, this.xrHdrMohleti, this.xrHdrTmSalgysy, this.xrHdrIsapSalgysy })
            {
                hc.Font          = new DXFont("Times New Roman", 9F, DXFontStyle.Bold);
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
                this.xrHdrNo, this.xrHdrFamiliyasy, this.xrHdrAdy, this.xrHdrDoglanSenesi,
                this.xrHdrJynsy, this.xrHdrRayatlygy, this.xrHdrPasport,
                this.xrHdrWezipesi, this.xrHdrMohleti, this.xrHdrTmSalgysy, this.xrHdrIsapSalgysy
            });
            this.xrRowHeader.HeightF = 80F;
            this.xrRowHeader.Name    = "xrRowHeader";

            this.xrTableHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableHeader.Name          = "xrTableHeader";
            this.xrTableHeader.Rows.Add(this.xrRowHeader);
            this.xrTableHeader.SizeF         = new System.Drawing.SizeF(969.291F, 80F);

            // ----------------------------------------------------------------
            // Detail — one data row per BusinessTrip
            // ----------------------------------------------------------------
            this.Detail.HeightF = 70F;
            this.Detail.CanGrow = true;
            this.Detail.Name    = "Detail";
            this.Detail.Controls.Add(this.xrTableData);

            this.xrCellNo.Name     = "xrCellNo";
            this.xrCellNo.Weight   = 28;
            this.xrCellNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "sumRecordNumber()"));
            this.xrCellNo.Summary  = new XRSummary { Running = SummaryRunning.Report };

            this.xrCellFamiliyasy.Name   = "xrCellFamiliyasy";
            this.xrCellFamiliyasy.Weight = 72;
            this.xrCellFamiliyasy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_LastName]"));

            this.xrCellAdy.Name   = "xrCellAdy";
            this.xrCellAdy.Weight = 60;
            this.xrCellAdy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_FirstName]"));

            this.xrCellDoglanSenesi.Name     = "xrCellDoglanSenesi";
            this.xrCellDoglanSenesi.Weight   = 80;
            this.xrCellDoglanSenesi.Multiline = true;
            this.xrCellDoglanSenesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Person_DateOfBirthText] + Char(10) + [Person_BirthPlace]"));

            this.xrCellJynsy.Name   = "xrCellJynsy";
            this.xrCellJynsy.Weight = 40;
            this.xrCellJynsy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_GenderTm]"));

            this.xrCellRayatlygy.Name   = "xrCellRayatlygy";
            this.xrCellRayatlygy.Weight = 50;
            this.xrCellRayatlygy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_NationalityCode]"));

            this.xrCellPasport.Name     = "xrCellPasport";
            this.xrCellPasport.Weight   = 80;
            this.xrCellPasport.Multiline = true;
            this.xrCellPasport.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Passport_Number] + Char(10) + [Passport_ExpirationDateText]"));

            this.xrCellWezipesi.Name   = "xrCellWezipesi";
            this.xrCellWezipesi.Weight = 145;
            this.xrCellWezipesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Position_NameTm]"));

            this.xrCellMohleti.Name     = "xrCellMohleti";
            this.xrCellMohleti.Weight   = 85;
            this.xrCellMohleti.Multiline = true;
            this.xrCellMohleti.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Visa_NumberAndType] + Char(10) + [Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText]"));

            this.xrCellTmSalgysy.Name   = "xrCellTmSalgysy";
            this.xrCellTmSalgysy.Weight = 165;
            this.xrCellTmSalgysy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Address_FullAddress]"));

            this.xrCellIsapSalgysy.Name   = "xrCellIsapSalgysy";
            this.xrCellIsapSalgysy.Weight = 164.291;
            this.xrCellIsapSalgysy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[BusinessTripAddress_FullAddress]"));

            foreach (var dc in new XRTableCell[] {
                this.xrCellNo, this.xrCellFamiliyasy, this.xrCellAdy, this.xrCellDoglanSenesi,
                this.xrCellJynsy, this.xrCellRayatlygy, this.xrCellPasport,
                this.xrCellWezipesi, this.xrCellMohleti, this.xrCellTmSalgysy, this.xrCellIsapSalgysy })
            {
                dc.Font          = new DXFont("Times New Roman", 9F);
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
                this.xrCellNo, this.xrCellFamiliyasy, this.xrCellAdy, this.xrCellDoglanSenesi,
                this.xrCellJynsy, this.xrCellRayatlygy, this.xrCellPasport,
                this.xrCellWezipesi, this.xrCellMohleti, this.xrCellTmSalgysy, this.xrCellIsapSalgysy
            });
            this.xrRowData.HeightF = 70F;
            this.xrRowData.CanGrow = true;
            this.xrRowData.Name    = "xrRowData";

            this.xrTableData.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableData.Name          = "xrTableData";
            this.xrTableData.Rows.Add(this.xrRowData);
            this.xrTableData.SizeF         = new System.Drawing.SizeF(969.291F, 70F);
            this.xrTableData.CanGrow       = true;

            // ----------------------------------------------------------------
            // ReportFooter — signatory block
            // ----------------------------------------------------------------
            this.ReportFooter.HeightF       = 80F;
            this.ReportFooter.Name          = "ReportFooter";
            this.ReportFooter.PrintAtBottom = false;
            this.ReportFooter.Controls.AddRange(new XRControl[] {
                this.xrLabelSignatoryPosition,
                this.xrLabelSignatoryFullName
            });

            this.xrLabelSignatoryPosition.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Application_CompanyHead_PositionTm]"));
            this.xrLabelSignatoryPosition.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelSignatoryPosition.LocationFloat = new DevExpress.Utils.PointFloat(0F, 40F);
            this.xrLabelSignatoryPosition.Name          = "xrLabelSignatoryPosition";
            this.xrLabelSignatoryPosition.SizeF         = new System.Drawing.SizeF(484F, 20F);
            this.xrLabelSignatoryPosition.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelSignatoryPosition.BackColor     = System.Drawing.Color.Transparent;

            this.xrLabelSignatoryFullName.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Application_CompanyHead_FullName]"));
            this.xrLabelSignatoryFullName.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelSignatoryFullName.LocationFloat = new DevExpress.Utils.PointFloat(485F, 40F);
            this.xrLabelSignatoryFullName.Name          = "xrLabelSignatoryFullName";
            this.xrLabelSignatoryFullName.SizeF         = new System.Drawing.SizeF(484.291F, 20F);
            this.xrLabelSignatoryFullName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelSignatoryFullName.BackColor     = System.Drawing.Color.Transparent;

            // ----------------------------------------------------------------
            // BottomMargin
            // ----------------------------------------------------------------
            this.BottomMargin.HeightF = 100F;
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
                this.AppDataSource
            });

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.AppDataSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
        }

        #endregion

        private TopMarginBand     TopMargin;
        private ReportHeaderBand  ReportHeader;
        private PageHeaderBand    PageHeader;
        private DetailBand        Detail;
        private ReportFooterBand  ReportFooter;
        private BottomMarginBand  BottomMargin;
        private XRLabel           xrLabelTitle;
        private XRTable           xrTableHeader;
        private XRTableRow        xrRowHeader;
        private XRTableCell       xrHdrNo;
        private XRTableCell       xrHdrFamiliyasy;
        private XRTableCell       xrHdrAdy;
        private XRTableCell       xrHdrDoglanSenesi;
        private XRTableCell       xrHdrJynsy;
        private XRTableCell       xrHdrRayatlygy;
        private XRTableCell       xrHdrPasport;
        private XRTableCell       xrHdrWezipesi;
        private XRTableCell       xrHdrMohleti;
        private XRTableCell       xrHdrTmSalgysy;
        private XRTableCell       xrHdrIsapSalgysy;
        private XRTable           xrTableData;
        private XRTableRow        xrRowData;
        private XRTableCell       xrCellNo;
        private XRTableCell       xrCellFamiliyasy;
        private XRTableCell       xrCellAdy;
        private XRTableCell       xrCellDoglanSenesi;
        private XRTableCell       xrCellJynsy;
        private XRTableCell       xrCellRayatlygy;
        private XRTableCell       xrCellPasport;
        private XRTableCell       xrCellWezipesi;
        private XRTableCell       xrCellMohleti;
        private XRTableCell       xrCellTmSalgysy;
        private XRTableCell       xrCellIsapSalgysy;
        private XRLabel           xrLabelSignatoryPosition;
        private XRLabel           xrLabelSignatoryFullName;
        private DevExpress.Persistent.Base.ReportsV2.CollectionDataSource AppDataSource;
    }
}
