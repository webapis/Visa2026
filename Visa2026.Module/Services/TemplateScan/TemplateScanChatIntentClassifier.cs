using System.Text.RegularExpressions;

#nullable enable

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>Local intent gate for scan clarification chat (S8). Runs before any provider call.</summary>
public enum TemplateScanChatIntent
{
    OutOfScopeContentEdit,
    MappingClarification,
    Unknown,
}

public static class TemplateScanChatIntentClassifier
{
    private static readonly Regex OutOfScope = new(
        @"\b(font|bold|italic|underline|layout|logo|wording|rewrite|rephrase|restyle|redesign|translate|translation|colour|color|margin|spacing|format|formatting|formal|paragraph|sentence|grammar|style|styles|russian|turkmen|turkish|english|pixel|scan\s+quality|photo|crop|rotate|skew|add\s+(a\s+)?(paragraph|sentence|row|column)|change\s+the\s+(font|logo|table|layout)|make\s+it\s+(more\s+)?formal|remove\s+(the\s+)?(paragraph|legal|boilerplate))\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex Mapping = new(
        @"\b(remap|re-?map|unmap|un-?map|map|mapping|placeholder|token|field|label|gap|header|roster|row|loop|passport|personal\s+number|company|short\s*code|application\s+date|contract\s+date|\{\{|\}\}|ds\.|\.PPN|\.PFN|clarify|disambiguate|which\s+token|should\s+be)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static TemplateScanChatIntent Classify(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return TemplateScanChatIntent.Unknown;

        if (OutOfScope.IsMatch(message))
            return TemplateScanChatIntent.OutOfScopeContentEdit;

        if (Mapping.IsMatch(message))
            return TemplateScanChatIntent.MappingClarification;

        return TemplateScanChatIntent.Unknown;
    }

    public const string OutOfScopeReply =
        "I can only clarify which scan labels map to placeholders in this profile. Wording, layout, and scan image edits are out of scope — adjust those after the draft template is generated.";
}
