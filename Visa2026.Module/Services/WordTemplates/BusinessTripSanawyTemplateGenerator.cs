using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;

namespace Visa2026.Module.Services.WordTemplates
{
    /// <summary>
    /// Generates the "Daşary ýurt raýatlarynyň sanawy" Word template for BusinessTrip list reports.
    /// Based on: App_Business_Trip_Arrival_BusinessTrip_map.md
    ///
    /// Template uses DocxTemplater syntax:
    ///   - {{#foreach rows}} / {{/foreach}} on the repeating data row
    ///   - {{rows.FieldName}} for per-row cell values
    ///   - {{Header_FieldName}} for header-level fields (passed as top-level dict keys)
    ///
    /// Call Generate() once to produce the .docx bytes, then save to
    /// Visa2026.Module/Resources/BusinessTrip_Sanawy.docx and register as EmbeddedResource.
    /// </summary>
    public static class BusinessTripSanawyTemplateGenerator
    {
        // ── Column definitions exactly as per map (total = 969F → scaled to twips for Word) ──────────────
        // Word uses twips (1/20 pt). A4 landscape printable = ~16838 twips at standard margins.
        // We scale the 969 DevExpress units proportionally to fill ~15600 twips (leaving small margins).
        // Each unit ≈ 16.1 twips.  Widths rounded to nearest 10 twips.

        private static readonly (string Header, string Placeholder, int WidthTwips, bool Centered)[] Columns =
        {
            ("№",                                "{{#foreach rows}}{{i+1}}",      450,  true),
            ("Familiýasy",                       "{{rows.Person_LastName}}",    1160, false),
            ("Ady",                              "{{rows.Person_FirstName}}",   970,  false),
            ("Doglan senesi\nwe ýeri",           "{{rows.Person_DateOfBirthText}}\n{{rows.Person_BirthPlace}}", 1290, false),
            ("Jynsy",                            "{{rows.Person_GenderTm}}",    645,  true),
            ("Raýatlygy",                        "{{rows.Person_NationalityCode}}", 805, true),
            ("Pasport belgisi\nwe möhleti",      "{{rows.Passport_Number}}\n{{rows.Passport_ExpirationDateText}}", 1290, false),
            ("Wezipesi",                         "{{rows.Position_NameTm}}",    2335, false),
            ("Möhleti we\nGezekligi",            "{{rows.Visa_NumberAndType}}\n{{rows.Visa_StartDateText}}\n{{rows.Visa_ExpirationDateText}}", 1370, false),
            ("Türkmenistandaky\nsalgysy",        "{{rows.Address_FullAddress}}", 2660, false),
            ("Iş saparynda\nboljak salgysy",     "{{rows.BusinessTripAddress_FullAddress}}\n{{/ds.rows}}", 2625, false),
        };

        // Page setup: A4 landscape
        // Paper size in twentieths of a point: 11906 × 16838
        private const uint PageWidthTwips  = 16838;
        private const uint PageHeightTwips = 11906;
        // Margins (map: L=100F, R=100F, T=50F, B=100F  → ~160, 160, 80, 160 twips at 1.6 ratio)
        private const uint MarginTop    = 720;
        private const uint MarginBottom = 720;
        private const uint MarginLeft   = 720;
        private const uint MarginRight  = 720;

        // Font
        private const string FontName = "Times New Roman";
        private const string FontSizeHalfPts = "24"; // 12pt = 24 half-points
        private const string TitleFontSizeHalfPts = "30"; // 15pt

        public static byte[] Generate()
        {
            using var ms = new MemoryStream();
            using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
            {
                var mainPart = doc.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                SetPageLayout(body);
                AddTitleParagraph(body);
                AddTable(body);
                AddSignatoryParagraphs(body);

                mainPart.Document.Save();
            }
            return ms.ToArray();
        }

        // ── Page layout ──────────────────────────────────────────────────────────────────────────────────

        private static void SetPageLayout(Body body)
        {
            var sectPr = new SectionProperties(
                new PageSize
                {
                    Width  = PageWidthTwips,
                    Height = PageHeightTwips,
                    Orient = PageOrientationValues.Landscape
                },
                new PageMargin
                {
                    Top    = (int)MarginTop,
                    Bottom = (int)MarginBottom,
                    Left   = MarginLeft,
                    Right  = MarginRight
                }
            );
            body.AppendChild(sectPr);
        }

        // ── Title paragraph ───────────────────────────────────────────────────────────────────────────────

