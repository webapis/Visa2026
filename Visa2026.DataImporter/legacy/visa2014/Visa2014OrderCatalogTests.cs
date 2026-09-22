using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014OrderCatalogTests
{
    [Fact]
    public void TopologicalSortSteps_OrdersDependenciesBeforeDependents()
    {
        var steps = new List<TenantCatalogGenerationStep>
        {
            new() { Id = "c", Script = "c.ps1", DependsOn = ["b"] },
            new() { Id = "a", Script = "a.ps1" },
            new() { Id = "b", Script = "b.ps1", DependsOn = ["a"] },
        };

        var sorted = Visa2014OrderCatalog.TopologicalSortSteps(steps);

        Assert.Equal(new[] { "a", "b", "c" }, sorted.Select(s => s.Id).ToArray());
    }

    [Fact]
    public void TopologicalSortSteps_Cycle_Throws()
    {
        var steps = new List<TenantCatalogGenerationStep>
        {
            new() { Id = "a", Script = "a.ps1", DependsOn = ["b"] },
            new() { Id = "b", Script = "b.ps1", DependsOn = ["a"] },
        };

        var ex = Assert.Throws<InvalidOperationException>(
            () => Visa2014OrderCatalog.TopologicalSortSteps(steps));

        Assert.Contains("Cycle", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'a'", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TopologicalSortSteps_Empty_ReturnsEmpty()
    {
        Assert.Empty(Visa2014OrderCatalog.TopologicalSortSteps(Array.Empty<TenantCatalogGenerationStep>()));
    }
}
