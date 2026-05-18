using System.Text.RegularExpressions;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Shared placeholder token syntax for Word and Excel user report templates.</summary>
public static class UserReportPlaceholderPatterns
{
    /// <summary>Matches <c>{{placeholder}}</c>, <c>{{#collection}}</c>, <c>{{/collection}}</c>, <c>{{.property}}</c>.</summary>
    public static readonly Regex PlaceholderRegex = new(
        @"\{\{([^}]+)\}\}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
}
