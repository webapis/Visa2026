using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppBorderZonePermissionItemReport
    {
        private void InitializeComponent()
        {
            this.xrLabelTitle          = new XRLabel();
            this.xrTableHeader         = new XRTable();
            this.xrRowHeader           = new XRTableRow();
            this.xrHdrNo               = new XRTableCell();
            this.xrHdrFamiliyasy       = new XRTableCell();
            this.xrHdrAdy              = new XRTableCell();
            this.xrHdrDoglanSenesi     = new XRTableCell();
            this.xrHdrJynsy            = new XRTableCell();
            this.xrHdrRayatlygy        = new XRTableCell();
            this.xrHdrPasport          = new XRTableCell();
            this.xrHdrWezipesi         = new XRTableCell();
            this.xrHdrMohleti          = new XRTableCell();
            this.xrHdrTmSalgysy        = new XRTableCell();
            this.xrHdrSerhet           = new XRTableCell();
            this.xrTableData           = new XRTable();
            this.xrRowData             = new XRTableRow();
            this.xrCellNo              = new XRTableCell();
            this.xrCellFamiliyasy      = new XRTableCell();
            this.xrCellAdy             = new XRTableCell();
            this.xrCellDoglanSenesi    = new XRTableCell();
            this.xrCellJynsy           = new XRTableCell();
            this.xrCellRayatlygy       = new XRTableCell();
            this.xrCellPasport         = new XRTableCell();
            this.xrCellWezipesi        = new XRTableCell();
            this.xrCellMohleti         = new XRTableCell();
            this.xrCellTmSalgysy       = new XRTableCell();
            this.xrCellSerhet          = new XRTableCell();

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
            // PageHeader — title (30F) + column header table (45F) = 75F
            // ----------------------------------------------------------------
            this.PageHeader.HeightF = 75F;

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
            // №=25, Fam=75, Ady=65, DoglanSenesi=85, Jynsy=40, Raýatlygy=45,
            // Pasport=85, Wezipesi=120, Möhleti=100, TmSalgysy=215, Serhet=274.291
            // ----------------------------------------------------------------
            this.xrHdrNo.Name           = "xrHdrNo";
            this.xrHdrNo.Text           = "\u2116";
            this.xrHdrNo.Weight         = 25;

            this.xrHdrFamiliyasy.Name   = "xrHdrFamiliyasy";
            this.xrHdrFamiliyasy.Text   = "Famil\u00FDasy";
            this.xrHdrFamiliyasy.Weight = 75;

            this.xrHdrAdy.Name          = "xrHdrAdy";
            this.xrHdrAdy.Text          = "Ady";
            this.xrHdrAdy.Weight        = 65;

            this.xrHdrDoglanSenesi.Name   = "xrHdrDoglanSenesi";
            this.xrHdrDoglanSenesi.Text   = "Doglan senesi we \u00FDeri";
            this.xrHdrDoglanSenesi.Weight = 85;

            this.xrHdrJynsy.Name        = "xrHdrJynsy";
            this.xrHdrJynsy.Text        = "Jynsy";
            this.xrHdrJynsy.Weight      = 40;

            this.xrHdrRayatlygy.Name    = "xrHdrRayatlygy";
            this.xrHdrRayatlygy.Text    = "Ra\u00FDatlygy";
            this.xrHdrRayatlygy.Weight  = 45;

            this.xrHdrPasport.Name      = "xrHdrPasport";
            this.xrHdrPasport.Text      = "Pasport belgisi we m\u00F6hleti";
            this.xrHdrPasport.Weight    = 85;

            this.xrHdrWezipesi.Name     = "xrHdrWezipesi";
            this.xrHdrWezipesi.Text     = "Wezipesi";
            this.xrHdrWezipesi.Weight   = 120;

            this.xrHdrMohleti.Name      = "xrHdrMohleti";
            this.xrHdrMohleti.Text      = "M\u00F6hleti we gezekligi";
            this.xrHdrMohleti.Weight    = 100;

            this.xrHdrTmSalgysy.Name    = "xrHdrTmSalgysy";
            this.xrHdrTmSalgysy.Text    = "T\u00FCrkmenistanaky salgysy";
            this.xrHdrTmSalgysy.Weight  = 215;

            this.xrHdrSerhet.Name       = "xrHdrSerhet";
            this.xrHdrSerhet.Text       = "Barjak serhet \u00FDakasy";
            this.xrHdrSerhet.Weight     = 274.291;

            foreach (var hc in new XRTableCell[] {
                this.xrHdrNo, this.xrHdrFamiliyasy, this.xrHdrAdy, this.xrHdrDoglanSenesi,
                this.xrHdrJynsy, this.xrHdrRayatlygy, this.xrHdrPasport,
                this.xrHdrWezipesi, this.xrHdrMohleti, this.xrHdrTmSalgysy, this.xrHdrSerhet })
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
                this.xrHdrNo, this.xrHdrFamiliyasy, this.xrHdrAdy, this.xrHdrDoglanSenesi,
                this.xrHdrJynsy, this.xrHdrRayatlygy, this.xrHdrPasport,
                this.xrHdrWezipesi, this.xrHdrMohleti, this.xrHdrTmSalgysy, this.xrHdrSerhet
            });
            this.xrRowHeader.HeightF = 45F;
            this.xrRowHeader.Name    = "xrRowHeader";

            this.xrTableHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 30F);
            this.xrTableHeader.Name          = "xrTableHeader";
            this.xrTableHeader.Rows.Add(this.xrRowHeader);
            this.xrTableHeader.SizeF         = new System.Drawing.SizeF(1129.291F, 45F);
            this.PageHeader.Controls.Add(this.xrTableHeader);

            // ----------------------------------------------------------------
            // Data row — Detail band, one row repeated per ApplicationItem
            // ----------------------------------------------------------------
            this.xrCellNo.Name    = "xrCellNo";
            this.xrCellNo.Weight  = 25;
            this.xrCellNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "sumRecordNumber()"));
            XRSummary xrSummaryNo = new XRSummary();
            xrSummaryNo.Running = SummaryRunning.Report;
            this.xrCellNo.Summary = xrSummaryNo;

            this.xrCellFamiliyasy.Name   = "xrCellFamiliyasy";
            this.xrCellFamiliyasy.Weight = 75;
            this.xrCellFamiliyasy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_LastName]"));

            this.xrCellAdy.Name   = "xrCellAdy";
            this.xrCellAdy.Weight = 65;
            this.xrCellAdy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_FirstName]"));

            this.xrCellDoglanSenesi.Name   = "xrCellDoglanSenesi";
            this.xrCellDoglanSenesi.Weight = 85;
            this.xrCellDoglanSenesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Person_DateOfBirthText] + Char(10) + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]"));

            this.xrCellJynsy.Name   = "xrCellJynsy";
            this.xrCellJynsy.Weight = 40;
            this.xrCellJynsy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_GenderTm]"));

            this.xrCellRayatlygy.Name   = "xrCellRayatlygy";
            this.xrCellRayatlygy.Weight = 45;
            this.xrCellRayatlygy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_NationalityCode]"));

            this.xrCellPasport.Name   = "xrCellPasport";
            this.xrCellPasport.Weight = 85;
            this.xrCellPasport.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Passport_Number] + Char(10) + [Passport_ExpirationDateText]"));

            this.xrCellWezipesi.Name   = "xrCellWezipesi";
            this.xrCellWezipesi.Weight = 120;
            this.xrCellWezipesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Position_PositionTm]"));

            this.xrCellMohleti.Name   = "xrCellMohleti";
            this.xrCellMohleti.Weight = 100;
            this.xrCellMohleti.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[WorkPermit_Number] + Char(10) + 'WP' + Char(10) + [WorkPermit_StartDateText] + Char(10) + [WorkPermit_ExpirationDateText]"));

            this.xrCellTmSalgysy.Name   = "xrCellTmSalgysy";
            this.xrCellTmSalgysy.Weight = 215;
            this.xrCellTmSalgysy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Address_FullAddress]"));

            this.xrCellSerhet.Name   = "xrCellSerhet";
            this.xrCellSerhet.Weight = 274.291;
            this.xrCellSerhet.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Application_BorderZoneLocation_NameTm]"));

            foreach (var dc in new XRTableCell[] {
                this.xrCellNo, this.xrCellFamiliyasy, this.xrCellAdy, this.xrCellDoglanSenesi,
                this.xrCellJynsy, this.xrCellRayatlygy, this.xrCellPasport,
                this.xrCellWezipesi, this.xrCellMohleti, this.xrCellTmSalgysy, this.xrCellSerhet })
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

            // Multiline required for Char(10) line breaks and for long text to wrap visually
            this.xrCellDoglanSenesi.Multiline = true;
            this.xrCellPasport.Multiline      = true;
            this.xrCellMohleti.Multiline      = true;
            this.xrCellWezipesi.Multiline     = true;

            this.xrRowData.Cells.AddRange(new XRTableCell[] {
                this.xrCellNo, this.xrCellFamiliyasy, this.xrCellAdy, this.xrCellDoglanSenesi,
                this.xrCellJynsy, this.xrCellRayatlygy, this.xrCellPasport,
                this.xrCellWezipesi, this.xrCellMohleti, this.xrCellTmSalgysy, this.xrCellSerhet
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
        private XRTableCell xrHdrFamiliyasy;
        private XRTableCell xrHdrAdy;
        private XRTableCell xrHdrDoglanSenesi;
        private XRTableCell xrHdrJynsy;
        private XRTableCell xrHdrRayatlygy;
        private XRTableCell xrHdrPasport;
        private XRTableCell xrHdrWezipesi;
        private XRTableCell xrHdrMohleti;
        private XRTableCell xrHdrTmSalgysy;
        private XRTableCell xrHdrSerhet;
        private XRTable     xrTableData;
        private XRTableRow  xrRowData;
        private XRTableCell xrCellNo;
        private XRTableCell xrCellFamiliyasy;
        private XRTableCell xrCellAdy;
        private XRTableCell xrCellDoglanSenesi;
        private XRTableCell xrCellJynsy;
        private XRTableCell xrCellRayatlygy;
        private XRTableCell xrCellPasport;
        private XRTableCell xrCellWezipesi;
        private XRTableCell xrCellMohleti;
        private XRTableCell xrCellTmSalgysy;
        private XRTableCell xrCellSerhet;
    }
}
