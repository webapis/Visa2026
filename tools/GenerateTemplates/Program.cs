using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

// ── Column definitions (App_Business_Trip_Arrival_BusinessTrip_map.md) ──────────────────────────
// A4 landscape, 1cm margins → printable ≈ 15400 twips
// Map total = 969 units → each unit ≈ 15.89 twips, widths rounded

var columns = new (string Header, string Placeholder, int Width, bool Centered)[]
{
    ("№",
     "{{#ds.rows}}\n{{.RowNumber}}",
     450, true),

    ("Familiýasy",
     "{{.Person_LastName}}",
     1160, false),

    ("Ady",
     "{{.Person_FirstName}}",
     970, false),

    ("Doglan senesi\nwe ýeri",
     "{{.Person_DateOfBirthText}}\n{{.Person_BirthPlace}}",
     1290, false),

    ("Jynsy",
     "{{.Person_GenderTm}}",
     645, true),

    ("Raýatlygy",
     "{{.Person_NationalityCode}}",
     805, true),

    ("Pasport belgisi\nwe möhleti",
     "{{.Passport_Number}}\n{{.Passport_ExpirationDateText}}",
     1290, false),

    ("Wezipesi",
     "{{.Position_NameTm}}",
     2335, false),

    ("Möhleti we\nGezekligi",
     "{{.Visa_NumberAndType}}\n{{.Visa_StartDateText}}\n{{.Visa_ExpirationDateText}}",
     1370, false),

    ("Türkmenistandaky\nsalgysy",
     "{{.Address_FullAddress}}",
     2660, false),

    ("Iş saparynda\nboljak salgysy",
     "{{.BusinessTripAddress_FullAddress}}\n{{/ds.rows}}",
     2625, false),
};

const uint   PW        = 16838; // A4 landscape width twips
const uint   PH        = 11906;
const uint   Mrg       = 720;   // ~1.27 cm

var outPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\BusinessTrip_Sanawy.docx"));

// Fallback: accept first CLI arg as explicit output path
if (args.Length > 0) outPath = args[0];

Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

using var ms = new MemoryStream();

using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
{
    var main = doc.AddMainDocumentPart();
    main.Document = new Document();
    var body = main.Document.AppendChild(new Body());

    // Title paragraph
    body.AppendChild(MakeTitle("Daşary ýurt raýatlarynyň sanawy"));

    // Table
    body.AppendChild(MakeTable(columns));

    // Signatory
    body.AppendChild(MakeSignatory((int)(PW - Mrg * 2)));

    // Page setup (SectionProperties must be last child of body)
    body.AppendChild(new SectionProperties(
        new PageSize { Width = PW, Height = PH, Orient = PageOrientationValues.Landscape },
        new PageMargin { Top = (int)Mrg, Bottom = (int)Mrg, Left = Mrg, Right = Mrg }
    ));

    main.Document.Save();
}

File.WriteAllBytes(outPath, ms.ToArray());
Console.WriteLine($"✓ {outPath}");
Console.WriteLine($"  {ms.Length:N0} bytes");

// ── BusinessTrip Arrival letter template ─────────────────────────────────────────────────────────
var arrivalLetterPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\BusinessTrip_Arrival_Letter.docx"));
if (args.Length > 1) arrivalLetterPath = args[1];
Directory.CreateDirectory(Path.GetDirectoryName(arrivalLetterPath)!);
var arrivalBytes = MakeBusinessTripLetterTemplate(isDeparture: false);
File.WriteAllBytes(arrivalLetterPath, arrivalBytes);
Console.WriteLine($"✓ {arrivalLetterPath}");
Console.WriteLine($"  {arrivalBytes.Length:N0} bytes");

// ── BusinessTrip Departure letter template ────────────────────────────────────────────────────────
var departureLetterPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\BusinessTrip_Departure_Letter.docx"));
if (args.Length > 2) departureLetterPath = args[2];
Directory.CreateDirectory(Path.GetDirectoryName(departureLetterPath)!);
var departureBytes = MakeBusinessTripLetterTemplate(isDeparture: true);
File.WriteAllBytes(departureLetterPath, departureBytes);
Console.WriteLine($"✓ {departureLetterPath}");
Console.WriteLine($"  {departureBytes.Length:N0} bytes");

// ── Helpers ─────────────────────────────────────────────────────────────────────────────────────

static Paragraph MakeTitle(string text)
{
    return new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Center },
            new SpacingBetweenLines { After = "200" }
        ),
        MakeRun(text, "30", bold: true)
    );
}

static Table MakeTable((string Header, string Placeholder, int Width, bool Centered)[] cols)
{
    var tbl = new Table();
    tbl.AppendChild(new TableProperties(
        new TableBorders(
            new TopBorder    { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
            new BottomBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
            new LeftBorder   { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
            new RightBorder  { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" },
            new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4, Space = 0, Color = "000000" }
        ),
        new TableWidth  { Width = "0",     Type = TableWidthUnitValues.Auto },
        new TableLayout { Type = TableLayoutValues.Fixed }
    ));

    var grid = new TableGrid();
    foreach (var c in cols) grid.AppendChild(new GridColumn { Width = c.Width.ToString() });
    tbl.AppendChild(grid);

    // Header row — TableHeader makes it repeat on every page
    var hRow = new TableRow(new TableRowProperties(
        new TableRowHeight { Val = 600, HeightType = HeightRuleValues.AtLeast },
        new TableHeader()
    ));
    foreach (var c in cols)
    {
        var cell = new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = c.Width.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center },
                new Shading { Val = ShadingPatternValues.Clear, Fill = "D9D9D9", Color = "auto" }
            )
        );
        cell.AppendChild(MakeMultilineParagraph(c.Header, c.Centered, bold: true));
        hRow.AppendChild(cell);
    }
    tbl.AppendChild(hRow);

    // Data row — CantSplit prevents row from being broken across pages
    var dRow = new TableRow(new TableRowProperties(
        new TableRowHeight { Val = 500, HeightType = HeightRuleValues.AtLeast },
        new CantSplit()
    ));
    foreach (var c in cols)
    {
        var cell = new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = c.Width.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }
            )
        );
        foreach (var para in MakeCellParagraphs(c.Placeholder, c.Centered))
            cell.AppendChild(para);
        dRow.AppendChild(cell);
    }
    tbl.AppendChild(dRow);

    return tbl;
}

static Paragraph MakeSignatory(int printableWidthTwips)
{
    return new Paragraph(
        new ParagraphProperties(
            new Tabs(new TabStop { Val = TabStopValues.Right, Position = printableWidthTwips }),
            new SpacingBetweenLines { Before = "400", After = "0" }
        ),
        MakeRun("{{ds.Application_CompanyHead_PositionTm}}", "30", bold: true),
        new Run(new TabChar()),
        MakeRun("{{ds.Application_CompanyHead_FullName}}", "30", bold: true)
    );
}

// Header cells: multi-line via <w:br> (static text, no DocxTemplater processing needed)
static Paragraph MakeMultilineParagraph(string text, bool centered, bool bold)
{
    var para = new Paragraph(new ParagraphProperties(
        new Justification { Val = centered ? JustificationValues.Center : JustificationValues.Left },
        new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
        new Indentation { Left = "57", Right = "57" }
    ));
    var lines = text.Split('\n');
    for (int i = 0; i < lines.Length; i++)
    {
        para.AppendChild(MakeRun(lines[i], "24", bold));
        if (i < lines.Length - 1)
            para.AppendChild(new Run(new Break()));
    }
    return para;
}

// Data cells: each \n-separated line becomes its own <w:p> paragraph.
// DocxTemplater requires {{#loop}} and {{/loop}} to be in their own paragraphs.
static IEnumerable<Paragraph> MakeCellParagraphs(string text, bool centered)
{
    var just = centered ? JustificationValues.Center : JustificationValues.Left;
    foreach (var line in text.Split('\n'))
    {
        var para = new Paragraph(new ParagraphProperties(
            new Justification { Val = just },
            new SpacingBetweenLines { Before = "0", After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto },
            new Indentation { Left = "57", Right = "57" }
        ));
        para.AppendChild(MakeRun(line, "24", false));
        yield return para;
    }
}

static Run MakeRun(string text, string halfPts, bool bold, bool italic = false, bool underline = false, string? colorHex = null, bool explicitNoUnderline = false)
{
    var rp = new RunProperties(
        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
        new FontSize { Val = halfPts }
    );
    if (bold) rp.AppendChild(new Bold());
    if (italic) rp.AppendChild(new Italic());
    if (underline) rp.AppendChild(new Underline { Val = UnderlineValues.Single });
    else if (explicitNoUnderline) rp.AppendChild(new Underline { Val = UnderlineValues.None });
    if (colorHex is not null) rp.AppendChild(new Color { Val = colorHex });
    return new Run(rp, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
}

// ── AppRegCheckIn letter template ────────────────────────────────────────────────────────────────
var regCheckInPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Reg_Check_In_Letter.docx"));
if (args.Length > 3) regCheckInPath = args[3];
Directory.CreateDirectory(Path.GetDirectoryName(regCheckInPath)!);
var regCheckInBytes = MakeSimpleLetterTemplate(
    bodyText:   "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň T\u00fcrkmenistana gelendigi seb\u00e4pli hasaba almagyňyzy Sizden ha\u00fdyş edy\u00e4ris.",
    includeResponsibility: true);
File.WriteAllBytes(regCheckInPath, regCheckInBytes);
Console.WriteLine($"✓ {regCheckInPath}");
Console.WriteLine($"  {regCheckInBytes.Length:N0} bytes");

// ── AppRegCheckInInternal letter template ─────────────────────────────────────────────────────────
var regCheckInInternalPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Reg_Check_In_Internal_Letter.docx"));
if (args.Length > 4) regCheckInInternalPath = args[4];
Directory.CreateDirectory(Path.GetDirectoryName(regCheckInInternalPath)!);
var regCheckInInternalBytes = MakeSimpleLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň ýaşaýan salgysyny {{ds.FromRegionName_Genitive}} {{ds.FromCityName_Ablative}} {{ds.ToRegionName_Genitive}} {{ds.ToCityName_Dative}} üýtgeýändigi sebäpli hasaba almagyňyzy Sizden haýyş edýäris.",
    includeResponsibility: true);
File.WriteAllBytes(regCheckInInternalPath, regCheckInInternalBytes);
Console.WriteLine($"✓ {regCheckInInternalPath}");
Console.WriteLine($"  {regCheckInInternalBytes.Length:N0} bytes");

// ── AppRegCheckOut letter template ───────────────────────────────────────────────────────────────
var regCheckOutPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Reg_Check_Out_Letter.docx"));
if (args.Length > 5) regCheckOutPath = args[5];
Directory.CreateDirectory(Path.GetDirectoryName(regCheckOutPath)!);
var regCheckOutBytes = MakeSimpleLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň T\u00fcrkmenistandan gidendigi seb\u00e4pli hasapdan doly \u00e7ykarmagynyzy Sizden ha\u00fdyş edy\u00e4ris.",
    includeResponsibility: true);
File.WriteAllBytes(regCheckOutPath, regCheckOutBytes);
Console.WriteLine($"✓ {regCheckOutPath}");
Console.WriteLine($"  {regCheckOutBytes.Length:N0} bytes");

// ── AppRegCheckOutInternal letter template ────────────────────────────────────────────────────────
var regCheckOutInternalPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Reg_Check_Out_Internal_Letter.docx"));
if (args.Length > 6) regCheckOutInternalPath = args[6];
Directory.CreateDirectory(Path.GetDirectoryName(regCheckOutInternalPath)!);
var regCheckOutInternalBytes = MakeSimpleLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň ýaşaýan salgysyny {{ds.FromRegionName_Genitive}} {{ds.FromCityName_Ablative}} {{ds.ToRegionName_Genitive}} {{ds.ToCityName_Dative}} üýtgeýändigi sebäpli hasapdan çykarmagyňyzy Sizden haýyş edýäris.",
    includeResponsibility: true);
File.WriteAllBytes(regCheckOutInternalPath, regCheckOutInternalBytes);
Console.WriteLine($"✓ {regCheckOutInternalPath}");
Console.WriteLine($"  {regCheckOutInternalBytes.Length:N0} bytes");

// ── AppRegExt letter template ─────────────────────────────────────────────────────────────────────
var regExtPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Reg_Ext_Letter.docx"));
if (args.Length > 7) regExtPath = args[7];
Directory.CreateDirectory(Path.GetDirectoryName(regExtPath)!);
var regExtBytes = MakeSimpleLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatlarynyň wiza m\u00f6hleti uzaldylandygy seb\u00e4pli hasaba alyş m\u00f6hletini uzaltmagyňyzy Sizden ha\u00fdyş edy\u00e4ris.",
    includeResponsibility: true);
File.WriteAllBytes(regExtPath, regExtBytes);
Console.WriteLine($"✓ {regExtPath}");
Console.WriteLine($"  {regExtBytes.Length:N0} bytes");

// ── AppRegInfoChangeAddress letter template ───────────────────────────────────────────────────────
var regInfoChangeAddressPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Reg_Info_Change_Address_Letter.docx"));
if (args.Length > 8) regInfoChangeAddressPath = args[8];
Directory.CreateDirectory(Path.GetDirectoryName(regInfoChangeAddressPath)!);
var regInfoChangeAddressBytes = MakeSimpleLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň ýaşaýan salgysyny çalyşandygy sebäpli täze öý salgysyna hasaba almagyňyzy Sizden haýyş edýäris.",
    includeResponsibility: true);
File.WriteAllBytes(regInfoChangeAddressPath, regInfoChangeAddressBytes);
Console.WriteLine($"✓ {regInfoChangeAddressPath}");
Console.WriteLine($"  {regInfoChangeAddressBytes.Length:N0} bytes");

// ── AppRegInfoChangePassport letter template ──────────────────────────────────────────────────────
var regInfoChangePassportPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Reg_Info_Change_Passport_Letter.docx"));
if (args.Length > 9) regInfoChangePassportPath = args[9];
Directory.CreateDirectory(Path.GetDirectoryName(regInfoChangePassportPath)!);
var regInfoChangePassportBytes = MakeSimpleLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň pasportyny çalyşmagy bilen baglanşykly hasaba durmagy\u0148 m\u00f6hletini täze pasportyna geçirmegini\u017eizi Sizden haýyş edýäris.",
    includeResponsibility: true);
File.WriteAllBytes(regInfoChangePassportPath, regInfoChangePassportBytes);
Console.WriteLine($"✓ {regInfoChangePassportPath}");
Console.WriteLine($"  {regInfoChangePassportBytes.Length:N0} bytes");

// ── AppInv letter template (Group A — Ministry recipient, Greeting, Maksady, attachments) ─────────
var appInvPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Inv_Letter.docx"));
if (args.Length > 10) appInvPath = args[10];
Directory.CreateDirectory(Path.GetDirectoryName(appInvPath)!);
var appInvBytes = MakeGroupALetterTemplate(
    body2Text: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen T\u00fcrkiýe Respublikasynyň \"{{ds.Company_Name}}\" kompaniýasyna degi\u015fli bolan sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatyna {{ds.VisaPeriod_NameTm}} m\u00f6hlet bilen {{ds.VisaCategory_NameTm}} çakylyk resmile\u015fdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.",
    attachmentsText: "Go\u015fundy: 1. Da\u015fary \u00fdurt ra\u00fdatlaryny\u0148 sanawy — {{ds.TotalPersonCount}}\n                2. {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň maglumaty");
