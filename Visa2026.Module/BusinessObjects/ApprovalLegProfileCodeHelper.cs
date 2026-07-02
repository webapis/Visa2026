using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.BusinessObjects;

/// <summary>Stable profile <see cref="ApprovalLegProfile.Code"/> from ordered ministry short names.</summary>
public static class ApprovalLegProfileCodeHelper
{
    public static string? ResolveCodeFromLegShortNames(IEnumerable<string?> shortNames)
    {
        var legs = shortNames?
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();

        if (legs is not { Count: > 0 })
            return null;

        return string.Join("-", legs.Select(ToProfileToken));
    }

    public static string ToProfileToken(string shortNameTm)
    {
        var folded = FoldTurkmen(shortNameTm).ToLowerInvariant();
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
            _ => new string(folded.Where(char.IsLetterOrDigit).Take(4).ToArray()).ToUpperInvariant() is { Length: > 0 } s
                ? s
                : "DF",
        };
    }

    internal static string FoldTurkmen(string value)
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
