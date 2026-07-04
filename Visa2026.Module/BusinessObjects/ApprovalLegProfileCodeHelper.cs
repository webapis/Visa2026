using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Builds <see cref="ApprovalLegProfile"/> <see cref="LookupBase.Code"/> values from ordered ministry short names.
/// Matches <c>tools/GenerateApprovalLegProfileCatalog</c> token rules.
/// </summary>
public static class ApprovalLegProfileCodeHelper
{
    public static string? BuildProfileCode(IReadOnlyList<string> orderedMinistryShortNamesTm)
    {
        if (orderedMinistryShortNamesTm == null || orderedMinistryShortNamesTm.Count == 0)
            return null;

        var tokens = orderedMinistryShortNamesTm
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(ToProfileToken)
            .ToList();

        return tokens.Count == 0 ? null : string.Join('-', tokens);
    }

    public static string? ResolveCodeFromLegShortNames(IReadOnlyList<string> orderedMinistryShortNamesTm) =>
        BuildProfileCode(orderedMinistryShortNamesTm);

    public static string BuildProfileNameTm(IReadOnlyList<string> orderedMinistryShortNamesTm) =>
        string.Join('-', orderedMinistryShortNamesTm
            .Select(s => s?.Trim())
            .Where(s => !string.IsNullOrWhiteSpace(s)!));

    public static string ToProfileToken(string shortNameTm)
    {
        var folded = FoldTurkmen(shortNameTm).ToLowerInvariant().Trim();
        return folded switch
        {
            "turkmenenergo" => "TE",
            "energetika" => "EN",
            "gurlusyk" => "GU",
            "turkmengaz" => "TG",
            "asgabat hakimlik" => "AH",
            "tngiz" => "NG",
            "turkmenhimiya" => "TH",
            "turkmennebit" => "TN",
            _ => new string(folded.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpperInvariant() is { Length: > 0 } token
                ? token
                : "DF",
        };
    }

    private static string FoldTurkmen(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var map = new Dictionary<char, char>
        {
            ['ý'] = 'y', ['Ý'] = 'y',
            ['ä'] = 'a', ['Ä'] = 'a',
            ['ö'] = 'o', ['Ö'] = 'o',
            ['ü'] = 'u', ['Ü'] = 'u',
            ['ç'] = 'c', ['Ç'] = 'c',
            ['ş'] = 's', ['Ş'] = 's',
            ['ň'] = 'n', ['Ň'] = 'n',
            ['ž'] = 'z', ['Ž'] = 'z',
            ['î'] = 'i', ['Î'] = 'i',
        };

        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (map.TryGetValue(chars[i], out var mapped))
                chars[i] = mapped;
        }

        return new string(chars);
    }
}