File.WriteAllBytes(appInvPath, appInvBytes);
Console.WriteLine($"✓ {appInvPath}");
Console.WriteLine($"  {appInvBytes.Length:N0} bytes");

// ── AppInvAndWP letter template (Group A) ────────────────────────────────────────────────────────
var appInvAndWpPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Inv_And_WP_Letter.docx"));
if (args.Length > 11) appInvAndWpPath = args[11];
Directory.CreateDirectory(Path.GetDirectoryName(appInvAndWpPath)!);
var appInvAndWpBytes = MakeAppInvAndWPLetterTemplate();
File.WriteAllBytes(appInvAndWpPath, appInvAndWpBytes);
Console.WriteLine($"✓ {appInvAndWpPath}");
Console.WriteLine($"  {appInvAndWpBytes.Length:N0} bytes");

// ── AppInvFM letter template (Group B — static intro paragraphs + FM request) ────────────────────
var appInvFmPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Inv_FM_Letter.docx"));
if (args.Length > 12) appInvFmPath = args[12];
Directory.CreateDirectory(Path.GetDirectoryName(appInvFmPath)!);
var appInvFmBytes = MakeGroupBLetterTemplate(
    body3Text: "T\u00fcrkmenistanda\u00e7a\u00e4klerinde amala a\u015fyrl\u00fdan taslamalar utga\u015fdyrmak bo\u00fdun\u00e7a \"{{ds.Company_Name}}\" kompaniýasyna degi\u015fli h\u00fcn\u00e4rmeni\u0148 ma\u015fgala agzalaryna ýagny, hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatyna {{ds.FamilyMember_Relationship_NameTm}} ({{ds.SponsoringEmployee_FullName}} - {{ds.SponsoringEmployee_PositionTm}}) {{ds.VisaPeriod_NameTm}} m\u00f6hlet bilen {{ds.VisaCategory_NameTm}} çakylyk resmile\u015fdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.",
    attachmentsText: "Go\u015fundy: 1. Da\u015fary \u00fdurt ra\u00fdatlaryny\u0148 sanawy — {{ds.TotalPersonCount}}\n                2. {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň maglumaty");
File.WriteAllBytes(appInvFmPath, appInvFmBytes);
Console.WriteLine($"✓ {appInvFmPath}");
Console.WriteLine($"  {appInvFmBytes.Length:N0} bytes");

// ── AppCancelVisa letter template (Group D — fixed recipient: national migration chief) ───────────
var appCancelVisaPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Cancel_Visa_Letter.docx"));
if (args.Length > 13) appCancelVisaPath = args[13];
Directory.CreateDirectory(Path.GetDirectoryName(appCancelVisaPath)!);
var appCancelVisaBytes = MakeGroupDLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň wizasyny ýatyrmagy\u0148yzy Sizden haýyş edýäris.");
File.WriteAllBytes(appCancelVisaPath, appCancelVisaBytes);
Console.WriteLine($"✓ {appCancelVisaPath}");
Console.WriteLine($"  {appCancelVisaBytes.Length:N0} bytes");

// ── AppCancelVisaAndWP letter template (Group D) ─────────────────────────────────────────────────
var appCancelVisaAndWpPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Cancel_Visa_And_WP_Letter.docx"));
if (args.Length > 14) appCancelVisaAndWpPath = args[14];
Directory.CreateDirectory(Path.GetDirectoryName(appCancelVisaAndWpPath)!);
var appCancelVisaAndWpBytes = MakeGroupDLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.CancelPersonCount}} ({{ds.CancelPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň T\u00fcrkmenistany\u0148 ç\u00e4ginden çykyp gidendigi sebäpli {{ds.CancelPersonCount}} ({{ds.CancelPersonCountText}}) sany wizasyny we {{ds.CancelWPCount}} ({{ds.CancelWPCountText}}) sany işlemek üçin rugsatnamasyny ýatyrmagy\u0148yzy Sizden haýyş edýäris.");
File.WriteAllBytes(appCancelVisaAndWpPath, appCancelVisaAndWpBytes);
Console.WriteLine($"✓ {appCancelVisaAndWpPath}");
Console.WriteLine($"  {appCancelVisaAndWpBytes.Length:N0} bytes");

// ── AppCancelInvWP letter template (Group D) ──────────────────────────────────────────────────────
var appCancelInvWpPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Cancel_Inv_WP_Letter.docx"));
if (args.Length > 15) appCancelInvWpPath = args[15];
Directory.CreateDirectory(Path.GetDirectoryName(appCancelInvWpPath)!);
var appCancelInvWpBytes = MakeGroupDLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.CancelPersonCount}} ({{ds.CancelPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň {{ds.CancelWPCount}} ({{ds.CancelWPCountText}}) sany işlemek üçin rugsatnamasyny we {{ds.CancelInvCount}} ({{ds.CancelInvCountText}}) sany çakylygyny ýatyrmagy\u0148yzy Sizden haýyş edýäris.");
File.WriteAllBytes(appCancelInvWpPath, appCancelInvWpBytes);
Console.WriteLine($"✓ {appCancelInvWpPath}");
Console.WriteLine($"  {appCancelInvWpBytes.Length:N0} bytes");

// ── AppChangePassport letter template (Group D) ───────────────────────────────────────────────────
var appChangePassportPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Change_Passport_Letter.docx"));
if (args.Length > 16) appChangePassportPath = args[16];
Directory.CreateDirectory(Path.GetDirectoryName(appChangePassportPath)!);
var appChangePassportBytes = MakeGroupDLetterTemplate(
    bodyText: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň wizasyny köne pasportdan täze pasporta geçirip bermegiňizi Sizden haýyş edýäris.");
File.WriteAllBytes(appChangePassportPath, appChangePassportBytes);
Console.WriteLine($"✓ {appChangePassportPath}");
Console.WriteLine($"  {appChangePassportBytes.Length:N0} bytes");

// ── AppExitVisa letter template (Group A) ─────────────────────────────────────────────────────────
var appExitVisaPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Exit_Visa_Letter.docx"));
if (args.Length > 17) appExitVisaPath = args[17];
Directory.CreateDirectory(Path.GetDirectoryName(appExitVisaPath)!);
var appExitVisaBytes = MakeGroupALetterTemplate(
    body2Text: "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen T\u00fcrkiýe Respublikasynyň \"{{ds.Company_Name}}\" kompaniýasyna degi\u015fli bolan sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) da\u015fary \u00fdurt ra\u00fdaty wizalaryny\u0148 tamamlanýan senesine çenli öz jogapkärçiligine degi\u015fli bolan işleri doly tamamlap ýetişmeýändikleri sebäpli olara T\u00fcrkmenistany\u0148 Döwlet migrasiýa gullugy tarapyndan {{ds.VisaPeriod_NameTm}} möhleti bilen çykyş wizasyny resmile\u015fdirmek meselesinde ýardam bermegiňizi Sizden haýyş edýäris.",
    attachmentsText: "Go\u015fundy:   1. {{ds.TotalPersonCount}}-pasport kopiýalary,\n           2. Go\u015fundy ({{ds.TotalPersonCount}}-da\u015fary \u00fdurt ra\u00fdatynyň maglumaty)");
File.WriteAllBytes(appExitVisaPath, appExitVisaBytes);
Console.WriteLine($"✓ {appExitVisaPath}");
Console.WriteLine($"  {appExitVisaBytes.Length:N0} bytes");

// ── AppAdditionalWPLocation letter template (Group C) ────────────────────────────────────────────
var appAdditionalWpPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Additional_WP_Location_Letter.docx"));
if (args.Length > 18) appAdditionalWpPath = args[18];
Directory.CreateDirectory(Path.GetDirectoryName(appAdditionalWpPath)!);
var appAdditionalWpBytes = MakeGroupCLetterTemplate(
    body2Text: "Şertname esasynda, öňde goýlan wezipeleri ýetinlikli durmuşa geçirmek üçin hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen \"{{ds.Company_Name}}\" kompaniýasyna degi\u015fli bolan {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň {{ds.MovementPermitLocation_NameTm}} iş rugsatnamalarynyň berilmegine ýardam bermegiňizi Sizden haýyş edýäris.",
    attachmentsText: "Go\u015fundy: 1. Da\u015fary \u00fdurt ra\u00fdatlaryny\u0148 sanawy — {{ds.TotalPersonCount}}\n                2. {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň maglumaty");
File.WriteAllBytes(appAdditionalWpPath, appAdditionalWpBytes);
Console.WriteLine($"✓ {appAdditionalWpPath}");
Console.WriteLine($"  {appAdditionalWpBytes.Length:N0} bytes");

// ── AppBorderZonePermission letter template (Group C) ────────────────────────────────────────────
var appBorderZonePath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Border_Zone_Permission_Letter.docx"));
if (args.Length > 19) appBorderZonePath = args[19];
Directory.CreateDirectory(Path.GetDirectoryName(appBorderZonePath)!);
var appBorderZoneBytes = MakeGroupCLetterTemplate(
    body2Text: "Şertname esasynda, öňde goýlan wezipeleri ýetinlikli durmuşa geçirmek üçin hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen \"{{ds.Company_Name}}\" kompaniýasyna degi\u015fli bolan {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň {{ds.BorderZoneLocation_NameTm}} serhet ýaka wizasynyň resmile\u015fdirilmegine ýardam bermegiňizi Sizden haýyş edýäris.",
    attachmentsText: "Go\u015fundy: 1. Da\u015fary \u00fdurt ra\u00fdatlaryny\u0148 sanawy — {{ds.TotalPersonCount}}\n                2. {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň maglumaty");
File.WriteAllBytes(appBorderZonePath, appBorderZoneBytes);
Console.WriteLine($"✓ {appBorderZonePath}");
Console.WriteLine($"  {appBorderZoneBytes.Length:N0} bytes");

// ── AppChangeInv letter template (custom — Group D recipient + invitation table) ─────────────────
var appChangeInvPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Change_Inv_Letter.docx"));
if (args.Length > 20) appChangeInvPath = args[20];
Directory.CreateDirectory(Path.GetDirectoryName(appChangeInvPath)!);
var appChangeInvBytes = MakeChangeInvLetterTemplate();
File.WriteAllBytes(appChangeInvPath, appChangeInvBytes);
Console.WriteLine($"✓ {appChangeInvPath}");
Console.WriteLine($"  {appChangeInvBytes.Length:N0} bytes");

// ── AppVisaExtFM letter template (Group B) ───────────────────────────────────────────────────────
var appVisaExtFmPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Visa_Ext_FM_Letter.docx"));
if (args.Length > 21) appVisaExtFmPath = args[21];
Directory.CreateDirectory(Path.GetDirectoryName(appVisaExtFmPath)!);
var appVisaExtFmBytes = MakeGroupBLetterTemplate(
    body3Text: "T\u00fcrkmenistanda\u00e7a\u00e4klerinde amala a\u015fyrl\u00fdan taslamalar utga\u015fdyrmak bo\u00fdun\u00e7a \"{{ds.Company_Name}}\" kompaniýasyna degi\u015fli h\u00fcn\u00e4rmeni\u0148 ma\u015fgala agzalaryna ýagny, hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatyna {{ds.FamilyMember_Relationship_NameTm}} wiza möhletine görä ({{ds.SponsoringEmployee_FullName}} - {{ds.SponsoringEmployee_PositionTm}}) {{ds.VisaCategory_NameTm}} wizalarynyň möhletiniň uzaldylmagyna ýardam bermegiňizi Sizden haýyş edýäris.",
    attachmentsText: "Go\u015fundy: 1. Da\u015fary \u00fdurt ra\u00fdatlaryny\u0148 sanawy — {{ds.TotalPersonCount}}\n                2. {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň maglumaty");
File.WriteAllBytes(appVisaExtFmPath, appVisaExtFmBytes);
Console.WriteLine($"✓ {appVisaExtFmPath}");
Console.WriteLine($"  {appVisaExtFmBytes.Length:N0} bytes");

// ── App_Sanawy shared 14-column landscape personnel list template ─────────────────────────────────
var appSanawyPath = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory,
        @"..\..\..\..\..\Visa2026.Module\Resources\App_Sanawy_Letter.docx"));
if (args.Length > 22) appSanawyPath = args[22];
Directory.CreateDirectory(Path.GetDirectoryName(appSanawyPath)!);
var appSanawyBytes = MakeSanawyTemplate();
File.WriteAllBytes(appSanawyPath, appSanawyBytes);
Console.WriteLine($"✓ {appSanawyPath}");
Console.WriteLine($"  {appSanawyBytes.Length:N0} bytes");

// ── AppCancelInvWPItem — 13-col portrait sanawy ───────────────────────────────────────────────────
var appCancelInvWpItemPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    @"..\..\..\..\..\Visa2026.Module\Resources\App_Cancel_Inv_WP_Item.docx"));
if (args.Length > 23) appCancelInvWpItemPath = args[23];
Directory.CreateDirectory(Path.GetDirectoryName(appCancelInvWpItemPath)!);
var appCancelInvWpItemBytes = MakeItemTableTemplate(portrait: true, new[]
{
    ("№",                      "{{#ds.rows}}{{ds.rows.RowNo}}",               300),
    ("AS-№",                   "{{ds.rows.WorkPermit_Number}}",               1100),
    ("Tassyk-nama belgisi",    "{{ds.rows.WorkPermit_ASNumber}}",              880),
    ("Familiýasy",             "{{ds.rows.Person_LastName}}",                  970),
    ("Ady",                    "{{ds.rows.Person_FirstName}}",                 800),
    ("Doglan senesi we şurdy", "{{ds.rows.Person_DateOfBirthText}}\n{{ds.rows.Person_CountryOfBirthTm}}/{{ds.rows.Person_BirthPlace}}", 1230),
    ("Pasport belgisi",        "{{ds.rows.Passport_Number}}",                 1100),
    ("Hünäri we bilimi",       "{{ds.rows.Education_LevelTm}}, {{ds.rows.Position_PositionTm}}", 2580),
    ("Hereket edýän çägi",     "{{ds.rows.WorkPermit_WorkPermittedLocations}}", 1420),
    ("Rugsat edilen möhleti",  "{{ds.rows.WorkPermit_StartDateText}}\n{{ds.rows.WorkPermit_ExpirationDateText}}", 970),
    ("Çakylyk belgisi",        "{{ds.rows.Invitation_Number}}",               1060),
    ("Çakylygyň resmil. senesi","{{ds.rows.Invitation_StartDateText}}",       1100),
    ("Çakylygyň möh. tamam. sene","{{ds.rows.Invitation_ExpirationDateText}}{{/ds.rows}}", 1102),
});
File.WriteAllBytes(appCancelInvWpItemPath, appCancelInvWpItemBytes);
Console.WriteLine($"✓ {appCancelInvWpItemPath}");

// ── AppCancelVisaAndWPItem — 12-col landscape sanawy ──────────────────────────────────────────────
var appCancelVisaAndWpItemPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    @"..\..\..\..\..\Visa2026.Module\Resources\App_Cancel_Visa_And_WP_Item.docx"));
if (args.Length > 24) appCancelVisaAndWpItemPath = args[24];
Directory.CreateDirectory(Path.GetDirectoryName(appCancelVisaAndWpItemPath)!);
var appCancelVisaAndWpItemBytes = MakeItemTableTemplate(portrait: false, new[]
{
    ("№",                       "{{#ds.rows}}{{ds.rows.RowNo}}",              280),
    ("AS-№",                    "{{ds.rows.WorkPermit_Number}}",              1100),
    ("Tassyk-nama belgisi",     "{{ds.rows.WorkPermit_ASNumber}}",             880),
    ("Familiýasy",              "{{ds.rows.Person_LastName}}",                 970),
    ("Ady",                     "{{ds.rows.Person_FirstName}}",                800),
    ("Doglan senesi we ýurdy",  "{{ds.rows.Person_DateOfBirthText}}\n{{ds.rows.Person_CountryOfBirthTm}}/{{ds.rows.Person_BirthPlace}}", 1230),
    ("Pasport belgisi",         "{{ds.rows.Passport_Number}}",                1100),
    ("Hünäri we bilimi",        "{{ds.rows.Education_LevelTm}}, {{ds.rows.Position_PositionTm}}", 2325),
    ("Hereket edýän çägi",      "{{ds.rows.WorkPermit_WorkPermittedLocations}}", 1290),
    ("Rugsat edililen möhleti", "{{ds.rows.WorkPermit_StartDateText}}\n{{ds.rows.WorkPermit_ExpirationDateText}}", 970),
    ("Wiza belgisi",            "{{ds.rows.Visa_Number}}",                     970),
    ("Wiza möhleti başl./tamam.", "{{ds.rows.Visa_StartDateText}}\n{{ds.rows.Visa_ExpirationDateText}}{{/ds.rows}}", 2680),
});
File.WriteAllBytes(appCancelVisaAndWpItemPath, appCancelVisaAndWpItemBytes);
Console.WriteLine($"✓ {appCancelVisaAndWpItemPath}");

