#nullable enable

using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Token text shared by the writer and the diff gate. Loop syntax matches what
/// <c>UserReportGenerator</c> and <c>ExcelReportGenerator</c> already consume (<c>{{#ds.rows}}</c>).
/// </summary>
public static class TemplateTokenSyntax
{
    public static string Wrap(string token)
    {
        var bare = Bare(token);
        return $"{{{{{bare}}}}}";
    }

    public static string LoopOpen(string collectionToken) => $"{{{{#{Bare(collectionToken)}}}}}";

    public static string LoopClose(string collectionToken) => $"{{{{/{Bare(collectionToken)}}}}}";

    /// <summary>
    /// Reduces a token to its catalog short code: <c>{{ds.PFN}}</c>, <c>{{.PFN}}</c>,
    /// <c>{{IMAGE:PPH}}</c>, <c>{{IMAGE:Person_Photo}}</c>, and a bare <c>PFN</c> all resolve.
    /// Loop markers and property paths with further nesting do not.
    /// </summary>
    public static bool TryGetShortCode(string? token, out string shortCode)
    {
        shortCode = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var value = Bare(token);
        if (value.Length == 0)
            return false;

        const string imagePrefix = "IMAGE:";
        if (value.StartsWith(imagePrefix, StringComparison.OrdinalIgnoreCase))
            value = value[imagePrefix.Length..];
        else if (value.StartsWith("ds.", StringComparison.OrdinalIgnoreCase))
            value = value[3..];
        else if (value.StartsWith('.'))
            value = value[1..];

        value = value.Trim();
        if (value.Length == 0)
            return false;

        if (UserReportPlaceholderAliasRegistry.TryGetShortCode(value, out var fromCanonical))
        {
            shortCode = fromCanonical;
            return true;
        }

        if (value.Contains('.', StringComparison.Ordinal))
            return false;

        shortCode = value;
        return true;
    }

    /// <summary>
    /// Every catalog short code in a token string, including compounds
    /// (<c>{{ds.RPPN}}, {{ds.RPPA}}</c>).
    /// </summary>
    public static IReadOnlyList<string> GetShortCodes(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Array.Empty<string>();

        var codes = new List<string>();
        var remaining = token.AsSpan();
        while (true)
        {
            var start = remaining.IndexOf("{{".AsSpan(), StringComparison.Ordinal);
            if (start < 0)
                break;

            remaining = remaining[(start + 2)..];
            var end = remaining.IndexOf("}}".AsSpan(), StringComparison.Ordinal);
            if (end < 0)
                break;

            var inner = remaining[..end].ToString();
            remaining = remaining[(end + 2)..];
            if (TryGetShortCode("{{" + inner + "}}", out var code)
                && !codes.Exists(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase)))
                codes.Add(code);
        }

        if (codes.Count == 0 && TryGetShortCode(token, out var single))
            codes.Add(single);

        return codes;
    }

    private static string Bare(string token)
    {
        var value = (token ?? string.Empty).Trim();
        while (value.StartsWith("{{", StringComparison.Ordinal))
            value = value[2..].TrimStart();
        while (value.EndsWith("}}", StringComparison.Ordinal))
            value = value[..^2].TrimEnd();

        return value.TrimStart('#', '/');
    }
}

/// <summary>
/// Applies span replacements to plain text. The diff gate uses this to derive the text it expects,
/// so it must stay behaviourally identical to the node-level edit in <c>WordTemplateTokenWriter</c>.
/// </summary>
public static class TemplateSpanEditor
{
    public static string Apply(string original, IReadOnlyList<(int Start, int Length, string Text)> edits)
    {
        ArgumentNullException.ThrowIfNull(edits);
        var value = original ?? string.Empty;

        foreach (var edit in edits.OrderByDescending(static e => e.Start).ThenByDescending(static e => e.Length))
        {
            if (edit.Start < 0 || edit.Length < 0 || edit.Start + edit.Length > value.Length)
                continue;

            value = value.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Text);
        }

        return value;
    }

    public static bool HasOverlap(IReadOnlyList<(int Start, int Length)> spans)
    {
        var ordered = spans.OrderBy(static s => s.Start).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Start < ordered[i - 1].Start + ordered[i - 1].Length)
                return true;
        }

        return false;
    }
}
