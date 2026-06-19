using System.Text.Json;
using System.Text.Json.Serialization;
using ClosedXML.Excel;

namespace Visa2026.Tools.CarboneSpike;

/// <summary>Minimal Carbone Studio smoke files — not Visa2026 ministry templates.</summary>
internal static class CarboneSmokeSampleFactory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string CreateAll()
    {
        var dir = Path.Combine(RepoPaths.Root(), "tools", "CarboneSpike", "templates", "carbone-smoke");
        Directory.CreateDirectory(dir);

        CreateMinimal(dir);
        CreateLoopTable(dir);

        return dir;
    }

    private static void CreateMinimal(string dir)
    {
        const string jsonPath = "carbone-smoke-minimal.json";
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = "Carbone smoke OK",
            ["subtitle"] = "If you see this text, Studio + JSON + tags work.",
        };
        File.WriteAllText(Path.Combine(dir, jsonPath), JsonSerializer.Serialize(data, JsonOptions));

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("Smoke");
        ws.Cell(1, 26).Value = "{o.converter=L}";
        ws.Cell(1, 1).Value = "{d.title}";
        ws.Cell(2, 1).Value = "{d.subtitle}";
        ws.Column(1).Width = 60;
        workbook.SaveAs(Path.Combine(dir, "carbone-smoke-minimal.xlsx"));
    }

    private static void CreateLoopTable(string dir)
    {
        const string jsonPath = "carbone-smoke-loop.json";
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["reportTitle"] = "Team list (smoke test)",
            ["people"] = new List<Dictionary<string, object>>
            {
                new(StringComparer.OrdinalIgnoreCase) { ["name"] = "Alice", ["role"] = "Engineer", ["hours"] = 40 },
                new(StringComparer.OrdinalIgnoreCase) { ["name"] = "Bob", ["role"] = "Analyst", ["hours"] = 32 },
                new(StringComparer.OrdinalIgnoreCase) { ["name"] = "Carol", ["role"] = "Designer", ["hours"] = 36 },
            },
        };
        File.WriteAllText(Path.Combine(dir, jsonPath), JsonSerializer.Serialize(data, JsonOptions));

        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("People");
        ws.Cell(1, 26).Value = "{o.converter=L}";
        ws.Cell(1, 1).Value = "{d.reportTitle}";
        ws.Cell(3, 1).Value = "Name";
        ws.Cell(3, 2).Value = "Role";
        ws.Cell(3, 3).Value = "Hours";
        ws.Cell(4, 1).Value = "{d.people[i].name}";
        ws.Cell(4, 2).Value = "{d.people[i].role}";
        ws.Cell(4, 3).Value = "{d.people[i].hours:formatN}";
        ws.Cell(5, 1).Value = "{d.people[i+1].name}";
        ws.Cell(5, 2).Value = "{d.people[i+1].role}";
        ws.Cell(5, 3).Value = "{d.people[i+1].hours:formatN}";
        ws.Columns(1, 3).AdjustToContents();
        workbook.SaveAs(Path.Combine(dir, "carbone-smoke-loop.xlsx"));
    }
}