// ── AppChangeInvItem — 13-col landscape sanawy ────────────────────────────────────────────────────
var appChangeInvItemPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    @"..\..\..\..\..\Visa2026.Module\Resources\App_Change_Inv_Item.docx"));
if (args.Length > 25) appChangeInvItemPath = args[25];
Directory.CreateDirectory(Path.GetDirectoryName(appChangeInvItemPath)!);
var appChangeInvItemBytes = MakeItemTableTemplate(portrait: false, new[]
{
    ("№",                       "{{#ds.rows}}{{ds.rows.RowNo}}",              260),
    ("Familiýasy",              "{{ds.rows.Person_LastName}}",                 910),
    ("Ady",                     "{{ds.rows.Person_FirstName}}",                780),
    ("Doglan senesi we ýurdy",  "{{ds.rows.Person_DateOfBirthText}}\n{{ds.rows.Person_CountryOfBirthTm}}/{{ds.rows.Person_BirthPlace}}", 1170),
    ("Jynsy",                   "{{ds.rows.Person_GenderTm}}",                 540),
    ("Raýatlygy",               "{{ds.rows.Person_NationalityCode}}",           540),
    ("Pasport belgisi we möhleti","{{ds.rows.Passport_Number}}\n{{ds.rows.Passport_ExpirationDateText}}", 1100),
    ("Bilimi we okan ýeri",     "{{ds.rows.Education_LevelTm}}\n{{ds.rows.Education_InstitutionName}}", 1230),
    ("Bilimine görä hünäri",    "{{ds.rows.Education_SpecialtyTm}}",           1230),
    ("Wezipesi",                "{{ds.rows.Position_PositionTm}}",             1550),
    ("Türkmenistandaky salgysy","{{ds.rows.Address_FullAddress}}",             1874),
    ("Daşary ýurtdaky salgysy", "{{ds.rows.Person_ForeignAddress}}",           1744),
    ("Barjak serhet ýakasy",    "{{ds.rows.WorkPermit_WorkPermittedLocations}}{{/ds.rows}}", 1680),
});
File.WriteAllBytes(appChangeInvItemPath, appChangeInvItemBytes);
Console.WriteLine($"✓ {appChangeInvItemPath}");

// ── AppBorderZonePermissionItem — 11-col landscape sanawy ────────────────────────────────────────
var appBorderZoneItemPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    @"..\..\..\..\..\Visa2026.Module\Resources\App_Border_Zone_Permission_Item.docx"));
if (args.Length > 26) appBorderZoneItemPath = args[26];
Directory.CreateDirectory(Path.GetDirectoryName(appBorderZoneItemPath)!);
var appBorderZoneItemBytes = MakeItemTableTemplate(portrait: false, new[]
{
    ("№",                        "{{#ds.rows}}{{ds.rows.RowNo}}",             325),
    ("Familiýasy",               "{{ds.rows.Person_LastName}}",                975),
    ("Ady",                      "{{ds.rows.Person_FirstName}}",               845),
    ("Doglan senesi we ýeri",    "{{ds.rows.Person_DateOfBirthText}}\n{{ds.rows.Person_CountryOfBirthTm}}/{{ds.rows.Person_BirthPlace}}", 1105),
    ("Jynsy",                    "{{ds.rows.Person_GenderTm}}",                520),
    ("Raýatlygy",                "{{ds.rows.Person_NationalityCode}}",          585),
    ("Pasport belgisi we möhleti","{{ds.rows.Passport_Number}}\n{{ds.rows.Passport_ExpirationDateText}}", 1105),
    ("Wezipesi",                 "{{ds.rows.Position_PositionTm}}",            1560),
    ("Möhleti we gezekligi",     "{{ds.rows.WorkPermit_Number}}\nWP\n{{ds.rows.WorkPermit_StartDateText}}\n{{ds.rows.WorkPermit_ExpirationDateText}}", 1300),
    ("Türkmenistandaky salgysy", "{{ds.rows.Address_FullAddress}}",            2795),
    ("Barjak serhet ýakasy",     "{{ds.rows.Application_BorderZoneLocation_NameTm}}{{/ds.rows}}", 3283),
});
File.WriteAllBytes(appBorderZoneItemPath, appBorderZoneItemBytes);
Console.WriteLine($"✓ {appBorderZoneItemPath}");

// ── AppInvAndWPBorcnamaItem — per-person commitment form ──────────────────────────────────────────
var borcnamaPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    @"..\..\..\..\..\Visa2026.Module\Resources\App_Inv_And_WP_Borcnama_Item.docx"));
if (args.Length > 27) borcnamaPath = args[27];
Directory.CreateDirectory(Path.GetDirectoryName(borcnamaPath)!);
var borcnamaBytes = MakeBorcnamaTemplate();
File.WriteAllBytes(borcnamaPath, borcnamaBytes);
Console.WriteLine($"✓ {borcnamaPath}");

// Labor contract: authored under Visa2026.Module/Resources/Templates/Contract_uzt.docx (user template seed).

// Energy → Construction ministry letter: authored under Resources/Templates/433_MINSTROY_uzt.docx (user template seed).

// GT-15 Çalık → Migration letter: authored under Resources/Templates/Sazakow_uzt.docx (user template seed).

// ── Letter template helpers ───────────────────────────────────────────────────────────────────────

/// <summary>
/// Builds a standard Group E letter template:
/// app number + date (right) → recipient → body paragraph(s) → optional responsibility → signatory.
/// bodyParts: each element becomes one justified paragraph with first-line indent.
/// boldParts: optional inline bold segments within body; if null the whole bodyText is plain.
/// </summary>
static byte[] MakeSimpleLetterTemplate(string bodyText, bool includeResponsibility = true, string? maksadyField = null)
{
    const uint PW_P = 11906;
    const uint PH_P = 16838;
    const uint MrgL  = 1800;
    const uint MrgR  = 1800;
    const uint MrgT  = 1440;
    const uint MrgB  = 1440;

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        body.AppendChild(MakeLetterRun("{{ds.FullApplicationNumber}}", rightAlign: true, bold: true));
        body.AppendChild(MakeLetterRun("{{ds.ApplicationDate}} ý.", rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeLetterRun("{{ds.MigrationService_NameTm}}", rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeJustifiedParagraph(bodyText));
        if (maksadyField != null)
            body.AppendChild(MakeMaksadyParagraph("Maksady:", maksadyField));
        if (includeResponsibility)
            body.AppendChild(MakeJustifiedParagraph(FormalCompanyLetterLayout.ResponsibilityPlain));
        body.AppendChild(new Paragraph());
        AppendSignatoryLetter(body);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Group A letter template: app number+date (right) → Ministry recipient (left bold) →
/// Urgency (italic, optional) → Greeting (center bold) → Body1 = ProjectContract_Description →
/// Body2 = caller-supplied paragraph → Responsibility → Attachments → Signatory.
/// </summary>
static byte[] MakeGroupALetterTemplate(string body2Text, string attachmentsText)
{
    const uint PW_P = 11906;
    const uint PH_P = 16838;
    const uint MrgL  = 1800;
    const uint MrgR  = 1800;
    const uint MrgT  = 1440;
    const uint MrgB  = 1440;

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        // Header: app number + date right-aligned
        body.AppendChild(MakeLetterRun("{{ds.FullApplicationNumber}}", rightAlign: true, bold: true));
        body.AppendChild(MakeLetterRun("{{ds.ApplicationDate}} ý.", rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());

        // Recipient block — left-aligned bold (Ministry)
        body.AppendChild(MakeLetterRun("{{ds.ProjectContract_Ministry_RecipientBlock}}", rightAlign: false, bold: true));
        body.AppendChild(new Paragraph());

        // Urgency line — italic, centre
        body.AppendChild(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = "30" },
                    new Italic()
                ),
                new Text("{{ds.Urgency_NameTm}}") { Space = SpaceProcessingModeValues.Preserve }
            )
        ));
        body.AppendChild(new Paragraph());

        // Greeting — centre bold
        body.AppendChild(MakeLetterRun("{{ds.ProjectContract_Ministry_FormOfAddress}}", rightAlign: false, bold: true));
        body.AppendChild(new Paragraph());

        // Body1 — Maksady (project contract description)
        body.AppendChild(MakeMaksadyParagraph("Maksady:", "{{ds.ProjectContract_Description}}"));

        // Body2 — invitation request
        body.AppendChild(MakeJustifiedParagraph(body2Text));

        // Responsibility
        body.AppendChild(MakeJustifiedParagraph(FormalCompanyLetterLayout.ResponsibilityPlain));
        body.AppendChild(new Paragraph());

        // Attachments line
        body.AppendChild(MakeJustifiedParagraph(attachmentsText));
        body.AppendChild(new Paragraph());

        // Signatory
        AppendSignatoryLetter(body);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Ministry addressee: full-width borderless table — a **wide left spacer** cell and a **right-hand address** cell
/// whose width is **capped** so short two-line blocks sit on the **right**; long lines wrap inside that column.
/// </summary>
static void AppendMinistryRecipientBlockRightColumnTable(Body body, int printableWidthTwips)
{
    // Wide address column + tiny spacer (old layout) left short ministry lines sitting left of page center.
    // Cap the address column and give the rest to the left cell so the two-line block sits on the **right**;
    // paragraphs stay left-justified inside that column (one shared left edge).
    var addressCol = Math.Min(
        FormalCompanyLetterLayout.MinistryRecipientTableAddressColumnMaxTwips,
        printableWidthTwips - FormalCompanyLetterLayout.MinistryRecipientTableMinSpacerTwips);
    if (addressCol < FormalCompanyLetterLayout.MinistryRecipientTableAddressColumnMinTwips)
        addressCol = FormalCompanyLetterLayout.MinistryRecipientTableAddressColumnMinTwips;
    var spacerCol = printableWidthTwips - addressCol;
    if (spacerCol < FormalCompanyLetterLayout.MinistryRecipientTableMinSpacerTwips)
    {
        spacerCol = FormalCompanyLetterLayout.MinistryRecipientTableMinSpacerTwips;
        addressCol = printableWidthTwips - spacerCol;
    }

    static TableBorders NilTableBorders() => new TableBorders(
        new TopBorder { Val = BorderValues.Nil },
        new LeftBorder { Val = BorderValues.Nil },
        new BottomBorder { Val = BorderValues.Nil },
        new RightBorder { Val = BorderValues.Nil },
        new InsideHorizontalBorder { Val = BorderValues.Nil },
        new InsideVerticalBorder { Val = BorderValues.Nil });

    static TableCellBorders NilCellBorders() => new TableCellBorders(
        new TopBorder { Val = BorderValues.Nil },
        new LeftBorder { Val = BorderValues.Nil },
        new BottomBorder { Val = BorderValues.Nil },
        new RightBorder { Val = BorderValues.Nil });

    var spacerCell = new TableCell(
        new TableCellProperties(
            new TableCellWidth { Width = spacerCol.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
            NilCellBorders()),
        new Paragraph(new ParagraphProperties()));

    var line1 = new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Left },
            new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPHeaderLineAfterTwips }),
        MakeRun("{{ds.ProjectContract_Ministry_RecipientBlock_Line1}}", "30", bold: true));

    var line2 = new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Left },
            new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPRecipientBlockEndAfterTwips }),
        MakeRun(
            "{?{ds.ProjectContract_Ministry_RecipientBlock_HasLine2}}{{ds.ProjectContract_Ministry_RecipientBlock_Line2}}{{/}}",
            "30",
            bold: true));

    var addressCell = new TableCell(
        new TableCellProperties(
            new TableCellWidth { Width = addressCol.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
            NilCellBorders()),
        line1,
        line2);

    body.AppendChild(new Table(
        new TableProperties(
            new TableWidth { Width = printableWidthTwips.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableLayout { Type = TableLayoutValues.Fixed },
            NilTableBorders()),
        new TableGrid(
            new GridColumn { Width = spacerCol.ToString() },
            new GridColumn { Width = addressCol.ToString() }),
        new TableRow(spacerCell, addressCell)));
}

/// <summary>
/// Letterhead block: № + date (left); ministry addressee in a right-hand table column; urgency (left) after the block.
/// </summary>
/// <param name="conditionalUrgency">When true, urgency text is wrapped in DocxTemplater <c>{?{ds.ApplicationType_ShowUrgency}}…{{/}}</c> (Group C types that hide urgency in XAF).</param>
static void AppendMinistrySteppedHeaderWithUrgency(Body body, bool conditionalUrgency, int printableWidthTwips)
{
    body.AppendChild(new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Left },
            new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPHeaderLineAfterTwips }),
        MakeRun("{{ds.FullApplicationNumber}}", "30", true)));
    body.AppendChild(new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Left },
            new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPHeaderLineAfterTwips }),
        MakeRun("{{ds.ApplicationDate}} ý.", "30", true)));
    AppendMinistryRecipientBlockRightColumnTable(body, printableWidthTwips);
    body.AppendChild(new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Left },
            new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPHeaderSalutationGapAfterTwips }),
        conditionalUrgency
            ? new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = "24" },
                    new Italic(),
                    new Color { Val = "000000" },
                    new Underline { Val = UnderlineValues.None }),
                new Text("{?{ds.ApplicationType_ShowUrgency}}{{ds.Urgency_NameTm}}{{/}}") { Space = SpaceProcessingModeValues.Preserve })
            : MakeRun("{{ds.Urgency_NameTm}}", "24", bold: false, italic: true, underline: false, colorHex: "000000", explicitNoUnderline: true)));
}

