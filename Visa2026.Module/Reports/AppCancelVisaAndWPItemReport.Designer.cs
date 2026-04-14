using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppCancelVisaAndWPItemReport
    {
        private void InitializeComponent()
        {
            this.xrLabelTitle              = new XRLabel();
            this.xrTableHeader             = new XRTable();
            this.xrRowHeader               = new XRTableRow();
            this.xrHdrNo                   = new XRTableCell();
            this.xrHdrASNo                 = new XRTableCell();
            this.xrHdrTassykNama           = new XRTableCell();
            this.xrHdrFamiliyasy           = new XRTableCell();
            this.xrHdrAdy                  = new XRTableCell();
            this.xrHdrDoglanSenesi         = new XRTableCell();
            this.xrHdrPasport              = new XRTableCell();
            this.xrHdrHunari               = new XRTableCell();
            this.xrHdrHereket              = new XRTableCell();
            this.xrHdrRugsat               = new XRTableCell();
            this.xrHdrWizaBelgisi          = new XRTableCell();
            this.xrHdrWizaMohleti          = new XRTableCell();
            this.xrTableData               = new XRTable();
            this.xrRowData                 = new XRTableRow();
            this.xrCellNo                  = new XRTableCell();
            this.xrCellASNo                = new XRTableCell();
            this.xrCellTassykNama          = new XRTableCell();
            this.xrCellFamiliyasy          = new XRTableCell();
            this.xrCellAdy                 = new XRTableCell();
            this.xrCellDoglanSenesi        = new XRTableCell();
            this.xrCellPasport             = new XRTableCell();
            this.xrCellHunari              = new XRTableCell();
            this.xrCellHereket             = new XRTableCell();
            this.xrCellRugsat              = new XRTableCell();
            this.xrCellWizaBelgisi         = new XRTableCell();
            this.xrCellWizaMohleti         = new XRTableCell();

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).BeginInit();

            // ----------------------------------------------------------------
            // Page — A4 Landscape
            // ----------------------------------------------------------------
            this.Landscape     = true;
            this.PageWidthF    = 1169.291F;
            this.PageHeightF   = 826.7717F;
            this.Margins       = new DXMargins(20F, 20F, 50F, 50F);

            // Hide app number/date labels inherited from AppItemBaseReport
            this.xrLabelAppNumber.Visible = false;
            this.xrLabelAppDate.Visible   = false;

            // ----------------------------------------------------------------
            // Signatory — reposition for A4 Landscape printable width (1129.291F)
            // ----------------------------------------------------------------
            this.ReportFooter.HeightF = 80F;
            this.xrLabelSignatoryPosition.LocationFloat = new DevExpress.Utils.PointFloat(0F, 40F);
            this.xrLabelSignatoryPosition.SizeF         = new System.Drawing.SizeF(564F, 20F);
            this.xrLabelSignatoryPosition.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelSignatoryFullName.LocationFloat = new DevExpress.Utils.PointFloat(565F, 40F);
            this.xrLabelSignatoryFullName.SizeF         = new System.Drawing.SizeF(564.291F, 20F);
            this.xrLabelSignatoryFullName.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;

            // ----------------------------------------------------------------
            // PageHeader — title (30F) + column header table (50F) = 80F
            // ----------------------------------------------------------------
            this.PageHeader.HeightF = 80F;

            //
            // xrLabelTitle
            //
            this.xrLabelTitle.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 5F);
            this.xrLabelTitle.Name          = "xrLabelTitle";
            this.xrLabelTitle.SizeF         = new System.Drawing.SizeF(1129.291F, 22F);
            this.xrLabelTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelTitle.Text          = "Da\u015Fary \u00FDurt ra\u00FDatlary\u0148y\u0148 sanawy";
            this.xrLabelTitle.BackColor     = System.Drawing.Color.Transparent;
            this.PageHeader.Controls.Add(this.xrLabelTitle);

            // ----------------------------------------------------------------
            // Column header row (weights sum = 1129.291F)
            // №=22, AS-№=85, Tassyk=68, Fam=75, Ady=62, Doglan=95,
            // Pasport=85, Hünäri=180, Hereket=100, Rugsat=75,
            // Wiza belgisi=75, Wiza möhleti=207.291
            // ----------------------------------------------------------------
            this.xrHdrNo.Name          = "xrHdrNo";
            this.xrHdrNo.Text          = "\u2116";
            this.xrHdrNo.Weight        = 22;

            this.xrHdrASNo.Name        = "xrHdrASNo";
            this.xrHdrASNo.Text        = "AS-\u2116";
            this.xrHdrASNo.Weight      = 85;

            this.xrHdrTassykNama.Name   = "xrHdrTassykNama";
            this.xrHdrTassykNama.Text   = "Tassyk-nama belgisi";
            this.xrHdrTassykNama.Weight = 68;

            this.xrHdrFamiliyasy.Name   = "xrHdrFamiliyasy";
            this.xrHdrFamiliyasy.Text   = "Famil\u00FDasy";
            this.xrHdrFamiliyasy.Weight = 75;

            this.xrHdrAdy.Name          = "xrHdrAdy";
            this.xrHdrAdy.Text          = "Ady";
            this.xrHdrAdy.Weight        = 62;

            this.xrHdrDoglanSenesi.Name   = "xrHdrDoglanSenesi";
            this.xrHdrDoglanSenesi.Text   = "Doglan senesi we \u015Furdy";
            this.xrHdrDoglanSenesi.Weight = 95;

            this.xrHdrPasport.Name      = "xrHdrPasport";
            this.xrHdrPasport.Text      = "Pasport belgisi";
            this.xrHdrPasport.Weight    = 85;

            this.xrHdrHunari.Name       = "xrHdrHunari";
            this.xrHdrHunari.Text       = "H\u00FCn\u00E4ri we bilimi";
            this.xrHdrHunari.Weight     = 180;

            this.xrHdrHereket.Name      = "xrHdrHereket";
            this.xrHdrHereket.Text      = "Hereket ed\u00FD\u00E4n \u00E7\u00E4gi";
            this.xrHdrHereket.Weight    = 100;

            this.xrHdrRugsat.Name       = "xrHdrRugsat";
            this.xrHdrRugsat.Text       = "Rugsat edililen m\u00F6hleti";
            this.xrHdrRugsat.Weight     = 75;

            this.xrHdrWizaBelgisi.Name  = "xrHdrWizaBelgisi";
            this.xrHdrWizaBelgisi.Text  = "Wiza belgisi";
            this.xrHdrWizaBelgisi.Weight = 75;

            this.xrHdrWizaMohleti.Name  = "xrHdrWizaMohleti";
            this.xrHdrWizaMohleti.Text  = "Wiza m\u00F6hleti ba\u015Flan\u00FDan we tamamlan\u00FDan senesi";
            this.xrHdrWizaMohleti.Weight = 207.291;

            foreach (var hc in new XRTableCell[] {
                this.xrHdrNo, this.xrHdrASNo, this.xrHdrTassykNama, this.xrHdrFamiliyasy,
                this.xrHdrAdy, this.xrHdrDoglanSenesi, this.xrHdrPasport, this.xrHdrHunari,
                this.xrHdrHereket, this.xrHdrRugsat, this.xrHdrWizaBelgisi, this.xrHdrWizaMohleti })
            {
                hc.Font          = new DXFont("Times New Roman", 7F, DXFontStyle.Bold);
                hc.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
                hc.WordWrap      = true;
                hc.Borders       = DevExpress.XtraPrinting.BorderSide.All;
                hc.BorderWidth   = 0.5F;
                hc.BorderColor   = System.Drawing.Color.Black;
                hc.BackColor     = System.Drawing.Color.Transparent;
                hc.StylePriority.UseBorders      = true;
                hc.StylePriority.UseBorderWidth  = true;
                hc.StylePriority.UseBorderColor  = true;
                hc.StylePriority.UseBackColor    = true;
            }

            this.xrRowHeader.Cells.AddRange(new XRTableCell[] {
                this.xrHdrNo, this.xrHdrASNo, this.xrHdrTassykNama, this.xrHdrFamiliyasy,
                this.xrHdrAdy, this.xrHdrDoglanSenesi, this.xrHdrPasport, this.xrHdrHunari,
                this.xrHdrHereket, this.xrHdrRugsat, this.xrHdrWizaBelgisi, this.xrHdrWizaMohleti
            });
            this.xrRowHeader.HeightF = 50F;
            this.xrRowHeader.Name    = "xrRowHeader";

            this.xrTableHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 30F);
            this.xrTableHeader.Name          = "xrTableHeader";
            this.xrTableHeader.Rows.Add(this.xrRowHeader);
            this.xrTableHeader.SizeF         = new System.Drawing.SizeF(1129.291F, 50F);
            this.PageHeader.Controls.Add(this.xrTableHeader);

            // ----------------------------------------------------------------
            // Data row — Detail band, one row per ApplicationItem
            // ----------------------------------------------------------------
            this.xrCellNo.Name   = "xrCellNo";
            this.xrCellNo.Weight = 22;
            this.xrCellNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "sumRecordNumber()"));
            XRSummary xrSummaryNo = new XRSummary();
            xrSummaryNo.Running = SummaryRunning.Report;
            this.xrCellNo.Summary = xrSummaryNo;

            this.xrCellASNo.Name   = "xrCellASNo";
            this.xrCellASNo.Weight = 85;
            this.xrCellASNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[WorkPermit_Number]"));

            this.xrCellTassykNama.Name   = "xrCellTassykNama";
            this.xrCellTassykNama.Weight = 68;
            this.xrCellTassykNama.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[WorkPermit_ASNumber]"));

            this.xrCellFamiliyasy.Name   = "xrCellFamiliyasy";
            this.xrCellFamiliyasy.Weight = 75;
            this.xrCellFamiliyasy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_LastName]"));

            this.xrCellAdy.Name   = "xrCellAdy";
            this.xrCellAdy.Weight = 62;
            this.xrCellAdy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_FirstName]"));

            this.xrCellDoglanSenesi.Name   = "xrCellDoglanSenesi";
            this.xrCellDoglanSenesi.Weight = 95;
            this.xrCellDoglanSenesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Person_DateOfBirthText] + Char(10) + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]"));

            this.xrCellPasport.Name   = "xrCellPasport";
            this.xrCellPasport.Weight = 85;
            this.xrCellPasport.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Passport_Number] + Char(10) + [Passport_ExpirationDateText]"));

            this.xrCellHunari.Name   = "xrCellHunari";
            this.xrCellHunari.Weight = 180;
            this.xrCellHunari.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Education_LevelTm] + ', ' + [Position_PositionTm]"));

            this.xrCellHereket.Name   = "xrCellHereket";
            this.xrCellHereket.Weight = 100;
            this.xrCellHereket.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[WorkPermit_WorkPermittedLocations]"));

            this.xrCellRugsat.Name   = "xrCellRugsat";
            this.xrCellRugsat.Weight = 75;
            this.xrCellRugsat.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[WorkPermit_StartDateText] + Char(10) + [WorkPermit_ExpirationDateText]"));

            this.xrCellWizaBelgisi.Name   = "xrCellWizaBelgisi";
            this.xrCellWizaBelgisi.Weight = 75;
            this.xrCellWizaBelgisi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Visa_Number]"));

            this.xrCellWizaMohleti.Name   = "xrCellWizaMohleti";
            this.xrCellWizaMohleti.Weight = 207.291;
            this.xrCellWizaMohleti.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText]"));

            foreach (var dc in new XRTableCell[] {
                this.xrCellNo, this.xrCellASNo, this.xrCellTassykNama, this.xrCellFamiliyasy,
                this.xrCellAdy, this.xrCellDoglanSenesi, this.xrCellPasport, this.xrCellHunari,
                this.xrCellHereket, this.xrCellRugsat, this.xrCellWizaBelgisi, this.xrCellWizaMohleti })
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
                dc.Padding       = new DevExpress.XtraPrinting.PaddingInfo(3, 3, 2, 2);
                dc.StylePriority.UseBorders      = true;
                dc.StylePriority.UseBorderWidth  = true;
                dc.StylePriority.UseBorderColor  = true;
                dc.StylePriority.UseBackColor    = true;
            }

            // Multiline required for Char(10) line breaks and long text wrapping
            this.xrCellDoglanSenesi.Multiline   = true;
            this.xrCellPasport.Multiline         = true;
            this.xrCellHunari.Multiline          = true;
            this.xrCellHereket.Multiline         = true;
            this.xrCellRugsat.Multiline          = true;
            this.xrCellWizaMohleti.Multiline     = true;

            this.xrRowData.Cells.AddRange(new XRTableCell[] {
                this.xrCellNo, this.xrCellASNo, this.xrCellTassykNama, this.xrCellFamiliyasy,
                this.xrCellAdy, this.xrCellDoglanSenesi, this.xrCellPasport, this.xrCellHunari,
                this.xrCellHereket, this.xrCellRugsat, this.xrCellWizaBelgisi, this.xrCellWizaMohleti
            });
            this.xrRowData.HeightF = 80F;
            this.xrRowData.CanGrow = true;
            this.xrRowData.Name    = "xrRowData";

            this.xrTableData.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableData.Name          = "xrTableData";
            this.xrTableData.Rows.Add(this.xrRowData);
            this.xrTableData.SizeF         = new System.Drawing.SizeF(1129.291F, 80F);

            this.Detail.HeightF  = 80F;
            this.Detail.CanGrow  = true;
            this.Detail.Controls.Add(this.xrTableData);

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).EndInit();
        }

        private XRLabel     xrLabelTitle;
        private XRTable     xrTableHeader;
        private XRTableRow  xrRowHeader;
        private XRTableCell xrHdrNo;
        private XRTableCell xrHdrASNo;
        private XRTableCell xrHdrTassykNama;
        private XRTableCell xrHdrFamiliyasy;
        private XRTableCell xrHdrAdy;
        private XRTableCell xrHdrDoglanSenesi;
        private XRTableCell xrHdrPasport;
        private XRTableCell xrHdrHunari;
        private XRTableCell xrHdrHereket;
        private XRTableCell xrHdrRugsat;
        private XRTableCell xrHdrWizaBelgisi;
        private XRTableCell xrHdrWizaMohleti;
        private XRTable     xrTableData;
        private XRTableRow  xrRowData;
        private XRTableCell xrCellNo;
        private XRTableCell xrCellASNo;
        private XRTableCell xrCellTassykNama;
        private XRTableCell xrCellFamiliyasy;
        private XRTableCell xrCellAdy;
        private XRTableCell xrCellDoglanSenesi;
        private XRTableCell xrCellPasport;
        private XRTableCell xrCellHunari;
        private XRTableCell xrCellHereket;
        private XRTableCell xrCellRugsat;
        private XRTableCell xrCellWizaBelgisi;
        private XRTableCell xrCellWizaMohleti;
    }
}
