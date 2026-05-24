using System.Text.Json;
using System.Text.Json.Serialization;

namespace Visa2026.DataImporter;

/// <summary>Exports <c>lookup.xlsm</c> sheets to <c>Visa2026.Module/DatabaseUpdate/LookupCatalogs/*.json</c>.</summary>
public static class LookupCatalogExporter
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Export(string lookupXlsmPath, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);
        var tenantDir = Path.Combine(outputDirectory, "tenant");
        Directory.CreateDirectory(tenantDir);

        var globalCatalogs = new List<object>();
        var tenantCatalogs = new List<object>();

        foreach (var sheetMap in ExcelMappings.LookupSheets)
        {
            if (ShouldSkipCatalogExport(sheetMap.EntityName))
                continue;

            var payloads = LookupSheetPayloadReader.Read(lookupXlsmPath, sheetMap);
            var rows = payloads.Select(ToJsonRow).ToList();

            var id = ToCatalogId(sheetMap.EntityName);
            var fileName = id + ".json";
            bool isTenant = IsTenantCatalog(sheetMap.EntityName);
            var filePath = isTenant
                ? Path.Combine(tenantDir, fileName)
                : Path.Combine(outputDirectory, fileName);

            var catalogFile = new { rows };
            File.WriteAllText(filePath, JsonSerializer.Serialize(catalogFile, WriteOptions));

            var entry = new
            {
                id,
                entity = sheetMap.EntityName,
                file = fileName,
                matchKey = ResolveMatchKey(sheetMap.EntityName),
                syncMode = "OverwriteScalars",
                optional = isTenant,
            };

            if (isTenant)
                tenantCatalogs.Add(entry);
            else
                globalCatalogs.Add(entry);

            var prefix = isTenant ? "tenant/" : "";
            Console.WriteLine($"  ✓ {prefix}{fileName}: {rows.Count} rows");
        }

        var manifest = new { version = 1, catalogs = globalCatalogs };
        File.WriteAllText(
            Path.Combine(outputDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, WriteOptions));

        var tenantManifest = new { version = 1, catalogs = tenantCatalogs };
        File.WriteAllText(
            Path.Combine(tenantDir, "manifest.json"),
            JsonSerializer.Serialize(tenantManifest, WriteOptions));

        File.WriteAllText(
            Path.Combine(tenantDir, "README.md"),
            """
            # Tenant / company-specific lookup catalogs

            Rows here are **not** shared ministry reference data. They belong to one deploying organization
            (e.g. **Position** job titles, **Company**, **ProjectContract**).

            - `manifest.json` in this folder is merged with the global `LookupCatalogs/manifest.json` on app startup.
            - JSON files are loaded from embedded resources (this repo) and/or `{AppBase}/LookupCatalogs/tenant/` on disk.

            For a new customer deployment, replace `position.json` (and company/project-contract overlays) with that customer's data.
            """);

        Console.WriteLine($"  ✓ manifest.json ({globalCatalogs.Count} global catalogs)");
        Console.WriteLine($"  ✓ tenant/manifest.json ({tenantCatalogs.Count} tenant catalogs)");
    }

    private static Dictionary<string, object?> ToJsonRow(Dictionary<string, object?> payload)
    {
        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in payload)
            row[key] = value;
        return row;
    }

    private static string ToCatalogId(string entityName) =>
        entityName switch
        {
            "CheckPoint" => "checkpoint",
            "PurposeOfTravel" => "purpose-of-travel",
            "EducationInstitution" => "education-institution",
            "ApplicationTypeFilter" => "application-type-filter",
            "ValidityDuration" => "validity-duration",
            "ApplicationState" => "application-state",
            "VisaIssuedPlace" => "visa-issued-place",
            "MigrationService" => "migration-service",
            "PassportType" => "passport-type",
            "VisaCategory" => "visa-category",
            "VisaPeriod" => "visa-period",
            "VisaType" => "visa-type",
            "EducationLevel" => "education-level",
            "MaritalStatus" => "marital-status",
            "BorderZoneLocation" => "border-zone-location",
            "MovementPermitLocation" => "movement-permit-location",
            "ApplicationLocation" => "application-location",
            "ProjectContract" => "project-contract",
            _ => PascalToKebab(entityName),
        };

    private static string PascalToKebab(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        var chars = new List<char>();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
                chars.Add('-');
            chars.Add(char.ToLowerInvariant(c));
        }
        return new string(chars.ToArray());
    }

    private static string ResolveMatchKey(string entityName) =>
        entityName switch
        {
            "Country" => "CodeOrName",
            "City" => "NameAndRegion",
            "ProjectContract" => "CodeOrName",
            "Position" => "CodeOrName",
            _ => "Name",
        };

    /// <summary>Company-specific catalogs (not shared across all Visa2026 installations).</summary>
    private static bool IsTenantCatalog(string entityName) =>
        entityName is "Position" or "CompanyProfile" or "ProjectContract"
            or "Specialty" or "EducationInstitution" or "Department" or "Ministry";

    /// <summary>Excluded from JSON catalog export and deploy sync (maintained in app if needed).</summary>
    private static bool ShouldSkipCatalogExport(string entityName) =>
        entityName is "ApplicationType" or "MovementPermitLocation";
}