static void AppendAppInvAndWPHeader(Body body) =>
    AppendMinistrySteppedHeaderWithUrgency(body, conditionalUrgency: false, FormalCompanyLetterLayout.AppInvAndWPPrintableWidthTwips);

static void AppendAppVisaAndWPExtHeader(Body body) =>
    AppendMinistrySteppedHeaderWithUrgency(body, conditionalUrgency: true, FormalCompanyLetterLayout.AppInvAndWPPrintableWidthTwips);

/// <summary>
/// App Inv+WP letter — typography/layout aligned with the ministry reference scan
/// (<c>Resources/FormTemplates/App_Inv_And_WP_app.jpg</c>). Data fields match
/// <c>Application</c> / <c>AppInvAndWPLetterReportDef</c> (see <c>App_Inv_And_WP_app_map.md</c>).
/// Where the scan differs from XAF RTF (e.g. no first-line indent on Maksady body; bold company name only in request paragraph; plain counts/period/category), Word follows the scan. Salutation is **bold only** (no underline) per product standard though the scan is underlined. Urgency is **italic, black, no underline** (scan shows red + underline).
/// Header: № + date (left); ministry addressee in a right-hand table column (left-aligned lines, shared left edge); urgency (left) after addressee.
/// Does not embed corporate letterhead/footer artwork. Goşundy list follows ministry sample
/// (<c>App_Inv_And_WP_app.jpg</c>): passport copies + foreign-citizen info — not the XAF <c>xrLabelAttachments</c> sanawy wording.
/// </summary>
static byte[] MakeAppInvAndWPLetterTemplate()
{
    const uint PW_P = FormalCompanyLetterLayout.LetterPortraitPageWidthTwips;
    const uint PH_P = 16838;
    const uint MrgL  = FormalCompanyLetterLayout.AppInvAndWPLetterMarginLeftTwips;
    const uint MrgR  = FormalCompanyLetterLayout.AppInvAndWPLetterMarginRightTwips;
    const uint MrgT  = 1440;
    const uint MrgB  = 1440;

    // Two paragraphs — Word needs <w:br/> or separate <w:p> for line breaks (raw \n in one run is unreliable).
    static Paragraph AttachLine(string text, string? spacingAfterTwips = null)
    {
        return new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Left },
                new SpacingBetweenLines { After = spacingAfterTwips ?? "0" }),
            MakeRun(text, "30", false));
    }

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        AppendAppInvAndWPHeader(body);

        // Salutation — centered bold (no underline; ministry scan shows underline but product standard drops it)
        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPSalutationAfterTwips }
            ),
            MakeRun("{{ds.ProjectContract_Ministry_FormOfAddress}}", "30", bold: true, underline: false, colorHex: "000000", explicitNoUnderline: true)
        ));

        body.AppendChild(MakeInvAndWPContractDescriptionParagraph());
        body.AppendChild(MakeInvAndWPRequestBodyParagraph());
        body.AppendChild(new Paragraph(
            InvAndWPLetterBodyParagraphProperties(FormalCompanyLetterLayout.InvAndWPResponsibilityParagraphAfterTwips),
            MakeRun(FormalCompanyLetterLayout.ResponsibilityPlain, "30", false)));
        body.AppendChild(AttachLine("Go\u015fundy: 1. {{ds.TotalPersonCount}}-pasport kopi\u00fdalary,"));
        body.AppendChild(AttachLine("                2. Go\u015fundy ({{ds.TotalPersonCount}}-da\u015fary \u00fdurt ra\u00fdatyny\u0148 maglumaty)", FormalCompanyLetterLayout.InvAndWPBeforeSignatoryGapAfterTwips));
        AppendSignatoryLetter(body, FormalCompanyLetterLayout.AppInvAndWPPrintableWidthTwips, FormalCompanyLetterLayout.InvAndWPSignatoryParagraphSpaceBeforeTwips);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = (int)MrgL, Right = (int)MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>Shared <c>w:pPr</c> for justified body paragraphs (same first-line indent). <paramref name="afterTwips"/> defaults to standard body gap.</summary>
static ParagraphProperties InvAndWPLetterBodyParagraphProperties(string? afterTwips = null) =>
    new ParagraphProperties(
        new Justification { Val = JustificationValues.Both },
        new Indentation { FirstLine = FormalCompanyLetterLayout.JustifiedBodyFirstLineIndentTwips },
        new SpacingBetweenLines { After = afterTwips ?? FormalCompanyLetterLayout.InvAndWPBodyParagraphAfterTwips });

/// <summary>
/// First body block: <c>ProjectContract.Description</c> via <c>{{ds.ProjectContract_Description}}</c> only (no "Maksady:" label — ministry output omits it).
/// Fully justified with first-line indent 720 twips (~0.5 in), same as other letter body paragraphs and XAF <c>\fi720</c>.
/// </summary>
static Paragraph MakeInvAndWPContractDescriptionParagraph()
{
    return new Paragraph(
        InvAndWPLetterBodyParagraphProperties(),
        MakeRun("{{ds.ProjectContract_Description}}", "30", bold: false));
}

/// <summary>
/// Second body paragraph for App Inv+WP — wording and placeholders per <c>App_Inv_And_WP_app_map.md</c> (xrRichBody2).
/// Typography follows <c>App_Inv_And_WP_app.jpg</c>: bold <c>Company_Name</c> only; counts, period, and category phrase plain (XAF RTF bolds those spans instead).
/// </summary>
static Paragraph MakeInvAndWPRequestBodyParagraph()
{
    var para = new Paragraph(InvAndWPLetterBodyParagraphProperties());
    para.AppendChild(MakeRun("Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen T\u00fcrki\u00fde Respublikasyny\u0148 \"", "30", false));
    para.AppendChild(MakeRun("{{ds.Company_Name}}", "30", true));
    para.AppendChild(MakeRun("\" kompani\u00fdasyna degi\u015fli bolan sanawdaky ", "30", false));
    para.AppendChild(MakeRun("{{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}})", "30", false));
    para.AppendChild(MakeRun(" sany da\u015fary \u00fdurt ra\u00fdatyna ", "30", false));
    para.AppendChild(MakeRun("{{ds.VisaPeriod_NameTm}} m\u00f6hlet", "30", false));
    para.AppendChild(MakeRun(" bilen ", "30", false));
    para.AppendChild(MakeRun("{{ds.VisaCategory_NameTm}} \u00e7akylyk we i\u015f rugsatnamasyny", "30", false));
    para.AppendChild(MakeRun(" resmile\u015fdirilmegine \u00fdardam bermegi\u0148izi Sizden ha\u00fdy\u015f ed\u00fd\u00e4ris.", "30", false));
    return para;
}

