using System.Text.Json.Serialization;

namespace UserManualManifestGenerator.Models;

public sealed class BoCatalogDocument
{
    public required DateTime GeneratedAt { get; init; }

    public required string AssemblyVersion { get; init; }

    public required IReadOnlyList<BoCatalogType> Types { get; init; }
}

public sealed class BoCatalogType
{
    public required string Name { get; init; }

    public required string FullName { get; init; }

    public required string DisplayName { get; init; }

    public string? NavigationPath { get; init; }

    public required string UserDocSlug { get; init; }

    public string? UserDocCategory { get; init; }

    public required IReadOnlyList<BoCatalogProperty> Properties { get; init; }

    public required IReadOnlyList<string> GuideSlugs { get; init; }
}

public sealed class BoCatalogProperty
{
    public required string Name { get; init; }

    public required string DisplayName { get; init; }

    public bool Required { get; init; }

    public string? HiddenWhen { get; init; }
}

public sealed class NavigationTreeDocument
{
    public required DateTime GeneratedAt { get; init; }

    public required IReadOnlyList<NavigationTreeNode> Paths { get; init; }
}

public sealed class NavigationTreeNode
{
    public required string Path { get; init; }

    public required IReadOnlyList<NavigationTreeType> Types { get; init; }
}

public sealed class NavigationTreeType
{
    public required string Name { get; init; }

    public required string DisplayName { get; init; }

    public required string UserDocSlug { get; init; }
}

public sealed class GuideFrontmatter
{
    public required string FilePath { get; init; }

    public required string Locale { get; init; }

    public string? Slug { get; init; }

    public string? Bo { get; init; }

    public string? Status { get; init; }
}
