using System.Text.RegularExpressions;

#nullable enable

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>Local intent gate for Preview chat (L8). Runs before any provider call.</summary>
public enum TemplateConvertChatIntent
{
    /// <summary>Rewrite, restyle, translate, layout - must never mutate the draft.</summary>
    OutOfScopeContentEdit,

    /// <summary>Looks like a mapping adjustment (remap / unmap / loop / token).</summary>
    MappingAdjustment,

    /// <summary>Could not classify; provider may still refuse.</summary>
    Unknown,
}

/// <summary>
/// Deterministic L8 classifier. Q11 depends on this short-circuiting before the provider so a
/// rewrite ask yields <see cref="ChatRejectReason.OutOfScopeContentEdit"/> even when AI is off.
/// </summary>
public static class TemplateConvertChatIntentClassifier
{
    private static readonly Regex OutOfScope = new(
        @"\b(font|bold|italic|underline|layout|logo|wording|rewrite|rephrase|restyle|redesign|translate|translation|colour|color|margin|spacing|format|formatting|formal|paragraph|sentence|grammar|style|styles|russian|turkmen|turkish|english|add\s+(a\s+)?(paragraph|sentence|row|column)|change\s+the\s+(font|logo|table|layout)|make\s+it\s+(more\s+)?formal)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Mapping = new(
        @"\b(remap|re-?map|unmap|un-?map|map|mapping|placeholder|token|loop|roster|field|span|cell|passport|personal\s+number|company|short\s*code|\{\{|\}\}|ds\.|\.PPN|\.PFN)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static TemplateConvertChatIntent Classify(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return TemplateConvertChatIntent.Unknown;

        // Out-of-scope wins when both match ("rewrite the passport mapping" is still a rewrite ask).
        if (OutOfScope.IsMatch(message))
            return TemplateConvertChatIntent.OutOfScopeContentEdit;

        if (Mapping.IsMatch(message))
            return TemplateConvertChatIntent.MappingAdjustment;

        return TemplateConvertChatIntent.Unknown;
    }

    public const string OutOfScopeReply =
        "I can only change which values become placeholders. Layout, wording, and formatting stay exactly as you uploaded them - open the template in desktop staging for that.";
}