/// <summary>
/// Borçnama (commitment) per-person form for App_Inv_And_WP.
/// Mirrors XAF AppInvAndWPBorcnamaItemReport and
/// <c>Resources/FormTemplates/App_Inv_And_WP_item_borcnama.png</c> — header, worker row, xrRichBody text, signatures.
/// Margins and rules stay within text column; legal body uses compact line spacing for one A4 page with typical data.
/// {{:s:}}{{:PageBreak}} between items only (DocxTemplater).
/// </summary>
static byte[] MakeBorcnamaTemplate()
{
    // xrRichBody wording split into shorter blocks (same content as single xrRichBody in XAF report).
    const string bodyP1a =
        "Türkmenistanyň kanunçylygyny berjaý etmäge, üpjün etmäge, Daşary ýurt raýatyna degişli bolan maglumatlaryň üýtgemegi " +
        "(iş ýeriniň, wezipesiniň ýa-da salgysynyň üçtgemegi, Türkmenistanda hereket etmegi, we ş.m.), barada Türkmenistanyň " +
        "Döwlet migrasiýa gullugyna 3 (üç) iş güniň dowamynda ýazmaça habar bermelidigine,";
    const string bodyP1b =
        "Iş beriji tarapyndan iş rugsatnamasy alýnandan soň, (gelenden soň) 30 senenama güniň dowamynda Türkmenistanyň " +
        "Döwlet migrasiýa gullugynyň edaralaryna işe çagyrylan daşary ýurt raýatynda adamyň immunýetýetmezlik wirusy sebäpli " +
        "döreýän keseliň (AIW ýokuşmasy) ýokdugy barada Türkmenistanyň ygtyýarly edaralary tarapyndan tassyklaýan kepilnamany getirilmägine,";
    const string bodyP1c = "Wizalaryň möhletini uzaltmak üçin resminamalaryny iki aý öňünden tabşyrmaga,";
    const string bodyP1d =
        "Wiza uzaltmak üçin tabşyrylan resminamalar boýunça kabul edilen çözgütleriň 45 güniň dowamynda ýerine ýetirmäge,";
    const string bodyP1e =
        "Türkmenistanyň kanunçylygynda göz öňünde tutulan hukuklaryny we borçlaryny öz wagtynda düşündirmäge borçlanýarys.";
    const string bodyP2 =
        "Daşary ýurt raýaty barada tabşyrylan resminamalarda galp maglumatlary görkezilen ýa-da galp resminamalar tabşyrylan " +
        "ýagdaýynda Türkmenistanyň kanunçylygynda bellenilen tertipde ýagtylykda doly jogapkärçilik çekýäris.";

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        const int pageW = 11906;
        const int mrgL = 1020, mrgR = 1020, mrgT = 720, mrgB = 720;
        const int contentW = pageW - mrgL - mrgR;
        const int wName = (int)(334f / 746f * contentW);
        const int wDob = (int)(168f / 746f * contentW);
        const int wResp = contentW - wName - wDob;

        static Paragraph P(string text, bool bold = false, bool italic = false,
            JustificationValues? just = null, int sz = 22,
            int indent = 0, int spaceBefore = 0, int spaceAfter = 0)
        {
            var rpr = new RunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = sz.ToString() }
            );
            if (bold) rpr.AppendChild(new Bold());
            if (italic) rpr.AppendChild(new Italic());
            var ppr = new ParagraphProperties(new Justification { Val = just ?? JustificationValues.Left });
            if (indent > 0) ppr.AppendChild(new Indentation { FirstLine = indent.ToString() });
            if (spaceBefore > 0 || spaceAfter > 0)
            {
                var sp = new SpacingBetweenLines();
                if (spaceBefore > 0) sp.Before = spaceBefore.ToString();
                if (spaceAfter > 0) sp.After = spaceAfter.ToString();
                ppr.AppendChild(sp);
            }
            return new Paragraph(ppr, new Run(rpr, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        }

        // Justified legal body — tight line spacing + small paragraph gaps so six blocks fit one A4 with signatures.
        static Paragraph BodyJustified(string text, int sz = 20, int firstLineTwips = 454, int lineTwips = 220)
        {
            var ppr = new ParagraphProperties(
                new Justification { Val = JustificationValues.Both },
                new Indentation { FirstLine = firstLineTwips.ToString() },
                new SpacingBetweenLines
                {
                    Before = "0",
                    After = "0",
                    Line = lineTwips.ToString(),
                    LineRule = LineSpacingRuleValues.AtLeast
                });
            var rpr = new RunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = sz.ToString() });
            return new Paragraph(ppr, new Run(rpr, new Text(text) { Space = SpaceProcessingModeValues.Preserve }));
        }

        static Paragraph FieldLineParagraph(string label, string fieldToken, JustificationValues just, int sz, bool italicField)
        {
            var ppr = new ParagraphProperties(new Justification { Val = just });
            var p = new Paragraph(ppr);
            if (!string.IsNullOrEmpty(label))
            {
                var rpl = new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = sz.ToString() });
                p.AppendChild(new Run(rpl, new Text(label) { Space = SpaceProcessingModeValues.Preserve }));
            }

            var rpf = new RunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = sz.ToString() });
            if (italicField)
                rpf.AppendChild(new Italic());
            p.AppendChild(new Run(rpf, new Text(fieldToken) { Space = SpaceProcessingModeValues.Preserve }));
            return p;
        }

        /// <summary>
        /// Single-column table: rule under the value row only; width = text column (<paramref name="tableWidthDxa"/>).
        /// Cell bottom padding keeps the border clear of following caption paragraphs outside the table.
        /// </summary>
        static Table FullWidthUnderlinedField(int tableWidthDxa, string label, string fieldToken,
            JustificationValues just, int sz = 20, bool italicField = true)
        {
            var inner = FieldLineParagraph(label, fieldToken, just, sz, italicField);
            var cellBorders = new TableCellBorders(
                new TopBorder { Val = BorderValues.Nil },
                new LeftBorder { Val = BorderValues.Nil },
                new RightBorder { Val = BorderValues.Nil },
                new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "000000" });
            const int cellPadLr = 60;
            const int cellPadBottom = 90;
            var cell = new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Width = tableWidthDxa.ToString(), Type = TableWidthUnitValues.Dxa },
                    cellBorders,
                    new TableCellMargin(
                        new LeftMargin { Width = cellPadLr.ToString(), Type = TableWidthUnitValues.Dxa },
                        new RightMargin { Width = cellPadLr.ToString(), Type = TableWidthUnitValues.Dxa },
                        new BottomMargin { Width = cellPadBottom.ToString(), Type = TableWidthUnitValues.Dxa })),
                inner);
            return new Table(
                new TableProperties(
                    new TableWidth { Width = tableWidthDxa.ToString(), Type = TableWidthUnitValues.Dxa },
                    new TableLayout { Type = TableLayoutValues.Fixed },
                    new TableBorders(
                        new TopBorder { Val = BorderValues.Nil },
                        new LeftBorder { Val = BorderValues.Nil },
                        new BottomBorder { Val = BorderValues.Nil },
                        new RightBorder { Val = BorderValues.Nil },
                        new InsideHorizontalBorder { Val = BorderValues.Nil },
                        new InsideVerticalBorder { Val = BorderValues.Nil })),
                new TableGrid(new GridColumn { Width = tableWidthDxa.ToString() }),
                new TableRow(cell));
        }

        static TableCellBorders CellBorders(bool bottomRule) => bottomRule
            ? new TableCellBorders(
                new TopBorder { Val = BorderValues.Nil },
                new LeftBorder { Val = BorderValues.Nil },
                new RightBorder { Val = BorderValues.Nil },
                new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "000000" })
            : new TableCellBorders(
                new TopBorder { Val = BorderValues.Nil },
                new LeftBorder { Val = BorderValues.Nil },
                new RightBorder { Val = BorderValues.Nil },
                new BottomBorder { Val = BorderValues.Nil });

        /// <summary>Name or DOB column: rule under value only; caption paragraph below the rule (inside cell).</summary>
        static TableCell WorkerValueCaptionCell(int widthDxa, string valuePlaceholder, string captionText)
        {
            var pprValue = new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new ParagraphBorders(
                    new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "000000" }),
                new SpacingBetweenLines { After = "36" });
            var valuePara = new Paragraph(pprValue,
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "20" },
                        new Italic()),
                    new Text(valuePlaceholder) { Space = SpaceProcessingModeValues.Preserve }));
            var pprCap = new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { Before = "18", After = "0" });
            var capPara = new Paragraph(pprCap,
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "14" },
                        new Italic()),
                    new Text(captionText) { Space = SpaceProcessingModeValues.Preserve }));
            return new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Width = widthDxa.ToString(), Type = TableWidthUnitValues.Dxa },
                    CellBorders(false),
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }),
                valuePara,
                capPara);
        }

        static TableCell WorkerRespPhraseCell(int widthDxa)
        {
            return new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Width = widthDxa.ToString(), Type = TableWidthUnitValues.Dxa },
                    CellBorders(false),
                    new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top }),
                new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                    new Run(
                        new RunProperties(
                            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                            new FontSize { Val = "20" }),
                        new Text("jogapkärçiligini öz üstümize alýarys:") { Space = SpaceProcessingModeValues.Preserve })));
        }

        // DocxTemplater row repeat open marker (own paragraph).
        body.AppendChild(P("{{#ds.rows}}", sz: 1));

        const int hdrSz = 20; // 10 pt
        body.AppendChild(P("Daşary ýurt raýatyna, raýatlygy bolmadyk adama", sz: hdrSz, just: JustificationValues.Center, spaceAfter: 10));
        body.AppendChild(P("Işçi (WP) wizasyny we iş rugsatnamasyny resmileşdirmek üçin talap edilen", sz: hdrSz, just: JustificationValues.Center, spaceAfter: 10));
        // Scan: next two lines are bold (context lines before BORÇNAMA title).
        body.AppendChild(P("Daşary ýurt raýatlaryň Türkmenistanda ýaşamagynyň", bold: true, sz: hdrSz, just: JustificationValues.Center, spaceAfter: 10));
        body.AppendChild(P("şertlerini üpjün etmek barada", bold: true, sz: hdrSz, just: JustificationValues.Center, spaceAfter: 18));
        body.AppendChild(P("BORÇNAMA", bold: true, sz: 28, just: JustificationValues.Center, spaceAfter: 18));

        body.AppendChild(FullWidthUnderlinedField(contentW, "", "{{ds.rows.Application_SponsorName}}",
            JustificationValues.Center, sz: 20));
        body.AppendChild(P("(kärhananyň ady, hukuk guramasyçylyk görnüşi)", italic: true, sz: 14, just: JustificationValues.Center, spaceBefore: 50, spaceAfter: 8));

        body.AppendChild(FullWidthUnderlinedField(contentW, "", "{{ds.rows.Application_CompanyRegistryAddressLine}}",
            JustificationValues.Center, sz: 20));
        body.AppendChild(P("(hasaba alynan belgisi, ýuridiki salgysy)", italic: true, sz: 14, just: JustificationValues.Center, spaceBefore: 50, spaceAfter: 14));

        body.AppendChild(P(
            "Ýokarda ady görkezilen kärhana tarapyndan Türkmenistanyň çäginde işlemek üçin çagyrylýan",
            sz: 20, spaceAfter: 10));

        // One row: name | DOB (paragraph rule under value; captions below rule) | responsibility phrase — top-aligned.
        var workerRow = new TableRow(
            WorkerValueCaptionCell(wName, "{{ds.rows.Person_FullName}}", "(ady, familliýasy, atasynyň ady, doglan senesi)"),
            WorkerValueCaptionCell(wDob, "{{ds.rows.Person_DateOfBirthText}}", "(doglan senesi)"),
            WorkerRespPhraseCell(wResp));

        body.AppendChild(new Table(
            new TableProperties(
                new TableWidth { Width = contentW.ToString(), Type = TableWidthUnitValues.Dxa },
                new TableLayout { Type = TableLayoutValues.Fixed },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil },
                    new InsideHorizontalBorder { Val = BorderValues.Nil },
                    new InsideVerticalBorder { Val = BorderValues.Nil })),
            new TableGrid(
                new GridColumn { Width = wName.ToString() },
                new GridColumn { Width = wDob.ToString() },
                new GridColumn { Width = wResp.ToString() }),
            workerRow));
        body.AppendChild(P("", spaceBefore: 24));

        body.AppendChild(P("Kärhananyň ýolbaşçysy", sz: 20, spaceAfter: 8));
        body.AppendChild(FullWidthUnderlinedField(contentW, "", "{{ds.rows.CompanyHead_FullName}}",
            JustificationValues.Center, sz: 20));
        body.AppendChild(P("(familliýasy, ady, atasynyň ady)", italic: true, sz: 14, just: JustificationValues.Center, spaceBefore: 50, spaceAfter: 8));
        body.AppendChild(FullWidthUnderlinedField(contentW, "pasporty ", "{{ds.rows.CompanyHead_PassportLine}}",
            JustificationValues.Center, sz: 20));
        body.AppendChild(P("(pasportyň seriýasy, belgisi, nirede we haçan berildi)", italic: true, sz: 14, just: JustificationValues.Center, spaceBefore: 50, spaceAfter: 14));

        body.AppendChild(P("we Kärhananyň wiza işleri boýunça ygtyýarly wekili:", sz: 20, spaceAfter: 8));
        body.AppendChild(FullWidthUnderlinedField(contentW, "", "{{ds.rows.Representative_FullName}}",
            JustificationValues.Center, sz: 20));
        body.AppendChild(P("(familliýasy, ady, atasynyň ady)", italic: true, sz: 14, just: JustificationValues.Center, spaceBefore: 50, spaceAfter: 8));
        body.AppendChild(FullWidthUnderlinedField(contentW, "pasporty ", "{{ds.rows.Representative_PassportLine}}",
            JustificationValues.Center, sz: 20));
        body.AppendChild(P("(pasportyň seriýasy, belgisi, nirede we haçan berildi, telefon belgisi)", italic: true, sz: 14, just: JustificationValues.Center, spaceBefore: 50, spaceAfter: 14));

        body.AppendChild(BodyJustified(bodyP1a));
        body.AppendChild(BodyJustified(bodyP1b));
        body.AppendChild(BodyJustified(bodyP1c));
        body.AppendChild(BodyJustified(bodyP1d));
        body.AppendChild(BodyJustified(bodyP1e));
        body.AppendChild(BodyJustified(bodyP2));

        body.AppendChild(P("Borçnamany tassyklaýarys:", sz: 20, spaceBefore: 72, spaceAfter: 16));

        body.AppendChild(FullWidthUnderlinedField(contentW, "Kärhananyň ýolbaşçysy:  ", "{{ds.rows.CompanyHead_FullName}}",
            JustificationValues.Left, sz: 20));
        body.AppendChild(P("(familliýasy, ady, atasynyň ady)", italic: true, sz: 14, just: JustificationValues.Center, spaceBefore: 50, spaceAfter: 8));
        body.AppendChild(P("(gol)", italic: true, sz: 14, just: JustificationValues.Right, spaceAfter: 12));
        body.AppendChild(FullWidthUnderlinedField(contentW, "Kärhananyň wiza işleri boýunça ygtyýarly wezipeli işgäri:  ", "{{ds.rows.Representative_FullName}}",
            JustificationValues.Left, sz: 20));
        body.AppendChild(P("(familliýasy, ady, atasynyň ady)", italic: true, sz: 14, just: JustificationValues.Center, spaceBefore: 50, spaceAfter: 8));
        body.AppendChild(P("(gol)", italic: true, sz: 14, just: JustificationValues.Right));

        // Between persons only (not after last): {{:s:}} separator + {{:PageBreak}} (library keyword — not a raw w:br).
        body.AppendChild(new Paragraph(
            new Run(new Text("{{:s:}}{{:PageBreak}}") { Space = SpaceProcessingModeValues.Preserve })));
        body.AppendChild(P("{{/ds.rows}}", sz: 1));

        body.AppendChild(new SectionProperties(
            new PageSize { Width = 11906, Height = 16838 },
            new PageMargin { Top = mrgT, Bottom = mrgB, Left = mrgL, Right = mrgR }));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Labor contract (Zähmet şertnamasy) per-person form for App_Inv_And_WP.
