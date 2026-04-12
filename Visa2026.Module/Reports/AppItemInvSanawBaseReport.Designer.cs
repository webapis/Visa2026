using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppItemInvSanawBaseReport
    {
        private void InitializeComponent()
        {
            this.xrLabelTitle            = new XRLabel();
            this.xrTableHeader           = new XRTable();
            this.xrRowHeader             = new XRTableRow();
            this.xrHdrNo                 = new XRTableCell();
            this.xrHdrFamiliyasy         = new XRTableCell();
            this.xrHdrAdy                = new XRTableCell();
            this.xrHdrDoglanSenesi       = new XRTableCell();
            this.xrHdrJynsy              = new XRTableCell();
            this.xrHdrRayatlygy          = new XRTableCell();
            this.xrHdrPasport            = new XRTableCell();
            this.xrHdrBilimi             = new XRTableCell();
            this.xrHdrHunari             = new XRTableCell();
            this.xrHdrWezipesi           = new XRTableCell();
            this.xrHdrMohleti            = new XRTableCell();
            this.xrHdrTmSalgysy          = new XRTableCell();
            this.xrHdrDasyrYurtSalgysy   = new XRTableCell();
            this.xrHdrSerhet             = new XRTableCell();
            this.xrTableData             = new XRTable();
            this.xrRowData               = new XRTableRow();
            this.xrCellNo                = new XRTableCell();
            this.xrCellFamiliyasy        = new XRTableCell();
            this.xrCellAdy               = new XRTableCell();
            this.xrCellDoglanSenesi      = new XRTableCell();
            this.xrCellJynsy             = new XRTableCell();
            this.xrCellRayatlygy         = new XRTableCell();
            this.xrCellPasport           = new XRTableCell();
            this.xrCellBilimi            = new XRTableCell();
            this.xrCellHunari            = new XRTableCell();
            this.xrCellWezipesi          = new XRTableCell();
            this.xrCellMohleti           = new XRTableCell();
            this.xrCellTmSalgysy         = new XRTableCell();
            this.xrCellDasyrYurtSalgysy  = new XRTableCell();
            this.xrCellSerhet            = new XRTableCell();

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
            // (item list forms do not show the application reference at top)
            this.xrLabelAppNumber.Visible = false;
            this.xrLabelAppDate.Visible   = false;

            // ----------------------------------------------------------------
            // Signatory — reposition for A4 Landscape printable width (1129.291F)
            // 40F top gap for breathing room; labels pulled inward (~42% each,
            // ~169F gap between) so position title and name sit closer together
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
            this.xrLabelTitle.Text          = "Daşary ýurt raýatlarynyň sanawy";
            this.xrLabelTitle.BackColor     = System.Drawing.Color.Transparent;
            this.PageHeader.Controls.Add(this.xrLabelTitle);

            // ----------------------------------------------------------------
            // Column header row  (widths sum = 1129.291F)
            // №=25, Fam=70, Ady=60, DoğlanSenesi=80, Jynsy=40, Raýatlygy=40,
            // Pasport=80, Bilimi=100, Hünäri=100, Wezipe=90, Möhlet=80,
            // TmSalgysy=120, DaSalgysy=120, Serhet=124.291
            // ----------------------------------------------------------------
            this.xrHdrNo.Name               = "xrHdrNo";
            this.xrHdrNo.Text               = "№";
            this.xrHdrNo.Weight             = 25;
            this.xrHdrFamiliyasy.Name       = "xrHdrFamiliyasy";
            this.xrHdrFamiliyasy.Text       = "Familiýasy";
            this.xrHdrFamiliyasy.Weight     = 70;
            this.xrHdrAdy.Name              = "xrHdrAdy";
            this.xrHdrAdy.Text              = "Ady";
            this.xrHdrAdy.Weight            = 60;
            this.xrHdrDoglanSenesi.Name     = "xrHdrDoglanSenesi";
            this.xrHdrDoglanSenesi.Text     = "Doglan senesi we ýeri";
            this.xrHdrDoglanSenesi.Weight   = 80;
            this.xrHdrJynsy.Name            = "xrHdrJynsy";
            this.xrHdrJynsy.Text            = "Jynsy";
            this.xrHdrJynsy.Weight          = 40;
            this.xrHdrRayatlygy.Name        = "xrHdrRayatlygy";
            this.xrHdrRayatlygy.Text        = "Raýatlygy";
            this.xrHdrRayatlygy.Weight      = 40;
            this.xrHdrPasport.Name          = "xrHdrPasport";
            this.xrHdrPasport.Text          = "Pasport belgisi we möhleti";
            this.xrHdrPasport.Weight        = 80;
            this.xrHdrBilimi.Name           = "xrHdrBilimi";
            this.xrHdrBilimi.Text           = "Bilimi we okan ýeri";
            this.xrHdrBilimi.Weight         = 100;
            this.xrHdrHunari.Name           = "xrHdrHunari";
            this.xrHdrHunari.Text           = "Bilimine görä hünäri";
            this.xrHdrHunari.Weight         = 100;
            this.xrHdrWezipesi.Name         = "xrHdrWezipesi";
            this.xrHdrWezipesi.Text         = "Wezipesi";
            this.xrHdrWezipesi.Weight       = 90;
            this.xrHdrMohleti.Name          = "xrHdrMohleti";
            this.xrHdrMohleti.Text          = "Möhleti we gezekligi";
            this.xrHdrMohleti.Weight        = 80;
            this.xrHdrTmSalgysy.Name        = "xrHdrTmSalgysy";
            this.xrHdrTmSalgysy.Text        = "Türkmenistanaky salgysy";
            this.xrHdrTmSalgysy.Weight      = 120;
            this.xrHdrDasyrYurtSalgysy.Name   = "xrHdrDasyrYurtSalgysy";
            this.xrHdrDasyrYurtSalgysy.Text   = "Daşary ýurtdaky salgysy";
            this.xrHdrDasyrYurtSalgysy.Weight = 120;
            this.xrHdrSerhet.Name           = "xrHdrSerhet";
            this.xrHdrSerhet.Text           = "Barjak serhet ýakasy";
            this.xrHdrSerhet.Weight         = 124.291;

            foreach (var hc in new XRTableCell[] {
                this.xrHdrNo, this.xrHdrFamiliyasy, this.xrHdrAdy, this.xrHdrDoglanSenesi,
                this.xrHdrJynsy, this.xrHdrRayatlygy, this.xrHdrPasport,
                this.xrHdrBilimi, this.xrHdrHunari, this.xrHdrWezipesi, this.xrHdrMohleti,
                this.xrHdrTmSalgysy, this.xrHdrDasyrYurtSalgysy, this.xrHdrSerhet })
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
                this.xrHdrBilimi, this.xrHdrHunari, this.xrHdrWezipesi, this.xrHdrMohleti,
                this.xrHdrTmSalgysy, this.xrHdrDasyrYurtSalgysy, this.xrHdrSerhet
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
            this.xrCellNo.Name              = "xrCellNo";
            this.xrCellNo.Weight            = 25;
            this.xrCellNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "RowNumber()"));

            this.xrCellFamiliyasy.Name      = "xrCellFamiliyasy";
            this.xrCellFamiliyasy.Weight    = 70;
            this.xrCellFamiliyasy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_LastName]"));

            this.xrCellAdy.Name             = "xrCellAdy";
            this.xrCellAdy.Weight           = 60;
            this.xrCellAdy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_FirstName]"));

            this.xrCellDoglanSenesi.Name    = "xrCellDoglanSenesi";
            this.xrCellDoglanSenesi.Weight  = 80;
            this.xrCellDoglanSenesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Person_DateOfBirthText] + Char(10) + [Person_CountryOfBirthTm] + '/' + [Person_BirthPlace]"));

            this.xrCellJynsy.Name           = "xrCellJynsy";
            this.xrCellJynsy.Weight         = 40;
            this.xrCellJynsy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_GenderTm]"));

            this.xrCellRayatlygy.Name       = "xrCellRayatlygy";
            this.xrCellRayatlygy.Weight     = 40;
            this.xrCellRayatlygy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_NationalityCode]"));

            this.xrCellPasport.Name         = "xrCellPasport";
            this.xrCellPasport.Weight       = 80;
            this.xrCellPasport.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Passport_Number] + Char(10) + [Passport_ExpirationDateText]"));

            this.xrCellBilimi.Name          = "xrCellBilimi";
            this.xrCellBilimi.Weight        = 100;
            this.xrCellBilimi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Education_LevelTm] + Char(10) + [Education_InstitutionName]"));

            this.xrCellHunari.Name          = "xrCellHunari";
            this.xrCellHunari.Weight        = 100;
            this.xrCellHunari.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Education_SpecialtyTm]"));

            this.xrCellWezipesi.Name        = "xrCellWezipesi";
            this.xrCellWezipesi.Weight      = 90;
            this.xrCellWezipesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Position_PositionTm]"));

            this.xrCellMohleti.Name         = "xrCellMohleti";
            this.xrCellMohleti.Weight       = 80;
            this.xrCellMohleti.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Application_VisaPeriod_NameTm] + ' ' + [Application_VisaCategory_NameTm]"));

            this.xrCellTmSalgysy.Name       = "xrCellTmSalgysy";
            this.xrCellTmSalgysy.Weight     = 120;
            this.xrCellTmSalgysy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Address_FullAddress]"));

            this.xrCellDasyrYurtSalgysy.Name    = "xrCellDasyrYurtSalgysy";
            this.xrCellDasyrYurtSalgysy.Weight  = 120;
            this.xrCellDasyrYurtSalgysy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_ForeignAddress]"));

            this.xrCellSerhet.Name          = "xrCellSerhet";
            this.xrCellSerhet.Weight        = 124.291;
            this.xrCellSerhet.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Application_BorderZoneLocation_NameTm]"));

            foreach (var dc in new XRTableCell[] {
                this.xrCellNo, this.xrCellFamiliyasy, this.xrCellAdy, this.xrCellDoglanSenesi,
                this.xrCellJynsy, this.xrCellRayatlygy, this.xrCellPasport,
                this.xrCellBilimi, this.xrCellHunari, this.xrCellWezipesi, this.xrCellMohleti,
                this.xrCellTmSalgysy, this.xrCellDasyrYurtSalgysy, this.xrCellSerhet })
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
                dc.StylePriority.UseBorders      = true;
                dc.StylePriority.UseBorderWidth  = true;
                dc.StylePriority.UseBorderColor  = true;
                dc.StylePriority.UseBackColor    = true;
            }

            this.xrRowData.Cells.AddRange(new XRTableCell[] {
                this.xrCellNo, this.xrCellFamiliyasy, this.xrCellAdy, this.xrCellDoglanSenesi,
                this.xrCellJynsy, this.xrCellRayatlygy, this.xrCellPasport,
                this.xrCellBilimi, this.xrCellHunari, this.xrCellWezipesi, this.xrCellMohleti,
                this.xrCellTmSalgysy, this.xrCellDasyrYurtSalgysy, this.xrCellSerhet
            });
            this.xrRowData.HeightF  = 80F;
            this.xrRowData.CanGrow  = true;
            this.xrRowData.Name     = "xrRowData";

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

        protected XRLabel     xrLabelTitle;
        protected XRTable     xrTableHeader;
        protected XRTableRow  xrRowHeader;
        protected XRTableCell xrHdrNo;
        protected XRTableCell xrHdrFamiliyasy;
        protected XRTableCell xrHdrAdy;
        protected XRTableCell xrHdrDoglanSenesi;
        protected XRTableCell xrHdrJynsy;
        protected XRTableCell xrHdrRayatlygy;
        protected XRTableCell xrHdrPasport;
        protected XRTableCell xrHdrBilimi;
        protected XRTableCell xrHdrHunari;
        protected XRTableCell xrHdrWezipesi;
        protected XRTableCell xrHdrMohleti;
        protected XRTableCell xrHdrTmSalgysy;
        protected XRTableCell xrHdrDasyrYurtSalgysy;
        protected XRTableCell xrHdrSerhet;
        protected XRTable     xrTableData;
        protected XRTableRow  xrRowData;
        protected XRTableCell xrCellNo;
        protected XRTableCell xrCellFamiliyasy;
        protected XRTableCell xrCellAdy;
        protected XRTableCell xrCellDoglanSenesi;
        protected XRTableCell xrCellJynsy;
        protected XRTableCell xrCellRayatlygy;
        protected XRTableCell xrCellPasport;
        protected XRTableCell xrCellBilimi;
        protected XRTableCell xrCellHunari;
        protected XRTableCell xrCellWezipesi;
        protected XRTableCell xrCellMohleti;
        protected XRTableCell xrCellTmSalgysy;
        protected XRTableCell xrCellDasyrYurtSalgysy;
        protected XRTableCell xrCellSerhet;
    }
}