        private static void AddTitleParagraph(Body body)
        {
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = JustificationValues.Center },
                    new SpacingBetweenLines { After = "120" }
                ),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = FontName, HighAnsi = FontName },
                        new FontSize { Val = TitleFontSizeHalfPts },
                        new Bold()
                    ),
                    new Text("Daşary ýurt raýatlarynyň sanawy")
                )
            );
            body.AppendChild(para);
        }

        // ── Main table ────────────────────────────────────────────────────────────────────────────────────

        private static void AddTable(Body body)
        {
            var table = new Table();

            // Table properties: all borders, full width
            table.AppendChild(new TableProperties(
                new TableBorders(
                    new TopBorder    { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
                    new LeftBorder   { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
                    new RightBorder  { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
                    new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" }
                ),
                new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
                new TableLayout { Type = TableLayoutValues.Fixed }
            ));

            // Column width definitions
            var tblGrid = new TableGrid();
            foreach (var col in Columns)
                tblGrid.AppendChild(new GridColumn { Width = col.WidthTwips.ToString() });
            table.AppendChild(tblGrid);

            // Header row
            table.AppendChild(BuildHeaderRow());

            // Data row (with DocxTemplater foreach markers embedded in first/last cells)
            table.AppendChild(BuildDataRow());

            body.AppendChild(table);
        }

        private static TableRow BuildHeaderRow()
        {
            var row = new TableRow(
                new TableRowProperties(
                    new TableRowHeight { Val = 600, HeightType = HeightRuleValues.AtLeast }
                )
            );

            foreach (var col in Columns)
            {
                var cell = new TableCell();
                cell.AppendChild(BuildCellProperties(col.WidthTwips, shading: true));
                cell.AppendChild(BuildParagraph(col.Header, col.Centered, bold: true, isHeader: true));
                row.AppendChild(cell);
            }
            return row;
        }

        private static TableRow BuildDataRow()
        {
            var row = new TableRow(
                new TableRowProperties(
                    new TableRowHeight { Val = 600, HeightType = HeightRuleValues.AtLeast }
                )
            );

            foreach (var col in Columns)
            {
                var cell = new TableCell();
                cell.AppendChild(BuildCellProperties(col.WidthTwips));
                cell.AppendChild(BuildParagraph(col.Placeholder, col.Centered, bold: false, isHeader: false));
                row.AppendChild(cell);
            }
            return row;
        }

        // ── Signatory block ───────────────────────────────────────────────────────────────────────────────

        private static void AddSignatoryParagraphs(Body body)
        {
            // Spacer
            body.AppendChild(new Paragraph(
                new ParagraphProperties(new SpacingBetweenLines { Before = "240", After = "0" })
            ));

            // Single paragraph: position left  ···tab···  full name right
            var sigPara = new Paragraph(
                new ParagraphProperties(
                    new Tabs(new TabStop { Val = TabStopValues.Right, Position = (int)(PageWidthTwips - MarginLeft - MarginRight) }),
                    new SpacingBetweenLines { After = "0" }
                ),
                BuildRun("{{Application_CompanyHead_PositionTm}}", bold: true),
                new Run(new TabChar()),
                BuildRun("{{Application_CompanyHead_FullName}}", bold: true)
            );
            body.AppendChild(sigPara);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────────────────────────

        private static TableCellProperties BuildCellProperties(int widthTwips, bool shading = false)
        {
            var props = new TableCellProperties(
                new TableCellWidth { Width = widthTwips.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            );
            if (shading)
                props.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "D9D9D9", Color = "auto" });
            return props;
        }

        private static Paragraph BuildParagraph(string text, bool centered, bool bold, bool isHeader)
        {
            var justification = centered ? JustificationValues.Center : JustificationValues.Left;
            var para = new Paragraph(
                new ParagraphProperties(
                    new Justification { Val = justification },
                    new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
                    new Indentation { Left = "57", Right = "57" }
                )
            );

            // Handle newlines in placeholder text as separate runs with line breaks
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var rp = new RunProperties(
                    new RunFonts { Ascii = FontName, HighAnsi = FontName },
                    new FontSize { Val = FontSizeHalfPts }
                );
                if (bold) rp.AppendChild(new Bold());

                var run = new Run(rp, new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });
                para.AppendChild(run);

                if (i < lines.Length - 1)
                    para.AppendChild(new Run(new Break()));
            }
            return para;
        }

        private static Run BuildRun(string text, bool bold)
        {
            var rp = new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new FontSize { Val = TitleFontSizeHalfPts }
            );
            if (bold) rp.AppendChild(new Bold());
            return new Run(rp, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        }
    }
}