/// 7-section contract with dynamic intro (names/position), section 5 (dates), section 6 (salary), signatures.
/// Uses {{#ds.rows}} … {{:s:}}{{:PageBreak}} … {{/ds.rows}} between items.
/// </summary>
static byte[] MakeLaborContractTemplate()
{
    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        static Paragraph P(string text, bool bold = false, bool italic = false,
            JustificationValues? just = null, int sz = 22, int indent = 0, int? spaceAfter = null)
        {
            var ppr = new ParagraphProperties(new Justification { Val = just ?? JustificationValues.Left });
            if (indent > 0) ppr.AppendChild(new Indentation { FirstLine = indent.ToString() });
            if (spaceAfter.HasValue) ppr.AppendChild(new SpacingBetweenLines { After = spaceAfter.Value.ToString() });
            var para = new Paragraph(ppr);

            // Split on newlines and create runs with Break elements
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var rpr = new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = sz.ToString() }
                );
                if (bold) rpr.AppendChild(new Bold());
                if (italic) rpr.AppendChild(new Italic());
                para.AppendChild(new Run(rpr, new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve }));
                if (i < lines.Length - 1)
                    para.AppendChild(new Run(new Break()));
            }
            return para;
        }

        // Helper: intro paragraph with bold name and position
        static Paragraph MakeIntroParagraph()
        {
            var ppr = new ParagraphProperties(new Justification { Val = JustificationValues.Both });
            ppr.AppendChild(new Indentation { FirstLine = "720" });
            ppr.AppendChild(new SpacingBetweenLines { After = "40" });
            var para = new Paragraph(ppr);

            static Run Run(string text, bool bold = false)
            {
                var rpr = new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = "22" }
                );
                if (bold) rpr.AppendChild(new Bold());
                return new Run(rpr, new Text(text) { Space = SpaceProcessingModeValues.Preserve });
            }

            para.AppendChild(Run("\""));
            para.AppendChild(Run("{{ds.rows.Application_SponsorName}}"));
            para.AppendChild(Run("\" Türk kärhanasynyň Türkmenistandaky şahamçasynyň Müdiri "));
            para.AppendChild(Run("{{ds.rows.Application_SponsorSignatory}}"));
            para.AppendChild(Run(" bilen mundan beýläk \"IŞ BERIJI\" diýip atlandyrylýan, beýleki tarapyndan \"IŞGÄR\" diýip atlandyrylýan "));
            para.AppendChild(Run("{{ds.rows.Person_FullName}}", bold: true));  // BOLD
            para.AppendChild(Run(" arasynda zähmet şertnamasy baglaşyldy. IŞGÄR "));
            para.AppendChild(Run("{{ds.rows.Position_PositionTm}}", bold: true));  // BOLD
            para.AppendChild(Run(" wezipesine işe kabul edilýär."));
            return para;
        }

        // Row repeat open
        body.AppendChild(P("{{#ds.rows}}", sz: 1));

        body.AppendChild(P("ZÄHMET ŞERTNAMASY", bold: true, sz: 22, just: JustificationValues.Center, spaceAfter: 40));
        body.AppendChild(P("Aşgabat şäheri", bold: true, sz: 22, spaceAfter: 40));

        // Intro paragraph — bold employee name and position (multi-run for selective bold)
        body.AppendChild(MakeIntroParagraph());

        // Section 1
        body.AppendChild(P("1. Iş berijiniň borçlary", bold: true, sz: 22, spaceAfter: 20));
        body.AppendChild(P("1.1. Hünärine görä iş bilen üpjün etmelidir.\n1.2. Her aý aýlyk zähmet hakyny bellenilen güni tölemelidir.\n1.3. Hereket edýän Türkmenistanyň Zähmet baradaky kanunlar kodeksine laýyklykda kesgitlenen möhletde ýyllyk zähmet rugsadyny bermelidir.\n1.4. Şertnamanyň möhleti boýunça hereket edýän Türkmenistanyň Zähmet baradaky kanunlar kodeksine laýyklykda iş üçin oňaýly şertleri örtäkmeli, sosial goraglary we beýleki kepillikleri bermelidir.",
            just: JustificationValues.Left, spaceAfter: 30));

        // Section 2
        body.AppendChild(P("2. Işgäriň borçlary", bold: true, sz: 22, spaceAfter: 20));
        body.AppendChild(P("2.1. Bu şertnama laýyklykda tabşyrylan işi etmeli.\n2.2. Kärhana hereket edýän içerki düzgüne, tehniki we önümçilik tertibine tabyn bolmaly.\n2.3. Öz iş ýerini, kärhananyň enjamlaryny arassa saklamaly.\n2.4. Kärhananyň iş syrlaryny aýan etmeli däldir.\n2.5. Işleýän bölüminiň ýolbaşçysynyň tabşyryklarynyň borçlaryny ak ýürek bilen ýerine ýetirmelidir.",
            just: JustificationValues.Left, spaceAfter: 30));

        // Section 3
        body.AppendChild(P("3. Iş we dynç alyş düzgüni", bold: true, sz: 22, spaceAfter: 20));
        body.AppendChild(P("3.1. Iş we dynç alyş wagtynyň tertibi kärhananyň içerki düzgünine laýyklykda kesgitlenilýär.\n3.2. Işgär üçin 8 (sekiz) sagatlyk iş günü we 6 (alty) günlük iş hepdesinde kesgitlenilýär.\n3.3. Önümçilik zerurlygy ýüze çykan wagty işgär iş wagtyndan artyk möhlet bilen işdedilip bilner.\n3.4. Aýlyk zähmet haky ştat birligine laýyklykda tölenýär.",
            just: JustificationValues.Left, spaceAfter: 30));

        // Section 4
        body.AppendChild(P("4. Zähmet şertnamasynyň ýatyrylmagy", bold: true, sz: 22, spaceAfter: 15));
        body.AppendChild(P("Zähmet şertnamasy \"IŞ BERIJI\" tarapyndan aşakdaky ýagdaýlarda ýatyrylýar:\n4.1. Zähmet şertnamasynyň möhletiniň gutarmagy;\n4.2. Işleriň gutarmagy;\n4.3. Iş möçberiniň azalmagy;\n4.4. Işe serhoş bolup, narkotiki maddalaryň täsiri astynda gelmegi;\n4.5. Öz üstüne tabşyrylan borçlary işgäriň birsygyn ýerine ýetirmezligi;\n4.6. Kärhana degişli emlägi ogurlamagy;\n4.7. Şu şertnamada kadalaşdyrylmadyk jedelli meseleler Türkmenistanyň hereket edýän kanunlary esasynda çözülýär.",
            just: JustificationValues.Left, spaceAfter: 20));

        // Section 5 — dynamic dates
        body.AppendChild(P("5. Zähmet şertnamasynyň hereket edýän möhleti", bold: true, sz: 22, spaceAfter: 30));
        body.AppendChild(P("Zähmet şertnamasy     {{ds.rows.Contract_StartDateText}}  -  {{ds.rows.Contract_ExpirationDateText}}     çenli.", bold: true, spaceAfter: 30));
        body.AppendChild(P("Zähmet şertnamasy Taraplar gol çekenlerinden soň güýje girýär.\nZähmet şertnamasynyň möhletiniň gutarmagyna garamazdan, eger iş gatnaşyklary hakykat ýüzünde dowam edýän bolsa we taraplardan hiç biri şertnamany ýatyrmak barada ikinji tarapa ýüz tutmasa onda onuň möhleti uzadylan hasap edilýär.",
            just: JustificationValues.Left, spaceAfter: 40));

        // Section 6 — dynamic salary
        body.AppendChild(P("6. Türkmenistanyň döwletinde alýan aýlyk zähmet haky", bold: true, sz: 22, spaceAfter: 15));
        body.AppendChild(P("Aýlyk zähmet haky {{ds.rows.Contract_SalaryText}} {{ds.rows.Salary_CurrencyCode}} Türkiýada Bankyň üsti bilen hasabyna geçirilýär.", spaceAfter: 20));

        // Section 7
        body.AppendChild(P("7. Taraplaryň gollary we salgylary", bold: true, sz: 22, spaceAfter: 20));

        // Signatures — two-column table: IŞ BERIJI (left) | IŞGÄR (right)
        var sigTable = new Table();
        sigTable.AppendChild(new TableProperties(
            new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
            new TableLayout { Type = TableLayoutValues.Fixed }
        ));

        // Two equal columns with small gap
        const int sigColWidth = 5000; // ~48% each with gap
        sigTable.AppendChild(new TableGrid(
            new GridColumn { Width = sigColWidth.ToString() },
            new GridColumn { Width = "200" }, // Gap
            new GridColumn { Width = sigColWidth.ToString() }
        ));

        // Helper for signature cells
        static TableCell SigCell(string text, int width, bool bold = false)
        {
            var rpr = new RunProperties(
                new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                new FontSize { Val = "22" }
            );
            if (bold) rpr.AppendChild(new Bold());
            return new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa },
                    new TableCellBorders(
                        new TopBorder { Val = BorderValues.Nil },
                        new BottomBorder { Val = BorderValues.Nil },
                        new LeftBorder { Val = BorderValues.Nil },
                        new RightBorder { Val = BorderValues.Nil }
                    )
                ),
                new Paragraph(
                    new ParagraphProperties(new SpacingBetweenLines { After = "40" }),
                    new Run(rpr, new Text(text) { Space = SpaceProcessingModeValues.Preserve })
                )
            );
        }

        static TableCell GapCell() => new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = "200", Type = TableWidthUnitValues.Dxa },
                new TableCellBorders(
                    new TopBorder { Val = BorderValues.Nil },
                    new BottomBorder { Val = BorderValues.Nil },
                    new LeftBorder { Val = BorderValues.Nil },
                    new RightBorder { Val = BorderValues.Nil }
                )
            ),
            new Paragraph()
        );

        // Row 1: Headers
        sigTable.AppendChild(new TableRow(SigCell("IŞ BERIJI:", sigColWidth, bold: true), GapCell(), SigCell("IŞGÄR:", sigColWidth, bold: true)));
        // Row 2: Names
        sigTable.AppendChild(new TableRow(SigCell("{{ds.rows.Application_SponsorSignatory}}", sigColWidth), GapCell(), SigCell("{{ds.rows.Person_FullName}}", sigColWidth)));
        // Row 3: Company / Passport
        sigTable.AppendChild(new TableRow(SigCell("{{ds.rows.Application_SponsorName}}", sigColWidth), GapCell(), SigCell("Pasport belgisi: {{ds.rows.Passport_Number}}", sigColWidth)));
        // Row 4: Address (employer only) / empty
        sigTable.AppendChild(new TableRow(SigCell("{{ds.rows.Application_CompanyAddress}}", sigColWidth), GapCell(), SigCell("", sigColWidth)));
        // Row 5: Signature lines
        sigTable.AppendChild(new TableRow(SigCell("___________________________", sigColWidth), GapCell(), SigCell("___________________________", sigColWidth)));

        body.AppendChild(sigTable);

        body.AppendChild(new Paragraph(
            new Run(new Text("{{:s:}}{{:PageBreak}}") { Space = SpaceProcessingModeValues.Preserve })));
        body.AppendChild(P("{{/ds.rows}}", sz: 1));

        // Aggressive margins for single-page fit: ~0.3" all around
        body.AppendChild(new SectionProperties(
            new PageSize { Width = 11906, Height = 16838 },
            new PageMargin { Top = 432, Bottom = 432, Left = 432, Right = 432 }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Builds a variable-column portrait or landscape "Daşary ýurt raýatlarynyň sanawy" item table.
/// cols: (headerText, fieldTemplate, widthTwips). First field must contain {{#ds.rows}}, last {{/ds.rows}}.
/// </summary>
static byte[] MakeItemTableTemplate(bool portrait, (string Header, string Field, int Width)[] cols)
{
    uint pw = portrait ? 11906u : 16838u;
    uint ph = portrait ? 16838u : 11906u;
    const uint MrgL = 720, MrgR = 720, MrgT = 720, MrgB = 720;

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        // Title
        body.AppendChild(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = "20" }, new Bold()
                ),
                new Text("Da\u015fary \u00fdurt ra\u00fdatlaryny\u0148 sanawy") { Space = SpaceProcessingModeValues.Preserve }
            )
        ));

        static TableCell MakeCell(string text, int width, bool bold = false) =>
            new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa },
                    new TableCellBorders(
                        new TopBorder    { Val = BorderValues.Single, Size = 4 },
                        new BottomBorder { Val = BorderValues.Single, Size = 4 },
                        new LeftBorder   { Val = BorderValues.Single, Size = 4 },
                        new RightBorder  { Val = BorderValues.Single, Size = 4 }
                    )
                ),
                new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(
                        new RunProperties(
                            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                            new FontSize { Val = bold ? "18" : "14" },
                            bold ? (OpenXmlElement)new Bold() : new Bold { Val = false }
                        ),
                        new Text(text) { Space = SpaceProcessingModeValues.Preserve }
                    )
                )
            );

        var tblProps = new TableProperties(new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto });
        var headerRow = new TableRow(cols.Select(c => MakeCell(c.Header, c.Width, bold: true)).ToArray());
        var dataRow   = new TableRow(cols.Select(c => MakeCell(c.Field,  c.Width, bold: false)).ToArray());
        var tbl = new Table(tblProps, headerRow, dataRow);
        body.AppendChild(tbl);
        body.AppendChild(new Paragraph());
        AppendSignatoryLetter(body);

        var orient = portrait ? PageOrientationValues.Portrait : PageOrientationValues.Landscape;
        body.AppendChild(new SectionProperties(
            new PageSize { Width = pw, Height = ph, Orient = orient },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Shared 14-column landscape "Daşary ýurt raýatlarynyň sanawy" personnel list template.
/// Title row + header row + DocxTemplater {{#ds.rows}} data row.
/// Columns: №, Familiýasy, Ady, Doglan senesi/ýeri, Jynsy, Raýatlygy,
///          Pasport belgisi/möhleti, Bilimi/okan ýeri, Hünäri,
///          Wezipesi, Möhleti/gezekligi, TM salgysy, DY salgysy, Serhet ýakasy.
/// </summary>
static byte[] MakeSanawyTemplate()
{
    // A4 Landscape
    const uint PW_L = 16838;
    const uint PH_L = 11906;
    const uint MrgL = 720;
    const uint MrgR = 720;
    const uint MrgT = 720;
    const uint MrgB = 720;

    // Column widths in twips (sum ≈ 15398 = 16838 - 720 - 720)
    var cols = new (string Header, string Field, int Width)[]
    {
        ("№",                        "{{#ds.rows}}{{ds.rows.RowNo}}",              350),
        ("Familiýasy",               "{{ds.rows.Person_LastName}}",                1000),
        ("Ady",                      "{{ds.rows.Person_FirstName}}",               800),
        ("Doglan senesi we ýeri",    "{{ds.rows.Person_DateOfBirthText}}\n{{ds.rows.Person_CountryOfBirthTm}}/{{ds.rows.Person_BirthPlace}}", 1150),
        ("Jynsy",                    "{{ds.rows.Person_GenderTm}}",                500),
        ("Raýatlygy",                "{{ds.rows.Person_NationalityCode}}",          860),
        ("Pasport belgisi we möhleti","{{ds.rows.Passport_Number}}\n{{ds.rows.Passport_ExpirationDateText}}", 1150),
        ("Bilimi we okan ýeri",      "{{ds.rows.Education_LevelTm}}\n{{ds.rows.Education_InstitutionName}}", 1368),
        ("Bilimine görä hünäri",     "{{ds.rows.Education_SpecialtyTm}}",           1368),
        ("Wezipesi",                 "{{ds.rows.Position_PositionTm}}",             1300),
        ("Möhleti we gezekligi",     "{{ds.rows.Application_VisaPeriod_NameTm}} {{ds.rows.Application_VisaCategory_NameTm}}", 1150),
        ("Türkmenistandaky salgysy", "{{ds.rows.Address_FullAddress}}",             1734),
        ("Daşary ýurtdaky salgysy",  "{{ds.rows.Person_ForeignAddress}}",           1734),
        ("Barjak serhet ýakasy",     "{{ds.rows.Application_BorderZoneLocation_NameTm}}{{/ds.rows}}", 1834),
    };

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        // Title
        body.AppendChild(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = "20" },
                    new Bold()
                ),
                new Text("Da\u015fary \u00fdurt ra\u00fdatlaryny\u0148 sanawy") { Space = SpaceProcessingModeValues.Preserve }
            )
        ));

        static TableCell MakeCell(string text, int width, bool bold = false, bool small = false) =>
            new TableCell(
                new TableCellProperties(
                    new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa },
                    new TableCellBorders(
                        new TopBorder    { Val = BorderValues.Single, Size = 4 },
                        new BottomBorder { Val = BorderValues.Single, Size = 4 },
                        new LeftBorder   { Val = BorderValues.Single, Size = 4 },
                        new RightBorder  { Val = BorderValues.Single, Size = 4 }
                    )
                ),
                new Paragraph(
                    new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                    new Run(
                        new RunProperties(
                            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                            new FontSize { Val = small ? "14" : "18" },
                            bold ? (OpenXmlElement)new Bold() : new Bold { Val = false }
                        ),
                        new Text(text) { Space = SpaceProcessingModeValues.Preserve }
                    )
                )
            );

        var tblProps = new TableProperties(
            new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto }
        );

        var headerRow = new TableRow(cols.Select(c => MakeCell(c.Header, c.Width, bold: true)).ToArray());
        var dataRow   = new TableRow(cols.Select(c => MakeCell(c.Field,  c.Width, bold: false, small: true)).ToArray());

        var tbl = new Table(tblProps, headerRow, dataRow);
        body.AppendChild(tbl);
        body.AppendChild(new Paragraph());
        AppendSignatoryLetter(body);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_L, Height = PH_L, Orient = PageOrientationValues.Landscape },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Change-Inv letter: Group D fixed recipient + body paragraph + responsibility +
/// table title + invitation table with DocxTemplater {{#ds.rows}} row-repeat.
/// </summary>
static byte[] MakeChangeInvLetterTemplate()
{
    const uint PW_P = 11906;
    const uint PH_P = 16838;
    const uint MrgL  = 1800;
    const uint MrgR  = 1800;
    const uint MrgT  = 1440;
    const uint MrgB  = 1440;

    const string recipient =
        "T\u00fcrkmenistany\u0148 D\u00f6wlet migrasi\u00fda gullugynyň başlygyna";
    const string bodyText =
        "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCount}} ({{ds.TotalPersonCountText}}) sany da\u015fary \u00fdurt ra\u00fdatynyň pasportyny çalyşandygy sebäpli aşakda görkezilen çakylyklary täze pasportyna görä resmile\u015fdirip bermegiňizi Sizden haýyş edýäris.";

    // Column widths in twentieths-of-a-point (twips). Printable width = 11906 - 1800 - 1800 = 8306.
    const int wNo     =  600;  // №
    const int wNum    = 3000;  // Çakylygyň belgisi
    const int wStart  = 2353;  // Resmileşdirilen senesi
    const int wExpiry = 2353;  // Möhleti

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        // Header
        body.AppendChild(MakeLetterRun("{{ds.FullApplicationNumber}}", rightAlign: true, bold: true));
        body.AppendChild(MakeLetterRun("{{ds.ApplicationDate}} ý.", rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeLetterRun(recipient, rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());

        // Body + responsibility
        body.AppendChild(MakeJustifiedParagraph(bodyText));
        body.AppendChild(MakeJustifiedParagraph(FormalCompanyLetterLayout.ResponsibilityPlain));
        body.AppendChild(new Paragraph());

        // Table title
        body.AppendChild(new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { After = "80" }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = "30" },
                    new Bold()
                ),
                new Text("\u00dcýtgedilmeli çakylyklar:") { Space = SpaceProcessingModeValues.Preserve }
            )
        ));

        // Invitation table
        var tblProps = new TableProperties(
            new TableWidth { Width = "0", Type = TableWidthUnitValues.Auto },
            new TableBorders(
                new TopBorder    { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder   { Val = BorderValues.Single, Size = 4 },
                new RightBorder  { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder   { Val = BorderValues.Single, Size = 4 }
            )
        );

        static TableCell MakeHeaderCell(string text, int width) => new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa }
            ),
            new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "30" },
                        new Bold()
                    ),
                    new Text(text)
                )
            )
        );

        static TableCell MakeDataCell(string text, int width) => new TableCell(
            new TableCellProperties(
                new TableCellWidth { Width = width.ToString(), Type = TableWidthUnitValues.Dxa }
            ),
            new Paragraph(
                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                        new FontSize { Val = "30" }
                    ),
                    new Text(text) { Space = SpaceProcessingModeValues.Preserve }
                )
            )
        );

        var headerRow = new TableRow(
            MakeHeaderCell("№",                       wNo),
            MakeHeaderCell("Çakylygyň belgisi",       wNum),
            MakeHeaderCell("Resmileşdirilen senesi",  wStart),
            MakeHeaderCell("Möhleti",                 wExpiry)
        );

        // DocxTemplater row repeat: begin marker in a cell, data cells, end marker in a cell
        var dataRow = new TableRow(
            MakeDataCell("{{#ds.rows}}{{ds.rows.RowNo}}", wNo),
            MakeDataCell("{{ds.rows.InvitationNumber}}",  wNum),
            MakeDataCell("{{ds.rows.StartDate}}",         wStart),
            MakeDataCell("{{ds.rows.ExpirationDate}}{{/ds.rows}}", wExpiry)
        );

        var tbl = new Table(tblProps, headerRow, dataRow);
        body.AppendChild(tbl);
        body.AppendChild(new Paragraph());

        // Signatory
        AppendSignatoryLetter(body);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Group C letter template: Ministry recipient (dynamic) + greeting (no urgency) +
