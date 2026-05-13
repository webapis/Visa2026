#!/usr/bin/env dotnet-script
#r "nuget: DocumentFormat.OpenXml, 3.3.0"

using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;

// ── Column definitions (from App_Business_Trip_Arrival_BusinessTrip_map.md) ──────────────────────
// Total printable width: A4 landscape at 1cm margins ≈ 15600 twips
// Original DevExpress units: 969F total — each ≈ 16.1 twips

var columns = new (string Header, string Placeholder, int WidthTwips, bool Centered)[]
{
    ("№",                           "{{#foreach rows}}\n{{i+1}}",                                                                       450,  true),
    ("Familiýasy",                  "{{rows.Person_LastName}}",                                                                         1160, false),
    ("Ady",                         "{{rows.Person_FirstName}}",                                                                        970,  false),
    ("Doglan senesi\nwe ýeri",      "{{rows.Person_DateOfBirthText}}\n{{rows.Person_BirthPlace}}",                                      1290, false),
    ("Jynsy",                       "{{rows.Person_GenderTm}}",                                                                         645,  true),
    ("Raýatlygy",                   "{{rows.Person_NationalityCode}}",                                                                  805,  true),
    ("Pasport belgisi\nwe möhleti", "{{rows.Passport_Number}}\n{{rows.Passport_ExpirationDateText}}",                                   1290, false),
    ("Wezipesi",                    "{{rows.Position_NameTm}}",                                                                         2335, false),
    ("Möhleti we\nGezekligi",       "{{rows.Visa_NumberAndType}}\n{{rows.Visa_StartDateText}}\n{{rows.Visa_ExpirationDateText}}",        1370, false),
    ("Türkmenistandaky\nsalgysy",   "{{rows.Address_FullAddress}}",                                                                     2660, false),
    ("Iş saparynda\nboljak salgysy","{{rows.BusinessTripAddress_FullAddress}}\n{{/foreach}}",                                           2625, false),
};

const string FontName = "Times New Roman";
const string FontSize = "24";        // 12pt = 24 half-points
const string TitleFontSize = "30";   // 15pt
const uint PageWidth  = 16838u;      // A4 landscape
const uint PageHeight = 11906u;
const uint Margin     = 720u;        // ~1.27 cm

// ── Build document ──────────────────────────────────────────────────────────────────────────────
var outputPath = @"c:\Users\webap\Documents\GitHub\Visa2026\Visa2026.Module\Resources\BusinessTrip_Sanawy.docx";
Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

using var ms = new MemoryStream();
using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
{
    var mainPart = doc.AddMainDocumentPart();
    mainPart.Document = new Document();
    var body = mainPart.Document.AppendChild(new Body());

    // ── Page layout ──
    body.AppendChild(new SectionProperties(
        new PageSize { Width = PageWidth, Height = PageHeight, Orient = PageOrientationValues.Landscape },
        new PageMargin { Top = (int)Margin, Bottom = (int)Margin, Left = Margin, Right = Margin }
    ));

    // ── Title ──
    body.InsertAt(new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { After = "200" }
        ),
        new Run(
            new RunProperties(
                new RunFonts { Ascii = FontName, HighAnsi = FontName },
                new FontSize { Val = TitleFontSize },
                new Bold()
            ),
            new Text("Daşary ýurt raýatlarynyň sanawy")
        )
    ), 0);

    // ── Table ──
    var table = new Table();
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

    var tblGrid = new TableGrid();
    foreach (var col in columns)
        tblGrid.AppendChild(new GridColumn { Width = col.WidthTwips.ToString() });
    table.AppendChild(tblGrid);

    // Header row
    var headerRow = new TableRow(new TableRowProperties(
        new TableRowHeight { Val = 600, HeightType = HeightRuleValues.AtLeast }
    ));
    foreach (var col in columns)
    {
        var cell = new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = col.WidthTwips.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "D9D9D9", Color = "auto" }
            )
        );
        cell.AppendChild(MakeParagraph(col.Header, col.Centered, bold: true));
        headerRow.AppendChild(cell);
    }
    table.AppendChild(headerRow);

    // Data row
    var dataRow = new TableRow(new TableRowProperties(
        new TableRowHeight { Val = 500, HeightType = HeightRuleValues.AtLeast }
    ));
    foreach (var col in columns)
    {
        var cell = new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = col.WidthTwips.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            )
        );
        cell.AppendChild(MakeParagraph(col.Placeholder, col.Centered, bold: false));
        dataRow.AppendChild(cell);
    }
    table.AppendChild(dataRow);

    body.InsertAt(table, 1); // after title

    // ── Signatory block ──
    int printableWidth = (int)(PageWidth - Margin * 2);
    body.InsertAt(new Paragraph(
        new ParagraphProperties(
            new Tabs(new TabStop { Val = TabStopValues.Right, Position = printableWidth }),
            new SpacingBetweenLines { Before = "400", After = "0" }
        ),
        MakeRun("{{Application_CompanyHead_PositionTm}}", TitleFontSize, bold: true),
        new Run(new TabChar()),
        MakeRun("{{Application_CompanyHead_FullName}}", TitleFontSize, bold: true)
    ), 2);

    mainPart.Document.Save();
}

File.WriteAllBytes(outputPath, ms.ToArray());
Console.WriteLine($"✓ Written: {outputPath}  ({ms.Length:N0} bytes)");

// ── Helpers ──────────────────────────────────────────────────────────────────────────────────────
Paragraph MakeParagraph(string text, bool centered, bool bold)
{
    var just = centered ? JustificationValues.Center : JustificationValues.Left;
    var para = new Paragraph(new ParagraphProperties(
        new Justification { Val = just },
        new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
        new Indentation { Left = "57", Right = "57" }
    ));
    var lines = text.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        para.AppendChild(MakeRun(lines[i], FontSize, bold));
        if (i < lines.Length - 1)
            para.AppendChild(new Run(new Break()));
    }
    return para;
}

Run MakeRun(string text, string halfPtSize, bool bold)
{
    var rp = new RunProperties(
        new RunFonts { Ascii = FontName, HighAnsi = FontName },
        new FontSize { Val = halfPtSize }
    );
    if (bold) rp.AppendChild(new Bold());
    return new Run(rp, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
}
