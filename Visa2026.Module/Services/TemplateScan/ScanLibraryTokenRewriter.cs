#nullable enable

using System.Text;
using Visa2026.Module.Services.TemplateConvert;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Forces library tokens onto the catalog Header/Row shape. Word yellow on şahsy-style letters
/// is classified Header, so AI and officer remap used to emit <c>{{ds.PVFM}}</c>. Row-only
/// codes must be <c>{{.PVFM}}</c> or Approve looks them up on ApplicationProfileInstance.
/// </summary>
public static class ScanLibraryTokenRewriter
{
    public static string Rewrite(string? token, ApplicationProfilePlaceholderSet placeholderSet)
    {
        ArgumentNullException.ThrowIfNull(placeholderSet);
        if (string.IsNullOrWhiteSpace(token))
            return token ?? string.Empty;

        var source = token;
        var builder = new StringBuilder(source.Length);
        var index = 0;
        while (index < source.Length)
        {
            var start = source.IndexOf("{{", index, StringComparison.Ordinal);
            if (start < 0)
            {
                builder.Append(source, index, source.Length - index);
                break;
            }

            builder.Append(source, index, start - index);
            var end = source.IndexOf("}}", start + 2, StringComparison.Ordinal);
            if (end < 0)
            {
                builder.Append(source, start, source.Length - start);
                break;
            }

            var inner = source[(start + 2)..end];
            builder.Append(RewriteOne("{{" + inner + "}}", inner, placeholderSet));
            index = end + 2;
        }

        return builder.ToString();
    }

    private static string RewriteOne(
        string wrapped,
        string inner,
        ApplicationProfilePlaceholderSet placeholderSet)
    {
        var trimmed = inner.Trim();
        if (trimmed.StartsWith('#')
            || trimmed.StartsWith('/')
            || trimmed.StartsWith(':'))
            return wrapped;

        if (!TemplateTokenSyntax.TryGetShortCode(wrapped, out var code)
            || !placeholderSet.Contains(code))
            return wrapped;

        var entry = placeholderSet.Allowed.FirstOrDefault(e =>
            string.Equals(e.ShortCode, code, StringComparison.OrdinalIgnoreCase));
        if (entry == null)
            return wrapped;

        if (entry.IsImage)
            return entry.BuildWordToken(UserReportPlaceholderScope.Header);

        var requested = trimmed.StartsWith("ds.", StringComparison.OrdinalIgnoreCase)
            || (!trimmed.StartsWith('.')
                && !trimmed.StartsWith("IMAGE:", StringComparison.OrdinalIgnoreCase))
            ? UserReportPlaceholderScope.Header
            : UserReportPlaceholderScope.Row;

        return entry.BuildWordToken(requested);
    }
}