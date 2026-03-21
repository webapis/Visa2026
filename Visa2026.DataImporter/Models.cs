using System.Text.Json.Serialization;

namespace Visa2026.DataImporter;

// -----------------------------------------------------------------------
// Mirror of Visa2026.Module.BusinessObjects.VisaType
// Only include the properties you need — OData ignores unknown ones.
// -----------------------------------------------------------------------
public class VisaType
{
    [JsonPropertyName("ID")]
    public Guid Id { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("NameTm")]
    public string NameTm { get; set; } = "";

    [JsonPropertyName("Code")]
    public string Code { get; set; } = "";

    [JsonPropertyName("IsDefault")]
    public bool IsDefault { get; set; }

    [JsonPropertyName("PdfForm_Code")]
    public int PdfFormCode { get; set; }

    public override string ToString() =>
        $"[{Id}] Name={Name}, Code={Code}, IsDefault={IsDefault}";
}

// -----------------------------------------------------------------------
// Add more entity models here as you expose them in Startup.cs, e.g.:
// public class VisaApplication { ... }
// -----------------------------------------------------------------------