/// body1 = ProjectContract_Description + body2 (derived) + responsibility + attachments + signatory.
/// </summary>
static byte[] MakeGroupCLetterTemplate(string body2Text, string attachmentsText)
{
    const uint PW_P = 11906;
    const uint PH_P = 16838;
    const uint MrgL  = 1800;
    const uint MrgR  = 1800;
    const uint MrgT  = 1440;
    const uint MrgB  = 1440;

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        body.AppendChild(MakeLetterRun("{{ds.FullApplicationNumber}}", rightAlign: true, bold: true));
        body.AppendChild(MakeLetterRun("{{ds.ApplicationDate}} ý.", rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeLetterRun("{{ds.ProjectContract_Ministry_RecipientBlock}}", rightAlign: false, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeLetterRun("{{ds.ProjectContract_Ministry_FormOfAddress}}", rightAlign: false, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeJustifiedParagraph("{{ds.ProjectContract_Description}}"));
        body.AppendChild(MakeJustifiedParagraph(body2Text));
        body.AppendChild(MakeJustifiedParagraph(FormalCompanyLetterLayout.ResponsibilityPlain));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeJustifiedParagraph(attachmentsText));
        body.AppendChild(new Paragraph());
        AppendSignatoryLetter(body);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Group D letter template: app number+date (right) → fixed static recipient (national migration chief) →
/// derived body1 paragraph → responsibility → signatory. No Maksady/greeting.
/// </summary>
static byte[] MakeGroupDLetterTemplate(string bodyText)
{
    const uint PW_P = 11906;
    const uint PH_P = 16838;
    const uint MrgL  = 1800;
    const uint MrgR  = 1800;
    const uint MrgT  = 1440;
    const uint MrgB  = 1440;

    const string recipient    = "T\u00fcrkmenistany\u0148 D\u00f6wlet migrasi\u00fda gullugynyň başlygyna";

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        body.AppendChild(MakeLetterRun("{{ds.FullApplicationNumber}}", rightAlign: true, bold: true));
        body.AppendChild(MakeLetterRun("{{ds.ApplicationDate}} ý.", rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeLetterRun(recipient, rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeJustifiedParagraph(bodyText));
        body.AppendChild(MakeJustifiedParagraph(FormalCompanyLetterLayout.ResponsibilityPlain));
        body.AppendChild(new Paragraph());
        AppendSignatoryLetter(body);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// Group B letter template: same as Group A but adds two static intro paragraphs
/// (Berkarar intro + Company partnership) before the derived body3 request paragraph.
/// </summary>
static byte[] MakeGroupBLetterTemplate(string body3Text, string attachmentsText)
{
    const uint PW_P = 11906;
    const uint PH_P = 16838;
    const uint MrgL  = 1800;
    const uint MrgR  = 1800;
    const uint MrgT  = 1440;
    const uint MrgB  = 1440;

    const string body1 =
        "Berkarar döwletimiziň bagtyýarlyk döwründe Hormatly Prezidentimiziň taýsyz tagallalary netijesinde ýurdumyzyň elektroenergetika pudagynda birnäçe iri taslamalar durmuşa geçirilýär.";
    const string body2 =
        "Şunuň bilen baglylykda, elektroenergetika pudagyny köp ýyllardan bäri hyzmatdaşy bolup gelýän \"{{ds.Company_Name}}\" kompaniýasy tarapyndan birnäçe taslamalar amala aşyrylýar.";

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        body.AppendChild(MakeLetterRun("{{ds.FullApplicationNumber}}", rightAlign: true, bold: true));
        body.AppendChild(MakeLetterRun("{{ds.ApplicationDate}} ý.", rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeLetterRun("{{ds.ProjectContract_Ministry_RecipientBlock}}", rightAlign: false, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(new Paragraph(
            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = "30" },
                    new Italic()
                ),
                new Text("{{ds.Urgency_NameTm}}") { Space = SpaceProcessingModeValues.Preserve }
            )
        ));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeLetterRun("{{ds.ProjectContract_Ministry_FormOfAddress}}", rightAlign: false, bold: true));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeMaksadyParagraph("Maksady:", "{{ds.ProjectContract_Description}}"));
        body.AppendChild(MakeJustifiedParagraph(body1));
        body.AppendChild(MakeJustifiedParagraph(body2));
        body.AppendChild(MakeJustifiedParagraph(body3Text));
        body.AppendChild(MakeJustifiedParagraph(FormalCompanyLetterLayout.ResponsibilityPlain));
        body.AppendChild(new Paragraph());
        body.AppendChild(MakeJustifiedParagraph(attachmentsText));
        body.AppendChild(new Paragraph());
        AppendSignatoryLetter(body);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

static byte[] MakeBusinessTripLetterTemplate(bool isDeparture)
{
    // Portrait A4
    const uint PW_P = 11906;
    const uint PH_P = 16838;
    const uint MrgL  = 1800; // ~3.17 cm (matches AppBaseReport 100F = ~1440 twips, generous for letterhead)
    const uint MrgR  = 1800;
    const uint MrgT  = 1440;
    const uint MrgB  = 1440;

    string bodyText = isDeparture
        ? "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCountText}} ({{ds.TotalPersonCount}}) sany da\u015fary \u00fdurt ra\u00fdatlary\u0148y\u0148 {{ds.BusinessTripStartDateText}}-den {{ds.BusinessTripEndDateText}}-ne \u00e7enli {{ds.BusinessTripDurationDays}} g\u00fcn m\u00f6hlet bilen {{ds.ToRegionName_Genitive}} i\u015f saparyna gid\u00fdandigini size habar ber\u00fd\u00e4ris."
        : "Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen sanawdaky {{ds.TotalPersonCountText}} ({{ds.TotalPersonCount}}) sany da\u015fary \u00fdurt ra\u00fdatly\u0148y\u0148 {{ds.BusinessTripStartDateText}}-den {{ds.BusinessTripEndDateText}}-ne \u00e7enli {{ds.BusinessTripDurationDays}} g\u00fcn m\u00f6hlet bilen {{ds.ToRegionName_Genitive}} {{ds.ToCityName_Dative}} i\u015f saparyna gelendigini size habar ber\u00fd\u00e4ris.";

    string purposeLabel = isDeparture ? "Maksady:" : "Maksady-";
    string purposeField = isDeparture ? "{{ds.ProjectContract_Description}}" : "{{ds.BusinessTripPurpose_NameTm}}";

    using var ms2 = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms2, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        // App number + date header (right-aligned block)
        body.AppendChild(MakeLetterRun("{{ds.FullApplicationNumber}}", rightAlign: true, bold: true));
        body.AppendChild(MakeLetterRun("{{ds.ApplicationDate}} ý.", rightAlign: true, bold: true));

        // Spacer
        body.AppendChild(new Paragraph());

        // Recipient (right-aligned, bold)
        body.AppendChild(MakeLetterRun("{{ds.MigrationService_NameTm}}", rightAlign: true, bold: true));

        // Spacer
        body.AppendChild(new Paragraph());

        // Body1 — notification paragraph (justified, first-line indent)
        body.AppendChild(MakeJustifiedParagraph(bodyText));

        // Body3 — Maksady
        body.AppendChild(MakeMaksadyParagraph(purposeLabel, purposeField));

        // Body2 — responsibility clause
        body.AppendChild(MakeJustifiedParagraph(FormalCompanyLetterLayout.ResponsibilityPlain));

        // Spacer before signatory
        body.AppendChild(new Paragraph());

        // Signatory: position (full width, wraps) + name (right-aligned)
        AppendSignatoryLetter(body);

        // Page setup
        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = MrgL, Right = MrgR }
        ));

        main.Document.Save();
    }
    return ms2.ToArray();
}

/// <summary>
/// <c>App_Visa_And_WP_Ext_GT15_Calik_Migration_Letter</c> — Çalık branch → Döwlet migrasiýa gullugy (GT-15 scan).
/// Plain body only: ref+date top-left, migration addressee, no header image (avoids Word corruption after DocxTemplater merge).
/// </summary>
static byte[] MakeAppVisaAndWPExtGt15CalikMigrationLetterTemplate()
{
    const uint PW_P = FormalCompanyLetterLayout.LetterPortraitPageWidthTwips;
    const uint PH_P = 16838;
    const uint MrgL = FormalCompanyLetterLayout.AppInvAndWPLetterMarginLeftTwips;
    const uint MrgR = FormalCompanyLetterLayout.AppInvAndWPLetterMarginRightTwips;
    const uint MrgT = 1440;
    const uint MrgB = 1440;

    const string p3CalikResponsibility =
        "Da\u015fary \u00fdurt ra\u00fdatyny\u0148 T\u00fcrkmenistana gelmegini\u0148, onda bolmagyny\u0148 we ondan gitmegini\u0148 d\u00fczg\u00fcnlerini berja\u00fd etmegine jogapk\u00e4r\u00e7iligi kompani\u00fdamyz \u00f6z \u00fcst\u00fcne al\u00fdar.";

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        body.AppendChild(MakeLetterRun("{{ds.FullApplicationNumber}}", rightAlign: false, bold: true));
        body.AppendChild(MakeLetterRun("{{ds.ApplicationDate}} \u00fd.", rightAlign: false, bold: true));
        body.AppendChild(new Paragraph());

        body.AppendChild(MakeLetterRun("{{ds.MigrationService_NameTm}}", rightAlign: true, bold: true));
        body.AppendChild(new Paragraph());

        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Left },
                new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPHeaderSalutationGapAfterTwips }),
            new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
                    new FontSize { Val = "30" },
                    new Bold(),
                    new Italic(),
                    new Color { Val = "000000" },
                    new Underline { Val = UnderlineValues.None }),
                new Text("{?{ds.ApplicationType_ShowUrgency}}{{ds.Urgency_NameTm}}{{/}}") { Space = SpaceProcessingModeValues.Preserve })));

        body.AppendChild(MakeJustifiedParagraph("{{ds.ProjectContract_Description}}"));

        var p2 = new Paragraph(InvAndWPLetterBodyParagraphProperties());
        p2.AppendChild(MakeRun("Hatymyzy\u0148 go\u015fundysynda g\u00f6rkezilen T\u00fcrki\u00fde Respublikasyny\u0148 \"\u00c7al\u0131k Enerji Sana\u00fdi we Ticaret A.\u015e\" kompani\u00fdasyna degi\u015fli bolan sanawdaky ", "30", false));
        p2.AppendChild(MakeRun("{{ds.CancelPersonCount}} ({{ds.CancelPersonCountText}})", "30", false));
        p2.AppendChild(MakeRun(" sany da\u015fary \u00fdurt ra\u00fdatyny\u0148 wizasyny\u0148 we i\u015f rugsatnamasyny\u0148 ", "30", false));
        p2.AppendChild(MakeRun("{{ds.VisaPeriod_NameTm}}", "30", false));
        p2.AppendChild(MakeRun(" ", "30", false));
        p2.AppendChild(MakeRun("{{ds.VisaCategory_NameTm}}", "30", false));
        p2.AppendChild(MakeRun(" m\u00f6hlet bilen uzaldylmagyna rugsat berilmegine Sizden ha\u00fdy\u015f ed\u00fd\u00e4ris.", "30", false));
        body.AppendChild(p2);

        body.AppendChild(MakeJustifiedParagraph(p3CalikResponsibility));

        AppendSignatoryLetter(body, FormalCompanyLetterLayout.AppInvAndWPPrintableWidthTwips, FormalCompanyLetterLayout.InvAndWPSignatoryParagraphSpaceBeforeTwips);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = (int)MrgL, Right = (int)MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>
/// <c>App_Visa_WP_Ext_Energy_To_Construction_Ministry_Letter</c> — reference
/// <c>FormTemplates/App_Visa_WP_Ext_Energy_To_Construction_Ministry_scan.png</c>.
/// Paragraph 1 text follows <c>FormTemplates/App_Visa_WP_Ext_Energy_To_Construction_Ministry_scan.png</c>
/// (may differ from <c>ProjectContract.Description</c> seed spelling, e.g. Çalyk / energogihan).
/// <c>{{ds.CancelPersonCount}}</c>, <c>{{ds.CancelPersonCountText}}</c>, <c>{{ds.VisaPeriod_NameTm}}</c> are merged.
/// </summary>
static byte[] MakeAppVisaWPExtEnergyToConstructionMinistryLetterTemplate()
{
    const uint PW_P = FormalCompanyLetterLayout.LetterPortraitPageWidthTwips;
    const uint PH_P = 16838;
    const uint MrgL = FormalCompanyLetterLayout.AppInvAndWPLetterMarginLeftTwips;
    const uint MrgR = FormalCompanyLetterLayout.AppInvAndWPLetterMarginRightTwips;
    const uint MrgT = 1440;
    const uint MrgB = 1440;
    const string p1Gt15 =
        "T\u00fcrkmenistany\u0148 Prezidentini\u0148 28.10.2023\u00fd. seneli, 754 belgili kararyna la\u00fdyklykda, T\u00fcrkmenistany\u0148 Energetika ministrligini\u0148 \"T\u00fcrkmenenergo\" d\u00f6wlet elektroenergetika korporasi\u00fdasy bilen T\u00fcrki\u00fde Respublikasyny\u0148 \"\u00c7alyk Enerji Senag\u00fdi ve Ticaret A.\u015e\" kompani\u00fdasyny\u0148 arasynda \"Balkan wela\u00fdatyndaky T\u00fcrkmenba\u015fydaky elektrik beketini\u0148 kuwwatlylygy 1574 MW bolup ulanmaga ta\u00fd\u00fdarlan\u00fdan d\u00f6wrebap elektrik stansi\u00fdasyny\u0148 we ony energogihan birle\u015fdirilmegini\u0148 ikin ge\u00e7ir bolan elektrik ge\u00e7iriji ulgamy\u0148 gurmak hem-de bar bolan d\u00f6wleti\u0148 elektrik stansi\u00fdalary \u00fc\u00e7in zerur bolan ta\u00fd\u00fdarlyk \u015fa\u00fdatlaryny satyn almak\" hakyndaky GT-15 belgili \u015fertnama 01.12.2023\u00fd. senesinde bagla\u015fyldy.";
    const string p3BoldOpening = "Hormatly Abdulla Muhammetgeldi\u00fdewi\u00e7, ";
    const string p3BodyAfterOpening =
        "\u00fdokarda be\u00fdan edilenleri g\u00f6z \u00f6\u0148\u00fcnde tutup, T\u00fcrkmenistany\u0148 Prezidentini\u0148 03.01.2020\u00fd. seneli, 1551 belgili kararyna la\u00fdyklykda, T\u00fcrkmenistany\u0148 Gurlu\u015fyk we binag\u00e4rlik ministrligi tarapyndan ylala\u015fylmak we resmile\u015fdirilmek \u015fe\u00fdle hem degi\u015fli Netijenama almak meselesinde \u00fdardam bermegi\u0148izi Sizden ha\u00fdy\u015f ed\u00fd\u00e4ris.";
    const string p4Responsibility =
        "T\u00fcrki\u00fde Respublikasyny\u0148 \"\u00c7alyk Enerji Sanayi ve Ticaret A.\u015e\" kompani\u00fdasy bilen T\u00fcrkmenistany\u0148 Energetika ministrligi da\u015fary \u00fdurt ra\u00fdatlaryny\u0148 T\u00fcrkmenistana giri\u015finde, \u00fderle\u015fi\u015finde we \u00fdurdundan \u00e7ykarylmagynda doly jogapk\u00e4r\u00e7ilik \u00e7ek\u00fd\u00e4r.";

    using var ms = new MemoryStream();
    using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
    {
        var main = doc.AddMainDocumentPart();
        main.Document = new Document();
        var body = main.Document.AppendChild(new Body());

        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Right },
                new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPHeaderLineAfterTwips }),
            MakeRun("T\u00fcrkmenistany\u0148 Gurlu\u015fyk", "30", true)));
        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Right },
                new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPHeaderLineAfterTwips }),
            MakeRun("we binag\u00e4rlik ministri", "30", true)));
        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Right },
                new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPRecipientBlockEndAfterTwips }),
            MakeRun("A.M.Geldi\u00fdewe", "30", true)));

        body.AppendChild(new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Center },
                new SpacingBetweenLines { After = FormalCompanyLetterLayout.InvAndWPSalutationAfterTwips }),
            MakeRun("Hormatly Abdulla Muhammetgeldi\u00fdewi\u00e7!", "30", true, underline: false, colorHex: "000000", explicitNoUnderline: true)));

        body.AppendChild(MakeJustifiedParagraph(p1Gt15));

        var p2 = new Paragraph(InvAndWPLetterBodyParagraphProperties());
        p2.AppendChild(MakeRun("\u015eunu\u0148 bilen baglylykda, gurlu\u015fyk i\u015flerini \u00fderine \u00fdetir\u00fd\u00e4n T\u00fcrki\u00fde Respublikasyny\u0148 \"\u00c7alyk Enerji Sanayi ve Ticaret A.\u015e\" kompani\u00fdasyny\u0148 ", "30", false));
        p2.AppendChild(MakeRun("{{ds.CancelPersonCount}} ({{ds.CancelPersonCountText}})", "30", false));
        p2.AppendChild(MakeRun(" sany da\u015fary \u00fdurt ra\u00fdatyny\u0148 wizasyny\u0148 we i\u015f rugsatnamasyny\u0148 ", "30", false));
        p2.AppendChild(MakeRun("{{ds.VisaPeriod_NameTm}}", "30", false));
        p2.AppendChild(MakeRun(" m\u00f6hlet bilen uzaltmak meselesi \u00fd\u00fcze \u00e7yk\u00fdandygyny bellenil\u00fd\u00e4r.", "30", false));
        body.AppendChild(p2);

        var p3 = new Paragraph(InvAndWPLetterBodyParagraphProperties());
        p3.AppendChild(MakeRun(p3BoldOpening, "30", true));
        p3.AppendChild(MakeRun(p3BodyAfterOpening, "30", false));
        body.AppendChild(p3);

        body.AppendChild(MakeJustifiedParagraph(p4Responsibility));

        AppendEnergyMinistryConstructionSignatory(body);

        body.AppendChild(new SectionProperties(
            new PageSize { Width = PW_P, Height = PH_P },
            new PageMargin { Top = (int)MrgT, Bottom = (int)MrgB, Left = (int)MrgL, Right = (int)MrgR }
        ));
        main.Document.Save();
    }
    return ms.ToArray();
}

