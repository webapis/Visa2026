using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.Services.EmailDocumentCopies;

public sealed class EmailTemplateContext
{
    public IReadOnlyList<string> PersonNames { get; init; } = Array.Empty<string>();

    /// <summary>One line per person: "Name: Passport X, Visa Y".</summary>
    public IReadOnlyList<string> DocumentLines { get; init; } = Array.Empty<string>();

    public string OfficerName { get; init; } = string.Empty;

    public DateTime Date { get; init; } = DateTime.Today;
}

/// <summary>Replaces <c>{PersonNames}</c>, <c>{PersonCount}</c>, <c>{DocumentList}</c>, <c>{OfficerName}</c>, <c>{Date}</c>.</summary>
public static class EmailTemplateRenderer
{
    public static string Render(string? text, EmailTemplateContext context)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text
            .Replace("{PersonNames}", string.Join(", ", context.PersonNames), StringComparison.OrdinalIgnoreCase)
            .Replace("{PersonCount}", context.PersonNames.Count.ToString(), StringComparison.OrdinalIgnoreCase)
            .Replace("{DocumentList}", string.Join(Environment.NewLine, context.DocumentLines.Select(l => "- " + l)), StringComparison.OrdinalIgnoreCase)
            .Replace("{OfficerName}", context.OfficerName, StringComparison.OrdinalIgnoreCase)
            .Replace("{Date}", context.Date.ToString("dd.MM.yyyy"), StringComparison.OrdinalIgnoreCase);
    }
}
