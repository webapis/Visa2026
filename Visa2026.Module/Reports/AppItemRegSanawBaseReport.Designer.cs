using DevExpress.XtraReports.UI;
using DevExpress.Drawing;

namespace Visa2026.Module.Reports
{
    partial class AppItemRegSanawBaseReport
    {
        private void InitializeComponent()
        {
            this.xrLabelTitle              = new XRLabel();
            this.xrTableHeader             = new XRTable();
            this.xrRowHeader               = new XRTableRow();
            this.xrHdrNo                   = new XRTableCell();
            this.xrHdrFamiliyasy           = new XRTableCell();
            this.xrHdrAdy                  = new XRTableCell();
            this.xrHdrDoglanSenesi         = new XRTableCell();
            this.xrHdrJynsy                = new XRTableCell();
            this.xrHdrRayatlygy            = new XRTableCell();
            this.xrHdrPasportBelgisi       = new XRTableCell();
            this.xrHdrPasportMohleti       = new XRTableCell();
            this.xrHdrGelmegiMaksady       = new XRTableCell();
            this.xrHdrWizaMaglumatlary     = new XRTableCell();
            this.xrHdrTmSalgysy            = new XRTableCell();
            this.xrTableData               = new XRTable();
            this.xrRowData                 = new XRTableRow();
            this.xrCellNo                  = new XRTableCell();
            this.xrCellFamiliyasy          = new XRTableCell();
            this.xrCellAdy                 = new XRTableCell();
            this.xrCellDoglanSenesi        = new XRTableCell();
            this.xrCellJynsy               = new XRTableCell();
            this.xrCellRayatlygy           = new XRTableCell();
            this.xrCellPasportBelgisi      = new XRTableCell();
            this.xrCellPasportMohleti      = new XRTableCell();
            this.xrCellGelmegiMaksady      = new XRTableCell();
            this.xrCellWizaMaglumatlary    = new XRTableCell();
            this.xrCellTmSalgysy           = new XRTableCell();

            ((System.ComponentModel.ISupportInitialize)(this.xrTableHeader)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTableData)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();

            // ----------------------------------------------------------------
            // Page — A4 Landscape
            // ----------------------------------------------------------------
            this.Landscape   = true;
            this.PageWidthF  = 1169.291F;
            this.PageHeightF = 826.7717F;
            this.Margins     = new DXMargins(20F, 20F, 50F, 50F);

            this.xrLabelAppNumber.Visible = false;
            this.xrLabelAppDate.Visible   = false;

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
            // Column header row (widths sum = 1129.291F)
            // №=25, Fam=90, Ady=80, Doğlan=70, Jynsy=45, Raýat=45,
            // PasportBelgisi=90, PasportMöhleti=90,
            // GelmegiMaksady=200, WizaMaglumatlary=180, TmSalgysy=214.291
            // ----------------------------------------------------------------
            this.xrHdrNo.Name                   = "xrHdrNo";
            this.xrHdrNo.Text                   = "№";
            this.xrHdrNo.Weight                 = 25;
            this.xrHdrFamiliyasy.Name           = "xrHdrFamiliyasy";
            this.xrHdrFamiliyasy.Text           = "Familiýasy";
            this.xrHdrFamiliyasy.Weight         = 90;
            this.xrHdrAdy.Name                  = "xrHdrAdy";
            this.xrHdrAdy.Text                  = "Ady";
            this.xrHdrAdy.Weight                = 80;
            this.xrHdrDoglanSenesi.Name         = "xrHdrDoglanSenesi";
            this.xrHdrDoglanSenesi.Text         = "Doglan senesi";
            this.xrHdrDoglanSenesi.Weight       = 70;
            this.xrHdrJynsy.Name                = "xrHdrJynsy";
            this.xrHdrJynsy.Text                = "Jynsy";
            this.xrHdrJynsy.Weight              = 45;
            this.xrHdrRayatlygy.Name            = "xrHdrRayatlygy";
            this.xrHdrRayatlygy.Text            = "Raýatlygy";
            this.xrHdrRayatlygy.Weight          = 45;
            this.xrHdrPasportBelgisi.Name       = "xrHdrPasportBelgisi";
            this.xrHdrPasportBelgisi.Text       = "Pasportynyň (şahsyýetini tassyklaýan resminamasynýň) belgisi";
            this.xrHdrPasportBelgisi.Weight     = 90;
            this.xrHdrPasportMohleti.Name       = "xrHdrPasportMohleti";
            this.xrHdrPasportMohleti.Text       = "Pasportynyň (şahsyýetini tassyklaýan resminamasynýň) möhleti";
            this.xrHdrPasportMohleti.Weight     = 90;
            this.xrHdrGelmegiMaksady.Name       = "xrHdrGelmegiMaksady";
            this.xrHdrGelmegiMaksady.Text       = "Gelmeginiň maksady";
            this.xrHdrGelmegiMaksady.Weight     = 200;
            this.xrHdrWizaMaglumatlary.Name     = "xrHdrWizaMaglumatlary";
            this.xrHdrWizaMaglumatlary.Text     = "Wiza maglumatlary";
            this.xrHdrWizaMaglumatlary.Weight   = 180;
            this.xrHdrTmSalgysy.Name            = "xrHdrTmSalgysy";
            this.xrHdrTmSalgysy.Text            = "Türkmenistanaky salgysy";
            this.xrHdrTmSalgysy.Weight          = 214.291;

            foreach (var hc in new XRTableCell[] {
                this.xrHdrNo, this.xrHdrFamiliyasy, this.xrHdrAdy, this.xrHdrDoglanSenesi,
                this.xrHdrJynsy, this.xrHdrRayatlygy, this.xrHdrPasportBelgisi,
                this.xrHdrPasportMohleti, this.xrHdrGelmegiMaksady,
                this.xrHdrWizaMaglumatlary, this.xrHdrTmSalgysy })
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
                this.xrHdrJynsy, this.xrHdrRayatlygy, this.xrHdrPasportBelgisi,
                this.xrHdrPasportMohleti, this.xrHdrGelmegiMaksady,
                this.xrHdrWizaMaglumatlary, this.xrHdrTmSalgysy
            });
            this.xrRowHeader.HeightF = 45F;
            this.xrRowHeader.Name    = "xrRowHeader";

