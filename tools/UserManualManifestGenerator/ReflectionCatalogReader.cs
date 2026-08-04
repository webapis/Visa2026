using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UserManualManifestGenerator.Models;
using Visa2026.Module.BusinessObjects;

namespace UserManualManifestGenerator;

internal sealed class ReflectionCatalogReader
{
    private const string UserDocumentationAttributeName = "Visa2026.Module.Documentation.UserDocumentationAttribute";

    public BoCatalogDocument Read(string? moduleAssemblyPath, IReadOnlyDictionary<string, IReadOnlyList<string>> guideSlugsByBo)
    {
        var assembly = ResolveModuleAssembly(moduleAssemblyPath);
        var assemblyVersion = assembly.GetName().Version?.ToString() ?? "0.0.0.0";

        var types = GetLoadableTypes(assembly)
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .Select(t => TryReadType(t, guideSlugsByBo))
            .Where(t => t is not null)
            .Cast<BoCatalogType>()
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToArray();

        return new BoCatalogDocument
        {
            GeneratedAt = DateTime.UtcNow,
            AssemblyVersion = assemblyVersion,
            Types = types,
        };
    }

    public NavigationTreeDocument BuildNavigationTree(BoCatalogDocument catalog)
    {
        var groups = catalog.Types
            .GroupBy(t => string.IsNullOrWhiteSpace(t.NavigationPath) ? "(root)" : t.NavigationPath!, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new NavigationTreeNode
            {
                Path = g.Key,
                Types = g.Select(t => new NavigationTreeType
                {
                    Name = t.Name,
                    DisplayName = t.DisplayName,
                    UserDocSlug = t.UserDocSlug,
                }).OrderBy(t => t.Name, StringComparer.Ordinal).ToArray(),
            })
            .ToArray();

        return new NavigationTreeDocument
        {
            GeneratedAt = catalog.GeneratedAt,
            Paths = groups,
        };
    }

    private static Assembly ResolveModuleAssembly(string? moduleAssemblyPath)
    {
        if (!string.IsNullOrWhiteSpace(moduleAssemblyPath))
        {
            var fullPath = Path.GetFullPath(moduleAssemblyPath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Module assembly not found.", fullPath);

            return Assembly.LoadFrom(fullPath);
        }

        return typeof(Person).Assembly;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static BoCatalogType? TryReadType(Type type, IReadOnlyDictionary<string, IReadOnlyList<string>> guideSlugsByBo)
    {
        var userDoc = type.GetCustomAttributesData()
            .FirstOrDefault(a => string.Equals(a.AttributeType.FullName, UserDocumentationAttributeName, StringComparison.Ordinal));
        if (userDoc is null)
            return null;

        var slug = userDoc.ConstructorArguments.Count > 0
            ? userDoc.ConstructorArguments[0].Value?.ToString()
            : null;
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var category = ReadNamedArgument(userDoc, "Category");
        var displayName = ReadDisplayName(type);
        var navigationPath = ReadNavigationPath(type);
        guideSlugsByBo.TryGetValue(type.Name, out var guideSlugs);

        return new BoCatalogType
        {
            Name = type.Name,
            FullName = type.FullName ?? type.Name,
            DisplayName = displayName,
            NavigationPath = navigationPath,
            UserDocSlug = slug,
            UserDocCategory = category,
            Properties = ReadProperties(type),
            GuideSlugs = guideSlugs ?? Array.Empty<string>(),
        };
    }

    private static IReadOnlyList<BoCatalogProperty> ReadProperties(Type type)
    {
        var visibilityByTarget = BuildAppearanceVisibilityMap(type);

        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(IsOfficerVisibleProperty)
            .Select(p => new BoCatalogProperty
            {
                Name = p.Name,
                DisplayName = ReadPropertyDisplayName(p),
                Required = HasRuleRequiredField(p),
                HiddenWhen = visibilityByTarget.TryGetValue(p.Name, out var hiddenWhen) ? hiddenWhen : null,
            })
            .OrderBy(p => p.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsOfficerVisibleProperty(PropertyInfo property)
    {
        if (property.GetIndexParameters().Length > 0)
            return false;

        if (property.GetCustomAttribute<BrowsableAttribute>() is { Browsable: false })
            return false;

        if (property.GetCustomAttributesData().Any(a =>
                string.Equals(a.AttributeType.FullName, "DevExpress.ExpressApp.Model.VisibleInDetailViewAttribute", StringComparison.Ordinal)
                && a.ConstructorArguments.Count > 0
                && a.ConstructorArguments[0].Value is false))
            return false;

        var propertyType = property.PropertyType;
        if (propertyType == typeof(string) || propertyType.IsPrimitive || propertyType.IsEnum)
            return true;

        if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?) ||
            propertyType == typeof(decimal) || propertyType == typeof(decimal?) ||
            propertyType == typeof(Guid) || propertyType == typeof(Guid?))
            return true;

        if (typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string))
            return false;

        if (propertyType.Namespace?.StartsWith("Visa2026.Module.BusinessObjects", StringComparison.Ordinal) == true)
            return true;

        return propertyType.Namespace?.StartsWith("DevExpress.", StringComparison.Ordinal) != true;
    }

    private static bool HasRuleRequiredField(PropertyInfo property)
    {
        return property.GetCustomAttributesData().Any(a =>
            string.Equals(a.AttributeType.FullName, "DevExpress.Persistent.Validation.RuleRequiredFieldAttribute", StringComparison.Ordinal));
    }

    private static Dictionary<string, string> BuildAppearanceVisibilityMap(Type type)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var data in type.GetCustomAttributesData())
        {
            if (!string.Equals(data.AttributeType.FullName, "DevExpress.ExpressApp.ConditionalAppearance.AppearanceAttribute", StringComparison.Ordinal))
                continue;

            var visibility = ReadNamedArgument(data, "Visibility");
            if (!string.Equals(visibility, "Hide", StringComparison.OrdinalIgnoreCase))
                continue;

            var criteria = ReadNamedArgument(data, "Criteria");
            var targetItems = ReadNamedArgument(data, "TargetItems");
            if (string.IsNullOrWhiteSpace(criteria) || string.IsNullOrWhiteSpace(targetItems))
                continue;

            foreach (var target in targetItems.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var propertyName = target.Split('.')[0];
                if (!map.ContainsKey(propertyName))
                    map[propertyName] = criteria;
            }
        }