/// <summary>Static signatory block: closing line, then one row with <c>Ministr</c> (left) and <c>A.Saparow</c> (right) on the same baseline.</summary>
static void AppendEnergyMinistryConstructionSignatory(Body body)
{
    var printableTwips = FormalCompanyLetterLayout.AppInvAndWPPrintableWidthTwips;
    const int leftCol = 5040;
    var rightCol = printableTwips - leftCol;
    const string signatoryBefore = "400";

    static TableBorders AllNil() => new TableBorders(
        new TopBorder { Val = BorderValues.Nil },
        new LeftBorder { Val = BorderValues.Nil },
        new BottomBorder { Val = BorderValues.Nil },
        new RightBorder { Val = BorderValues.Nil },
        new InsideHorizontalBorder { Val = BorderValues.Nil },
        new InsideVerticalBorder { Val = BorderValues.Nil });

    static TableCellBorders CellAllNil() => new TableCellBorders(
        new TopBorder { Val = BorderValues.Nil },
        new LeftBorder { Val = BorderValues.Nil },
        new BottomBorder { Val = BorderValues.Nil },
        new RightBorder { Val = BorderValues.Nil });

    var row1Left = new TableCell(
        new TableCellProperties(
            new TableCellWidth { Width = leftCol.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
            CellAllNil()),
        new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Left },
                new SpacingBetweenLines { Before = signatoryBefore, After = "0" }),
            MakeRun("Hormatlamak bilen,", "30", false)));

    var row1Right = new TableCell(
        new TableCellProperties(
            new TableCellWidth { Width = rightCol.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
            CellAllNil()),
        new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Right },
                new SpacingBetweenLines { Before = signatoryBefore, After = "0" }),
            new Run(new Text(" ") { Space = SpaceProcessingModeValues.Preserve })));

    var row2Left = new TableCell(
        new TableCellProperties(
            new TableCellWidth { Width = leftCol.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
            CellAllNil()),
        new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Left },
                new SpacingBetweenLines { Before = "0", After = "0" }),
            MakeRun("Ministr", "30", true)));

    var row2Right = new TableCell(
        new TableCellProperties(
            new TableCellWidth { Width = rightCol.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
            CellAllNil()),
        new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Right },
                new SpacingBetweenLines { Before = "0", After = "0" }),
            MakeRun("A.Saparow", "30", true)));

    body.AppendChild(new Table(
        new TableProperties(
            new TableWidth { Width = printableTwips.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableLayout { Type = TableLayoutValues.Fixed },
            AllNil()),
        new TableGrid(
            new GridColumn { Width = leftCol.ToString() },
            new GridColumn { Width = rightCol.ToString() }),
        new TableRow(row1Left, row1Right),
        new TableRow(row2Left, row2Right)));
}

static Paragraph MakeLetterRun(string text, bool rightAlign, bool bold)
{
    return new Paragraph(
        new ParagraphProperties(
            new Justification { Val = rightAlign ? JustificationValues.Right : JustificationValues.Left },
            new SpacingBetweenLines { After = "80" }
        ),
        MakeRun(text, "30", bold)
    );
}

static Paragraph MakeJustifiedParagraph(string text)
{
    return new Paragraph(
        new ParagraphProperties(
            new Justification { Val = JustificationValues.Both },
            new Indentation { FirstLine = FormalCompanyLetterLayout.JustifiedBodyFirstLineIndentTwips },
            new SpacingBetweenLines { After = "160" }
        ),
        MakeRun(text, "30", false)
    );
}

static Paragraph MakeMaksadyParagraph(string label, string field)
{
    var para = new Paragraph(new ParagraphProperties(
        new Justification { Val = JustificationValues.Both },
        new Indentation { FirstLine = FormalCompanyLetterLayout.JustifiedBodyFirstLineIndentTwips },
        new SpacingBetweenLines { After = "160" }
    ));
    para.AppendChild(MakeRun(label + " ", "30", bold: true));
    para.AppendChild(MakeRun(field, "30", bold: false));
    return para;
}

/// <summary>
/// Borderless two-column row: position (left cell, wraps) and name (right cell, right-aligned), both vertically top
/// so the name lines up with the first line of the title.
/// </summary>
/// <param name="printableWidthTwips">Text column width (page width − left margin − right margin). Omit for default L1–L3 margins.</param>
/// <param name="signatorySpaceBeforeTwips"><c>w:spacing w:before</c> on signatory paras; omit for <see cref="FormalCompanyLetterLayout.SignatoryParagraphSpaceBefore"/>.</param>
static void AppendSignatoryLetter(Body body, int? printableWidthTwips = null, string? signatorySpaceBeforeTwips = null)
{
    var printableTwips = printableWidthTwips ?? FormalCompanyLetterLayout.DefaultLetterPrintableWidthTwips;
    var signatoryBefore = signatorySpaceBeforeTwips ?? FormalCompanyLetterLayout.SignatoryParagraphSpaceBefore;
    var leftCol = FormalCompanyLetterLayout.SignatoryLeftColumnTwips;
    var rightCol = printableTwips - leftCol;

    static TableBorders AllNil() => new TableBorders(
        new TopBorder { Val = BorderValues.Nil },
        new LeftBorder { Val = BorderValues.Nil },
        new BottomBorder { Val = BorderValues.Nil },
        new RightBorder { Val = BorderValues.Nil },
        new InsideHorizontalBorder { Val = BorderValues.Nil },
        new InsideVerticalBorder { Val = BorderValues.Nil });

    static TableCellBorders CellAllNil() => new TableCellBorders(
        new TopBorder { Val = BorderValues.Nil },
        new LeftBorder { Val = BorderValues.Nil },
        new BottomBorder { Val = BorderValues.Nil },
        new RightBorder { Val = BorderValues.Nil });

    var leftCell = new TableCell(
        new TableCellProperties(
            new TableCellWidth { Width = leftCol.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
            CellAllNil()),
        new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Left },
                new SpacingBetweenLines { Before = signatoryBefore, After = "0" }),
            MakeRun("{{ds.Application_CompanyHead_PositionTm}}", "30", bold: true)));

    var rightCell = new TableCell(
        new TableCellProperties(
            new TableCellWidth { Width = rightCol.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Top },
            CellAllNil()),
        new Paragraph(
            new ParagraphProperties(
                new Justification { Val = JustificationValues.Right },
                new SpacingBetweenLines { Before = signatoryBefore, After = "0" }),
            MakeRun("{{ds.Application_CompanyHead_FullName}}", "30", bold: true)));

    body.AppendChild(new Table(
        new TableProperties(
            new TableWidth { Width = printableTwips.ToString(), Type = TableWidthUnitValues.Dxa },
            new TableLayout { Type = TableLayoutValues.Fixed },
            AllNil()),
        new TableGrid(
            new GridColumn { Width = leftCol.ToString() },
            new GridColumn { Width = rightCol.ToString() }),
        new TableRow(leftCell, rightCell)));
}

/// <summary>
/// Typography shared by formal company letters (L1–L3): ministry-bound, company-headed signatory.
/// Body paragraphs use <c>w:firstLine</c> = 720 twips (same as <c>AppBaseReport.RtfResponsibility</c> <c>\fi720</c>).
/// Do not prefix static Turkmen with ASCII spaces — that doubles the visual indent.
/// </summary>
file static class FormalCompanyLetterLayout
{
    public const uint LetterPortraitPageWidthTwips = 11906;

    /// <summary>Default L1–L3 side margins (~3.17 cm).</summary>
    public const uint LetterMarginLeftTwips = 1800;

    public const uint LetterMarginRightDefaultTwips = 1800;

    /// <summary>Printable width for default symmetric margins (used by <c>AppendSignatoryLetter</c> when no override).</summary>
    public const int DefaultLetterPrintableWidthTwips =
        (int)(LetterPortraitPageWidthTwips - LetterMarginLeftTwips - LetterMarginRightDefaultTwips);

    /// <summary><c>App_Inv_And_WP_Letter</c>: symmetric side margins (~2.12 cm each).</summary>
    public const uint AppInvAndWPLetterMarginLeftTwips = 1200;

    public const uint AppInvAndWPLetterMarginRightTwips = 1200;

    /// <summary>Printable width for Inv+WP / Visa+WP Ext letters (<c>11906 − 1200 − 1200</c> twips).</summary>
    public const int AppInvAndWPPrintableWidthTwips =
        (int)(LetterPortraitPageWidthTwips - AppInvAndWPLetterMarginLeftTwips - AppInvAndWPLetterMarginRightTwips);

    /// <summary>Minimum width of the empty left cell in the ministry addressee table (twips).</summary>
    public const int MinistryRecipientTableMinSpacerTwips = 360;

    /// <summary>Maximum width of the right-hand address column — keeps long ministry lines usable while leaving room for a wide left spacer so the block sits on the right.</summary>
    public const int MinistryRecipientTableAddressColumnMaxTwips = 6400;

    /// <summary>Minimum address-column width if printable width is ever constrained.</summary>
    public const int MinistryRecipientTableAddressColumnMinTwips = 4800;

    /// <summary><c>w:spacing w:after</c> on compact header lines (№, date, addressee line 1).</summary>
    public const string InvAndWPHeaderLineAfterTwips = "24";

    /// <summary><c>w:spacing w:after</c> after stepped addressee line 2, before urgency.</summary>
    public const string InvAndWPRecipientBlockEndAfterTwips = "72";

    /// <summary><c>w:spacing w:after</c> on urgency paragraph (gap before salutation; avoids a standalone empty <c>w:p</c>).</summary>
    public const string InvAndWPHeaderSalutationGapAfterTwips = "72";

    /// <summary><c>w:spacing w:after</c> on salutation paragraph (includes space that previously came from a separate empty paragraph before body).</summary>
    public const string InvAndWPSalutationAfterTwips = "200";

    /// <summary><c>w:spacing w:after</c> on justified body paragraphs (description, request, responsibility).</summary>
    public const string InvAndWPBodyParagraphAfterTwips = "100";

    /// <summary><c>w:spacing w:after</c> on responsibility paragraph only — smaller gap before Goşundy block (no blank paragraph between).</summary>
    public const string InvAndWPResponsibilityParagraphAfterTwips = "40";

    /// <summary><c>w:spacing w:after</c> on last Goşundy line (gap before signatory; avoids a standalone empty <c>w:p</c>).</summary>
    public const string InvAndWPBeforeSignatoryGapAfterTwips = "80";

    /// <summary><c>w:spacing w:before</c> on signatory cells — tighter than default L1–L3 for this letter only.</summary>
    public const string InvAndWPSignatoryParagraphSpaceBeforeTwips = "160";

    public const string JustifiedBodyFirstLineIndentTwips = "720";

    /// <summary>Plain text of AppBaseReport.RtfResponsibility (no leading spaces).</summary>
    public const string ResponsibilityPlain =
        "Da\u015fary \u00fdurt ra\u00fdatynyň T\u00fcrkmenistana gelmeginiň, onda bolmagynyň we ondan gitmeginiň düzgünlerini berjaý etmegine jogapkärçiligi kompaniýamyz öz üstüne alýar.";

    // Signatory block — see .cursor/skills/visa2026-word-reports/reference.md (Letter category → Signatory block).
    /// <summary>Left table column width (twips): capacity / position line; may wrap. Right column fills printable width.</summary>
    public const int SignatoryLeftColumnTwips = 5200;

    /// <summary><c>w:spacing</c> <c>w:before</c> on both signatory paragraphs (twips).</summary>
    public const string SignatoryParagraphSpaceBefore = "480";

    /// <summary>Legacy stepped indent (twips) for line 2 — superseded by ministry recipient table layout in <c>AppendMinistryRecipientBlockRightColumnTable</c>.</summary>
    public const string RecipientBlockLine2LeftIndentTwips = "720";
}
