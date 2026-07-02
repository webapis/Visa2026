using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal sealed class Visa2014OrderDocument
{
    public TenantCatalogGenerationSection? TenantCatalogGeneration { get; set; }
}

internal sealed class TenantCatalogGenerationSection
{
    public string? RunBeforeImportPhase { get; set; }
    public List<string>? LegacySources { get; set; }
    public List<TenantCatalogGenerationStep>? Steps { get; set; }
}

internal sealed class TenantCatalogGenerationStep
{
    public string Id { get; set; } = "";
    public string Script { get; set; } = "";
    public List<string>? DependsOn { get; set; }
    public List<string>? Outputs { get; set; }
}

internal static class Visa2014OrderCatalog
{
    public static TenantCatalogGenerationSection LoadTenantCatalogGeneration(string dataImporterRoot)
    {
        var path = Path.Combine(Visa2014ContentRoot.LegacyRoot(dataImporterRoot), "order.yaml");
        if (!File.Exists(path))
            throw new FileNotFoundException("order.yaml not found.", path);

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var doc = deserializer.Deserialize<Visa2014OrderDocument>(yaml);
        return doc.TenantCatalogGeneration
            ?? throw new InvalidOperationException("order.yaml has no tenantCatalogGeneration section.");
    }

    public static IReadOnlyList<TenantCatalogGenerationStep> TopologicalSortSteps(
        IReadOnlyList<TenantCatalogGenerationStep> steps)
    {
        var byId = steps.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        var sorted = new List<TenantCatalogGenerationStep>();
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(string id)
        {
            if (visited.Contains(id))
                return;
            if (visiting.Contains(id))
                throw new InvalidOperationException($"Cycle in tenantCatalogGeneration dependsOn at '{id}'.");

            visiting.Add(id);
            if (byId.TryGetValue(id, out var step))
            {
                foreach (var dep in step.DependsOn ?? [])
                    Visit(dep);
                sorted.Add(step);
            }

            visiting.Remove(id);
            visited.Add(id);
        }

        foreach (var step in steps)
            Visit(step.Id);

        return sorted;
    }
}