            this.xrTableHeader.LocationFloat = new DevExpress.Utils.PointFloat(0F, 30F);
            this.xrTableHeader.Name          = "xrTableHeader";
            this.xrTableHeader.Rows.Add(this.xrRowHeader);
            this.xrTableHeader.SizeF         = new System.Drawing.SizeF(1129.291F, 45F);
            this.PageHeader.Controls.Add(this.xrTableHeader);

            // ----------------------------------------------------------------
            // Data row
            // ----------------------------------------------------------------
            this.xrCellNo.Name              = "xrCellNo";
            this.xrCellNo.Weight            = 25;
            this.xrCellNo.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "RowNumber()"));

            this.xrCellFamiliyasy.Name      = "xrCellFamiliyasy";
            this.xrCellFamiliyasy.Weight    = 90;
            this.xrCellFamiliyasy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_LastName]"));

            this.xrCellAdy.Name             = "xrCellAdy";
            this.xrCellAdy.Weight           = 80;
            this.xrCellAdy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_FirstName]"));

            this.xrCellDoglanSenesi.Name    = "xrCellDoglanSenesi";
            this.xrCellDoglanSenesi.Weight  = 70;
            this.xrCellDoglanSenesi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_DateOfBirthText]"));

            this.xrCellJynsy.Name           = "xrCellJynsy";
            this.xrCellJynsy.Weight         = 45;
            this.xrCellJynsy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_GenderTm]"));

            this.xrCellRayatlygy.Name       = "xrCellRayatlygy";
            this.xrCellRayatlygy.Weight     = 45;
            this.xrCellRayatlygy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Person_NationalityCode]"));

            this.xrCellPasportBelgisi.Name      = "xrCellPasportBelgisi";
            this.xrCellPasportBelgisi.Weight    = 90;
            this.xrCellPasportBelgisi.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Passport_Number]"));

            this.xrCellPasportMohleti.Name      = "xrCellPasportMohleti";
            this.xrCellPasportMohleti.Weight    = 90;
            this.xrCellPasportMohleti.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Passport_ExpirationDateText]"));

            this.xrCellGelmegiMaksady.Name      = "xrCellGelmegiMaksady";
            this.xrCellGelmegiMaksady.Weight    = 200;
            this.xrCellGelmegiMaksady.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Position_PositionTm]"));

            this.xrCellWizaMaglumatlary.Name    = "xrCellWizaMaglumatlary";
            this.xrCellWizaMaglumatlary.Weight  = 180;
            this.xrCellWizaMaglumatlary.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text",
                "[Visa_Number] + Char(10) + [Visa_CategoryTm] + Char(10) + [Visa_StartDateText] + Char(10) + [Visa_ExpirationDateText]"));

            this.xrCellTmSalgysy.Name       = "xrCellTmSalgysy";
            this.xrCellTmSalgysy.Weight     = 214.291;
            this.xrCellTmSalgysy.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Text", "[Address_FullAddress]"));

            foreach (var dc in new XRTableCell[] {
                this.xrCellNo, this.xrCellFamiliyasy, this.xrCellAdy, this.xrCellDoglanSenesi,
                this.xrCellJynsy, this.xrCellRayatlygy, this.xrCellPasportBelgisi,
                this.xrCellPasportMohleti, this.xrCellGelmegiMaksady,
                this.xrCellWizaMaglumatlary, this.xrCellTmSalgysy })
            {
                dc.Font          = new DXFont("Times New Roman", 7F);
                dc.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
                dc.WordWrap      = true;
                dc.CanGrow       = true;
                dc.Borders       = DevExpress.XtraPrinting.BorderSide.All;
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
                this.xrCellJynsy, this.xrCellRayatlygy, this.xrCellPasportBelgisi,
                this.xrCellPasportMohleti, this.xrCellGelmegiMaksady,
                this.xrCellWizaMaglumatlary, this.xrCellTmSalgysy
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
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
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
        protected XRTableCell xrHdrPasportBelgisi;
        protected XRTableCell xrHdrPasportMohleti;
        protected XRTableCell xrHdrGelmegiMaksady;
        protected XRTableCell xrHdrWizaMaglumatlary;
        protected XRTableCell xrHdrTmSalgysy;
        protected XRTable     xrTableData;
        protected XRTableRow  xrRowData;
        protected XRTableCell xrCellNo;
        protected XRTableCell xrCellFamiliyasy;
        protected XRTableCell xrCellAdy;
        protected XRTableCell xrCellDoglanSenesi;
        protected XRTableCell xrCellJynsy;
        protected XRTableCell xrCellRayatlygy;
        protected XRTableCell xrCellPasportBelgisi;
        protected XRTableCell xrCellPasportMohleti;
        protected XRTableCell xrCellGelmegiMaksady;
        protected XRTableCell xrCellWizaMaglumatlary;
        protected XRTableCell xrCellTmSalgysy;
    }
}