        return map;
    }

    private static string ReadDisplayName(Type type)
    {
        var xaf = type.GetCustomAttributesData()
            .FirstOrDefault(a => string.Equals(a.AttributeType.FullName, "DevExpress.ExpressApp.DC.XafDisplayNameAttribute", StringComparison.Ordinal));
        if (xaf?.ConstructorArguments.Count > 0)
            return xaf.ConstructorArguments[0].Value?.ToString() ?? type.Name;

        var display = type.GetCustomAttribute<DisplayNameAttribute>();
        if (!string.IsNullOrWhiteSpace(display?.DisplayName))
            return display.DisplayName;

        return SplitPascalCase(type.Name);
    }

    private static string ReadPropertyDisplayName(PropertyInfo property)
    {
        var xaf = property.GetCustomAttributesData()
            .FirstOrDefault(a => string.Equals(a.AttributeType.FullName, "DevExpress.ExpressApp.DC.XafDisplayNameAttribute", StringComparison.Ordinal));
        if (xaf?.ConstructorArguments.Count > 0)
            return xaf.ConstructorArguments[0].Value?.ToString() ?? property.Name;

        var display = property.GetCustomAttribute<DisplayNameAttribute>();
        if (!string.IsNullOrWhiteSpace(display?.DisplayName))
            return display.DisplayName;

        return SplitPascalCase(property.Name);
    }

    private static string? ReadNavigationPath(Type type)
    {
        var nav = type.GetCustomAttributesData()
            .FirstOrDefault(a => string.Equals(a.AttributeType.FullName, "DevExpress.ExpressApp.NavigationItemAttribute", StringComparison.Ordinal));
        if (nav is null)
            return null;

        if (nav.ConstructorArguments.Count == 0)
            return null;

        var value = nav.ConstructorArguments[0].Value;
        return value switch
        {
            false => null,
            true => type.Name,
            string path when !string.IsNullOrWhiteSpace(path) => path,
            _ => null,
        };
    }

    private static string? ReadNamedArgument(CustomAttributeData data, string name)
    {
        foreach (var named in data.NamedArguments)
        {
            if (!string.Equals(named.MemberName, name, StringComparison.Ordinal))
                continue;

            return named.TypedValue.Value?.ToString();
        }

        return null;
    }

    private static string SplitPascalCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        var builder = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
                builder.Append(' ');
            builder.Append(c);
        }

        return builder.ToString();
    }
}
