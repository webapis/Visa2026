using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppCancelInvWPItemReport
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
            this.xrHdrCakylyk              = new XRTableCell();
            this.xrHdrResmilesen           = new XRTableCell();
            this.xrHdrMohletTamam          = new XRTableCell();
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
            this.xrCellCakylyk             = new XRTableCell();
            this.xrCellResmilesen          = new XRTableCell();
            this.xrCellMohletTamam         = new XRTableCell();

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).BeginInit();

            // ----------------------------------------------------------------
            // Page — A4 Portrait (default from AppItemBaseReport, no override)
            // Printable width = 826.7717 - 20 - 20 = 786.7717F
            // ----------------------------------------------------------------

            // ----------------------------------------------------------------
            // PageHeader — title (27F) + column header table (55F) = 82F
            // ----------------------------------------------------------------
            this.PageHeader.HeightF = 82F;

            //
            // xrLabelTitle
            //
            this.xrLabelTitle.Font          = new DXFont("Times New Roman", 10F, DXFontStyle.Bold);
            this.xrLabelTitle.LocationFloat = new DevExpress.Utils.PointFloat(0F, 5F);
            this.xrLabelTitle.Name          = "xrLabelTitle";
            this.xrLabelTitle.SizeF         = new System.Drawing.SizeF(786.7717F, 20F);
            this.xrLabelTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabelTitle.Text          = "Da\u015Fary \u00FDurt ra\u00FDatlary\u0148y\u0148 sanawy";
            this.xrLabelTitle.BackColor     = System.Drawing.Color.Transparent;
            this.PageHeader.Controls.Add(this.xrLabelTitle);

            // ----------------------------------------------------------------
            // Column header row (weights sum = 786.7717F)
            // №=18, AS-№=60, Tassyk=47, Fam=52, Ady=44, Doglan=68,
            // Pasport=60, Hünäri=115, Hereket=72, Rugsat=52,
            // Çakylyk=57, Resmileşen=63, MöhletTamam=78.7717
            // ----------------------------------------------------------------
            this.xrHdrNo.Name          = "xrHdrNo";
            this.xrHdrNo.Text          = "\u2116";
            this.xrHdrNo.Weight        = 18;

            this.xrHdrASNo.Name        = "xrHdrASNo";
            this.xrHdrASNo.Text        = "AS-\u2116";
            this.xrHdrASNo.Weight      = 60;

            this.xrHdrTassykNama.Name  = "xrHdrTassykNama";
            this.xrHdrTassykNama.Text  = "Tassyk-nama belgisi";
            this.xrHdrTassykNama.Weight = 47;

            this.xrHdrFamiliyasy.Name  = "xrHdrFamiliyasy";
            this.xrHdrFamiliyasy.Text  = "Famil\u00FDasy";
            this.xrHdrFamiliyasy.Weight = 52;

            this.xrHdrAdy.Name         = "xrHdrAdy";
            this.xrHdrAdy.Text         = "Ady";
            this.xrHdrAdy.Weight       = 44;

            this.xrHdrDoglanSenesi.Name   = "xrHdrDoglanSenesi";
            this.xrHdrDoglanSenesi.Text   = "Doglan senesi we \u015Furdy";
            this.xrHdrDoglanSenesi.Weight = 68;

            this.xrHdrPasport.Name     = "xrHdrPasport";
            this.xrHdrPasport.Text     = "Pasport belgisi";
            this.xrHdrPasport.Weight   = 60;

            this.xrHdrHunari.Name      = "xrHdrHunari";
            this.xrHdrHunari.Text      = "H\u00FCn\u00E4ri we bilimi";
            this.xrHdrHunari.Weight    = 115;

            this.xrHdrHereket.Name     = "xrHdrHereket";
            this.xrHdrHereket.Text     = "Hereket ed\u00FD\u00E4n \u00E7\u00E4gi";
            this.xrHdrHereket.Weight   = 72;

            this.xrHdrRugsat.Name      = "xrHdrRugsat";
            this.xrHdrRugsat.Text      = "Rugsat edilen m\u00F6hleti";
            this.xrHdrRugsat.Weight    = 52;

            this.xrHdrCakylyk.Name     = "xrHdrCakylyk";
            this.xrHdrCakylyk.Text     = "\u00C7akylyk belgisi";
            this.xrHdrCakylyk.Weight   = 57;

            this.xrHdrResmilesen.Name  = "xrHdrResmilesen";
            this.xrHdrResmilesen.Text  = "\u00C7akylyk\u00FD\u0148 resmile\u015Fdirilen senesi";
            this.xrHdrResmilesen.Weight = 63;

            this.xrHdrMohletTamam.Name  = "xrHdrMohletTamam";
            this.xrHdrMohletTamam.Text  = "\u00C7akylyk\u00FD\u0148 m\u00F6hleti tamamlan\u00FDan sene";
            this.xrHdrMohletTamam.Weight = 78.7717;

            foreach (var hc in new XRTableCell[] {
                this.xrHdrNo, this.xrHdrASNo, this.xrHdrTassykNama, this.xrHdrFamiliyasy,
                this.xrHdrAdy, this.xrHdrDoglanSenesi, this.xrHdrPasport, this.xrHdrHunari,
                this.xrHdrHereket, this.xrHdrRugsat, this.xrHdrCakylyk,
                this.xrHdrResmilesen, this.xrHdrMohletTamam })
            {
                hc.Font          = new DXFont("Times New Roman", 6F, DXFontStyle.Bold);
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
                this.xrHdrHereket, this.xrHdrRugsat, this.xrHdrCakylyk,
                this.xrHdrResmilesen, this.xrHdrMohletTamam
            });
            this.xrRowHeader.HeightF = 55F;
            this.xrRowHeader.Name    = "xrRowHeader";

            this.xrTableHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 27F);
            this.xrTableHeader.Name          = "xrTableHeader";
            this.xrTableHeader.Rows.Add(this.xrRowHeader);
            this.xrTableHeader.SizeF         = new System.Drawing.SizeF(786.7717F, 55F);
            this.PageHeader.Controls.Add(this.xrTableHeader);

            // ----------------------------------------------------------------
            // Data row — Detail band, one row per ApplicationItem
            // ----------------------------------------------------------------
            this.xrCellNo.Name   = "xrCellNo";
            this.xrCellNo.Weight = 18;
            this.xrCellNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "sumRecordNumber()"));
            XRSummary xrSummaryNo = new XRSummary();
            xrSummaryNo.Running = SummaryRunning.Report;
            this.xrCellNo.Summary = xrSummaryNo;

            this.xrCellASNo.Name   = "xrCellASNo";
            this.xrCellASNo.Weight = 60;
            this.xrCellASNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[WorkPermit_Number]"));

            this.xrCellTassykNama.Name   = "xrCellTassykNama";
            this.xrCellTassykNama.Weight = 47;
            this.xrCellTassykNama.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[WorkPermit_ASNumber]"));

            this.xrCellFamiliyasy.Name   = "xrCellFamiliyasy";
            this.xrCellFamiliyasy.Weight = 52;
            this.xrCellFamiliyasy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_LastName]"));

            this.xrCellAdy.Name   = "xrCellAdy";
            this.xrCellAdy.Weight = 44;
            this.xrCellAdy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_FirstName]"));

            this.xrCellDoglanSenesi.Name   = "xrCellDoglanSenesi";
            this.xrCellDoglanSenesi.Weight = 68;
            this.xrCellDoglanSenesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Person_DateOfBirthText] + Char(10) + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]"));

            this.xrCellPasport.Name   = "xrCellPasport";
            this.xrCellPasport.Weight = 60;
            this.xrCellPasport.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Passport_Number] + Char(10) + [Passport_ExpirationDateText]"));

            this.xrCellHunari.Name   = "xrCellHunari";
            this.xrCellHunari.Weight = 115;
            this.xrCellHunari.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Education_LevelTm] + ', ' + [Position_PositionTm]"));

            this.xrCellHereket.Name   = "xrCellHereket";
            this.xrCellHereket.Weight = 72;
            this.xrCellHereket.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[WorkPermit_WorkPermittedLocations]"));

            this.xrCellRugsat.Name   = "xrCellRugsat";
            this.xrCellRugsat.Weight = 52;
            this.xrCellRugsat.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[WorkPermit_ExpirationDateText]"));

            this.xrCellCakylyk.Name   = "xrCellCakylyk";
            this.xrCellCakylyk.Weight = 57;
            this.xrCellCakylyk.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Invitation_Number]"));

            this.xrCellResmilesen.Name   = "xrCellResmilesen";
            this.xrCellResmilesen.Weight = 63;
            this.xrCellResmilesen.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Invitation_StartDateText]"));

            this.xrCellMohletTamam.Name   = "xrCellMohletTamam";
            this.xrCellMohletTamam.Weight = 78.7717;
            this.xrCellMohletTamam.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Invitation_ExpirationDateText]"));

            foreach (var dc in new XRTableCell[] {
                this.xrCellNo, this.xrCellASNo, this.xrCellTassykNama, this.xrCellFamiliyasy,
                this.xrCellAdy, this.xrCellDoglanSenesi, this.xrCellPasport, this.xrCellHunari,
                this.xrCellHereket, this.xrCellRugsat, this.xrCellCakylyk,
                this.xrCellResmilesen, this.xrCellMohletTamam })
            {
                dc.Font          = new DXFont("Times New Roman", 6F);
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
            this.xrCellHunari.Multiline       = true;
            this.xrCellHereket.Multiline      = true;

            this.xrRowData.Cells.AddRange(new XRTableCell[] {
                this.xrCellNo, this.xrCellASNo, this.xrCellTassykNama, this.xrCellFamiliyasy,
                this.xrCellAdy, this.xrCellDoglanSenesi, this.xrCellPasport, this.xrCellHunari,
                this.xrCellHereket, this.xrCellRugsat, this.xrCellCakylyk,
                this.xrCellResmilesen, this.xrCellMohletTamam
            });
            this.xrRowData.HeightF = 70F;
            this.xrRowData.CanGrow = true;
            this.xrRowData.Name    = "xrRowData";

            this.xrTableData.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTableData.Name          = "xrTableData";
            this.xrTableData.Rows.Add(this.xrRowData);
            this.xrTableData.SizeF         = new System.Drawing.SizeF(786.7717F, 70F);

            this.Detail.HeightF  = 70F;
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
        private XRTableCell xrHdrCakylyk;
        private XRTableCell xrHdrResmilesen;
        private XRTableCell xrHdrMohletTamam;
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
        private XRTableCell xrCellCakylyk;
        private XRTableCell xrCellResmilesen;
        private XRTableCell xrCellMohletTamam;
    }
}
